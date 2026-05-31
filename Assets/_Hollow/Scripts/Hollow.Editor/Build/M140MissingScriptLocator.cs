using System;
using System.Collections.Generic;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hollow.Editor.Build
{
    public static class M140MissingScriptLocator
    {
        private const int DefaultMaxResults = 128;

        public static string[] FindMissingScriptLocations(M140BuildRealGateProfileDefinition profile, int maxResults = DefaultMaxResults)
        {
            var results = new List<string>(Mathf.Min(Mathf.Max(1, maxResults), DefaultMaxResults));
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var limit = Mathf.Max(1, maxResults);
            foreach (var scenePath in profile?.RequiredScenes ?? Array.Empty<string>())
            {
                if (results.Count >= limit)
                {
                    break;
                }

                ScanScene(scenePath, results, seen, limit);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Hollow" }))
            {
                if (results.Count >= limit)
                {
                    break;
                }

                var path = AssetDatabase.GUIDToAssetPath(guid);
                ScanPrefab(path, results, seen, limit);
            }

            return results.ToArray();
        }

        private static void ScanScene(string scenePath, List<string> results, HashSet<string> seen, int limit)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return;
            }

            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                try
                {
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        ScanGameObject(root, scenePath, results, seen, limit);
                        if (results.Count >= limit)
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, removeScene: true);
                }
            }
            catch (Exception exception)
            {
                Add(results, seen, limit, $"{scenePath}::<scene scan failed: {exception.GetType().Name}>");
            }
        }

        private static void ScanPrefab(string prefabPath, List<string> results, HashSet<string> seen, int limit)
        {
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                return;
            }

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(prefabPath);
                ScanGameObject(root, prefabPath, results, seen, limit);
            }
            catch (Exception exception)
            {
                Add(results, seen, limit, $"{prefabPath}::<prefab scan failed: {exception.GetType().Name}>");
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void ScanGameObject(GameObject root, string assetPath, List<string> results, HashSet<string> seen, int limit)
        {
            if (root == null || results.Count >= limit)
            {
                return;
            }

            var missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
            if (missingCount > 0)
            {
                Add(results, seen, limit, $"{assetPath}::{HierarchyPath(root.transform)} ({missingCount} missing script component(s))");
            }

            var transform = root.transform;
            for (var index = 0; index < transform.childCount && results.Count < limit; index++)
            {
                ScanGameObject(transform.GetChild(index).gameObject, assetPath, results, seen, limit);
            }
        }

        private static string HierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            var stack = new Stack<string>();
            var current = transform;
            while (current != null)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", stack);
        }

        private static void Add(List<string> results, HashSet<string> seen, int limit, string value)
        {
            if (results.Count >= limit || string.IsNullOrWhiteSpace(value) || !seen.Add(value))
            {
                return;
            }

            results.Add(value);
        }
    }
}
