using MathNet.Numerics.LinearAlgebra;
using UnityEngine;
using System.Text;
using System.IO;

public class PlyWriter : MonoBehaviour
{
    public PointCloudStyleExtractor m_input;
    public PointCloudStyleExtractor m_target;
    public StyleTransfer m_styleTransfer;

    public enum Method {
        IGD, 
        IGD_N,
        MGD,
        MGD_N
    }
    public Method m_method;

    int m_nbPoints;
    // Structure of a scene point
    struct Point {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 color;
    };
    // The last point hit in the scene
    Point m_point;

    public string m_savePath;
    public bool m_save;

    // String builder to save fixation points as a .ply file
    StringBuilder m_stringBuilder;


    // Update is called once per frame
    void Update() {
        if (m_save) {
            m_save = false;
            SavePointCloud(m_savePath);
        }
    }

    // Start is called before the first frame update
    public void SavePointCloud(string path) {
        m_nbPoints = m_input.m_plyRenderer.GetNbPoints();
        m_stringBuilder = new StringBuilder();
        CompleteHeader(m_nbPoints);
        m_point = new Point();

        foreach (PointCloud pc in m_input.m_plyRenderer.GetPointClouds()) {
            for (int i = 0; i < pc.m_vertices.Count; i++) {
                m_point.position = pc.m_vertices[i];
                m_point.normal = pc.m_normals[i];

                switch (m_method) {
                    case Method.IGD:
                        m_point.color = IGDStyleTransfer(pc.m_colors[i]);
                        break;
                    case Method.IGD_N:
                        m_point.color = IGDNStyleTransfer(pc.m_colors[i], pc.m_normals[i]);
                        break;
                    case Method.MGD:
                        m_point.color = MGDStyleTransfer(pc.m_colors[i]);
                        break;
                    case Method.MGD_N:
                        m_point.color = MGDNStyleTransfer(pc.m_colors[i], pc.m_normals[i]);
                        break;
                }

                m_stringBuilder.AppendLine(PointToLine(m_point));
            }
        }

        // Save .ply file
        StreamWriter outStream = System.IO.File.CreateText(path);
        outStream.WriteLine(m_stringBuilder);
        outStream.Close();
    }


    Vector3 IGDStyleTransfer(Color color) {
        // Save data point
        Color lab = ColorUtils.ReinhardRGBtoLab(color);

        float[] values3D = { lab[0], lab[1], lab[2] };
        Vector<float> val = CreateVector.Dense<float>(values3D);

        // Reinhard transfer
        val = val - m_input.GetMeans().SubVector(0, 3);
        val = val.PointwiseMultiply(m_target.GetStds().SubVector(0, 3).PointwiseDivide(m_input.GetStds().SubVector(0, 3)));
        val = val + m_target.GetMeans().SubVector(0, 3);

        lab = new Color(val[0], val[1], val[2]);
        Color rgb = ColorUtils.ReinhardLabtoRGB(lab);
        rgb *= m_styleTransfer.m_exposure;

        return new Vector3(rgb.r, rgb.g, rgb.b);
    }

    Vector3 IGDNStyleTransfer(Color color, Vector3 normal) {
        // Transform normal
        Vector<float> vNormal = CreateVector.Dense<float>(3);
        vNormal[0] = normal.x;
        vNormal[1] = normal.y;
        vNormal[2] = normal.z;
        vNormal = vNormal * m_input.m_eigenNormals;
        vNormal = vNormal.Normalize(1.0f);

        int x_side;
        if (vNormal[0] < 0.0f) { x_side = 0; } else { x_side = 1; }

        int y_side;
        if (vNormal[1] < 0.0) { y_side = 2; } else { y_side = 3; }

        int z_side;
        if (vNormal[2] < 0.0) { z_side = 4; } else { z_side = 5; }

        float norm = Mathf.Abs(vNormal[0]) + Mathf.Abs(vNormal[1]) + Mathf.Abs(vNormal[2]);
        float nx = Mathf.Abs(vNormal[0]) / norm;
        float ny = Mathf.Abs(vNormal[1]) / norm;
        float nz = Mathf.Abs(vNormal[2]) / norm;

        Vector<float> inputMeansX = CreateVector.Dense<float>(3);
        Vector<float> inputMeansY = CreateVector.Dense<float>(3);
        Vector<float> inputMeansZ = CreateVector.Dense<float>(3);
        inputMeansX[0] = m_input.GetMeansL()[x_side];
        inputMeansX[1] = m_input.GetMeansA()[x_side];
        inputMeansX[2] = m_input.GetMeansB()[x_side];
        inputMeansY[0] = m_input.GetMeansL()[y_side];
        inputMeansY[1] = m_input.GetMeansA()[y_side];
        inputMeansY[2] = m_input.GetMeansB()[y_side];
        inputMeansZ[0] = m_input.GetMeansL()[z_side];
        inputMeansZ[1] = m_input.GetMeansA()[z_side];
        inputMeansZ[2] = m_input.GetMeansB()[z_side];
        Vector<float> inputMeans = nx * inputMeansX + ny * inputMeansY + nz * inputMeansZ;

        Vector<float> inputStdsX = CreateVector.Dense<float>(3);
        Vector<float> inputStdsY = CreateVector.Dense<float>(3);
        Vector<float> inputStdsZ = CreateVector.Dense<float>(3);
        inputStdsX[0] = m_input.GetStdsL()[x_side];
        inputStdsX[1] = m_input.GetStdsA()[x_side];
        inputStdsX[2] = m_input.GetStdsB()[x_side];
        inputStdsY[0] = m_input.GetStdsL()[y_side];
        inputStdsY[1] = m_input.GetStdsA()[y_side];
        inputStdsY[2] = m_input.GetStdsB()[y_side];
        inputStdsZ[0] = m_input.GetStdsL()[z_side];
        inputStdsZ[1] = m_input.GetStdsA()[z_side];
        inputStdsZ[2] = m_input.GetStdsB()[z_side];
        Vector<float> inputStds = nx * inputStdsX + ny * inputStdsY + nz * inputStdsZ;

        Vector<float> targetMeansX = CreateVector.Dense<float>(3);
        Vector<float> targetMeansY = CreateVector.Dense<float>(3);
        Vector<float> targetMeansZ = CreateVector.Dense<float>(3);
        targetMeansX[0] = m_target.GetMeansL()[x_side];
        targetMeansX[1] = m_target.GetMeansA()[x_side];
        targetMeansX[2] = m_target.GetMeansB()[x_side];
        targetMeansY[0] = m_target.GetMeansL()[y_side];
        targetMeansY[1] = m_target.GetMeansA()[y_side];
        targetMeansY[2] = m_target.GetMeansB()[y_side];
        targetMeansZ[0] = m_target.GetMeansL()[z_side];
        targetMeansZ[1] = m_target.GetMeansA()[z_side];
        targetMeansZ[2] = m_target.GetMeansB()[z_side];
        Vector<float> targetMeans = nx * targetMeansX + ny * targetMeansY + nz * targetMeansZ;

        Vector<float> targetStdsX = CreateVector.Dense<float>(3);
        Vector<float> targetStdsY = CreateVector.Dense<float>(3);
        Vector<float> targetStdsZ = CreateVector.Dense<float>(3);
        targetStdsX[0] = m_target.GetStdsL()[x_side];
        targetStdsX[1] = m_target.GetStdsA()[x_side];
        targetStdsX[2] = m_target.GetStdsB()[x_side];
        targetStdsY[0] = m_target.GetStdsL()[y_side];
        targetStdsY[1] = m_target.GetStdsA()[y_side];
        targetStdsY[2] = m_target.GetStdsB()[y_side];
        targetStdsZ[0] = m_target.GetStdsL()[z_side];
        targetStdsZ[1] = m_target.GetStdsA()[z_side];
        targetStdsZ[2] = m_target.GetStdsB()[z_side];
        Vector<float> targetStds = nx * targetStdsX + ny * targetStdsY + nz * targetStdsZ;
        
        // Save data point
        Color lab = ColorUtils.ReinhardRGBtoLab(color);

        float[] values3D = { lab[0], lab[1], lab[2] };
        Vector<float> val = CreateVector.Dense<float>(values3D);

        // Reinhard transfer
        val = val - inputMeans;
        val = val.PointwiseMultiply(targetStds.PointwiseDivide(inputStds));
        val = val + targetMeans;

        lab = new Color(val[0], val[1], val[2]);
        Color rgb = ColorUtils.ReinhardLabtoRGB(lab);
        rgb *= m_styleTransfer.m_exposure;

        return new Vector3(rgb.r, rgb.g, rgb.b);
    }

    Vector3 MGDStyleTransfer(Color color) {
        // Save data point
        Color lab = ColorUtils.ReinhardRGBtoLab(color);

        float[] values3D = { lab[0], lab[1], lab[2] };
        Vector<float> val = CreateVector.Dense<float>(values3D);

        // Pitie transfer
        val = val - m_input.GetMeans().SubVector(0, 3);
        val = val * m_styleTransfer.GetMongeKantorovichMatrix().SubMatrix(0, 3, 0, 3);
        val = val + m_target.GetMeans().SubVector(0, 3);

        lab = new Color(val[0], val[1], val[2]);
        Color rgb = ColorUtils.ReinhardLabtoRGB(lab);
        rgb *= m_styleTransfer.m_exposure;

        return new Vector3(rgb.r, rgb.g, rgb.b);
    }

    Vector3 MGDNStyleTransfer(Color color, Vector3 normal) {
        // Transform normal
        Vector<float> vNormal = CreateVector.Dense<float>(3);
        vNormal[0] = normal.x;
        vNormal[1] = normal.y;
        vNormal[2] = normal.z;
        vNormal = vNormal * m_input.m_eigenNormals;
        vNormal = vNormal.Normalize(1.0f);
        
        // Save data point
        float[] nn = { Mathf.Abs(Mathf.Min(vNormal[0], 0.0f)), Mathf.Max(vNormal[0], 0.0f), Mathf.Abs(Mathf.Min(vNormal[1], 0.0f)), Mathf.Max(vNormal[1], 0.0f), Mathf.Abs(Mathf.Min(vNormal[2], 0.0f)), Mathf.Max(vNormal[2], 0.0f) };
        Vector<float> n = CreateVector.Dense<float>(nn);

        Color lab = ColorUtils.ReinhardRGBtoLab(color);

        float[] values9D = { lab[0], lab[1], lab[2], n[0], n[1], n[2], n[3], n[4], n[5] };
        Vector<float> val = CreateVector.Dense<float>(values9D);

        // Pitie transfer
        val = val - m_input.GetMeans();
        val = val * m_styleTransfer.GetMongeKantorovichMatrix();
        val = val + m_target.GetMeans();

        lab = new Color(val[0], val[1], val[2]);
        Color rgb = ColorUtils.ReinhardLabtoRGB(lab);
        rgb *= m_styleTransfer.m_exposure;

        return new Vector3(rgb.r, rgb.g, rgb.b);
    }


    void CompleteHeader(int nbPoints) {
        m_stringBuilder.AppendLine("ply");
        m_stringBuilder.AppendLine("format ascii 1.0");
        m_stringBuilder.AppendLine("element vertex " + nbPoints.ToString());
        m_stringBuilder.AppendLine("property float x");
        m_stringBuilder.AppendLine("property float y");
        m_stringBuilder.AppendLine("property float z");
        m_stringBuilder.AppendLine("property float nx");
        m_stringBuilder.AppendLine("property float ny");
        m_stringBuilder.AppendLine("property float nz");
        m_stringBuilder.AppendLine("property float r");
        m_stringBuilder.AppendLine("property float g");
        m_stringBuilder.AppendLine("property float b");
        m_stringBuilder.AppendLine("end_header");
    }

    string PointToLine(Point point) {
        string line = "";
        line += VectorToLine(point.position) + " ";
        line += VectorToLine(point.normal) + " ";
        line += VectorToLine(point.color);
        return line;
    }

    string VectorToLine(Vector3 vec) {
        return vec.x.ToString().Replace(",", ".") + " " + vec.y.ToString().Replace(",", ".") + " " + vec.z.ToString().Replace(",", ".");
    }
}
