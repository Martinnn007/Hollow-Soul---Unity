using System.IO;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone20AssetGenerator
    {
        public const string BranchFeaturePrefabDirectory = "Assets/_Hollow/Prefabs/Branches/M20";
        public const string BossKeyPickupPrefabPath = BranchFeaturePrefabDirectory + "/BossKeyPickup.prefab";
        public const string HubShopPrefabPath = BranchFeaturePrefabDirectory + "/HubShop.prefab";
        public const string NextBranchPortalPrefabPath = BranchFeaturePrefabDirectory + "/NextBranchPortal.prefab";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 20 Assets")]
        public static void Generate()
        {
            Milestone19AssetGenerator.Generate();
            Directory.CreateDirectory(BranchFeaturePrefabDirectory);
            var bossKey = SaveBossKeyPrefab();
            var shop = SaveHubShopPrefab();
            var portal = SaveNextBranchPortalPrefab();
            AssignFeaturePrefabsToGameScenes(bossKey, shop, portal);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 20 branch feature prefabs and scene wiring.");
        }

        private static GameObject SaveBossKeyPrefab()
        {
            var root = new GameObject("BossKeyPickup");
            root.name = "BossKeyPickup";
            root.transform.localScale = Vector3.one * 0.32f;
            root.AddComponent<BossKeyPickup>();
            return SavePrefab(root, BossKeyPickupPrefabPath);
        }

        private static GameObject SaveHubShopPrefab()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "HubShop";
            root.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            root.AddComponent<HubShopController>();
            DisableCollider(root);
            MaterialResolver.ApplyTo(root, MaterialRole.HubShop);
            return SavePrefab(root, HubShopPrefabPath);
        }

        private static GameObject SaveNextBranchPortalPrefab()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "NextBranchPortal";
            root.transform.localScale = new Vector3(0.42f, 0.08f, 0.42f);
            root.AddComponent<NextBranchPortal>();
            DisableCollider(root);
            MaterialResolver.ApplyTo(root, MaterialRole.NextBranchPortal);
            return SavePrefab(root, NextBranchPortalPrefabPath);
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void DisableCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static void AssignFeaturePrefabsToGameScenes(GameObject bossKey, GameObject shop, GameObject portal)
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureBranchFeaturePrefabs(bossKey, shop, portal);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
