using Hollow.Presentation;

namespace Hollow.Rooms
{
    public static class RoomLightingPrewarm
    {
        public static bool Prepare(RoomRuntimeRoot root, bool applyGlobalSettings = false)
        {
            if (root == null)
            {
                return false;
            }

            var lighting = GetOrAddLightingController(root);
            if (applyGlobalSettings)
            {
                lighting.ApplyBiome(root.BiomeId, force: true);
            }
            else
            {
                lighting.PrepareBiome(root.BiomeId, force: false);
            }

            return lighting.IsPreparedFor(root.BiomeId);
        }

        public static bool ApplyForEntry(RoomRuntimeRoot root)
        {
            if (root == null)
            {
                return false;
            }

            var lighting = GetOrAddLightingController(root);
            lighting.ApplyBiome(root.BiomeId, force: true);
            return lighting.IsPreparedFor(root.BiomeId) && lighting.AppliedProfile != null;
        }

        private static RoomLightingController GetOrAddLightingController(RoomRuntimeRoot root)
        {
            var lighting = root.GetComponent<RoomLightingController>();
            return lighting != null ? lighting : root.gameObject.AddComponent<RoomLightingController>();
        }
    }
}
