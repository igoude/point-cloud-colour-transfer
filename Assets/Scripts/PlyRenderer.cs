using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PlyRenderer : MonoBehaviour
{
    public string m_plyFilePath;

    public GameObject m_pointCloudPrefab;
    List<GameObject> m_pointClouds;
    Bounds m_pointCloudBounds;

    Dictionary<string, (int, string)> m_plyProperties;

    [Range(0.0f, 1.0f)]
    public float m_pointSize = 0.1f;

    public int m_nbPointsRestriction = -1;
    public bool m_floatAsUchar = false;
    int m_nbPointsTotal;
    int m_maxPoints = 65535;
    int m_nbElemTotal;


    public enum PositionSpace {
        Color,
        Normal
    }
    public PositionSpace m_positionSpace;

    [Range(0.0f, 1.0f)]
    public float m_switchSpace;

    enum Format {
        ascii,
        binary_little_endian
    }
    Format m_format;
    MeshTopology m_topology;

    StreamReader m_inStream;
    BinaryReader m_inBinary;
    char[] m_separator = { ' ', '\n' };

    // Start is called before the first frame update
    void Start()
    {
        m_plyProperties = new Dictionary<string, (int, string)>();
        m_pointClouds = new List<GameObject>();
        m_pointCloudBounds = new Bounds(Vector3.zero, Vector3.one);

        m_inStream = System.IO.File.OpenText(m_plyFilePath);

        ParseHeader(m_inStream);

        // In case of binary format file
        if(m_format != Format.ascii) {
            m_inStream.Close();
            m_inBinary = new BinaryReader(File.Open(m_plyFilePath, FileMode.Open));

            System.Text.StringBuilder str = new System.Text.StringBuilder();

            // Skip header
            while(str.ToString() != "end_header") {
                char currentChar = m_inBinary.ReadChar();
                str.Append(currentChar);
                if(currentChar == '\n') {
                    str.Clear();
                }
            }
            m_inBinary.ReadChar();    // Final '\n'
        }
        

        // Start loading point cloud
        if(m_nbPointsRestriction != -1 && m_nbPointsTotal > m_nbPointsRestriction) {
            m_nbPointsTotal = m_nbPointsRestriction;
        }

        int nbPoints = Mathf.Min(m_nbPointsTotal, m_maxPoints);

        GameObject first = Instantiate(m_pointCloudPrefab);
        first.transform.parent = this.transform;
        first.transform.localPosition = Vector3.zero;
        m_pointClouds.Add(first);

        PointCloud currentPC = first.GetComponent<PointCloud>();
        currentPC.m_vertices = new List<Vector3>(nbPoints);
        currentPC.m_normals = new List<Vector3>(nbPoints);
        currentPC.m_colors = new List<Color>(nbPoints);
        currentPC.m_uvs = new List<Vector2>(nbPoints);
        currentPC.m_indices = new List<int>(nbPoints);
        currentPC.m_triangles = new List<int>();
        currentPC.m_topology = m_topology;

        // Get points
        int index = 0;
        int nbObjects = 0;
        for (int i = 0; i < m_nbPointsTotal; i++) {
            ReadPoint(currentPC);

            currentPC.m_uvs.Add(Vector2.zero);
            currentPC.m_indices.Add(index);
            index++;

            if (index >= m_maxPoints) {
                GameObject next = Instantiate(m_pointCloudPrefab);
                next.transform.parent = this.transform;
                next.transform.localPosition = Vector3.zero;
                m_pointClouds.Add(next);

                nbObjects++;
                currentPC = m_pointClouds[nbObjects].GetComponent<PointCloud>();

                index = 0;
                nbPoints = Mathf.Min(m_nbPointsTotal - (nbObjects * (m_maxPoints)), m_maxPoints);

                currentPC.m_vertices = new List<Vector3>(nbPoints);
                currentPC.m_normals = new List<Vector3>(nbPoints);
                currentPC.m_colors = new List<Color>(nbPoints);
                currentPC.m_uvs = new List<Vector2>(nbPoints);
                currentPC.m_indices = new List<int>(nbPoints);
                currentPC.m_triangles = new List<int>();
                currentPC.m_topology = m_topology;
            }
        }

        // List of element indices
        if (m_topology == MeshTopology.Triangles) {
            for (int i = 0; i < m_nbElemTotal; i++) {
                ReadElem();
            }
        }

        // Update point clouds
        m_pointClouds[0].GetComponent<PointCloud>().UpdateCloud();
        m_pointCloudBounds = m_pointClouds[0].GetComponent<PointCloud>().m_mesh.bounds;
        for(int i=1; i<m_pointClouds.Count; i++) {
            PointCloud pc = m_pointClouds[i].GetComponent<PointCloud>();
            pc.UpdateCloud();
            m_pointCloudBounds.SetMinMax(Vector3.Min(m_pointCloudBounds.min, pc.m_mesh.bounds.min), Vector3.Max(m_pointCloudBounds.max, pc.m_mesh.bounds.max));
        }

        // Position
        Vector3 translate = this.transform.position;
        this.transform.position = Vector3.zero;
        for (int i = 0; i < m_pointClouds.Count; i++) {
            m_pointClouds[i].transform.position = -m_pointCloudBounds.min;
        }

        // Scale
        this.transform.localScale = (Vector3.one*Mathf.Sqrt(2.0f)) / m_pointCloudBounds.size.magnitude;

        // Replace
        this.transform.position = translate;
        

        // Close file
        if (m_format != Format.ascii) {
            m_inBinary.Close();
        } else {
            m_inStream.Close();
        }
    }


    void Update() {
        foreach (GameObject pc in m_pointClouds) {
            pc.GetComponent<MeshRenderer>().material.SetFloat("_Size", m_pointSize);
            pc.GetComponent<MeshRenderer>().material.SetFloat("_SwitchSpace", m_switchSpace);
            pc.GetComponent<MeshRenderer>().material.SetInt("_PositionSpace", (int)m_positionSpace);
            pc.GetComponent<MeshRenderer>().material.SetVector("_Translation", this.transform.position);
            pc.GetComponent<MeshRenderer>().material.SetVector("_MinPosition", m_pointCloudBounds.min);
            pc.GetComponent<MeshRenderer>().material.SetVector("_MaxPosition", m_pointCloudBounds.max);
        }
    }

    public int GetNbPoints() {
        return m_nbPointsTotal;
    }

    public List<PointCloud> GetPointClouds() {
        List<PointCloud> result = new List<PointCloud>();

        foreach(GameObject obj in m_pointClouds) {
            result.Add(obj.GetComponent<PointCloud>());
        }

        return result;
    }
    
    public Bounds GetBounds() {
        return m_pointCloudBounds;
    }

    void ParseHeader(StreamReader inStream) {
        // Skip header
        inStream.ReadLine();

        // Get format
        string format = inStream.ReadLine().Split(m_separator)[1];
        switch (format) {
            case "ascii": m_format = Format.ascii; break;
            case "binary_little_endian": m_format = Format.binary_little_endian; break;
            // TODO: binary_big_endian
            default: break;
        }

        // Get number of vertices
        string[] props;
        do {
            props = inStream.ReadLine().Split(m_separator);
        } while (props[0] != "element");
        m_nbPointsTotal = int.Parse(props[2]);
        Debug.Log("Number of points: " + m_nbPointsTotal);

        // Get properties
        int index = 0;
        do {
            props = inStream.ReadLine().Split(m_separator);
        } while (props[0] != "property");
        do {
            string type = props[1];
            string name = props[2];
            m_plyProperties.Add(name, (index, type));

            index++;
            props = inStream.ReadLine().Split(m_separator);
        } while (props[0] == "property");
        m_topology = MeshTopology.Points;

        // Element indices
        if (props.Length > 1 && props[1] == "face") {
            m_nbElemTotal = int.Parse(props[2]);
            if(m_nbElemTotal > 0) {
                m_topology = MeshTopology.Triangles;
            }
        }

        //... could exist something else there

        // Already ended
        if (props[0] == "end_header") return;

        // Go to end of header
        while (inStream.ReadLine() != "end_header");
    }

    void ReadPoint(PointCloud pc) {
        switch (m_format) { 
            case Format.ascii:
                string[] props = m_inStream.ReadLine().Split(m_separator);

                // Position
                if (m_plyProperties.ContainsKey("x")) {
                    float x = float.Parse(props[m_plyProperties["x"].Item1].Replace(".", ","));
                    float y = float.Parse(props[m_plyProperties["y"].Item1].Replace(".", ","));
                    float z = float.Parse(props[m_plyProperties["z"].Item1].Replace(".", ","));
                    pc.m_vertices.Add(new Vector3(x, y, z));
                }

                // Normal
                if (m_plyProperties.ContainsKey("nx")) {
                    float nx = float.Parse(props[m_plyProperties["nx"].Item1].Replace(".", ","));
                    float ny = float.Parse(props[m_plyProperties["ny"].Item1].Replace(".", ","));
                    float nz = float.Parse(props[m_plyProperties["nz"].Item1].Replace(".", ","));
                    pc.m_normals.Add(new Vector3(nx, ny, nz));
                } else {
                    pc.m_normals.Add(new Vector3(0.0f, 0.0f, 1.0f));
                }

                // Color
                if (m_plyProperties.ContainsKey("r")) {
                    float r = float.Parse(props[m_plyProperties["r"].Item1].Replace(".", ","));
                    float g = float.Parse(props[m_plyProperties["g"].Item1].Replace(".", ","));
                    float b = float.Parse(props[m_plyProperties["b"].Item1].Replace(".", ","));
                    if (m_floatAsUchar) pc.m_colors.Add(new Color(r, g, b) / 255.0f);
                    else pc.m_colors.Add(new Color(r, g, b));
                }
                else if (m_plyProperties.ContainsKey("red")) {
                    float r = float.Parse(props[m_plyProperties["red"].Item1].Replace(".", ","));
                    float g = float.Parse(props[m_plyProperties["green"].Item1].Replace(".", ","));
                    float b = float.Parse(props[m_plyProperties["blue"].Item1].Replace(".", ","));
                    if (m_floatAsUchar) pc.m_colors.Add(new Color(r, g, b) / 255.0f);
                    else pc.m_colors.Add(new Color(r, g, b));
                }

                break;

            case Format.binary_little_endian:
                Vector3 position = Vector3.zero;
                Vector3 normal = Vector3.zero;
                Color color = Color.black;

                foreach(KeyValuePair<string, (int, string)> prop in m_plyProperties) {
                    float value = 0.0f;
                    switch (prop.Value.Item2) {
                        case "float":
                            value = m_inBinary.ReadSingle();
                            break;
                        case "uint":
                            value = (float)m_inBinary.ReadUInt32();
                            break;
                        case "ushort":
                            value = (float)m_inBinary.ReadUInt16();
                            break;
                        case "uchar":
                            value = (float)m_inBinary.ReadByte() / 255.0f;
                            break;
                        default: break;
                    }

                    switch (prop.Key) {
                        case "x": position.x = value; break;
                        case "y": position.y = value; break;
                        case "z": position.z = value; break;
                        case "nx": normal.x = value; break;
                        case "ny": normal.y = value; break;
                        case "nz": normal.z = value; break;
                        case "r": color.r = value; break;
                        case "g": color.g = value; break;
                        case "b": color.b = value; break;
                        case "red": color.r = value; break;
                        case "green": color.g = value; break;
                        case "blue": color.b = value; break;
                    }
                }

                // Normal null
                if(normal.magnitude == 0.0f) {
                    normal = new Vector3(0.0f, 0.0f, 1.0f);
                }

                pc.m_vertices.Add(position);
                pc.m_normals.Add(normal);
                pc.m_colors.Add(color);
                break;

            default: break;
        }
    }

    void ReadElem() {
        int[] elems = new int[0];
        uint nbElem = 0;
        int idPC = 0;
        bool sameSubMesh = true;

        switch (m_format) {
            case Format.ascii:
                string[] props = m_inStream.ReadLine().Split(m_separator);
                nbElem = uint.Parse(props[0]);
                elems = new int[nbElem];
                for (int i = 0; i < nbElem; i++) {
                    int currentElem = int.Parse(props[i + 1]);
                    
                    if (i == 0) {
                        idPC = currentElem / m_maxPoints;
                    } else {
                        if (idPC != currentElem / m_maxPoints)
                            sameSubMesh = false;
                    }

                    elems[i] = currentElem;
                }
                break;
            case Format.binary_little_endian:
                nbElem = (uint)m_inBinary.ReadByte();
                elems = new int[nbElem];
                for (int i = 0; i < nbElem; i++) {
                    int currentElem = m_inBinary.ReadInt32();
                    
                    if (i == 0) {
                        idPC = currentElem / m_maxPoints;
                    } else {
                        if (idPC != currentElem / m_maxPoints)
                            sameSubMesh = false;
                    }

                    elems[i] = currentElem;
                }
                break;
            default: break;
        }

        if (sameSubMesh) {
            PointCloud currentPC = m_pointClouds[idPC].GetComponent<PointCloud>();
            for (int i = 0; i < nbElem; i++) {
                int id = elems[i] - (idPC * m_maxPoints);
                currentPC.m_triangles.Add(id);
            }
        }
    }
}
