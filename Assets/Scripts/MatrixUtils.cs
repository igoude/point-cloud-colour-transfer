using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.LinearAlgebra;

using Accord.Statistics.Analysis;
using Accord.Math.Decompositions;
using Accord.MachineLearning;

using UnityEngine;

public class MatrixUtils : MonoBehaviour {

    public static Matrix4x4 ToUnityMatrix(Matrix<float> mat) {
        Vector4 column0 = new Vector4(mat.Column(0)[0], mat.Column(0)[1], mat.Column(0)[2]);
        Vector4 column1 = new Vector4(mat.Column(1)[0], mat.Column(1)[1], mat.Column(1)[2]);
        Vector4 column2 = new Vector4(mat.Column(2)[0], mat.Column(2)[1], mat.Column(2)[2]);
        Vector4 column3 = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

        return new Matrix4x4(column0, column1, column2, column3);
    }

    public static Matrix<float> FromUnityMatrix(Matrix4x4 mat) {
        Matrix<float> result = CreateMatrix.Dense<float>(3, 3);

        float[] col = { mat.GetColumn(0).x, mat.GetColumn(0).y, mat.GetColumn(0).z };
        result.SetColumn(0, col);
        col = new float[]{ mat.GetColumn(1).x, mat.GetColumn(1).y, mat.GetColumn(1).z };
        result.SetColumn(1, col);
        col = new float[] { mat.GetColumn(2).x, mat.GetColumn(2).y, mat.GetColumn(2).z };
        result.SetColumn(2, col);

        return result;
    }

    public static float ComputeMean(Vector<float> values) {
        float mean = 0.0f;
        int size = 0;

        for (int i = 0; i < values.Count; i++) {
            mean += values[i];
            size++;
        }

        float result = (float)mean / size;
        return float.IsNaN(result) ? 0.0f : result;
    }

    public static float ComputeMean(Matrix<float> values, int parameter) {
        float mean = 0.0f;
        int size = 0;

        for (int i = 0; i < values.RowCount; i++) {
            mean += values[i, parameter];
            size++;
        }

        float result = (float)mean / size;
        return float.IsNaN(result) ? 0.0f : result;
    }

    public static Vector<float> ComputeMeans(Matrix<float> values) {
        int dimension = values.ColumnCount;
        float[] means = new float[dimension];

        for (int i = 0; i < dimension; i++) {
            means[i] = ComputeMean(values, i);
        }

        return CreateVector.DenseOfArray<float>(means);
    }

    public static float ComputeVariance(Matrix<float> values, float mean, int parameter) {
        float sqMean = mean * mean;
        float sumMeans = 0.0f;
        int size = 0;

        for (int i = 0; i < values.RowCount; i++) {
            sumMeans += (values[i, parameter] * values[i, parameter]) - sqMean;
            size++;
        }

        float result = (float)sumMeans / size;
        return float.IsNaN(result) ? 0.0f : result;
    }

    public static Vector<float> ComputeVariances(Matrix<float> values, Vector<float> means) {
        int dimension = values.ColumnCount;
        float[] vars = new float[dimension];

        for (int i = 0; i < dimension; i++) {
            vars[i] = ComputeVariance(values, means[i], i);
        }

        return CreateVector.DenseOfArray<float>(vars);
    }

    public static Vector<float> ComputeStandardDeviations(Matrix<float> values, Vector<float> means) {
        int dimension = values.ColumnCount;
        float[] vars = new float[dimension];

        for (int i = 0; i < dimension; i++) {
            vars[i] = Mathf.Sqrt(ComputeVariance(values, means[i], i));
        }

        return CreateVector.DenseOfArray<float>(vars);
    }

    public static float ComputeCovarianceElement(Matrix<float> values, Vector<float> means, int paramA, int paramB) {
        float sumMeans = 0.0f;
        int size = 0;

        for (int i = 0; i < values.RowCount; i++) {
            float valA = values[i, paramA] - means[paramA];
            float valB = values[i, paramB] - means[paramB];

            sumMeans += valA * valB;
            size++;
        }

        float result = (float)sumMeans / (size - 1);
        return float.IsNaN(result) ? 0.0f : result;
    }

    public static Matrix<float> ComputeCovariance(Matrix<float> values, Vector<float> means) {
        int dimension = means.Count;
        float[,] covMatrix = new float[dimension, dimension];

        for (int i = 0; i < dimension; i++) {
            for (int j = 0; j <= i; j++) {
                float cov = ComputeCovarianceElement(values, means, i, j);
                covMatrix[i, j] = cov;
                covMatrix[j, i] = cov;
            }
        }

        return CreateMatrix.DenseOfArray<float>(covMatrix);
    }

    public static Matrix<float> ComputeCorrelation(Matrix<float> values) {
        Vector<float> means = ComputeMeans(values);
        Matrix<float> cov = ComputeCovariance(values, means);
        Vector<float> stds = cov.Diagonal().PointwiseSqrt();

        int dimension = means.Count;

        for (int i = 0; i < dimension; i++) {
            for (int j = 0; j <= i; j++) {
                float deviation = stds[i]*stds[j];
                cov[i, j] /= deviation;
                cov[j, i] /= deviation;
            }
        }

        return cov;
    }

    public static Matrix<float> CenterValues(Matrix<float> values, Vector<float> means) {
        Matrix<float> result = CreateMatrix.Dense<float>(values.RowCount, values.ColumnCount);

        for (int i = 0; i < values.RowCount; i++) {
            result.SetRow(i, values.Row(i) - means);
        }

        return result;
    }

    public static Matrix<float> MatrixSquareRoot(Matrix<float> values) {
        Evd<float> evd = values.Evd(Symmetricity.Symmetric);
        Matrix<float> diagMatrix = evd.D;
        Matrix<float> eigenVectors = evd.EigenVectors;
        
        for (int i = 0; i < diagMatrix.RowCount; i++) {
            float sqrtValue = Mathf.Sqrt(Mathf.Abs(diagMatrix[i, i]));
            diagMatrix[i, i] = float.IsNaN(sqrtValue) ? 0.0f : sqrtValue;
        }

        Matrix<float> sqrtRootMatrix = eigenVectors * diagMatrix * eigenVectors.Inverse();

        return sqrtRootMatrix;
    }

    public static Matrix<float> ClosedFormMatrix(Matrix<float> refCovs, Matrix<float> tarCovs) {
        Matrix<float> refSqrtRootCovMat = MatrixSquareRoot(refCovs);
        Matrix<float> refSqrtRootCovMatInv = refSqrtRootCovMat.Inverse();
       
        Matrix<float> middleTerm = refSqrtRootCovMat * tarCovs * refSqrtRootCovMat;
        middleTerm = MatrixSquareRoot(middleTerm);

        Matrix<float> matrixT = refSqrtRootCovMatInv * middleTerm * refSqrtRootCovMatInv;

        return matrixT;
    }

    public static Matrix<float> PcaTransform(Matrix<float> data) {
        double[][] X = new double[data.RowCount][];
        for (int i = 0; i < data.RowCount; i++) {
            X[i] = new double[data.ColumnCount];
            for (int j = 0; j < data.ColumnCount; j++) {
                X[i][j] = (double)data[i, j];
            }
        }

        PrincipalComponentAnalysis pca = new PrincipalComponentAnalysis();
        pca.Learn(X);

        Matrix<float> demix = CreateMatrix.Dense<float>(pca.ComponentVectors.Length, pca.ComponentVectors.Length);
        for (int i = 0; i < demix.ColumnCount; i++) {
            float[] col = new float[demix.RowCount];
            for (int j = 0; j < demix.ColumnCount; j++) {
                col[j] = (float)pca.ComponentVectors[i][j];
            }
            demix.SetColumn(i, col);
        }

        return demix.Transpose();
    }
}
