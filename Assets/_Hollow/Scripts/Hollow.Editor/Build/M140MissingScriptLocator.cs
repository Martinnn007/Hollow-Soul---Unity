using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
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
            var scannedAssets = new HashSet<string>(StringComparer.Ordinal);
            var limit = Mathf.Max(1, maxResults);
            foreach (var scenePath in EnumerateScenePaths(profile))
            {
                if (results.Count >= limit)
                {
                    break;
                }

                ScanAssetPath(scenePath, results, seen, scannedAssets, limit);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                if (results.Count >= limit)
                {
                    break;
                }

                var path = AssetDatabase.GUIDToAssetPath(guid);
                ScanAssetPath(path, results, seen, scannedAssets, limit);
            }

            foreach (var path in EnumerateAddressableAssetAndDependencyPaths())
            {
                if (results.Count >= limit)
                {
                    break;
                }

                ScanAssetPath(path, results, seen, scannedAssets, limit);
            }

            foreach (var path in EnumerateSerializedAssetPaths())
            {
                if (results.Count >= limit)
                {
                    break;
                }

                ScanAssetPath(path, results, seen, scannedAssets, limit);
            }

            return results.ToArray();
        }

        private static IEnumerable<string> EnumerateScenePaths(M140BuildRealGateProfileDefinition profile)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var scenePath in profile?.RequiredScenes ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(scenePath) && seen.Add(scenePath))
                {
                    yield return scenePath;
                }
            }

            foreach (var scene in EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>())
            {
                if (scene == null || string.IsNullOrWhiteSpace(scene.path) || !seen.Add(scene.path))
                {
                    continue;
                }

                yield return scene.path;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrWhiteSpace(scenePath) && seen.Add(scenePath))
                {
                    yield return scenePath;
                }
            }
        }

        private static IEnumerable<string> EnumerateAddressableAssetAndDependencyPaths()
        {
            AddressableAssetSettings settings;
            try
            {
                settings = AddressableAssetSettingsDefaultObject.GetSettings(create: false);
            }
            catch
            {
                yield break;
            }

            if (settings == null)
            {
                yield break;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in settings.groups ?? Enumerable.Empty<AddressableAssetGroup>())
            {
                if (group == null)
                {
                    continue;
                }

                foreach (var entry in group.entries ?? Enumerable.Empty<AddressableAssetEntry>())
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.AssetPath))
                    {
                        continue;
                    }

                    if (seen.Add(entry.AssetPath))
                    {
                        yield return entry.AssetPath;
                    }

                    string[] dependencies;
                    try
                    {
                        dependencies = AssetDatabase.GetDependencies(entry.AssetPath, recursive: true);
                    }
                    catch
                    {
                        dependencies = Array.Empty<string>();
                    }

                    foreach (var dependency in dependencies ?? Array.Empty<string>())
                    {
                        if (!string.IsNullOrWhiteSpace(dependency) &&
                            dependency.StartsWith("Assets/", StringComparison.Ordinal) &&
                            seen.Add(dependency))
                        {
                            yield return dependency;
                        }
                    }
                }
            }
        }

        private static void ScanAssetPath(
            string assetPath,
            List<string> results,
            HashSet<string> seen,
            HashSet<string> scannedAssets,
            int limit)
        {
            if (string.IsNullOrWhiteSpace(assetPath) ||
                results.Count >= limit ||
                !IsScannableProjectPath(assetPath) ||
                !scannedAssets.Add(assetPath))
            {
                return;
            }

            var extension = Path.GetExtension(assetPath);
            if (string.Equals(extension, ".unity", StringComparison.OrdinalIgnoreCase))
            {
                ScanScene(assetPath, results, seen, limit);
                ScanSerializedAsset(assetPath, results, seen, limit);
                return;
            }

            if (string.Equals(extension, ".prefab", StringComparison.OrdinalIgnoreCase))
            {
                ScanPrefab(assetPath, results, seen, limit);
                ScanSerializedAsset(assetPath, results, seen, limit);
                return;
            }

            ScanSerializedAsset(assetPath, results, seen, limit);
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

        private static IEnumerable<string> EnumerateSerializedAssetPaths()
        {
            foreach (var path in AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal) &&
                    (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ||
                     path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                     path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                     path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) ||
                     path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase) ||
                     path.EndsWith(".playable", StringComparison.OrdinalIgnoreCase) ||
                     path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                     path.EndsWith(".controller", StringComparison.OrdinalIgnoreCase) ||
                     path.EndsWith(".overrideController", StringComparison.OrdinalIgnoreCase))))
            {
                yield return path;
            }

            if (!Directory.Exists("ProjectSettings"))
            {
                yield break;
            }

            foreach (var path in Directory.EnumerateFiles("ProjectSettings", "*.asset", SearchOption.AllDirectories))
            {
                yield return path.Replace('\\', '/');
            }
        }

        private static void ScanSerializedAsset(string assetPath, List<string> results, HashSet<string> seen, int limit)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || results.Count >= limit)
            {
                return;
            }

            var absolutePath = Path.GetFullPath(assetPath);
            if (!File.Exists(absolutePath))
            {
                return;
            }

            string text;
            try
            {
                text = File.ReadAllText(absolutePath);
            }
            catch (Exception exception)
            {
                Add(results, seen, limit, $"{assetPath}::<serialized scan failed: {exception.GetType().Name}>");
                return;
            }

            if (text.Contains("m_Script: {fileID: 0}", StringComparison.Ordinal) ||
                text.Contains("guid: 00000000000000000000000000000000", StringComparison.Ordinal))
            {
                Add(results, seen, limit, $"{assetPath}::<serialized missing script marker>");
            }

            ScanSerializedScriptGuidReferences(assetPath, text, results, seen, limit);
        }

        private static bool IsScannableProjectPath(string assetPath)
        {
            return assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                assetPath.StartsWith("ProjectSettings/", StringComparison.Ordinal);
        }

        private static void ScanSerializedScriptGuidReferences(
            string assetPath,
            string text,
            List<string> results,
            HashSet<string> seen,
            int limit)
        {
            if (string.IsNullOrEmpty(text) || results.Count >= limit)
            {
                return;
            }

            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (var index = 0; index < lines.Length && results.Count < limit; index++)
            {
                var line = lines[index];
                if (line.IndexOf("m_Script:", StringComparison.Ordinal) < 0 ||
                    line.IndexOf("guid:", StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                var guid = ExtractInlineGuid(line);
                if (string.IsNullOrEmpty(guid) ||
                    string.Equals(guid, "00000000000000000000000000000000", StringComparison.Ordinal))
                {
                    continue;
                }

                var scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrWhiteSpace(scriptPath))
                {
                    continue;
                }

                Add(results, seen, limit, $"{assetPath}::<missing script guid {guid} at line {index + 1}>");
            }
        }

        private static string ExtractInlineGuid(string line)
        {
            var marker = line.IndexOf("guid:", StringComparison.Ordinal);
            if (marker < 0)
            {
                return string.Empty;
            }

            var cursor = marker + "guid:".Length;
            while (cursor < line.Length && char.IsWhiteSpace(line[cursor]))
            {
                cursor++;
            }

            var start = cursor;
            while (cursor < line.Length && IsGuidCharacter(line[cursor]))
            {
                cursor++;
            }

            var length = cursor - start;
            return length == 32 ? line.Substring(start, length) : string.Empty;
        }

        private static bool IsGuidCharacter(char value)
        {
            return (value >= '0' && value <= '9') ||
                (value >= 'a' && value <= 'f') ||
                (value >= 'A' && value <= 'F');
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
