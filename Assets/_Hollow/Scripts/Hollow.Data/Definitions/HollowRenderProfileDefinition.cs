using UnityEngine;
using UnityEngine.Rendering;

namespace Hollow.Data.Definitions
{
    public enum HollowRenderProfileKind
    {
        DevCool = 0,
        WindowsQuality = 1,
        VisionOSBounded = 2,
        VisionOSImmersive = 3
    }

    [CreateAssetMenu(menuName = "Hollow/Rendering/Render Profile", fileName = "HollowRenderProfile")]
    public sealed class HollowRenderProfileDefinition : HollowDefinition
    {
        [SerializeField] private HollowRenderProfileKind profileKind = HollowRenderProfileKind.WindowsQuality;
        [SerializeField] private RenderPipelineAsset renderPipelineAsset;
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private int vSyncCount;
        [SerializeField] private float renderScale = 1f;
        [SerializeField] private bool supportsHdr = true;
        [SerializeField] private bool requiresDepthTexture = true;
        [SerializeField] private bool requiresOpaqueTexture = true;
        [SerializeField] private int mainLightShadowResolution = 2048;
        [SerializeField] private float shadowDistance = 50f;
        [SerializeField] private int shadowCascadeCount = 2;
        [SerializeField] private bool additionalLightShadows;
        [SerializeField] private int maxAdditionalLights = 2;
        [SerializeField] private bool screenSpaceAmbientOcclusion;
        [SerializeField] private int maxActiveParticleSystems = 64;
        [SerializeField] private int maxActiveVfx = 48;
        [SerializeField] private int maxActiveLights = 12;
        [SerializeField] private int worldTextureMaxSize = 2048;
        [SerializeField] private int uiSpriteMaxSize = 1024;
        [SerializeField] private float gpuFrameP95BudgetMs = 16.67f;

        public HollowRenderProfileKind ProfileKind => profileKind;

        public RenderPipelineAsset RenderPipelineAsset => renderPipelineAsset;

        public int TargetFrameRate => targetFrameRate;

        public int VSyncCount => vSyncCount;

        public float RenderScale => renderScale;

        public bool SupportsHdr => supportsHdr;

        public bool RequiresDepthTexture => requiresDepthTexture;

        public bool RequiresOpaqueTexture => requiresOpaqueTexture;

        public int MainLightShadowResolution => mainLightShadowResolution;

        public float ShadowDistance => shadowDistance;

        public int ShadowCascadeCount => shadowCascadeCount;

        public bool AdditionalLightShadows => additionalLightShadows;

        public int MaxAdditionalLights => maxAdditionalLights;

        public bool ScreenSpaceAmbientOcclusion => screenSpaceAmbientOcclusion;

        public int MaxActiveParticleSystems => maxActiveParticleSystems;

        public int MaxActiveVfx => maxActiveVfx;

        public int MaxActiveLights => maxActiveLights;

        public int WorldTextureMaxSize => worldTextureMaxSize;

        public int UiSpriteMaxSize => uiSpriteMaxSize;

        public float GpuFrameP95BudgetMs => gpuFrameP95BudgetMs;

        public void Configure(
            HollowRenderProfileKind nextProfileKind,
            RenderPipelineAsset nextRenderPipelineAsset,
            int nextTargetFrameRate,
            int nextVSyncCount,
            float nextRenderScale,
            bool nextSupportsHdr,
            bool nextRequiresDepthTexture,
            bool nextRequiresOpaqueTexture,
            int nextMainLightShadowResolution,
            float nextShadowDistance,
            int nextShadowCascadeCount,
            bool nextAdditionalLightShadows,
            int nextMaxAdditionalLights,
            bool nextScreenSpaceAmbientOcclusion,
            int nextMaxActiveParticleSystems,
            int nextMaxActiveVfx,
            int nextMaxActiveLights,
            int nextWorldTextureMaxSize,
            int nextUiSpriteMaxSize,
            float nextGpuFrameP95BudgetMs)
        {
            profileKind = nextProfileKind;
            renderPipelineAsset = nextRenderPipelineAsset;
            targetFrameRate = Mathf.Max(30, nextTargetFrameRate);
            vSyncCount = Mathf.Clamp(nextVSyncCount, 0, 4);
            renderScale = Mathf.Clamp(nextRenderScale, 0.5f, 1.5f);
            supportsHdr = nextSupportsHdr;
            requiresDepthTexture = nextRequiresDepthTexture;
            requiresOpaqueTexture = nextRequiresOpaqueTexture;
            mainLightShadowResolution = Mathf.Max(256, nextMainLightShadowResolution);
            shadowDistance = Mathf.Max(0f, nextShadowDistance);
            shadowCascadeCount = Mathf.Clamp(nextShadowCascadeCount, 0, 4);
            additionalLightShadows = nextAdditionalLightShadows;
            maxAdditionalLights = Mathf.Max(0, nextMaxAdditionalLights);
            screenSpaceAmbientOcclusion = nextScreenSpaceAmbientOcclusion;
            maxActiveParticleSystems = Mathf.Max(0, nextMaxActiveParticleSystems);
            maxActiveVfx = Mathf.Max(0, nextMaxActiveVfx);
            maxActiveLights = Mathf.Max(0, nextMaxActiveLights);
            worldTextureMaxSize = Mathf.Max(128, nextWorldTextureMaxSize);
            uiSpriteMaxSize = Mathf.Max(128, nextUiSpriteMaxSize);
            gpuFrameP95BudgetMs = Mathf.Max(1f, nextGpuFrameP95BudgetMs);
        }
    }
}
