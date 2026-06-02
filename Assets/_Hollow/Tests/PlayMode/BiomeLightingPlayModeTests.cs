using System;
using System.Collections;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hollow.Tests.PlayMode
{
    public sealed class BiomeLightingPlayModeTests
    {
        [UnityTest]
        public IEnumerator FullBranchRoomCacheAppliesLightingBeforeFirstVisibleFrame()
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
            var parentObject = new GameObject("FullBranchLightingPrewarmPlayModeParent");
            var cache = new BranchLiveRoomCache();

            try
            {
                var room = CreateBranchRoom(BranchRoomId.Origin, "playmode_verdant_asset", BranchRoomRole.Origin);
                var asset = CreateRoomAsset(RoomBiomeIds.VerdantRuins);

                yield return cache.BuildBranchRooms(new[] { room }, _ => asset, parentObject.transform);

                Assert.IsTrue(cache.TryGetRoom(room.Id, out var instance));
                Assert.IsFalse(instance.RootObject.activeSelf, "Cached room should still be hidden after branch prewarm.");

                var lighting = instance.RuntimeRoot.GetComponent<RoomLightingController>();
                Assert.NotNull(lighting);
                Assert.IsTrue(lighting.IsPreparedFor(RoomBiomeIds.VerdantRuins));
                Assert.AreEqual(string.Empty, lighting.AppliedBiomeId, "Prewarm should not apply active-room globals while hidden.");

                Assert.IsTrue(cache.PrepareRoomForEntry(room.Id));
                Assert.IsTrue(cache.ActivateRoom(room.Id));
                var revealFrame = Time.frameCount;

                Assert.IsTrue(instance.RootObject.activeSelf);
                Assert.NotNull(lighting.AppliedProfile);
                Assert.AreEqual(RoomBiomeIds.VerdantRuins, lighting.AppliedBiomeId);
                Assert.LessOrEqual(lighting.GlobalAppliedFrame, revealFrame);
                Assert.AreEqual(RoomBiomeIds.VerdantRuins, BiomeLightingDiagnostics.LastSnapshot.BiomeId);
                AssertColorApproximately(lighting.AppliedProfile.AmbientSkyColor, RenderSettings.ambientSkyColor);

                yield return null;

                Assert.AreEqual(RoomBiomeIds.VerdantRuins, lighting.AppliedBiomeId);
                Assert.LessOrEqual(lighting.GlobalAppliedFrame, revealFrame);
            }
            finally
            {
                cache.DisposeBranchRooms();
                Object.Destroy(parentObject);
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

        [UnityTest]
        public IEnumerator RuntimeRoomLightingBinderAppliesBiomeTransitions()
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
            var cameraObject = new GameObject("BiomeLightingTestCamera");
            var loopObject = new GameObject("BiomeLightingTestLoop");
            var hollow = CreateRoomRoot(RoomBiomeIds.HollowThreshold);
            var verdant = CreateRoomRoot(RoomBiomeIds.VerdantRuins);
            var ashen = CreateRoomRoot(RoomBiomeIds.CorruptedAshenShrine);

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                var loop = loopObject.AddComponent<RoomLightingRuntimeLoop>();
                hollow.gameObject.SetActive(false);
                verdant.gameObject.SetActive(false);
                ashen.gameObject.SetActive(false);

                yield return AssertActiveBiome(loop, camera, hollow, RoomBiomeIds.HollowThreshold, verdant, ashen);
                yield return AssertActiveBiome(loop, camera, verdant, RoomBiomeIds.VerdantRuins, hollow, ashen);
                yield return AssertActiveBiome(loop, camera, ashen, RoomBiomeIds.CorruptedAshenShrine, hollow, verdant);
            }
            finally
            {
                Object.Destroy(cameraObject);
                Object.Destroy(loopObject);
                Object.Destroy(hollow.gameObject);
                Object.Destroy(verdant.gameObject);
                Object.Destroy(ashen.gameObject);
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

        [UnityTest]
        public IEnumerator RenderProfileSwitchDoesNotReapplyActiveRoomGlobals()
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
            var previousTargetFrameRate = Application.targetFrameRate;
            var previousVSyncCount = QualitySettings.vSyncCount;
            var previousPipeline = QualitySettings.renderPipeline;
            var hadPreviousPreference = PlayerPrefs.HasKey(RuntimeRenderProfileSettings.PlayerPrefsKey);
            var previousPreference = hadPreviousPreference ? PlayerPrefs.GetString(RuntimeRenderProfileSettings.PlayerPrefsKey) : string.Empty;
            var room = CreateRoomRoot(RoomBiomeIds.VerdantRuins);

            try
            {
                PlayerPrefs.DeleteKey(RuntimeRenderProfileSettings.PlayerPrefsKey);
                RuntimeRenderProfileSettings.ResetForTests();
                room.gameObject.SetActive(false);

                Assert.IsTrue(RoomLightingPrewarm.Prepare(room));
                Assert.IsTrue(RoomLightingPrewarm.ApplyForEntry(room));
                var lighting = room.GetComponent<RoomLightingController>();
                Assert.NotNull(lighting);
                var preRevealGlobalFrame = lighting.GlobalAppliedFrame;

                room.gameObject.SetActive(true);
                var revealFrame = Time.frameCount;

                Assert.AreEqual(preRevealGlobalFrame, lighting.GlobalAppliedFrame, "Room enable should not reapply globals after pre-entry lighting was applied.");
                Assert.AreEqual(RoomBiomeIds.VerdantRuins, lighting.AppliedBiomeId);
                Assert.LessOrEqual(lighting.GlobalAppliedFrame, revealFrame);

                RuntimeRenderProfileSettings.SetMode(RuntimeRenderProfileMode.Cool, persist: false);
                RuntimeRenderProfileSettings.SetMode(RuntimeRenderProfileMode.Quality, persist: false);
                yield return null;

                Assert.IsTrue(lighting.IsPreparedFor(RoomBiomeIds.VerdantRuins));
                Assert.AreEqual(RoomBiomeIds.VerdantRuins, lighting.AppliedBiomeId);
                Assert.AreEqual(preRevealGlobalFrame, lighting.GlobalAppliedFrame);
            }
            finally
            {
                Object.Destroy(room.gameObject);
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
                Application.targetFrameRate = previousTargetFrameRate;
                QualitySettings.vSyncCount = previousVSyncCount;
                QualitySettings.renderPipeline = previousPipeline;
                if (hadPreviousPreference)
                {
                    PlayerPrefs.SetString(RuntimeRenderProfileSettings.PlayerPrefsKey, previousPreference);
                }
                else
                {
                    PlayerPrefs.DeleteKey(RuntimeRenderProfileSettings.PlayerPrefsKey);
                }

                RuntimeRenderProfileSettings.ResetForTests();
            }
        }

        private static IEnumerator AssertActiveBiome(RoomLightingRuntimeLoop loop, Camera camera, RoomRuntimeRoot active, string expectedBiomeId, params RoomRuntimeRoot[] inactive)
        {
            for (var index = 0; index < inactive.Length; index++)
            {
                inactive[index].gameObject.SetActive(false);
            }

            active.gameObject.SetActive(true);
            loop.ApplyActiveRoomLighting();
            yield return null;

            var controller = active.GetComponent<RoomLightingController>();
            var profile = controller != null ? controller.AppliedProfile : null;
            var snapshot = BiomeLightingDiagnostics.LastSnapshot;

            Assert.NotNull(controller);
            Assert.NotNull(profile);
            Assert.AreEqual(RoomBiomeIds.Normalize(expectedBiomeId), controller.AppliedBiomeId);
            Assert.AreEqual(RoomBiomeIds.Normalize(expectedBiomeId), profile.BiomeId);
            Assert.AreEqual(RoomBiomeIds.Normalize(expectedBiomeId), snapshot.BiomeId);
            Assert.AreEqual(profile.ProfileId, snapshot.ProfileId);
            Assert.AreEqual(AmbientMode.Trilight, RenderSettings.ambientMode);
            AssertColorApproximately(profile.AmbientSkyColor, RenderSettings.ambientSkyColor);
            AssertColorApproximately(profile.CameraBackgroundColor, camera.backgroundColor);
            Assert.NotNull(active.transform.Find("RoomLightingRig/BiomeKeyLight"));
            Assert.NotNull(active.transform.Find("RoomLightingRig/BiomeFillLight"));
            Assert.NotNull(active.transform.Find("RoomLightingRig/BiomeRimLight"));
        }

        private static BranchRoomState CreateBranchRoom(BranchRoomId id, string roomAssetId, BranchRoomRole role)
        {
            return new BranchRoomState(
                id,
                Vector2Int.zero,
                new BranchRoomInstanceId(id.Value),
                roomAssetId,
                null,
                role);
        }

        private static RoomRuntimeRoot CreateRoomRoot(string biomeId)
        {
            var gameObject = new GameObject("LightingRoom_" + biomeId);
            var root = gameObject.AddComponent<RoomRuntimeRoot>();
            root.BuildFrom(CreateRoomAsset(biomeId), RoomNavMeshRuntimeFallbackMode.EditorOrDevelopmentRuntimeBake);
            return root;
        }

        private static ImportedRoomRuntimeAsset CreateRoomAsset(string biomeId)
        {
            const int width = 5;
            const int height = 5;
            var bounds = Rect.MinMaxRect(-2.5f, -2.5f, 2.5f, 2.5f);
            var layout = new RoomLayout(
                width,
                height,
                bounds,
                new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1)
                },
                Array.Empty<Vector2Int>(),
                new[]
                {
                    new RoomLayoutFloorRegion("floor_a", Vector3.zero, new Vector2(2.5f, 2.5f))
                },
                Array.Empty<RoomLayoutObstacle>());
            var footprint = new RoomInstanceFootprint(Vector2Int.zero, new[] { Vector2Int.zero }, new Vector2Int(width, height));
            return new ImportedRoomRuntimeAsset(
                "lighting_room_" + biomeId,
                "Lighting Room " + biomeId,
                biomeId,
                layout,
                footprint,
                new[] { CreateDoorPort("north_0", "north", 0f) },
                Array.Empty<ImportedSpawnPoint>(),
                Array.Empty<ImportedSpawnPoint>(),
                new ImportedSpawnPoint
                {
                    id = "safe_start",
                    kind = "spawnSafeStart",
                    position = CreateVector3(0f, 0f, -1f)
                },
                Array.Empty<ImportedRoomHazard>(),
                Array.Empty<ImportedRoomInteractiveObject>(),
                Array.Empty<ImportedRoomDecor>(),
                new ImportedHollowRoomManifest
                {
                    hollowRuntime = new ImportedHollowRuntime
                    {
                        canonicalRoomId = "lighting_room_" + biomeId,
                        biomeId = biomeId,
                        displayName = "Lighting Room " + biomeId
                    }
                });
        }

        private static RoomDoorPort CreateDoorPort(string id, string direction, float x)
        {
            return new RoomDoorPort(
                id,
                direction,
                0,
                new Vector2Int(0, 2),
                new Vector2(x, 2.5f),
                new Vector3(x, 0f, 2.5f),
                "standard");
        }

        private static ImportedVector3 CreateVector3(float x, float y, float z)
        {
            return new ImportedVector3 { x = x, y = y, z = z };
        }

        private static void AssertColorApproximately(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.001f);
            Assert.AreEqual(expected.g, actual.g, 0.001f);
            Assert.AreEqual(expected.b, actual.b, 0.001f);
            Assert.AreEqual(expected.a, actual.a, 0.001f);
        }
    }
}
