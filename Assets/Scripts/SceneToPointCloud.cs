using UnityEngine;
using System.Text;
using System.IO;

/* 
 * @class           SceneToPointCloud.cs
 * @author          Ific Goudé
 * @institution     Irisa, Rennes, France
 * @date            07/02/2020
 * 
 * Transform the 3D scene into a point cloud (.ply) containing
 *      - point position
 *      - point normal
 *      - point color (specular is not considered)
 */
public class SceneToPointCloud : MonoBehaviour {
    // User define number of sample for the shortest side of the scene
    public int m_nbSamplePerSide;

    public string m_savePath;
    public bool m_save;

    // Structure of a scene point
    struct HitPoint {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 color;
    };
    // The last point hit in the scene
    HitPoint m_lastPoint;
    // Total number of points hit
    int m_nbPoints;

    // Helper attributes
    [HideInInspector]
    /*public*/ Bounds m_sceneBounds;        // Bounds of the scene
    /*public*/ Vector3 m_nbSamples;         // Number of sample for each side
    /*public*/ float m_step;                // Length of a step
    /*public*/ float m_maxDistance;         // Lenght of the longest side
    StringBuilder m_stringBuilder;      // String builder to save fixation points as a .ply file
    Light[] m_lights;                   // List of all lights in the scene

    // DEBUG: show all hit points in the scene
    public bool m_debugPoints;


    private void Update() {
        if (m_save) {
            m_save = false;
            SaveScene(m_savePath);
        }    
    }

    // Start is called before the first frame update
    public void SaveScene(string path) {
        // Define scene bounds containing all the game objects
        DefineSceneBounds();

        // Define the step between two sampler rays
        float minSideSize = Mathf.Min(m_sceneBounds.size.x, m_sceneBounds.size.y, m_sceneBounds.size.z);
        m_step = minSideSize / (float)m_nbSamplePerSide;

        // Define max distance for a ray
        m_maxDistance = (m_sceneBounds.max - m_sceneBounds.min).magnitude;

        // Define number of samples for each axis
        m_nbSamples = m_sceneBounds.size / m_step;

        // Define the .ply file
        m_nbPoints = 0;
        m_lastPoint = new HitPoint();
        m_stringBuilder = new StringBuilder();
        CompleteHeader();

        // Get all lights in the scene
        m_lights = GameObject.FindObjectsOfType<Light>();

        // Start ray sampling
        SampleFace(Vector3.right, 1);      // +X face
        SampleFace(-Vector3.right, -1);    // -X face
        SampleFace(Vector3.up, 1);         // +Y face
        SampleFace(-Vector3.up, -1);       // -Y face
        SampleFace(Vector3.forward, 1);    // +Z face
        SampleFace(-Vector3.forward, -1);  // -Z face

        // Add element vertex
        Debug.Log("Number of sampled points: " + m_nbPoints);
        m_stringBuilder.Insert(38, m_nbPoints);

        // Save .ply file
        StreamWriter outStream = System.IO.File.CreateText(path);
        outStream.WriteLine(m_stringBuilder);
        outStream.Close();
    }


    void SampleFace(Vector3 direction, int side) {
        int samples1 = 0, samples2 = 0;
        
        Vector3 origin = side == 1 ? m_sceneBounds.min : m_sceneBounds.max;
        origin -= direction * m_step;

        // Define axis to sample
        if (direction.x == side) {
            samples1 = Mathf.CeilToInt(m_nbSamples.y);
            samples2 = Mathf.CeilToInt(m_nbSamples.z);
        }
        if (direction.y == side) {
            samples1 = Mathf.CeilToInt(m_nbSamples.x);
            samples2 = Mathf.CeilToInt(m_nbSamples.z);
        }
        if (direction.z == side) {
            samples1 = Mathf.CeilToInt(m_nbSamples.x);
            samples2 = Mathf.CeilToInt(m_nbSamples.y);
        }

        // Loop over both axis
        for (int i = 0; i <= samples1; i++) {
            for (int j = 0; j <= samples2; j++) {
                // Define the delta step
                Vector3 delta = Vector3.zero;
                if (direction.x == side) {
                    delta.y = i * m_step;
                    delta.z = j * m_step;
                }
                if (direction.y == side) {
                    delta.x = i * m_step;
                    delta.z = j * m_step;
                }
                if (direction.z == side) {
                    delta.x = i * m_step;
                    delta.y = j * m_step;
                }
                Vector3 o = origin + delta * side;

                foreach (RaycastHit hit in Physics.RaycastAll(o, direction, m_maxDistance)) {
                    if (m_debugPoints) {
                        Debug.DrawLine(hit.point - direction * m_step, hit.point, Color.red, 3.0f);
                    }

                    // Compute point data
                    m_lastPoint.position = hit.point;
                    m_lastPoint.normal = hit.normal;
                    m_lastPoint.color = ComputeHitPointColor(hit);

                    m_stringBuilder.AppendLine(HitPointToLine(m_lastPoint));
                    m_nbPoints++;
                }
            }
        }
    }

    Vector3 ComputeHitPointColor(RaycastHit hit) {
        Color result = Color.black;

        MeshRenderer meshRenderer = hit.transform.GetComponent<MeshRenderer>();
        Material material = meshRenderer.material;

        // Multiple materials
        //if(meshRenderer.materials.Length > 1) {
        //    int triangleIdx = hit.triangleIndex;

        //    Mesh mesh = hit.transform.GetComponent<MeshFilter>().mesh;
        //    int lookupIdx1 = mesh.triangles[triangleIdx * 3];
        //    int lookupIdx2 = mesh.triangles[triangleIdx * 3 + 1];
        //    int lookupIdx3 = mesh.triangles[triangleIdx * 3 + 2];

        //    for (int i = 0; i < mesh.subMeshCount; i++) {
        //        int[] tr = mesh.GetTriangles(i);
        //        for (int j = 0; j < tr.Length; j++) {
        //            if (tr[j] == lookupIdx1 && tr[j + 1] == lookupIdx2 && tr[j + 2] == lookupIdx3) {
        //                material = meshRenderer.materials[i];
        //                break;
        //            }
        //        }
        //    }
        //}


        // Texture coordinates (To debug... if any)
        Vector2 texCoord = hit.textureCoord;
        texCoord *= material.mainTextureScale;
        texCoord += material.mainTextureOffset;
        //return new Vector3(texCoord.x, texCoord.y, 0.0f);

        // Albedo
        result = material.color;

        // Diffuse Texture 
        Texture2D mainTex = (Texture2D)material.mainTexture;
        if(mainTex != null) {
            result *= mainTex.GetPixel((int)(texCoord.x * mainTex.width), (int)(texCoord.y * mainTex.height));
        }

        // Specular color ignored for now... How to place the "eye" in the scene when samping a point cloud ???

        // Lights: Does not work yet !!!
        Color lightColor = Color.black;
        foreach (Light light in m_lights) {
            switch (light.type) {
                // Directional lights
                case LightType.Directional: {
                    float strength = 1.0f;
                    Vector3 lightDirection = -light.transform.forward;
                    if (light.shadows != LightShadows.None) {
                        if (Physics.Raycast(hit.point, lightDirection, m_maxDistance)) {
                            strength = 1.0f - light.shadowStrength;
                        }
                    }
                    float angle = Mathf.Max(0.0f, Vector3.Dot(hit.normal, lightDirection.normalized));
                    lightColor += light.color * light.intensity * angle * strength;
                    break;
                }
                // Point lights
                case LightType.Point: {
                        Vector3 lightPosition = light.transform.position;
                        Vector3 lightDirection = lightPosition - hit.point;
                        float lightDistance = lightDirection.magnitude;
                        float strength = 1.0f;
                        if (light.shadows != LightShadows.None) {
                            if (Physics.Raycast(hit.point, lightDirection, lightDistance - light.shadowNearPlane)) {
                                strength = 1.0f - light.shadowStrength;
                            }
                        }
                        float angle = Mathf.Max(0.0f, Vector3.Dot(hit.normal, lightDirection.normalized));
                        float r = Mathf.Max(lightDistance / light.range, 0.0f);
                        float attenuation = 1.0f / (1.0f + 25.0f * r * r);

                        lightColor += light.color * light.intensity * angle * strength * attenuation;
                        break;
                    }
                // Spot lights
                case LightType.Spot: {
                        // TODO...
                        break;
                    }
                default: break;
            }
        }

        // Ambient
        if(RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat) {
            lightColor += RenderSettings.ambientLight;
        }
        result *= lightColor;
        
        // Emission
        Texture2D emissionTex = (Texture2D)material.GetTexture("_EmissionMap");
        if (emissionTex != null) {
            Color emissionColor = material.GetColor("_EmissionColor");
            result += emissionColor * emissionTex.GetPixel((int)(texCoord.x * emissionTex.width), (int)(texCoord.y * emissionTex.height));
        }

        return new Vector3(result.r, result.g, result.b);
    }

    void DefineSceneBounds() {
        m_sceneBounds = new Bounds();
        m_sceneBounds.SetMinMax(Vector3.one * float.MaxValue, Vector3.one * float.MinValue);
        foreach (Collider goCollider in GameObject.FindObjectsOfType<Collider>()) {
            Vector3 minBound = Vector3.zero;
            Vector3 maxBound = Vector3.zero;
            Bounds goBounds = goCollider.bounds;

            minBound.x = Mathf.Min(m_sceneBounds.min.x, goBounds.min.x);
            minBound.y = Mathf.Min(m_sceneBounds.min.y, goBounds.min.y);
            minBound.z = Mathf.Min(m_sceneBounds.min.z, goBounds.min.z);

            maxBound.x = Mathf.Max(m_sceneBounds.max.x, goBounds.max.x);
            maxBound.y = Mathf.Max(m_sceneBounds.max.y, goBounds.max.y);
            maxBound.z = Mathf.Max(m_sceneBounds.max.z, goBounds.max.z);

            m_sceneBounds.SetMinMax(minBound, maxBound);
        }
    }

    void CompleteHeader() {
        m_stringBuilder.AppendLine("ply");
        m_stringBuilder.AppendLine("format ascii 1.0");
        m_stringBuilder.AppendLine("element vertex ");
        // Change this value before save (index 38)
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

    string HitPointToLine(HitPoint point) {
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
