using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeatGlow.Gameplay
{
    /// <summary>
    /// Parents logos to the note-highway transform so 360 and 90 degree maps
    /// carry them with the runway. Never searches for a mesh named Runway and
    /// never parents under environment light strips.
    /// </summary>
    internal static class RunwayAnchor
    {
        private static readonly FieldInfo OriginParentField = typeof(PlayerTransforms).GetField(
            "_originParentTransform",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo OriginField = typeof(PlayerTransforms).GetField(
            "_originTransform",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo UseParentField = typeof(PlayerTransforms).GetField(
            "_useOriginParentTransformForPseudoLocalCalculations",
            BindingFlags.Instance | BindingFlags.NonPublic);

        internal static Transform Find()
        {
            BeatmapObjectSpawnController[] spawners = Resources.FindObjectsOfTypeAll<BeatmapObjectSpawnController>();
            for (int i = 0; i < spawners.Length; i++)
            {
                BeatmapObjectSpawnController spawner = spawners[i];
                if (IsLive(spawner))
                    return spawner.transform;
            }

            Plugin.Log?.Warn("BeatGlow: note highway was not found. Falling back to the player origin.");
            return FindPlayerOrigin();
        }

        private static Transform FindPlayerOrigin()
        {
            PlayerTransforms[] players = Resources.FindObjectsOfTypeAll<PlayerTransforms>();
            for (int i = 0; i < players.Length; i++)
            {
                PlayerTransforms player = players[i];
                if (!IsLive(player))
                    continue;

                bool useParent = UseParentField != null && UseParentField.GetValue(player) is bool flag && flag;
                if (useParent && OriginParentField != null && OriginParentField.GetValue(player) is Transform parent && parent != null)
                    return parent;
                if (OriginField != null && OriginField.GetValue(player) is Transform origin && origin != null)
                    return origin;
            }

            return null;
        }

        private static bool IsLive(Component component)
        {
            if (component == null || !component.gameObject.activeInHierarchy)
                return false;
            Scene scene = component.gameObject.scene;
            return scene.IsValid() && scene.isLoaded;
        }
    }
}
