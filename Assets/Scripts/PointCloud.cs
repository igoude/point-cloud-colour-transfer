using System.Collections.Generic;
using UnityEngine;

public class PointCloud : MonoBehaviour
{
    [HideInInspector]
    public Mesh m_mesh;

    [HideInInspector]
    public List<Vector3> m_vertices;
    [HideInInspector]
    public List<Vector3> m_normals;
    [HideInInspector]
    public List<Color> m_colors;
    [HideInInspector]
    public List<Vector2> m_uvs;
    [HideInInspector]
    public List<int> m_triangles;
    [HideInInspector]
    public List<int> m_indices;

    [HideInInspector]
    public Material m_material;

    [HideInInspector]
    public MeshTopology m_topology;


    public void UpdateCloud() {
        m_material = GetComponent<MeshRenderer>().material;

        m_mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = m_mesh;

        m_mesh.SetVertices(m_vertices);
        m_mesh.SetNormals(m_normals);
        m_mesh.SetColors(m_colors);
        m_mesh.SetUVs(0, m_uvs);

        //if (m_topology == MeshTopology.Triangles && m_triangles.Count > 0) {
        if (m_triangles.Count > 0) {
            // Used to compute mesh collider
            m_mesh.SetIndices(m_triangles.ToArray(), MeshTopology.Triangles, 0);
            m_mesh.RecalculateNormals();
            m_mesh.RecalculateBounds();

            MeshCollider collider = gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = m_mesh;

        }

        // Then render as a point cloud
        m_mesh.SetIndices(m_indices.ToArray(), MeshTopology.Points, 0);
    }
    

    public void UpdateMaterial(Material newMat) {
        GetComponent<MeshRenderer>().material = newMat;
        m_material = GetComponent<MeshRenderer>().material;
    }
}
