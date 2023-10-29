using UnityEngine;

public class Noise
{
    public static float[] generate(int dimension)
    {
        float[] result = new float[dimension * dimension * dimension];
        for (int x = 0; x < dimension; x++)
        {
            for (int y = 0; y < dimension; y++)
            {
                for (int z = 0; z < dimension; z++)
                {
                    float u = x - (dimension / 2f);
                    float v = y - (dimension / 2f);
                    float w = z - (dimension / 2f);
                    result[Coordinates.cubicToLinear(dimension, x, y, z)] =
                        (1f - (Mathf.Sqrt(u * u + v * v + w * w) / dimension)) * 0.9f;
                }
            }
        }

        return result;
    }

    private static Texture3D makeNoise(int d)
    {
        var result = new Texture3D(d, d, d, TextureFormat.RFloat, false);

        var colors = new Color[d * d * d];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = new Color(Random.value, 0f, 0f);
        }
        result.SetPixels(colors);
        result.wrapMode = TextureWrapMode.Repeat;
        result.Apply();

        return result;
    }
}