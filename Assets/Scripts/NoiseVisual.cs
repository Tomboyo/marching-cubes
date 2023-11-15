using UnityEngine;

public class NoiseVisual : MonoBehaviour
{

    public ComputeShader marchingShader;
    ComputeBuffer weightsBuffer;
    ComputeBuffer vertexBuffer;
    ComputeBuffer vertexCountBuffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer triangleCountBuffer;

    struct Vertex
    {
        public int id;
        public Vector3 p;

        public static int SizeOf => sizeof(int) + sizeof(float) * 3;
    }

    struct Triangle
    {
        public int v1, v2, v3;

        public static int SizeOf => sizeof(int) * 3;
    }

    public int weightsDimension = 8;
    public MeshFilter meshFilter;
    public float isoLevel = 0.5f;

    float[] weights;

    void Awake()
    {
        // buffer stride should be a multiple of 16 per https://docs.unity3d.com/ScriptReference/ComputeBufferType.Default.html
        weightsBuffer = new ComputeBuffer(
            weightsDimension * weightsDimension * weightsDimension,
            sizeof(float));
        // buffer stride should be a multiple of 4 per https://docs.unity3d.com/ScriptReference/ComputeBufferType.Append.html
        vertexBuffer = new ComputeBuffer(
            3 * 5 * weightsDimension * weightsDimension * weightsDimension,
            Vertex.SizeOf,
            ComputeBufferType.Append);
        vertexCountBuffer = new ComputeBuffer(
            1,
            sizeof(int),
            ComputeBufferType.Raw);
        // buffer stride should be a multiple of 4 per https://docs.unity3d.com/ScriptReference/ComputeBufferType.Append.html
        triangleBuffer = new ComputeBuffer(
            // There are at most 5 triangles per cube
            5 * weightsDimension * weightsDimension * weightsDimension,
            Triangle.SizeOf,
            ComputeBufferType.Append);
        triangleCountBuffer = new ComputeBuffer(
            1,
            sizeof(int),
            ComputeBufferType.Raw);
    }

    void OnDestroy()
    {
        weightsBuffer.Release();
        vertexBuffer.Release();
        vertexCountBuffer.Release();
        triangleBuffer.Release();
        triangleCountBuffer.Release();
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
        marchingShader.SetBuffer(0, "vertexBuffer", vertexBuffer);
        marchingShader.SetBuffer(0, "triangleBuffer", triangleBuffer);

        weightsBuffer.SetData(weights);
        vertexBuffer.SetCounterValue(0);
        triangleBuffer.SetCounterValue(0);

        // The shader uses numthreads(8, 8, 8), so we need one thread group per 8x8x8 subdivision of the weights.
        marchingShader.Dispatch(
            0,
            weightsDimension / 8,
            weightsDimension / 8,
            weightsDimension / 8);

        var vertices = new Vertex[extractCount(vertexBuffer, vertexCountBuffer)];
        vertexBuffer.GetData(vertices);
        var triangles = new Triangle[extractCount(triangleBuffer, triangleCountBuffer)];
        triangleBuffer.GetData(triangles);

        // at most 3 vertices per 5 triangles per cube
        var meshVertices = new Vector3[3 * 5 * (weightsDimension) * (weightsDimension) * (weightsDimension)];
        var meshTriangles = new int[3 * triangles.Length];

        // The shader sends us a globally unique vertex ID per vertex which the
        // triangles buffer uses to describe triangles. These IDs are sparse --
        // most IDs are empty space. Rather than use each vertex ID as the mesh
        // vertex ID directly, which wastes space, we pack the unique vertices
        // into the array and keep a mapping from vertex ID to mesh vertex array
        // offset. We will reference the mapping when construting the triangle
        // mesh array.
        var mapping = new int?[meshVertices.Length];
        for (int i = 0, j = 0; i < vertices.Length; i++)
        {
            var v = vertices[i];
            if (!mapping[v.id].HasValue)
            {
                mapping[v.id] = j;
                meshVertices[j] = v.p;
                j += 1;
            }
        }

        for (int i = 0; i < triangles.Length; i++)
        {
            int j = 3 * i;
            var t = triangles[i];
            meshTriangles[j] = mapping[t.v3].Value;
            meshTriangles[j + 1] = mapping[t.v2].Value;
            meshTriangles[j + 2] = mapping[t.v1].Value;
        }

        var mesh = new Mesh();
        mesh.vertices = meshVertices;
        mesh.triangles = meshTriangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private int extractCount(ComputeBuffer buffer, ComputeBuffer countBuffer)
    {
        ComputeBuffer.CopyCount(buffer, countBuffer, 0);
        int[] tmp = new int[] { 0 };
        countBuffer.GetData(tmp);
        return tmp[0];
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
