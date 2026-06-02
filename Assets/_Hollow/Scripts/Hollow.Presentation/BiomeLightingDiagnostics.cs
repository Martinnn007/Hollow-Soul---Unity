using System.Globalization;

namespace Hollow.Presentation
{
    public readonly struct BiomeLightingSnapshot
    {
        public BiomeLightingSnapshot(
            string biomeId,
            string profileId,
            int activeLightCount,
            int activeLocalLightCount,
            int shadowedLightCount,
            int activePropLightCount,
            int activeDynamicEffectLightCount,
            int maxActiveLocalLights,
            int maxShadowedLocalLights,
            int maxPropLights,
            int maxDynamicEffectLights,
            float gpuFrameP95BudgetMs)
        {
            BiomeId = biomeId ?? string.Empty;
            ProfileId = profileId ?? string.Empty;
            ActiveLightCount = activeLightCount;
            ActiveLocalLightCount = activeLocalLightCount;
            ShadowedLightCount = shadowedLightCount;
            ActivePropLightCount = activePropLightCount;
            ActiveDynamicEffectLightCount = activeDynamicEffectLightCount;
            MaxActiveLocalLights = maxActiveLocalLights;
            MaxShadowedLocalLights = maxShadowedLocalLights;
            MaxPropLights = maxPropLights;
            MaxDynamicEffectLights = maxDynamicEffectLights;
            GpuFrameP95BudgetMs = gpuFrameP95BudgetMs;
        }

        public string BiomeId { get; }

        public string ProfileId { get; }

        public int ActiveLightCount { get; }

        public int ActiveLocalLightCount { get; }

        public int ShadowedLightCount { get; }

        public int ActivePropLightCount { get; }

        public int ActiveDynamicEffectLightCount { get; }

        public int MaxActiveLocalLights { get; }

        public int MaxShadowedLocalLights { get; }

        public int MaxPropLights { get; }

        public int MaxDynamicEffectLights { get; }

        public float GpuFrameP95BudgetMs { get; }

        public bool ExceedsLocalLightBudget => MaxActiveLocalLights > 0 && ActiveLocalLightCount > MaxActiveLocalLights;

        public bool ExceedsShadowBudget => MaxShadowedLocalLights >= 0 && ShadowedLightCount > MaxShadowedLocalLights;
    }

    public static class BiomeLightingDiagnostics
    {
        private static BiomeLightingSnapshot lastSnapshot;

        public static BiomeLightingSnapshot LastSnapshot => lastSnapshot;

        public static void RecordSnapshot(BiomeLightingSnapshot snapshot)
        {
            lastSnapshot = snapshot;
        }

        public static string Describe(BiomeLightingSnapshot snapshot)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Lighting biome={0} profile={1}\nlights={2} local={3}/{4} shadowed={5}/{6}\nprops={7}/{8} effects={9}/{10} budgetP95={11:0.00}ms",
                snapshot.BiomeId,
                snapshot.ProfileId,
                snapshot.ActiveLightCount,
                snapshot.ActiveLocalLightCount,
                snapshot.MaxActiveLocalLights,
                snapshot.ShadowedLightCount,
                snapshot.MaxShadowedLocalLights,
                snapshot.ActivePropLightCount,
                snapshot.MaxPropLights,
                snapshot.ActiveDynamicEffectLightCount,
                snapshot.MaxDynamicEffectLights,
                snapshot.GpuFrameP95BudgetMs);
        }
    }
}
