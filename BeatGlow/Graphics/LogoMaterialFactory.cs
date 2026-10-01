using UnityEngine;
using UnityEngine.Rendering;

namespace BeatGlow.Graphics
{
    /// <summary>
    /// Uses the same unlit material Beat Saber's menu images use.
    /// Shader.Find("Custom/UINoGlow") fails in gameplay because that shader is not
    /// in the always-included list. The live menu image material still has it.
    /// </summary>
    internal static class LogoMaterialFactory
    {
        private static Shader _shader;
        private static Material _template;

        internal static void Warm()
        {
            if (_shader != null)
                return;
            FindShader();
        }

        internal static Material Create(Texture texture)
        {
            Shader shader = FindShader();
            if (shader == null)
            {
                Plugin.Log?.Warn("BeatGlow: no unlit logo shader was loaded. Logo not shown.");
                return null;
            }

            Material material = _template != null ? new Material(_template) : new Material(shader);
            material.name = "BeatGlow.Logo";
            material.mainTexture = texture;
            material.color = Color.white;
            material.renderQueue = 3000;
            if (material.HasProperty("_Cull"))
                material.SetInt("_Cull", (int)CullMode.Off);
            if (material.HasProperty("_ZWrite"))
                material.SetInt("_ZWrite", 0);
            return material;
        }

        internal static void ConfigureRenderer(Renderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        private static Shader FindShader()
        {
            if (_shader != null)
                return _shader;

            Material menuMaterial = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
            if (menuMaterial != null && menuMaterial.shader != null)
            {
                _template = menuMaterial;
                _shader = menuMaterial.shader;
                Plugin.Log?.Info("BeatGlow: logo shader is " + _shader.name);
                return _shader;
            }

            Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null || material.shader == null)
                    continue;
                string name = material.shader.name;
                if (name != "Custom/UINoGlow" && !name.EndsWith("/UINoGlow"))
                    continue;
                _template = material;
                _shader = material.shader;
                Plugin.Log?.Info("BeatGlow: logo shader is " + _shader.name);
                return _shader;
            }

            _shader = Shader.Find("Custom/UINoGlow");
            if (_shader != null)
                Plugin.Log?.Info("BeatGlow: logo shader is " + _shader.name);
            return _shader;
        }
    }
}
