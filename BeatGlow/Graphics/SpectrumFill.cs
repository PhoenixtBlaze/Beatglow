using System;

namespace BeatGlow.Graphics
{
    /// <summary>
    /// Full-spectrum color along the height of a logo or name.
    /// Rows at or below the music level stay bright. Rows above it stay dim,
    /// so the rainbow climbs and falls like an equalizer. Channels stay in 0-1.
    /// </summary>
    internal static class SpectrumFill
    {
        internal static void Rainbow(float height01, float phase, out float r, out float g, out float b)
        {
            float wrapped = height01 + phase;
            wrapped -= (float)Math.Floor(wrapped);
            float hue = wrapped * 6f;
            int sector = (int)hue;
            if (sector < 0)
                sector = 0;
            if (sector > 5)
                sector = 5;
            float fraction = hue - sector;
            switch (sector)
            {
                case 0:
                    r = 1f;
                    g = fraction;
                    b = 0f;
                    break;
                case 1:
                    r = 1f - fraction;
                    g = 1f;
                    b = 0f;
                    break;
                case 2:
                    r = 0f;
                    g = 1f;
                    b = fraction;
                    break;
                case 3:
                    r = 0f;
                    g = 1f - fraction;
                    b = 1f;
                    break;
                case 4:
                    r = fraction;
                    g = 0f;
                    b = 1f;
                    break;
                default:
                    r = 1f;
                    g = 0f;
                    b = 1f - fraction;
                    break;
            }
        }

        internal static void ColorAt(float height01, float level, float phase, float dim, out float r, out float g, out float b)
        {
            if (height01 < 0f)
                height01 = 0f;
            else if (height01 > 1f)
                height01 = 1f;
            if (level < 0f)
                level = 0f;
            else if (level > 1f)
                level = 1f;
            if (dim < 0f)
                dim = 0f;
            else if (dim > 1f)
                dim = 1f;

            Rainbow(height01, phase, out r, out g, out b);
            float edge = (level - height01) / 0.07f;
            if (edge < 0f)
                edge = 0f;
            else if (edge > 1f)
                edge = 1f;
            float gain = dim + ((1f - dim) * edge);
            r *= gain;
            g *= gain;
            b *= gain;
        }
    }
}
