using UnityEngine;
using UnityEngine.Rendering;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Presentation/Biome Lighting Profile", fileName = "BiomeLightingProfile")]
    public sealed class BiomeLightingProfileDefinition : ScriptableObject
    {
        [SerializeField] private string profileId = RoomBiomeIds.HollowThreshold;
        [SerializeField] private string biomeId = RoomBiomeIds.HollowThreshold;
        [SerializeField] private Color cameraBackgroundColor = new(0.018f, 0.023f, 0.034f, 1f);
        [SerializeField] private Color ambientSkyColor = new(0.212f, 0.227f, 0.259f, 1f);
        [SerializeField] private Color ambientEquatorColor = new(0.114f, 0.125f, 0.133f, 1f);
        [SerializeField] private Color ambientGroundColor = new(0.047f, 0.043f, 0.035f, 1f);
        [SerializeField] private float ambientIntensity = 1f;
        [SerializeField] private bool fogEnabled;
        [SerializeField] private Color fogColor = new(0.08f, 0.1f, 0.12f, 1f);
        [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;
        [SerializeField] private float fogDensity = 0.01f;
        [SerializeField] private float linearFogStart = 0f;
        [SerializeField] private float linearFogEnd = 36f;
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private float reflectionIntensity = 0.65f;
        [SerializeField] private Color keyLightColor = new(1f, 0.94f, 0.84f, 1f);
        [SerializeField] private Vector3 keyLightEulerAngles = new(55f, -35f, 0f);
        [SerializeField] private float keyLightIntensity = 1.05f;
        [SerializeField] private bool keyLightCastsShadows = true;
        [SerializeField] private float keyLightShadowStrength = 0.55f;
        [SerializeField] private bool fillLightEnabled = true;
        [SerializeField] private Color fillLightColor = new(0.55f, 0.72f, 1f, 1f);
        [SerializeField] private Vector3 fillLightLocalPosition = new(-4.5f, 5.5f, -3.5f);
        [SerializeField] private float fillLightIntensity = 0.75f;
        [SerializeField] private float fillLightRange = 18f;
        [SerializeField] private bool rimLightEnabled = true;
        [SerializeField] private Color rimLightColor = new(0.45f, 1f, 0.78f, 1f);
        [SerializeField] private Vector3 rimLightLocalPosition = new(5f, 3.5f, 4.5f);
        [SerializeField] private float rimLightIntensity = 0.45f;
        [SerializeField] private float rimLightRange = 14f;
        [SerializeField] private Color primaryAccentColor = new(0.35f, 0.9f, 1f, 1f);
        [SerializeField] private Color secondaryAccentColor = new(1f, 0.72f, 0.48f, 1f);
        [SerializeField] private Color bloomTint = new(0.8f, 0.92f, 1f, 1f);
        [SerializeField] private int maxActiveLocalLights = 8;
        [SerializeField] private int maxShadowedLocalLights = 1;
        [SerializeField] private int maxPropLights = 5;
        [SerializeField] private int maxDynamicEffectLights = 3;
        [SerializeField] private int maxActiveParticleSystems = 48;
        [SerializeField] private float gpuFrameP95BudgetMs = 16.67f;

        public string ProfileId => string.IsNullOrWhiteSpace(profileId) ? BiomeId : profileId.Trim();

        public string BiomeId => RoomBiomeIds.Normalize(biomeId);

        public Color CameraBackgroundColor => cameraBackgroundColor;

        public Color AmbientSkyColor => ambientSkyColor;

        public Color AmbientEquatorColor => ambientEquatorColor;

        public Color AmbientGroundColor => ambientGroundColor;

        public float AmbientIntensity => Mathf.Max(0f, ambientIntensity);

        public bool FogEnabled => fogEnabled;

        public Color FogColor => fogColor;

        public FogMode FogMode => fogMode;

        public float FogDensity => Mathf.Max(0f, fogDensity);

        public float LinearFogStart => Mathf.Max(0f, linearFogStart);

        public float LinearFogEnd => Mathf.Max(LinearFogStart + 0.1f, linearFogEnd);

        public Material SkyboxMaterial => skyboxMaterial;

        public float ReflectionIntensity => Mathf.Max(0f, reflectionIntensity);

        public Color KeyLightColor => keyLightColor;

        public Vector3 KeyLightEulerAngles => keyLightEulerAngles;

        public float KeyLightIntensity => Mathf.Max(0f, keyLightIntensity);

        public bool KeyLightCastsShadows => keyLightCastsShadows;

        public float KeyLightShadowStrength => Mathf.Clamp01(keyLightShadowStrength);

        public bool FillLightEnabled => fillLightEnabled;

        public Color FillLightColor => fillLightColor;

        public Vector3 FillLightLocalPosition => fillLightLocalPosition;

        public float FillLightIntensity => Mathf.Max(0f, fillLightIntensity);

        public float FillLightRange => Mathf.Max(0.1f, fillLightRange);

        public bool RimLightEnabled => rimLightEnabled;

        public Color RimLightColor => rimLightColor;

        public Vector3 RimLightLocalPosition => rimLightLocalPosition;

        public float RimLightIntensity => Mathf.Max(0f, rimLightIntensity);

        public float RimLightRange => Mathf.Max(0.1f, rimLightRange);

        public Color PrimaryAccentColor => primaryAccentColor;

        public Color SecondaryAccentColor => secondaryAccentColor;

        public Color BloomTint => bloomTint;

        public int MaxActiveLocalLights => Mathf.Max(0, maxActiveLocalLights);

        public int MaxShadowedLocalLights => Mathf.Max(0, maxShadowedLocalLights);

        public int MaxPropLights => Mathf.Max(0, maxPropLights);

        public int MaxDynamicEffectLights => Mathf.Max(0, maxDynamicEffectLights);

        public int MaxActiveParticleSystems => Mathf.Max(0, maxActiveParticleSystems);

        public float GpuFrameP95BudgetMs => Mathf.Max(1f, gpuFrameP95BudgetMs);

        public void Configure(
            string nextProfileId,
            string nextBiomeId,
            Color nextCameraBackgroundColor,
            Color nextAmbientSkyColor,
            Color nextAmbientEquatorColor,
            Color nextAmbientGroundColor,
            float nextAmbientIntensity,
            bool nextFogEnabled,
            Color nextFogColor,
            float nextFogDensity,
            Color nextKeyLightColor,
            Vector3 nextKeyLightEulerAngles,
            float nextKeyLightIntensity,
            Color nextFillLightColor,
            Color nextRimLightColor,
            Color nextPrimaryAccentColor,
            Color nextSecondaryAccentColor,
            int nextMaxActiveLocalLights,
            int nextMaxShadowedLocalLights,
            int nextMaxPropLights,
            int nextMaxDynamicEffectLights)
        {
            profileId = string.IsNullOrWhiteSpace(nextProfileId) ? RoomBiomeIds.Normalize(nextBiomeId) : nextProfileId.Trim();
            biomeId = RoomBiomeIds.Normalize(nextBiomeId);
            cameraBackgroundColor = nextCameraBackgroundColor;
            ambientSkyColor = nextAmbientSkyColor;
            ambientEquatorColor = nextAmbientEquatorColor;
            ambientGroundColor = nextAmbientGroundColor;
            ambientIntensity = Mathf.Max(0f, nextAmbientIntensity);
            fogEnabled = nextFogEnabled;
            fogColor = nextFogColor;
            fogDensity = Mathf.Max(0f, nextFogDensity);
            keyLightColor = nextKeyLightColor;
            keyLightEulerAngles = nextKeyLightEulerAngles;
            keyLightIntensity = Mathf.Max(0f, nextKeyLightIntensity);
            fillLightColor = nextFillLightColor;
            rimLightColor = nextRimLightColor;
            primaryAccentColor = nextPrimaryAccentColor;
            secondaryAccentColor = nextSecondaryAccentColor;
            maxActiveLocalLights = Mathf.Max(0, nextMaxActiveLocalLights);
            maxShadowedLocalLights = Mathf.Max(0, nextMaxShadowedLocalLights);
            maxPropLights = Mathf.Max(0, nextMaxPropLights);
            maxDynamicEffectLights = Mathf.Max(0, nextMaxDynamicEffectLights);
        }
    }
}
