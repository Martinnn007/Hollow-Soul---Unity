using Hollow.Data.Definitions;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hollow.Presentation
{
    public sealed class RoomLightingController : MonoBehaviour
    {
        private const string LightingRootName = "RoomLightingRig";
        private Transform lightingRoot;
        private Light keyLight;
        private Light fillLight;
        private Light rimLight;
        private string appliedBiomeId;
        private BiomeLightingProfileDefinition appliedProfile;
        private string preparedBiomeId;
        private BiomeLightingProfileDefinition preparedProfile;
        private int preparedFrame = -1;
        private int globalAppliedFrame = -1;

        public string AppliedBiomeId => appliedBiomeId ?? string.Empty;

        public BiomeLightingProfileDefinition AppliedProfile => appliedProfile;

        public string PreparedBiomeId => preparedBiomeId ?? string.Empty;

        public BiomeLightingProfileDefinition PreparedProfile => preparedProfile;

        public int PreparedFrame => preparedFrame;

        public int GlobalAppliedFrame => globalAppliedFrame;

        private void OnEnable()
        {
            if (!string.IsNullOrWhiteSpace(appliedBiomeId) &&
                appliedProfile != null &&
                globalAppliedFrame == Time.frameCount)
            {
                ApplyEmitterBudget(appliedProfile);
                BiomeLightingDiagnostics.RecordSnapshot(BuildSnapshot(appliedProfile, appliedBiomeId));
                return;
            }

            if (!string.IsNullOrWhiteSpace(preparedBiomeId) && string.IsNullOrWhiteSpace(appliedBiomeId))
            {
                ApplyBiome(preparedBiomeId, force: true);
                return;
            }

            if (!string.IsNullOrWhiteSpace(appliedBiomeId))
            {
                ApplyBiome(appliedBiomeId, force: true);
            }
        }

        public void ApplyCurrentBiome(bool force = false)
        {
            var biomeId = string.IsNullOrWhiteSpace(appliedBiomeId) ? RoomBiomeIds.HollowThreshold : appliedBiomeId;
            ApplyBiome(biomeId, force);
        }

        public void ApplyCurrentBiome(string biomeId, bool force = false)
        {
            ApplyBiome(biomeId, force);
        }

        public bool PrepareBiome(string biomeId, bool force = false)
        {
            var normalizedBiomeId = RoomBiomeIds.Normalize(biomeId);
            return PrepareBiomeInternal(normalizedBiomeId, force) != null;
        }

        public bool IsPreparedFor(string biomeId)
        {
            var normalizedBiomeId = RoomBiomeIds.Normalize(biomeId);
            return preparedProfile != null && RoomBiomeIds.Matches(preparedBiomeId, normalizedBiomeId);
        }

        public void ApplyBiome(string biomeId, bool force = false)
        {
            var normalizedBiomeId = RoomBiomeIds.Normalize(biomeId);
            var profile = PrepareBiomeInternal(normalizedBiomeId, force);
            if (!force && appliedProfile == profile && RoomBiomeIds.Matches(appliedBiomeId, normalizedBiomeId))
            {
                ApplyEmitterBudget(profile);
                BiomeLightingDiagnostics.RecordSnapshot(BuildSnapshot(profile, normalizedBiomeId));
                return;
            }

            appliedBiomeId = normalizedBiomeId;
            appliedProfile = profile;
            ApplyGlobalSettings(profile);
            globalAppliedFrame = Time.frameCount;
            ApplyCameraSettings(profile);
            ApplyEmitterBudget(profile);
            BiomeLightingDiagnostics.RecordSnapshot(BuildSnapshot(profile, normalizedBiomeId));
        }

        private BiomeLightingProfileDefinition PrepareBiomeInternal(string normalizedBiomeId, bool force)
        {
            var profile = ResolveProfile(normalizedBiomeId);
            if (!force && preparedProfile == profile && RoomBiomeIds.Matches(preparedBiomeId, normalizedBiomeId))
            {
                ApplyEmitterBudget(profile);
                BiomeLightingDiagnostics.RecordSnapshot(BuildSnapshot(profile, normalizedBiomeId));
                return profile;
            }

            preparedBiomeId = normalizedBiomeId;
            preparedProfile = profile;
            preparedFrame = Time.frameCount;
            ApplyRig(profile);
            ApplyEmitterBudget(profile);
            BiomeLightingDiagnostics.RecordSnapshot(BuildSnapshot(profile, normalizedBiomeId));
            return profile;
        }

        private static BiomeLightingProfileDefinition ResolveProfile(string biomeId)
        {
            var catalog = RoomBiomeCatalogDefinition.LoadDefault();
            if (catalog != null && catalog.TryGetBiome(biomeId, out var biome) && biome != null && biome.LightingProfile != null)
            {
                return biome.LightingProfile;
            }

            if (catalog != null && catalog.TryGetBiome(RoomBiomeIds.HollowThreshold, out var fallback) && fallback != null)
            {
                return fallback.LightingProfile;
            }

            return null;
        }

        private static void ApplyGlobalSettings(BiomeLightingProfileDefinition profile)
        {
            if (profile == null)
            {
                return;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = profile.AmbientSkyColor;
            RenderSettings.ambientEquatorColor = profile.AmbientEquatorColor;
            RenderSettings.ambientGroundColor = profile.AmbientGroundColor;
            RenderSettings.ambientIntensity = profile.AmbientIntensity;
            RenderSettings.fog = profile.FogEnabled;
            RenderSettings.fogColor = profile.FogColor;
            RenderSettings.fogMode = profile.FogMode;
            RenderSettings.fogDensity = profile.FogDensity;
            RenderSettings.fogStartDistance = profile.LinearFogStart;
            RenderSettings.fogEndDistance = profile.LinearFogEnd;
            RenderSettings.reflectionIntensity = profile.ReflectionIntensity;
            if (profile.SkyboxMaterial != null)
            {
                RenderSettings.skybox = profile.SkyboxMaterial;
            }
        }

        private static void ApplyCameraSettings(BiomeLightingProfileDefinition profile)
        {
            if (profile == null)
            {
                return;
            }

            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
            for (var index = 0; index < cameras.Length; index++)
            {
                if (cameras[index] == null || cameras[index].cameraType != CameraType.Game)
                {
                    continue;
                }

                cameras[index].backgroundColor = profile.CameraBackgroundColor;
            }
        }

        private void ApplyRig(BiomeLightingProfileDefinition profile)
        {
            if (profile == null)
            {
                return;
            }

            EnsureRig();
            ConfigureDirectionalLight(keyLight, profile);
            ConfigurePointLight(fillLight, profile.FillLightEnabled, profile.FillLightColor, profile.FillLightLocalPosition, profile.FillLightIntensity, profile.FillLightRange);
            ConfigurePointLight(rimLight, profile.RimLightEnabled, profile.RimLightColor, profile.RimLightLocalPosition, profile.RimLightIntensity, profile.RimLightRange);
        }

        private void EnsureRig()
        {
            if (lightingRoot != null)
            {
                return;
            }

            var existing = transform.Find(LightingRootName);
            lightingRoot = existing != null ? existing : new GameObject(LightingRootName).transform;
            lightingRoot.SetParent(transform, false);
            keyLight = EnsureLight("BiomeKeyLight", LightType.Directional);
            fillLight = EnsureLight("BiomeFillLight", LightType.Point);
            rimLight = EnsureLight("BiomeRimLight", LightType.Point);
        }

        private Light EnsureLight(string objectName, LightType lightType)
        {
            var existing = lightingRoot.Find(objectName);
            var lightObject = existing != null ? existing.gameObject : new GameObject(objectName);
            lightObject.transform.SetParent(lightingRoot, false);
            var light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }

            light.type = lightType;
            return light;
        }

        private static void ConfigureDirectionalLight(Light light, BiomeLightingProfileDefinition profile)
        {
            if (light == null)
            {
                return;
            }

            light.gameObject.SetActive(profile.KeyLightIntensity > 0f);
            light.type = LightType.Directional;
            light.color = profile.KeyLightColor;
            light.intensity = profile.KeyLightIntensity;
            light.shadows = profile.KeyLightCastsShadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = profile.KeyLightShadowStrength;
            light.transform.localRotation = Quaternion.Euler(profile.KeyLightEulerAngles);
        }

        private static void ConfigurePointLight(Light light, bool enabled, Color color, Vector3 localPosition, float intensity, float range)
        {
            if (light == null)
            {
                return;
            }

            light.gameObject.SetActive(enabled && intensity > 0f && range > 0f);
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            light.transform.localPosition = localPosition;
        }

        private static void ApplyEmitterBudget(BiomeLightingProfileDefinition profile)
        {
            BiomeLightEmitter.ApplyBudgets(profile);
        }

        private BiomeLightingSnapshot BuildSnapshot(BiomeLightingProfileDefinition profile, string biomeId)
        {
            var activeLights = 0;
            var shadowedLights = 0;
            var localLights = 0;
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            for (var index = 0; index < lights.Length; index++)
            {
                var light = lights[index];
                if (light == null || !light.enabled || !light.gameObject.activeInHierarchy)
                {
                    continue;
                }

                activeLights++;
                if (light.type != LightType.Directional)
                {
                    localLights++;
                }

                if (light.shadows != LightShadows.None)
                {
                    shadowedLights++;
                }
            }

            return new BiomeLightingSnapshot(
                biomeId,
                profile != null ? profile.ProfileId : string.Empty,
                activeLights,
                localLights,
                shadowedLights,
                BiomeLightEmitter.ActivePropLightCount,
                BiomeLightEmitter.ActiveDynamicEffectLightCount,
                profile != null ? profile.MaxActiveLocalLights : 0,
                profile != null ? profile.MaxShadowedLocalLights : 0,
                profile != null ? profile.MaxPropLights : 0,
                profile != null ? profile.MaxDynamicEffectLights : 0,
                profile != null ? profile.GpuFrameP95BudgetMs : 0f);
        }
    }
}
