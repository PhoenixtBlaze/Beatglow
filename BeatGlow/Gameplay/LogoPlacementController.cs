using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BeatGlow.Graphics;
using BeatSaberMarkupLanguage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatGlow.Gameplay
{
    /// <summary>
    /// Spawns the active logo on both sides of the note highway when a map starts
    /// and destroys it when the menu returns. A song keeps the image it started with.
    /// </summary>
    internal sealed class LogoPlacementController : MonoBehaviour
    {
        internal static bool SongActive { get; private set; }

        private static LogoPlacementController _instance;

        private int _token;
        private bool _alive = true;
        private LogoTextures _textures;
        private Material _artMaterial;
        private Material _whiteMaterial;
        private Mesh _artMesh;
        private Mesh _whiteMesh;
        private readonly List<RawImage> _whiteRows = new List<RawImage>();
        private float _logoWidth = 1f;
        private float _logoHeight = 1f;
        private TextMeshPro[] _labels = new TextMeshPro[0];
        private float _anchorY = -0.35f;
        private bool _standUp;

        internal static void OnGameSceneLoaded()
        {
            SongActive = true;
            DestroyExisting();

            PluginConfig config = PluginConfig.Instance;
            if (config == null || !config.Enabled)
                return;

            GameObject root = new GameObject("BeatGlow.Logo");
            _instance = root.AddComponent<LogoPlacementController>();
            _instance.Begin();
        }

        internal static void OnMenuSceneLoaded()
        {
            SongActive = false;
            DestroyExisting();
        }

        private static void DestroyExisting()
        {
            if (_instance == null)
                return;
            Object.Destroy(_instance.gameObject);
            _instance = null;
        }

        private void Begin()
        {
            BeatGlowCoroutineHost.Run(PlaceWhenAnchorReady());
        }

        private IEnumerator PlaceWhenAnchorReady()
        {
            Transform parent = null;
            for (int frame = 0; frame < 180 && _alive; frame++)
            {
                parent = RunwayAnchor.Find();
                if (parent != null)
                    break;
                yield return null;
            }

            if (!_alive)
                yield break;

            if (parent == null)
            {
                Plugin.Log?.Warn("BeatGlow: no runway anchor. Nothing will be shown.");
                Destroy(gameObject);
                yield break;
            }

            transform.SetParent(parent, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            PluginConfig config = PluginConfig.Instance;
            _anchorY = config != null ? config.SanitizedHeight() : -0.35f;
            _standUp = config != null && config.StandUp;
            if (config != null && config.UseGamertag())
            {
                string gamertag = GamertagText.Sanitize(config.Gamertag);
                if (gamertag.Length == 0)
                {
                    Plugin.Log?.Warn("BeatGlow: gamertag mode is on, but the name is empty. Nothing will be shown.");
                    Destroy(gameObject);
                    yield break;
                }

                CreateGamertags(gamertag);
                if (_labels.Length == 0)
                {
                    Destroy(gameObject);
                    yield break;
                }

                EnsureDriver();
                BringIntoView(_labels[0].transform, _labels[1].transform);
                Plugin.Log?.Info(
                    "BeatGlow: gamertag '" + gamertag + "' parent=" + parent.name
                    + " world=" + _labels[0].transform.position + " / " + _labels[1].transform.position);
                yield break;
            }

            string path = ImagePaths.ResolveLogoPath(rememberChoice: true);
            bool hasImage = !string.IsNullOrEmpty(path) && File.Exists(path);
            if (!hasImage)
            {
                Plugin.Log?.Warn("BeatGlow: logo mode is on, but no PNG was found in " + ImagePaths.GetImageDirectory());
                Destroy(gameObject);
                yield break;
            }

            Plugin.Log?.Info("BeatGlow: loading logo " + Path.GetFileName(path));

            int token = ++_token;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                byte[] bytes = null;
                string error = null;
                try
                {
                    bytes = File.ReadAllBytes(path);
                }
                catch (System.Exception ex)
                {
                    error = ex.Message;
                }

                BeatGlowCoroutineHost.Enqueue(() =>
                {
                    if (!_alive || token != _token)
                        return;
                    if (error != null || bytes == null || bytes.Length == 0)
                    {
                        Plugin.Log?.Warn("BeatGlow: could not read the logo. " + (error ?? "The file was empty."));
                        AbandonLogo();
                        return;
                    }

                    try
                    {
                        BuildVisuals(bytes);
                    }
                    catch (System.Exception ex)
                    {
                        Plugin.Log?.Warn("BeatGlow: logo build failed. The song will continue. " + ex.Message);
                        ClearVisuals();
                        AbandonLogo();
                    }
                });
            });
        }

        private void BuildVisuals(byte[] pngBytes)
        {
            PluginConfig config = PluginConfig.Instance;
            float luma = config != null ? config.SanitizedLumaMin() : 0.72f;
            float chroma = config != null ? config.SanitizedChromaMax() : 0.18f;
            LogoTextures textures = LogoMaskBuilder.Build(pngBytes, luma, chroma);
            if (textures == null)
            {
                Plugin.Log?.Warn("BeatGlow: the PNG could not be decoded. The song will continue without a logo.");
                AbandonLogo();
                return;
            }

            if (!textures.HasWhite)
                Plugin.Log?.Warn("BeatGlow: the PNG has no white pixels, so the picture stays as drawn.");

            _textures = textures;
            _whiteRows.Clear();
            float scale = config != null ? config.SanitizedScale() : 1.2f;
            float height = scale;
            float width = textures.Height > 0 ? scale * (textures.Width / (float)textures.Height) : scale;
            const float maxLogoWidth = 3.2f;
            if (width > maxLogoWidth && width > 0.001f)
            {
                float shrink = maxLogoWidth / width;
                width = maxLogoWidth;
                height *= shrink;
            }

            _logoWidth = width;
            _logoHeight = height;

            float clearance = config != null ? config.SanitizedLateral() : 2.6f;
            float bottom = config != null ? config.SanitizedHeight() : -0.35f;
            float z = config != null ? config.SanitizedDistance() : 2f;
            float center = clearance + (width * 0.5f);
            // Flat art sits on the runway. A negative height would bury the plane under the floor.
            float planeY = _standUp ? bottom + (height * 0.5f) : 0.04f;
            CreateSide("Left", -center, planeY, z);
            CreateSide("Right", center, planeY, z);

            Transform leftSide = transform.Find("Left");
            Transform rightSide = transform.Find("Right");
            BringIntoView(leftSide, rightSide);
            EnsureDriver();
            string leftPos = leftSide != null ? leftSide.position.ToString() : "?";
            string rightPos = rightSide != null ? rightSide.position.ToString() : "?";
            Plugin.Log?.Info("BeatGlow: logo canvas placed world=" + leftPos + " / " + rightPos + " rows=" + _whiteRows.Count);
        }

        private void AbandonLogo()
        {
            if (_labels.Length == 0)
                Destroy(gameObject);
        }

        private void EnsureDriver()
        {
            MusicColorDriver driver = GetComponent<MusicColorDriver>();
            if (driver == null)
                driver = gameObject.AddComponent<MusicColorDriver>();
            driver.Bind(_whiteMesh, _whiteMaterial, _labels, _whiteRows.ToArray());
        }

        private void CreateGamertags(string gamertag)
        {
            TMP_FontAsset font = BeatSaberUI.MainTextFont;
            if (font == null)
            {
                Plugin.Log?.Warn("BeatGlow: text font was not found. Gamertag not shown.");
                return;
            }

            PluginConfig config = PluginConfig.Instance;
            float clearance = config != null ? config.SanitizedLateral() : 2.6f;
            float z = config != null ? config.SanitizedDistance() : 2f;
            float y = _anchorY;
            TextMeshPro left = CreateLabel("GamertagLeft", gamertag, font, -clearance, y, z);
            TextMeshPro right = CreateLabel("GamertagRight", gamertag, font, clearance, y, z);
            _labels = new[] { left, right };
        }

        private TextMeshPro CreateLabel(string name, string gamertag, TMP_FontAsset font, float x, float y, float z)
        {
            GameObject side = new GameObject(name);
            side.transform.SetParent(transform, false);

            TextMeshPro label = side.AddComponent<TextMeshPro>();
            label.font = font;
            label.text = gamertag;
            label.richText = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 10f;
            label.color = Color.white;
            label.rectTransform.sizeDelta = new Vector2(20f, 4f);
            label.ForceMeshUpdate(true);

            PluginConfig config = PluginConfig.Instance;
            float targetHeight = config != null ? config.SanitizedScale() : 1.2f;
            float meshHeight = label.textBounds.size.y;
            float meshWidth = label.textBounds.size.x;
            float fitted = 1f;
            if (meshHeight > 0.001f)
                fitted = targetHeight / meshHeight;
            const float maxNameWidth = 3.2f;
            if (meshWidth > 0.001f)
            {
                float widthFitted = maxNameWidth / meshWidth;
                if (widthFitted < fitted)
                    fitted = widthFitted;
            }

            float halfWidth = meshWidth * 0.5f * fitted;
            float center = System.Math.Abs(x) + halfWidth;
            if (x < 0f)
                center = -center;
            float across = center - (label.textBounds.center.x * fitted);
            float along = z;
            float planeY = y;
            if (_standUp)
                planeY = y - (label.textBounds.min.y * fitted);
            else
                along = z - (label.textBounds.center.y * fitted);
            Vector3 localPos = new Vector3(across, planeY, along);
            side.transform.localPosition = localPos;
            side.transform.localScale = new Vector3(fitted, fitted, fitted);
            Orient(side.transform, localPos);

            if (label.fontMaterial != null)
            {
                label.fontMaterial.EnableKeyword("GLOW_ON");
                if (label.fontMaterial.HasProperty("_CullMode"))
                    label.fontMaterial.SetFloat("_CullMode", 0f);
            }

            if (label.renderer != null)
                LogoMaterialFactory.ConfigureRenderer(label.renderer);
            return label;
        }

        /// <summary>
        /// The mesh sits in local XY and is drawn from local -Z.
        /// Flat lays that face up on the runway. Stand-up turns the face toward the player.
        /// </summary>
        private void Orient(Transform side, Vector3 localPos)
        {
            if (_standUp)
            {
                Vector3 outward = new Vector3(localPos.x, 0f, localPos.z);
                if (outward.sqrMagnitude < 0.0001f)
                    outward = Vector3.forward;
                side.localRotation = Quaternion.LookRotation(outward.normalized, Vector3.up);
                return;
            }

            side.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        }

        /// <summary>
        /// A world canvas is drawn from local +Z. Flat points that face up. Stand-up points it at the player.
        /// The texture is flipped in U so the picture is not mirrored.
        /// </summary>
        private void OrientLogo(Transform side, Vector3 localPos)
        {
            if (_standUp)
            {
                Vector3 inward = new Vector3(-localPos.x, 0f, -localPos.z);
                if (inward.sqrMagnitude < 0.0001f)
                    inward = -Vector3.forward;
                side.localRotation = Quaternion.LookRotation(inward.normalized, Vector3.up);
                return;
            }

            side.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
        }

        /// <summary>
        /// If the highway's local +Z points back at the player, the copies sit behind the camera.
        /// Flip them onto the side the player is looking at.
        /// </summary>
        private void BringIntoView(Transform left, Transform right)
        {
            Camera camera = Camera.main;
            if (camera == null || left == null || right == null)
                return;

            Vector3 mid = (left.position + right.position) * 0.5f;
            float ahead = Vector3.Dot(mid - camera.transform.position, camera.transform.forward);
            if (ahead > -0.5f)
                return;

            FlipForward(left);
            FlipForward(right);
            Plugin.Log?.Info("BeatGlow: the first placement was behind the camera, so both sides were moved forward.");
        }

        private void FlipForward(Transform side)
        {
            Vector3 localPos = side.localPosition;
            localPos.z = -localPos.z;
            side.localPosition = localPos;
            if (side.GetComponent<TextMeshPro>() != null)
                Orient(side, localPos);
            else
                OrientLogo(side, localPos);
        }

        private void CreateSide(string name, float x, float y, float z)
        {
            GameObject side = new GameObject(name);
            side.transform.SetParent(transform, false);
            Vector3 localPos = new Vector3(x, y, z);
            side.transform.localPosition = localPos;
            side.transform.localScale = Vector3.one;
            OrientLogo(side.transform, localPos);

            GameObject canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(side.transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(_logoWidth, _logoHeight);
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.localPosition = Vector3.zero;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one;

            if (_textures != null && _textures.Art != null)
                CreateFill(canvasRect, "Art", _textures.Art);

            if (_textures != null && _textures.HasWhite && _textures.White != null)
                CreateWhiteRows(canvasRect, _textures.White);
        }

        private void CreateWhiteRows(RectTransform parent, Texture texture)
        {
            int rows = LogoMeshFactory.Rows;
            for (int i = 0; i < rows; i++)
            {
                float v0 = i / (float)rows;
                float v1 = (i + 1f) / rows;
                RawImage image = CreateFill(parent, "White" + i, texture);
                image.uvRect = new Rect(1f, v0, -1f, v1 - v0);
                RectTransform rect = image.rectTransform;
                rect.anchorMin = new Vector2(0f, v0);
                rect.anchorMax = new Vector2(1f, v1);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                _whiteRows.Add(image);
            }
        }

        private static RawImage CreateFill(Transform parent, string name, Texture texture)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            RawImage image = child.AddComponent<RawImage>();
            image.texture = texture;
            image.uvRect = new Rect(1f, 0f, -1f, 1f);
            image.color = Color.white;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return image;
        }

        private void OnDestroy()
        {
            _alive = false;
            _token++;
            MusicColorDriver driver = GetComponent<MusicColorDriver>();
            if (driver != null)
                driver.enabled = false;
            if (_instance == this)
                _instance = null;
            ClearVisuals();
        }

        private void ClearVisuals()
        {
            if (_textures != null)
            {
                _textures.DestroyTextures();
                _textures = null;
            }

            DestroyOwned(_artMaterial);
            DestroyOwned(_whiteMaterial);
            DestroyOwned(_artMesh);
            DestroyOwned(_whiteMesh);
            _artMaterial = null;
            _whiteMaterial = null;
            _artMesh = null;
            _whiteMesh = null;
            _whiteRows.Clear();
            MusicColorDriver driver = GetComponent<MusicColorDriver>();
            if (driver != null)
                driver.ClearLogo();
        }

        private static void DestroyOwned(Object obj)
        {
            if (obj != null)
                Object.Destroy(obj);
        }
    }
}
