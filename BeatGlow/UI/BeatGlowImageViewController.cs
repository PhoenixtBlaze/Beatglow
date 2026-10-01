using System;
using System.Collections.Generic;
using System.IO;
using BeatGlow.Gameplay;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using UnityEngine;
using UnityEngine.UI;

namespace BeatGlow.UI
{
    [ViewDefinition("BeatGlow.UI.Views.BeatGlowImageSelect.bsml")]
    public sealed class BeatGlowImageViewController : BSMLAutomaticViewController
    {
#pragma warning disable 0649
        [UIComponent("status-text")]
        private FormattableText _status;

        [UIComponent("mode-text")]
        private FormattableText _modeText;

        [UIComponent("png-0")]
        private Button _png0;

        [UIComponent("png-1")]
        private Button _png1;

        [UIComponent("png-2")]
        private Button _png2;

        [UIComponent("png-3")]
        private Button _png3;

        [UIComponent("png-4")]
        private Button _png4;

        [UIComponent("png-5")]
        private Button _png5;

        [UIComponent("logo-section")]
        private RectTransform _logoSection;

        [UIComponent("text-section")]
        private RectTransform _textSection;
#pragma warning restore 0649

        private const int SlotCount = 6;
        private readonly List<string> _fileNames = new List<string>();

        [UIValue("stand-up")]
        public bool StandUpValue
        {
            get
            {
                return PluginConfig.Instance != null && PluginConfig.Instance.StandUp;
            }
            set
            {
                if (PluginConfig.Instance == null)
                    return;
                PluginConfig.Instance.StandUp = value;
            }
        }

        [UIValue("gamertag")]
        public string GamertagValue
        {
            get
            {
                return PluginConfig.Instance != null ? PluginConfig.Instance.Gamertag ?? "" : "";
            }
            set
            {
                if (PluginConfig.Instance == null)
                    return;
                PluginConfig.Instance.Gamertag = GamertagText.Sanitize(value);
            }
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            Graphics.LogoMaterialFactory.Warm();
            ApplyModeVisibility();
            RefreshImages();
        }

        [UIAction("choose-logo")]
        private void ChooseLogo()
        {
            SetDisplayMode("Logo");
            RefreshImages();
        }

        [UIAction("choose-text")]
        private void ChooseText()
        {
            SetDisplayMode("Gamertag");
        }

        private void SetDisplayMode(string mode)
        {
            if (PluginConfig.Instance != null)
                PluginConfig.Instance.DisplayMode = mode;
            ApplyModeVisibility();
        }

        private void ApplyModeVisibility()
        {
            bool text = PluginConfig.Instance != null && PluginConfig.Instance.UseGamertag();
            if (_logoSection != null)
                _logoSection.gameObject.SetActive(!text);
            if (_textSection != null)
                _textSection.gameObject.SetActive(text);
            if (_modeText != null)
                _modeText.text = text
                    ? "Next song shows the gamertag."
                    : "Next song shows the logo.";
        }

        [UIAction("pick-0")] private void Pick0() { Pick(0); }
        [UIAction("pick-1")] private void Pick1() { Pick(1); }
        [UIAction("pick-2")] private void Pick2() { Pick(2); }
        [UIAction("pick-3")] private void Pick3() { Pick(3); }
        [UIAction("pick-4")] private void Pick4() { Pick(4); }
        [UIAction("pick-5")] private void Pick5() { Pick(5); }

        private void Pick(int index)
        {
            if (LogoPlacementController.SongActive)
            {
                Plugin.Log?.Warn("BeatGlow: image change ignored because a song is running.");
                SetStatus("A song is running. Choose the PNG after you return to the menu.");
                return;
            }

            if (index < 0 || index >= _fileNames.Count || PluginConfig.Instance == null)
                return;

            PluginConfig.Instance.ActiveImage = _fileNames[index];
            Plugin.Log?.Info("BeatGlow: selected " + _fileNames[index]);
            RefreshImages();
        }

        internal void RefreshImages()
        {
            try
            {
                _fileNames.Clear();
                string directory = ImagePaths.GetImageDirectory();
                Directory.CreateDirectory(directory);

                string active = PluginConfig.Instance != null ? Path.GetFileName(PluginConfig.Instance.ActiveImage ?? "") : "";
                string[] files = Directory.GetFiles(directory, "*.png", SearchOption.TopDirectoryOnly);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                bool activeFound = false;
                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileName(files[i]);
                    if (!ImagePaths.IsPngFileName(fileName))
                        continue;
                    if (string.Equals(fileName, active, StringComparison.OrdinalIgnoreCase))
                        activeFound = true;
                    _fileNames.Add(fileName);
                }

                if (!activeFound && _fileNames.Count == 1 && PluginConfig.Instance != null)
                {
                    PluginConfig.Instance.ActiveImage = _fileNames[0];
                    active = _fileNames[0];
                    activeFound = true;
                    Plugin.Log?.Info("BeatGlow: kept the only PNG as the selected logo: " + active);
                }

                Plugin.Log?.Info("BeatGlow: showing " + _fileNames.Count + " PNG button(s) from " + directory + ". Saved logo is '" + active + "'.");
                ShowFileButtons(active);

                if (!string.IsNullOrEmpty(active) && !activeFound)
                {
                    Plugin.Log?.Warn("BeatGlow: saved image '" + active + "' was not found.");
                    SetStatus("That saved PNG is missing. Click one of the files below.");
                }
                else if (_fileNames.Count == 0)
                {
                    SetStatus("No PNGs yet. Put them in UserData/BeatGlow, then open this screen again.");
                }
                else if (_fileNames.Count > SlotCount)
                {
                    SetStatus("Click a PNG. Showing the first " + SlotCount + " of " + _fileNames.Count + ".");
                }
                else if (activeFound)
                {
                    SetStatus("Selected. The next song uses " + active + ".");
                }
                else
                {
                    SetStatus("Click a PNG below. Logo mode uses that image on the next song.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warn("BeatGlow could not refresh the image list: " + ex.Message);
                SetStatus("The image list could not be loaded.");
            }
        }

        private void ShowFileButtons(string active)
        {
            Button[] slots = { _png0, _png1, _png2, _png3, _png4, _png5 };
            int shown = _fileNames.Count < SlotCount ? _fileNames.Count : SlotCount;
            for (int i = 0; i < slots.Length; i++)
            {
                Button button = slots[i];
                if (button == null)
                    continue;

                bool visible = i < shown;
                button.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                string fileName = _fileNames[i];
                bool isActive = string.Equals(fileName, active, StringComparison.OrdinalIgnoreCase);
                BeatSaberUI.SetButtonText(button, isActive ? fileName + "  (selected)" : fileName);
            }
        }

        private void SetStatus(string message)
        {
            if (_status != null)
                _status.text = message;
        }
    }
}
