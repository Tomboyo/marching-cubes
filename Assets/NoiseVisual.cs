using UnityEngine;

public class NoiseVisual : MonoBehaviour
{

    public int weightsDimension = 8;
    float[] weights;

    // Start is called before the first frame update
    void Start()
    {
        weights = new float[weightsDimension * weightsDimension * weightsDimension];
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = Random.value;
        }

        Debug.Log($"Weights: {weights}");
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
                    var index = cubicToLinear(x, y, z);
                    var weight = weights[index];
                    Gizmos.color = Color.Lerp(Color.black, Color.white, weight);
                    Gizmos.DrawCube(new Vector3(x, y, z), Vector3.one * .2f);
                }
            }
        }

    }


    private int cubicToLinear(int x, int y, int z)
    {
        return
            (z * weightsDimension * weightsDimension)
            + (y * weightsDimension)
            + x;
    }
}
