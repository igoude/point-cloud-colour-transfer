using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

public class StyleTransfer : MonoBehaviour
{
    public PointCloudStyleExtractor m_input;
    public PointCloudStyleExtractor m_target;

    public Shader m_styleTransferShader;
    
    public PlyRenderer.PositionSpace m_positionSpace;
    [Range(0.0f, 1.0f)]
    public float m_switchSpace;


    public enum TransferMethod {
        IGD = 0,
        IGD_N= 1,
        MGD = 2,
        MGD_N = 3
    }
    public TransferMethod m_transferMethod;
    TransferMethod m_previousMethod;

    [Range(0.0f, 1.0f)]
    public float m_switchTransfer;

    [Range(0.0f, 1.0f)]
    public float m_switchNormal = 1.0f;
    
    public float m_exposure = 1.0f;

    float[] m_transformL;
    float[] m_transformA;
    float[] m_transformB;
    Matrix<float> m_matrixT;

    float[] m_transformLNormal;
    float[] m_transformANormal;
    float[] m_transformBNormal;
    Matrix<float> m_matrixTNormal;
    
    bool m_dataLoaded = false;

    public bool m_normalsDebuger;

    // Update is called once per frame
    void Update() {
        if (!m_dataLoaded) {
            if(m_input.Ready() && m_target.Ready()) {
                m_input.ExtractStyle();
                m_target.ExtractStyle();

                ComputePitieTransform();

                m_transferMethod = TransferMethod.IGD;
                m_previousMethod = TransferMethod.IGD;
                m_switchTransfer = 0.0f;
                
                // Init input
                foreach (PointCloud pc in m_input.m_plyRenderer.GetPointClouds()) {
                    pc.UpdateMaterial(new Material(m_styleTransferShader));
                    
                    pc.m_material.SetFloatArray("_InputMeans", m_input.GetMeans().ToArray());
                    pc.m_material.SetFloatArray("_InputStds", m_input.GetStds().ToArray());
                    pc.m_material.SetFloatArray("_TargetMeans", m_target.GetMeans().ToArray());
                    pc.m_material.SetFloatArray("_TargetStds", m_target.GetStds().ToArray());
                    pc.m_material.SetInt("_TransferMethod", 0);

                    pc.m_material.SetMatrix("_EigenNormals", m_input.GetEigenNormals());
                }

                // Init target
                foreach (PointCloud pc in m_target.m_plyRenderer.GetPointClouds()) {
                    pc.m_material.SetMatrix("_EigenNormals", m_target.GetEigenNormals());
                }
                
                m_dataLoaded = true;                       
            }
        }

        if(m_transferMethod != m_previousMethod) {
            UpdateTransfer();
            m_previousMethod = m_transferMethod;
        }

        foreach (PointCloud pc in m_input.m_plyRenderer.GetPointClouds()) {
            pc.m_material.SetFloat("_SwitchTransfer", m_switchTransfer);
            pc.m_material.SetFloat("_SwitchNormal", m_switchNormal);
            pc.m_material.SetFloat("_Exposure", m_exposure);
            pc.m_material.SetInt("_NormalsDebuger", m_normalsDebuger ? 1 : 0);
        }

        foreach (PointCloud pc in m_target.m_plyRenderer.GetPointClouds()) {
            pc.m_material.SetFloat("_SwitchNormal", m_switchNormal);
            pc.m_material.SetInt("_NormalsDebuger", m_normalsDebuger ? 1 : 0);
        }
        
        m_input.m_plyRenderer.m_positionSpace = m_positionSpace;
        m_input.m_plyRenderer.m_switchSpace = m_switchSpace;

        m_target.m_plyRenderer.m_positionSpace = m_positionSpace;
        m_target.m_plyRenderer.m_switchSpace = m_switchSpace;
    }

    void UpdateTransfer() {
        foreach (PointCloud pc in m_input.m_plyRenderer.GetPointClouds()) {
            switch (m_transferMethod) {
                case TransferMethod.IGD:
                    pc.m_material.SetFloatArray("_InputMeans", m_input.GetMeans().ToArray());
                    pc.m_material.SetFloatArray("_InputStds", m_input.GetStds().ToArray());
                    pc.m_material.SetFloatArray("_TargetMeans", m_target.GetMeans().ToArray());
                    pc.m_material.SetFloatArray("_TargetStds", m_target.GetStds().ToArray());
                    break;

                case TransferMethod.IGD_N:
                    pc.m_material.SetFloatArray("_InputMeansL", m_input.GetMeansL());
                    pc.m_material.SetFloatArray("_InputMeansA", m_input.GetMeansA());
                    pc.m_material.SetFloatArray("_InputMeansB", m_input.GetMeansB());
                    pc.m_material.SetFloatArray("_InputStdsL", m_input.GetStdsL());
                    pc.m_material.SetFloatArray("_InputStdsA", m_input.GetStdsA());
                    pc.m_material.SetFloatArray("_InputStdsB", m_input.GetStdsB());
                    pc.m_material.SetFloatArray("_TargetMeansL", m_target.GetMeansL());
                    pc.m_material.SetFloatArray("_TargetMeansA", m_target.GetMeansA());
                    pc.m_material.SetFloatArray("_TargetMeansB", m_target.GetMeansB());
                    pc.m_material.SetFloatArray("_TargetStdsL", m_target.GetStdsL());
                    pc.m_material.SetFloatArray("_TargetStdsA", m_target.GetStdsA());
                    pc.m_material.SetFloatArray("_TargetStdsB", m_target.GetStdsB());
                    break;

                case TransferMethod.MGD:
                    pc.m_material.SetFloatArray("_InputMeans", m_input.GetMeans().ToArray());
                    pc.m_material.SetFloatArray("_TargetMeans", m_target.GetMeans().ToArray());
                    pc.m_material.SetFloatArray("_TransformL", m_transformL);
                    pc.m_material.SetFloatArray("_TransformA", m_transformA);
                    pc.m_material.SetFloatArray("_TransformB", m_transformB);
                    break;

                case TransferMethod.MGD_N:
                    pc.m_material.SetFloatArray("_InputMeans", m_input.GetMeans().ToArray());
                    pc.m_material.SetFloatArray("_TargetMeans", m_target.GetMeans().ToArray());
                    pc.m_material.SetFloatArray("_TransformLNormal", m_transformLNormal);
                    pc.m_material.SetFloatArray("_TransformANormal", m_transformANormal);
                    pc.m_material.SetFloatArray("_TransformBNormal", m_transformBNormal);
                    break;

                default: break;
            }

            pc.m_material.SetInt("_TransferMethod", (int)m_transferMethod);
        }
    }


    void ComputePitieTransform() {
        // 3D Monge-Kantorovich mapping
        Matrix<float> inpCov = m_input.GetCovs().SubMatrix(0, 3, 0, 3);
        Matrix<float> tarCov = m_target.GetCovs().SubMatrix(0, 3, 0, 3);
        m_matrixT = MatrixUtils.ClosedFormMatrix(inpCov, tarCov);
        
        m_transformL = m_matrixT.Column(0).ToArray();
        m_transformA = m_matrixT.Column(1).ToArray();
        m_transformB = m_matrixT.Column(2).ToArray();


        // 9D Monge-Kantorovich mapping
        m_matrixTNormal = MatrixUtils.ClosedFormMatrix(m_input.GetCovs(), m_target.GetCovs());

        m_transformLNormal = m_matrixTNormal.Column(0).ToArray();
        m_transformANormal = m_matrixTNormal.Column(1).ToArray();
        m_transformBNormal = m_matrixTNormal.Column(2).ToArray();
    }

    public Matrix<float> GetMongeKantorovichMatrix() {
        return m_matrixTNormal;
    }
}
