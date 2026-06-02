using Hollow.Data.Definitions;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class BiomeLightingProfileTests
    {
        [Test]
        public void EveryCatalogBiomeHasAValidLightingProfile()
        {
            var catalog = RoomBiomeCatalogDefinition.LoadDefault();

            Assert.NotNull(catalog);
            Assert.Greater(catalog.Biomes.Count, 0);

            foreach (var biome in catalog.Biomes)
            {
                Assert.NotNull(biome);
                Assert.False(string.IsNullOrWhiteSpace(biome.BiomeId));

                var profile = biome.LightingProfile;

                Assert.NotNull(profile, $"Biome '{biome.BiomeId}' should reference a lighting profile.");
                Assert.False(string.IsNullOrWhiteSpace(profile.ProfileId), biome.BiomeId);
                Assert.AreEqual(biome.BiomeId, profile.BiomeId, biome.BiomeId);
                Assert.Greater(profile.KeyLightIntensity, 0f, biome.BiomeId);
                Assert.GreaterOrEqual(profile.MaxActiveLocalLights, 1, biome.BiomeId);
                Assert.GreaterOrEqual(profile.MaxPropLights, 0, biome.BiomeId);
            }
        }

        [Test]
        public void RoomLightingControllerAppliesBiomeLightingAndRecordsDiagnostics()
        {
            var previousAmbientMode = RenderSettings.ambientMode;
            var previousAmbientSky = RenderSettings.ambientSkyColor;
            var previousAmbientEquator = RenderSettings.ambientEquatorColor;
            var previousAmbientGround = RenderSettings.ambientGroundColor;
            var previousAmbientIntensity = RenderSettings.ambientIntensity;
            var previousFog = RenderSettings.fog;
            var previousFogMode = RenderSettings.fogMode;
            var previousFogColor = RenderSettings.fogColor;
            var previousFogDensity = RenderSettings.fogDensity;
            var previousSkybox = RenderSettings.skybox;
            var previousReflectionIntensity = RenderSettings.reflectionIntensity;
            var room = new GameObject("BiomeLightingTestRoom");

            try
            {
                var controller = room.AddComponent<RoomLightingController>();

                controller.ApplyBiome("verdant_ruins", force: true);

                Assert.AreEqual("verdant_ruins", controller.AppliedBiomeId);
                Assert.NotNull(controller.AppliedProfile);
                Assert.AreEqual(AmbientMode.Trilight, RenderSettings.ambientMode);

                var snapshot = BiomeLightingDiagnostics.LastSnapshot;

                Assert.AreEqual("verdant_ruins", snapshot.BiomeId);
                Assert.AreEqual(controller.AppliedProfile.ProfileId, snapshot.ProfileId);
                Assert.Greater(snapshot.ActiveLightCount, 0);
            }
            finally
            {
                Object.DestroyImmediate(room);
                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientSkyColor = previousAmbientSky;
                RenderSettings.ambientEquatorColor = previousAmbientEquator;
                RenderSettings.ambientGroundColor = previousAmbientGround;
                RenderSettings.ambientIntensity = previousAmbientIntensity;
                RenderSettings.fog = previousFog;
                RenderSettings.fogMode = previousFogMode;
                RenderSettings.fogColor = previousFogColor;
                RenderSettings.fogDensity = previousFogDensity;
                RenderSettings.skybox = previousSkybox;
                RenderSettings.reflectionIntensity = previousReflectionIntensity;
            }
        }
    }
}
