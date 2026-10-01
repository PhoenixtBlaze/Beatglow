namespace BeatGlow.Graphics
{
    /// <summary>
    /// A pixel is white when it is bright and has almost no chroma.
    /// Alpha is not part of the mask. The caller multiplies by source alpha.
    /// </summary>
    internal static class WhiteMaskMath
    {
        internal static float Mask(byte r, byte g, byte b, float lumaMin, float chromaMax)
        {
            float rf = r / 255f;
            float gf = g / 255f;
            float bf = b / 255f;
            float max = rf > gf ? (rf > bf ? rf : bf) : (gf > bf ? gf : bf);
            float min = rf < gf ? (rf < bf ? rf : bf) : (gf < bf ? gf : bf);
            float chroma = max - min;
            float luma = (rf + gf + bf) / 3f;

            float lumaRange = 1f - lumaMin;
            if (lumaRange < 0.0001f)
                lumaRange = 0.0001f;
            float lumaMask = (luma - lumaMin) / lumaRange;
            if (lumaMask < 0f)
                lumaMask = 0f;
            else if (lumaMask > 1f)
                lumaMask = 1f;

            if (chromaMax < 0.0001f)
                chromaMax = 0.0001f;
            float chromaAllow = 1f - (chroma / chromaMax);
            if (chromaAllow < 0f)
                chromaAllow = 0f;
            else if (chromaAllow > 1f)
                chromaAllow = 1f;

            return lumaMask * chromaAllow;
        }
    }
}
