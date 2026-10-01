using System;
using System.IO;
using IPA.Utilities;

namespace BeatGlow
{
    /// <summary>
    /// Resolves logo files under UserData only. The menu never browses the rest of the disk.
    /// </summary>
    internal static class ImagePaths
    {
        internal static string GetImageDirectory()
        {
            string folder = "BeatGlow";
            PluginConfig config = PluginConfig.Instance;
            if (config != null && !string.IsNullOrWhiteSpace(config.ImageFolder))
                folder = config.ImageFolder.Trim();

            folder = folder.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(folder) || folder.Contains(".."))
            {
                Plugin.Log?.Warn("BeatGlow image folder must stay under UserData. Using BeatGlow.");
                folder = "BeatGlow";
            }

            string root = Path.GetFullPath(UnityGame.UserDataPath);
            string path = Path.GetFullPath(Path.Combine(root, folder));
            if (!IsUnder(root, path))
            {
                Plugin.Log?.Warn("BeatGlow rejected an image folder outside UserData.");
                path = Path.Combine(root, "BeatGlow");
            }

            return path;
        }

        internal static string GetActiveImagePath()
        {
            PluginConfig config = PluginConfig.Instance;
            if (config == null || string.IsNullOrWhiteSpace(config.ActiveImage))
                return null;

            string fileName = Path.GetFileName(config.ActiveImage);
            if (!IsPngFileName(fileName))
                return null;

            string path = Path.GetFullPath(Path.Combine(GetImageDirectory(), fileName));
            if (!IsUnder(GetImageDirectory(), path))
                return null;
            return path;
        }

        /// <summary>
        /// The chosen PNG if it is still in the folder. If none is chosen, the first PNG in the folder.
        /// </summary>
        internal static string ResolveLogoPath(bool rememberChoice)
        {
            string configured = GetActiveImagePath();
            if (!string.IsNullOrEmpty(configured) && File.Exists(configured))
                return configured;

            string directory = GetImageDirectory();
            if (!Directory.Exists(directory))
                return null;

            string[] files = Directory.GetFiles(directory, "*.png", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            int pngCount = 0;
            string onlyName = null;
            string onlyPath = null;
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                if (!IsPngFileName(fileName))
                    continue;

                pngCount++;
                if (pngCount == 1)
                {
                    onlyName = fileName;
                    onlyPath = files[i];
                }
            }

            if (pngCount == 0)
                return null;

            bool savedMissing = !string.IsNullOrEmpty(configured) && !File.Exists(configured);
            if (rememberChoice && PluginConfig.Instance != null)
            {
                bool empty = string.IsNullOrWhiteSpace(PluginConfig.Instance.ActiveImage);
                if (empty || (savedMissing && pngCount == 1))
                {
                    PluginConfig.Instance.ActiveImage = onlyName;
                    Plugin.Log?.Info("BeatGlow: saved the logo choice " + onlyName);
                }
            }

            return onlyPath;
        }

        internal static bool IsPngFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;
            if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return false;
            return fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        private static bool IsUnder(string root, string path)
        {
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullPath = Path.GetFullPath(path);
            if (string.Equals(fullRoot, fullPath, StringComparison.OrdinalIgnoreCase))
                return true;
            return fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }
}
