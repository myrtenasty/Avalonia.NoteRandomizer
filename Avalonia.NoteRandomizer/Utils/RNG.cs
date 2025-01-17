using System;

namespace Avalonia.VisualCalibration3D.Utils;

public static class RNG
{
    private static readonly Random Rng = new Random();

    public static float RandomFloatBetween(float f1, float f2)
    {
        float min = MathF.Min(f1, f2);
        return Rng.NextSingle() * MathF.Abs(f2 - f1) + min;
    }

    public static float RandomFloatAtCenter(float center, float range)
    {
        return center + Rng.NextSingle() * range * 2 - range;
    }
}