using UnityEngine;

namespace BeatGlow.Graphics
{
    internal static class LogoMeshFactory
    {
        internal const int Rows = 32;

        internal static Mesh CreateArtQuad(float width, float height)
        {
            Mesh mesh = new Mesh();
            mesh.name = "BeatGlow.Art";
            float halfW = width * 0.5f;
            float halfH = height * 0.5f;
            mesh.vertices = new[]
            {
                new Vector3(-halfW, -halfH, 0f),
                new Vector3(halfW, -halfH, 0f),
                new Vector3(-halfW, halfH, 0f),
                new Vector3(halfW, halfH, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// One quad per horizontal row so vertex color can climb the logo.
        /// Vertices are not shared between rows.
        /// </summary>
        internal static Mesh CreateWhiteRows(float width, float height)
        {
            int rows = Rows;
            Vector3[] vertices = new Vector3[rows * 4];
            Vector2[] uv = new Vector2[rows * 4];
            int[] triangles = new int[rows * 6];
            Color32[] colors = new Color32[rows * 4];
            Color32 dim = new Color32(255, 255, 255, 255);
            float halfW = width * 0.5f;
            float halfH = height * 0.5f;

            for (int i = 0; i < rows; i++)
            {
                float v0 = i / (float)rows;
                float v1 = (i + 1) / (float)rows;
                float y0 = -halfH + (v0 * height);
                float y1 = -halfH + (v1 * height);
                int v = i * 4;
                vertices[v] = new Vector3(-halfW, y0, 0f);
                vertices[v + 1] = new Vector3(halfW, y0, 0f);
                vertices[v + 2] = new Vector3(-halfW, y1, 0f);
                vertices[v + 3] = new Vector3(halfW, y1, 0f);
                uv[v] = new Vector2(0f, v0);
                uv[v + 1] = new Vector2(1f, v0);
                uv[v + 2] = new Vector2(0f, v1);
                uv[v + 3] = new Vector2(1f, v1);
                colors[v] = dim;
                colors[v + 1] = dim;
                colors[v + 2] = dim;
                colors[v + 3] = dim;

                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 2;
                triangles[t + 4] = v + 3;
                triangles[t + 5] = v + 1;
            }

            Mesh mesh = new Mesh();
            mesh.name = "BeatGlow.White";
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.colors32 = colors;
            mesh.MarkDynamic();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
