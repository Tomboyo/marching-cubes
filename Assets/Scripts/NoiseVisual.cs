using UnityEngine;

public class NoiseVisual : MonoBehaviour
{

    public ComputeShader marchingShader;
    ComputeBuffer weightsBuffer;
    ComputeBuffer trianglesBuffer;
    ComputeBuffer trianglesCountBuffer;

    struct Triangle
    {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public static int SizeOf => sizeof(float) * 3 * 3;
    }

    public int weightsDimension = 8;
    public MeshFilter meshFilter;
    public float isoLevel = 0.5f;

    float[] weights;

    void Awake()
    {
        // TODO: buffer stride should be a multiple of 16 per https://docs.unity3d.com/ScriptReference/ComputeBufferType.Default.html
        weightsBuffer = new ComputeBuffer(
            weightsDimension * weightsDimension * weightsDimension,
            sizeof(float));
        // TODO: buffer stride should be a multiple of 4 per https://docs.unity3d.com/ScriptReference/ComputeBufferType.Append.html
        trianglesBuffer = new ComputeBuffer(
            // There are at most 5 triangles per cube
            5 * weightsDimension * weightsDimension * weightsDimension,
            Triangle.SizeOf,
            ComputeBufferType.Append);
        trianglesCountBuffer = new ComputeBuffer(
            1,
            sizeof(int),
            ComputeBufferType.Raw);
    }

    void OnDestroy()
    {
        weightsBuffer.Release();
        trianglesBuffer.Release();
        trianglesCountBuffer.Release();
    }


    // Start is called before the first frame update
    void Start()
    {
        weights = Noise.generate(weightsDimension);
        meshFilter.mesh = marchingCubes();
    }

    private Mesh marchingCubes()
    {

        // Bind input buffers
        marchingShader.SetBuffer(0, "weights", weightsBuffer);
        marchingShader.SetFloat("isoLevel", isoLevel);
        marchingShader.SetInt("dimension", weightsDimension);
        // Bind output buffer
        marchingShader.SetBuffer(0, "triangles", trianglesBuffer);

        weightsBuffer.SetData(weights);
        trianglesBuffer.SetCounterValue(0);

        // The shader uses numthreads(8, 8, 8), so we need one thread group per 8x8x8 subdivision of the weights.
        marchingShader.Dispatch(
            0,
            weightsDimension / 8,
            weightsDimension / 8,
            weightsDimension / 8);

        // Read how many triangles were appended to the triangle buffer by the shader
        ComputeBuffer.CopyCount(trianglesBuffer, trianglesCountBuffer, 0);
        int[] tmp = new int[] { 0 };
        trianglesCountBuffer.GetData(tmp);
        int count = tmp[0];

        // Now we can read out the triangles
        var triangles = new Triangle[count];
        trianglesBuffer.GetData(triangles);

        // Convert the buffer to a vertex array and a triangle array
        // The triangle array's elememts are indexes into the vertex array.
        // Every three elements describes the points of a triangle.
        var meshVertices = new Vector3[3 * count];
        var meshTriangles = new int[3 * count];
        for (int i = 0; i < count; i++)
        {
            int j = 3 * i;

            meshVertices[j] = triangles[i].c;
            meshVertices[j + 1] = triangles[i].b;
            meshVertices[j + 2] = triangles[i].a;

            meshTriangles[j] = j;
            meshTriangles[j + 1] = j + 1;
            meshTriangles[j + 2] = j + 2;
        }

        var mesh = new Mesh();
        mesh.vertices = meshVertices;
        mesh.triangles = meshTriangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private void OnDrawGizmos()
    {
        if (weights == null || weights.Length == 0)
            return;

        for (int x = 0; x < weightsDimension; x++)
        {
            for (int y = 0; y < weightsDimension; y++)
            {
                for (int z = 0; z < weightsDimension; z++)
                {
                    var index = Coordinates.cubicToLinear(weightsDimension, x, y, z);
                    var weight = weights[index];
                    Gizmos.color = Color.Lerp(Color.black, Color.white, weight);
                    Gizmos.DrawCube(new Vector3(x, y, z), Vector3.one * .1f);
                }
            }
        }

    }
}
