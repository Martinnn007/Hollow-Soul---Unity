using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEngine;

namespace Hollow.Rooms
{
    public sealed class RoomLightingRuntimeLoop : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 0.25f;
        private float nextRefreshTime;
        private RoomRuntimeRoot cachedActiveRoot;

        public static bool DebugHudEnabled { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (FindAnyObjectByType<RoomLightingRuntimeLoop>() != null)
            {
                return;
            }

            var runtimeObject = new GameObject("RoomLightingRuntimeLoop");
            DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<RoomLightingRuntimeLoop>();
        }

        private void Update()
        {
            if (!DebugHudEnabled && ActiveRoomLightingIsCurrent())
            {
                return;
            }

            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
            ApplyActiveRoomLighting();
        }

        public void ApplyActiveRoomLighting()
        {
            var roots = Object.FindObjectsByType<RoomRuntimeRoot>(FindObjectsInactive.Exclude);
            for (var index = 0; index < roots.Length; index++)
            {
                var root = roots[index];
                if (root == null || !root.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var lighting = root.GetComponent<RoomLightingController>();
                if (lighting == null)
                {
                    lighting = root.gameObject.AddComponent<RoomLightingController>();
                }

                lighting.ApplyBiome(root.BiomeId);
                cachedActiveRoot = root;
                break;
            }
        }

        private bool ActiveRoomLightingIsCurrent()
        {
            if (cachedActiveRoot == null || !cachedActiveRoot.gameObject.activeInHierarchy)
            {
                cachedActiveRoot = null;
                return false;
            }

            var lighting = cachedActiveRoot.GetComponent<RoomLightingController>();
            if (lighting == null ||
                lighting.AppliedProfile == null ||
                !lighting.IsPreparedFor(cachedActiveRoot.BiomeId) ||
                !RoomBiomeIds.Matches(lighting.AppliedBiomeId, cachedActiveRoot.BiomeId))
            {
                return false;
            }

            return true;
        }

        private void OnGUI()
        {
            if (!DebugHudEnabled)
            {
                return;
            }

            var snapshot = BiomeLightingDiagnostics.LastSnapshot;
            GUI.Label(new Rect(12f, 12f, 720f, 120f), BiomeLightingDiagnostics.Describe(snapshot));
        }
    }
}
