namespace BeatGlow.Graphics
{
    /// <summary>
    /// Bass, mids, and highs each keep their own color. The loudest band
    /// sets the brightness. Channels stay in 0-1 so the name does not become a light.
    /// </summary>
    internal static class BandColor
    {
        internal static void Mix(float bass, float mid, float high, float dim, out float r, out float g, out float b, out float energy)
        {
            bass = Clamp01(bass);
            mid = Clamp01(mid);
            high = Clamp01(high);
            dim = Clamp01(dim);

            float rr = (0.10f * bass) + (0.05f * mid) + (1.00f * high);
            float gg = (0.20f * bass) + (1.00f * mid) + (0.18f * high);
            float bb = (1.00f * bass) + (0.30f * mid) + (0.12f * high);

            energy = bass;
            if (mid > energy)
                energy = mid;
            if (high > energy)
                energy = high;

            float gain = dim + ((1f - dim) * energy);
            float max = rr;
            if (gg > max)
                max = gg;
            if (bb > max)
                max = bb;

            if (max < 0.0001f)
            {
                r = 0.12f * gain;
                g = 0.12f * gain;
                b = 0.16f * gain;
                return;
            }

            float scale = gain / max;
            r = Clamp01(rr * scale);
            g = Clamp01(gg * scale);
            b = Clamp01(bb * scale);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }
    }
}
