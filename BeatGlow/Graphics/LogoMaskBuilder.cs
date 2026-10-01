using UnityEngine;

namespace BeatGlow.Graphics
{
    internal sealed class LogoTextures
    {
        internal Texture2D Art;
        internal Texture2D White;
        internal int Width;
        internal int Height;
        internal bool HasWhite;

        internal void DestroyTextures()
        {
            if (Art != null)
            {
                Object.Destroy(Art);
                Art = null;
            }

            if (White != null)
            {
                Object.Destroy(White);
                White = null;
            }
        }
    }

    /// <summary>
    /// Builds the art texture and the white-pixel texture once per song.
    /// White pixels are removed from the art alpha and kept on the white texture,
    /// so a tint only reaches the text. Transparent pixels stay alpha 0 on both.
    /// </summary>
    internal static class LogoMaskBuilder
    {
        private const int MaxSide = 1024;
        private const float WhitePresentThreshold = 0.05f;

        internal static LogoTextures Build(byte[] pngBytes, float lumaMin, float chromaMax)
        {
            if (pngBytes == null || pngBytes.Length == 0)
                return null;

            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!source.LoadImage(pngBytes))
                    return null;

                Color32[] pixels = source.GetPixels32();
                int width = source.width;
                int height = source.height;
                Downsample(ref pixels, ref width, ref height, MaxSide);

                Color32[] art = new Color32[pixels.Length];
                Color32[] white = new Color32[pixels.Length];
                float strongest = 0f;

                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 pixel = pixels[i];
                    float mask = WhiteMaskMath.Mask(pixel.r, pixel.g, pixel.b, lumaMin, chromaMax);
                    if (mask > strongest)
                        strongest = mask;

                    float srcA = pixel.a / 255f;
                    byte artA = ToByte(srcA * (1f - mask));
                    byte whiteA = ToByte(srcA * mask);
                    art[i] = new Color32(pixel.r, pixel.g, pixel.b, artA);
                    white[i] = new Color32(255, 255, 255, whiteA);
                }

                LogoTextures textures = new LogoTextures
                {
                    Width = width,
                    Height = height,
                    HasWhite = strongest >= WhitePresentThreshold,
                    Art = CreateTexture(art, width, height),
                    White = CreateTexture(white, width, height)
                };
                return textures;
            }
            finally
            {
                Object.Destroy(source);
            }
        }

        private static Texture2D CreateTexture(Color32[] pixels, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static byte ToByte(float value)
        {
            if (value < 0f)
                value = 0f;
            else if (value > 1f)
                value = 1f;
            return (byte)(value * 255f + 0.5f);
        }

        private static void Downsample(ref Color32[] pixels, ref int width, ref int height, int maxSide)
        {
            int largest = width > height ? width : height;
            if (largest <= maxSide || width < 1 || height < 1)
                return;

            float scale = maxSide / (float)largest;
            int newWidth = width * scale < 1f ? 1 : (int)(width * scale);
            int newHeight = height * scale < 1f ? 1 : (int)(height * scale);
            Color32[] destination = new Color32[newWidth * newHeight];
            for (int y = 0; y < newHeight; y++)
            {
                int sourceY = y * height / newHeight;
                int sourceRow = sourceY * width;
                int destRow = y * newWidth;
                for (int x = 0; x < newWidth; x++)
                {
                    int sourceX = x * width / newWidth;
                    destination[destRow + x] = pixels[sourceRow + sourceX];
                }
            }

            pixels = destination;
            width = newWidth;
            height = newHeight;
        }
    }
}
