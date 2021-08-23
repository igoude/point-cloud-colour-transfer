using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

public class PointCloudStyleExtractor : StyleExtractor {
    public PlyRenderer m_plyRenderer;

    public bool m_normalsPCA;

    [HideInInspector]
    public Matrix<float> m_eigenNormals;

    public Matrix4x4 GetEigenNormals() {
        Vector4 column0 = new Vector4(m_eigenNormals.Column(0)[0], m_eigenNormals.Column(0)[1], m_eigenNormals.Column(0)[2]);
        Vector4 column1 = new Vector4(m_eigenNormals.Column(1)[0], m_eigenNormals.Column(1)[1], m_eigenNormals.Column(1)[2]);
        Vector4 column2 = new Vector4(m_eigenNormals.Column(2)[0], m_eigenNormals.Column(2)[1], m_eigenNormals.Column(2)[2]);
        Vector4 column3 = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

        return new Matrix4x4(column0, column1, column2, column3);
    }

    public bool HasNormals() {
        Vector3 normalsStds = new Vector3(m_std[3], m_std[4], m_std[5]);
        return !normalsStds.Equals(Vector3.zero);
    }
    
    public override bool Ready() {
        return m_plyRenderer.GetNbPoints() > 0;
    }

    public override void ExtractStyle() {
        m_styleType = StyleType.PointCloud;
        m_nbData = m_plyRenderer.GetNbPoints();

        Bounds pcBounds = m_plyRenderer.GetBounds();

        int dim = 9;
        m_mean = CreateVector.Dense<float>(dim);
        m_std = CreateVector.Dense<float>(dim);
        m_cov = CreateMatrix.Dense<float>(dim, dim);

        m_meanNormal = CreateMatrix.Dense<float>(3, 6);
        m_stdNormal = CreateMatrix.Dense<float>(3, 6);

        Vector<float> normNormal = CreateVector.Dense<float>(6);

        m_eigenNormals = CreateMatrix.DiagonalIdentity<float>(3);
        
        int id = 0;
        if (m_normalsPCA) {
            Matrix<float> normals = CreateMatrix.Dense<float>(m_nbData, 3);

            foreach (PointCloud pc in m_plyRenderer.GetPointClouds()) {
                for (int i = 0; i < pc.m_vertices.Count; i++) {
                    // Get normal
                    float[] ns = { pc.m_normals[i].x, pc.m_normals[i].y, pc.m_normals[i].z };
                    normals.SetRow(id, ns);
                    id++;
                }
            }
            
            m_eigenNormals = MatrixUtils.PcaTransform(normals);

            // Automatic arrangement
            Vector<float> n0 = m_eigenNormals.Row(0);
            Vector<float> n1 = m_eigenNormals.Row(1);
            Vector<float> n2 = m_eigenNormals.Row(2);

            Vector<float> x = CreateVector.Dense<float>(3);
            Vector<float> y = CreateVector.Dense<float>(3);
            Vector<float> z = CreateVector.Dense<float>(3);
            x[0] = 1.0f;
            y[1] = 1.0f;
            z[2] = 1.0f;

            bool Xfound = false;
            bool Yfound = false;
            bool Zfound = false;

            // Start with the first vector
            float xAngle = Mathf.Abs(n0.DotProduct(x));
            float yAngle = Mathf.Abs(n0.DotProduct(y));
            float zAngle = Mathf.Abs(n0.DotProduct(z));

            if (xAngle >= yAngle && xAngle >= zAngle) {
                Xfound = true;
                m_eigenNormals.SetRow(0, n0.DotProduct(x) > 0.0f ? n0 : -n0);
            } else if (yAngle > xAngle && yAngle >= zAngle) {
                Yfound = true;
                m_eigenNormals.SetRow(1, n0.DotProduct(y) > 0.0f ? n0 : -n0);
            } else if (zAngle > xAngle && zAngle > yAngle) {
                Zfound = true;
                m_eigenNormals.SetRow(2, n0.DotProduct(z) > 0.0f ? n0 : -n0);
            }

            // Second vector
            xAngle = Mathf.Abs(n1.DotProduct(x));
            yAngle = Mathf.Abs(n1.DotProduct(y));
            zAngle = Mathf.Abs(n1.DotProduct(z));
            if (!Xfound && xAngle >= yAngle && xAngle >= zAngle) {
                Xfound = true;
                m_eigenNormals.SetRow(0, n1.DotProduct(x) > 0.0f ? n1 : -n1);
            } else if (!Yfound && yAngle > xAngle && yAngle >= zAngle) {
                Yfound = true;
                m_eigenNormals.SetRow(1, n1.DotProduct(y) > 0.0f ? n1 : -n1);
            } else if (!Zfound && zAngle > xAngle && zAngle > yAngle) {
                Zfound = true;
                m_eigenNormals.SetRow(2, n1.DotProduct(z) > 0.0f ? n1 : -n1);
            }

            // Third vector
            xAngle = Mathf.Abs(n2.DotProduct(x));
            yAngle = Mathf.Abs(n2.DotProduct(y));
            zAngle = Mathf.Abs(n2.DotProduct(z));
            if (!Xfound && xAngle >= yAngle && xAngle >= zAngle) {
                Xfound = true;
                m_eigenNormals.SetRow(0, n2.DotProduct(x) > 0.0f ? n2 : -n2);
            } else if (!Yfound && yAngle > xAngle && yAngle >= zAngle) {
                Yfound = true;
                m_eigenNormals.SetRow(1, n2.DotProduct(y) > 0.0f ? n2 : -n2);
            } else if (!Zfound && zAngle > xAngle && zAngle > yAngle) {
                Zfound = true;
                m_eigenNormals.SetRow(2, n2.DotProduct(z) > 0.0f ? n2 : -n2);
            }
        } 


        // Extract color data
        foreach (PointCloud pc in m_plyRenderer.GetPointClouds()) {
            for (int i = 0; i < pc.m_vertices.Count; i++) {
                // Transform normal in eigen normal space
                float[] ns = { pc.m_normals[i].x, pc.m_normals[i].y, pc.m_normals[i].z };
                Vector<float> normal = CreateVector.Dense<float>(ns);

                // Transform in Eigen Normal Space
                Vector<float> eigenNormal = normal * m_eigenNormals;
                normal = eigenNormal.Normalize(2.0f);

                // Get normalized data color
                float r = pc.m_colors[i].r;
                float g = pc.m_colors[i].g;
                float b = pc.m_colors[i].b;

                // Transform color in lab space
                Color lab = ColorUtils.ReinhardRGBtoLab(new Color(r, g, b));
                
                // Save values depending on normal direction
                normal = normal.Normalize(1.0f);

                // Matrix writing
                float[] nn = { Mathf.Abs(Mathf.Min(normal[0], 0.0f)), Mathf.Max(normal[0], 0.0f), Mathf.Abs(Mathf.Min(normal[1], 0.0f)), Mathf.Max(normal[1], 0.0f), Mathf.Abs(Mathf.Min(normal[2], 0.0f)), Mathf.Max(normal[2], 0.0f) };
                Vector<float> n = CreateVector.Dense<float>(nn);

                float[] cc = { lab[0], lab[1], lab[2] };
                Vector<float> c = CreateVector.Dense<float>(cc);

                m_meanNormal += c.ToColumnMatrix().KroneckerProduct(n.ToRowMatrix());
                m_stdNormal += c.PointwiseMultiply(c).ToColumnMatrix().KroneckerProduct(n.ToRowMatrix());
                normNormal += n;

                // Save data point
                float[] values = { lab[0], lab[1], lab[2], n[0], n[1], n[2], n[3], n[4], n[5] };
                Vector<float> val = CreateVector.Dense<float>(values);

                m_mean += val;
                m_cov += val.ToColumnMatrix().KroneckerProduct(val.ToRowMatrix());
            }
            pc.UpdateCloud();
        }

        // Normalize mean and std of each normal direction
        for(int i = 0; i < 3; i++) {
            m_meanNormal.SetRow(i, m_meanNormal.Row(i).PointwiseDivide(normNormal));
            m_stdNormal.SetRow(i, m_stdNormal.Row(i).PointwiseDivide(normNormal));
        }
        m_stdNormal = (m_stdNormal - m_meanNormal.PointwiseMultiply(m_meanNormal)).PointwiseSqrt();
        

        m_mean /= (float)m_nbData;
        m_cov /= (float)(m_nbData - 1);
        // Remove singularities
        for(int i = 0; i < m_cov.RowCount; i++) {
            for (int j = 0; j < m_cov.ColumnCount; j++) {
                if(m_cov[i, j] == 0.0f) {
                    m_cov[i, j] = 1e-3f;
                }
            }
        }
        m_cov -= m_mean.ToColumnMatrix().KroneckerProduct(m_mean.ToRowMatrix());
        m_std = m_cov.Diagonal().PointwiseSqrt();
    }
}
