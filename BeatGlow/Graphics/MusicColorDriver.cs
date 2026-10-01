using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatGlow.Graphics
{
    /// <summary>
    /// Reads the song spectrum only. Does not subscribe to beatmap light events.
    /// Both runway copies share one white mesh, so one color write moves both.
    /// </summary>
    internal sealed class MusicColorDriver : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int GlowPowerId = Shader.PropertyToID("_GlowPower");
        private static readonly int GlowOuterId = Shader.PropertyToID("_GlowOuter");
        private static readonly int GlowInnerId = Shader.PropertyToID("_GlowInner");
        private static readonly int GlowOffsetId = Shader.PropertyToID("_GlowOffset");

        private readonly float[] _spectrum = new float[256];
        private readonly Color32[] _rowColors = new Color32[LogoMeshFactory.Rows * 4];

        private AudioSource _audio;
        private Mesh _whiteMesh;
        private Material _whiteMaterial;
        private RawImage[] _rows = new RawImage[0];
        private TextMeshPro[] _labels = new TextMeshPro[0];
        private float _bass;
        private float _mid;
        private float _high;
        private float _level;
        private float _phase;
        private float _bassPeak = 0.0001f;
        private float _midPeak = 0.0001f;
        private float _highPeak = 0.0001f;
        private int _audioSearchFrames;
        private bool _spectrumFailed;
        private bool _loggedSpectrum;

        internal void ClearLogo()
        {
            _whiteMesh = null;
            _whiteMaterial = null;
            _rows = new RawImage[0];
        }

        internal void Bind(Mesh whiteMesh, Material whiteMaterial, TextMeshPro[] labels, RawImage[] rows)
        {
            if (whiteMesh != null)
                _whiteMesh = whiteMesh;
            if (whiteMaterial != null)
                _whiteMaterial = whiteMaterial;
            if (labels != null)
                _labels = labels;
            if (rows != null)
                _rows = rows;
            ApplySpectrum();
        }

        private void Update()
        {
            if (_whiteMesh == null && _labels.Length == 0 && _rows.Length == 0)
                return;

            PluginConfig config = PluginConfig.Instance;
            float dt = Time.deltaTime;
            float attack = config != null ? config.SanitizedAttack() : 48f;
            float release = config != null ? config.SanitizedRelease() : 14f;
            SampleBands(dt, attack, release);
            ApplySpectrum();
        }

        private void SampleBands(float dt, float attack, float release)
        {
            if (_spectrumFailed || !TryReadSpectrum())
            {
                _bass = EqualizerLevel.Advance(_bass, 0f, attack, release, dt);
                _mid = EqualizerLevel.Advance(_mid, 0f, attack, release, dt);
                _high = EqualizerLevel.Advance(_high, 0f, attack, release, dt);
                _level = EqualizerLevel.Advance(_level, 0f, attack, release, dt);
                AdvancePhase(dt);
                return;
            }

            float bassRaw = Average(1, 8);
            float midRaw = Average(8, 40);
            float highRaw = Average(40, 140);
            _bassPeak = FollowPeak(_bassPeak, bassRaw, dt);
            _midPeak = FollowPeak(_midPeak, midRaw, dt);
            _highPeak = FollowPeak(_highPeak, highRaw, dt);
            _bass = EqualizerLevel.Advance(_bass, Shape(bassRaw, _bassPeak), attack, release, dt);
            _mid = EqualizerLevel.Advance(_mid, Shape(midRaw, _midPeak), attack, release, dt);
            _high = EqualizerLevel.Advance(_high, Shape(highRaw, _highPeak), attack, release, dt);
            float loudest = _bass;
            if (_mid > loudest)
                loudest = _mid;
            if (_high > loudest)
                loudest = _high;
            float blended = (_bass * 0.50f) + (_mid * 0.30f) + (_high * 0.20f);
            _level = blended > loudest ? blended : loudest;
            AdvancePhase(dt);
        }

        private void AdvancePhase(float dt)
        {
            if (dt < 0f)
                dt = 0f;
            _phase += dt * (0.08f + (_level * 0.85f));
            while (_phase >= 1f)
                _phase -= 1f;
        }

        private bool TryReadSpectrum()
        {
            if (_audio == null)
            {
                _audioSearchFrames++;
                if (_audioSearchFrames > 600)
                {
                    _spectrumFailed = true;
                    if (!_loggedSpectrum)
                    {
                        _loggedSpectrum = true;
                        Plugin.Log?.Warn("BeatGlow: song audio source was not found. Logo stays dim.");
                    }
                    return false;
                }

                if ((_audioSearchFrames % 10) != 0)
                    return false;

                _audio = FindSongAudio();
                if (_audio == null)
                    return false;
            }

            if (!_audio.isPlaying)
                return false;

            try
            {
                _audio.GetSpectrumData(_spectrum, 0, FFTWindow.Hamming);
            }
            catch (System.Exception ex)
            {
                _spectrumFailed = true;
                Plugin.Log?.Warn("BeatGlow: spectrum read failed: " + ex.Message);
                return false;
            }

            return true;
        }

        private static float FollowPeak(float peak, float raw, float dt)
        {
            if (raw > peak)
                return raw;
            float decayed = peak * Mathf.Exp(-3.2f * (dt < 0f ? 0f : dt));
            if (decayed < raw)
                decayed = raw;
            if (decayed < 0.0001f)
                decayed = 0.0001f;
            return decayed;
        }

        private static float Shape(float raw, float peak)
        {
            if (peak < 0.00001f)
                return 0f;
            float normalized = raw / peak;
            if (normalized < 0f)
                normalized = 0f;
            else if (normalized > 1f)
                normalized = 1f;
            return Mathf.Pow(normalized, 0.4f);
        }

        private float Average(int start, int end)
        {
            if (end > _spectrum.Length)
                end = _spectrum.Length;
            if (start < 0)
                start = 0;
            if (end <= start)
                return 0f;

            float sum = 0f;
            for (int i = start; i < end; i++)
                sum += _spectrum[i];
            return sum / (end - start);
        }

        private void ApplySpectrum()
        {
            PluginConfig config = PluginConfig.Instance;
            float cap = config != null ? config.SanitizedBrightness() : 1f;
            float dimAmount = config != null ? config.SanitizedDim() : 0.12f;

            if (_whiteMaterial != null)
                _whiteMaterial.SetColor(ColorId, Color.white);

            float edgeR;
            float edgeG;
            float edgeB;
            SpectrumFill.Rainbow(_level, _phase, out edgeR, out edgeG, out edgeB);
            Color edge = new Color(Clamp01(edgeR * cap), Clamp01(edgeG * cap), Clamp01(edgeB * cap), 1f);
            float glow = 0.1f + (0.45f * _level);

            for (int labelIndex = 0; labelIndex < _labels.Length; labelIndex++)
            {
                TextMeshPro label = _labels[labelIndex];
                if (label == null)
                    continue;
                PaintLabel(label, dimAmount, cap);
                Material material = label.fontMaterial;
                if (material == null)
                    continue;
                if (material.HasProperty(GlowColorId))
                    material.SetColor(GlowColorId, edge);
                if (material.HasProperty(GlowPowerId))
                    material.SetFloat(GlowPowerId, glow);
                if (material.HasProperty(GlowOuterId))
                    material.SetFloat(GlowOuterId, 0.05f + (0.28f * _level));
                if (material.HasProperty(GlowInnerId))
                    material.SetFloat(GlowInnerId, 0.02f);
                if (material.HasProperty(GlowOffsetId))
                    material.SetFloat(GlowOffsetId, 0f);
            }

            int rows = LogoMeshFactory.Rows;
            bool paintMesh = _whiteMesh != null;
            bool paintImages = _rows.Length > 0;
            if (!paintMesh && !paintImages)
                return;

            for (int i = 0; i < rows; i++)
            {
                float height01 = (i + 0.5f) / rows;
                float red;
                float green;
                float blue;
                SpectrumFill.ColorAt(height01, _level, _phase, dimAmount, out red, out green, out blue);
                Color32 packed = new Color32(ToByte(red * cap), ToByte(green * cap), ToByte(blue * cap), 255);
                int offset = i * 4;
                _rowColors[offset] = packed;
                _rowColors[offset + 1] = packed;
                _rowColors[offset + 2] = packed;
                _rowColors[offset + 3] = packed;
            }

            if (paintMesh)
                _whiteMesh.SetColors(_rowColors);

            for (int imageIndex = 0; imageIndex < _rows.Length; imageIndex++)
            {
                RawImage image = _rows[imageIndex];
                if (image == null)
                    continue;
                image.color = _rowColors[(imageIndex % rows) * 4];
            }
        }

        private void PaintLabel(TextMeshPro label, float dim, float cap)
        {
            TMP_TextInfo info = label.textInfo;
            if (info == null || info.characterCount == 0 || info.meshInfo == null)
                return;

            float minY = label.textBounds.min.y;
            float span = label.textBounds.size.y;
            if (span < 0.0001f)
                span = 1f;

            for (int c = 0; c < info.characterCount; c++)
            {
                TMP_CharacterInfo character = info.characterInfo[c];
                if (!character.isVisible)
                    continue;
                int materialIndex = character.materialReferenceIndex;
                if (materialIndex < 0 || materialIndex >= info.meshInfo.Length)
                    continue;

                TMP_MeshInfo meshInfo = info.meshInfo[materialIndex];
                Vector3[] vertices = meshInfo.vertices;
                Color32[] colors = meshInfo.colors32;
                int vertexIndex = character.vertexIndex;
                if (vertices == null || colors == null || vertexIndex < 0 || vertexIndex + 3 >= colors.Length)
                    continue;

                for (int corner = 0; corner < 4; corner++)
                {
                    float height01 = (vertices[vertexIndex + corner].y - minY) / span;
                    float red;
                    float green;
                    float blue;
                    SpectrumFill.ColorAt(height01, _level, _phase, dim, out red, out green, out blue);
                    colors[vertexIndex + corner] = new Color32(ToByte(red * cap), ToByte(green * cap), ToByte(blue * cap), 255);
                }
            }

            label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        private static byte ToByte(float value)
        {
            if (value < 0f)
                value = 0f;
            else if (value > 1f)
                value = 1f;
            return (byte)((value * 255f) + 0.5f);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        private static AudioSource FindSongAudio()
        {
            AudioTimeSyncController[] controllers = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>();
            for (int i = 0; i < controllers.Length; i++)
            {
                AudioTimeSyncController controller = controllers[i];
                if (controller == null || !controller.gameObject.scene.IsValid())
                    continue;

                AudioSource source = AudioSourceField.Get(controller);
                if (source != null)
                    return source;
            }

            return null;
        }

        private static class AudioSourceField
        {
            private static readonly System.Reflection.FieldInfo Field =
                typeof(AudioTimeSyncController).GetField("_audioSource", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            internal static AudioSource Get(AudioTimeSyncController controller)
            {
                if (Field == null || controller == null)
                    return null;
                return Field.GetValue(controller) as AudioSource;
            }
        }
    }
}
