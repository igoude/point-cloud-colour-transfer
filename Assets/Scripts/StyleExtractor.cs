using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

public abstract class StyleExtractor : MonoBehaviour
{
    protected int m_nbData;
    protected Matrix<float> m_data;

    protected Vector<float> m_mean;
    protected Vector<float> m_std;
    protected Matrix<float> m_cov;

    protected Matrix<float> m_meanNormal;
    protected Matrix<float> m_stdNormal;

    public enum StyleType {
        PointCloud,
        Image
    }
    protected StyleType m_styleType;

    public abstract bool Ready();
    public abstract void ExtractStyle();
    
    // Init
    void Start() {
        m_nbData = 0;
        m_data = CreateMatrix.Dense<float>(1, 1);
        m_mean = CreateVector.Dense<float>(1);
        m_std = CreateVector.Dense<float>(1);
        m_cov = CreateMatrix.Dense<float>(1, 1);
        m_meanNormal = CreateMatrix.Dense<float>(1, 1);
        m_stdNormal = CreateMatrix.Dense<float>(1, 1);
    }
    

    public StyleType GetStyleType() {
        return m_styleType;
    }

    public Vector<float> GetMeans() {
        return m_mean;
    }

    public Vector<float> GetStds() {
        return m_std;
    }

    public Matrix<float> GetCovs() {
        return m_cov;
    }
    
    public float[] GetMeansL() {
        return m_meanNormal.Row(0).AsArray();
    }

    public float[] GetStdsL() {
        return m_stdNormal.Row(0).AsArray();
    }

    public float[] GetMeansA() {
        return m_meanNormal.Row(1).AsArray();
    }

    public float[] GetStdsA() {
        return m_stdNormal.Row(1).AsArray();
    }

    public float[] GetMeansB() {
        return m_meanNormal.Row(2).AsArray();
    }

    public float[] GetStdsB() {
        return m_stdNormal.Row(2).AsArray();
    }
}
