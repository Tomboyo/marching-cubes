public class Coordinates
{

    public static int cubicToLinear(int d, int x, int y, int z)
    {
        return
            (z * d * d) + (y * d) + x;
    }
}