using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.EnemyAuthoring
{
    public sealed class EnemyAuthoringDraft : IDisposable
    {
        private readonly List<UnityEngine.Object> temporaryObjects = new();

        public UnityEngine.Object Source { get; private set; }

        public UnityEngine.Object Draft { get; private set; }

        public string SourcePath { get; private set; } = string.Empty;

        public string SourceJson { get; private set; } = string.Empty;

        public bool IsDirty => Draft != null && !string.Equals(SourceJson, ToJson(Draft), StringComparison.Ordinal);

        public void Load(UnityEngine.Object source)
        {
            Dispose();
            Source = source;
            SourcePath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
            SourceJson = source != null ? ToJson(source) : string.Empty;
            Draft = CloneForDraft(source);
        }

        public void Discard()
        {
            Load(Source);
        }

        public string BuildDiffSummary()
        {
            if (Source == null || Draft == null)
            {
                return "No selected source asset.";
            }

            if (!IsDirty)
            {
                return "No draft changes.";
            }

            return $"{Source.name}: draft differs from source ({Hash(SourceJson)} -> {Hash(ToJson(Draft))}).";
        }

        public void Apply(string notes)
        {
            if (Source == null || Draft == null || !IsDirty)
            {
                return;
            }

            if (Source is EnemyBehaviorTreeDefinition sourceTree && Draft is EnemyBehaviorTreeDefinition draftTree)
            {
                ApplyBehaviorTreeDraft(sourceTree, draftTree);
            }
            else
            {
                var sourceName = Source.name;
                Undo.RecordObject(Source, "Apply Enemy Authoring Draft");
                EditorUtility.CopySerialized(Draft, Source);
                Source.name = sourceName;
                EditorUtility.SetDirty(Source);
            }

            EnemyAuthoringReportWriter.WriteApplyReport(Source, SourcePath, SourceJson, ToJson(Draft), notes);
            EnemyAuthoringProtectionRegistryUtility.RecordProtected(Source, notes);
            AssetDatabase.SaveAssets();
            SourceJson = ToJson(Source);
        }

        public void Dispose()
        {
            for (var index = temporaryObjects.Count - 1; index >= 0; index--)
            {
                var temporary = temporaryObjects[index];
                if (temporary != null)
                {
                    UnityEngine.Object.DestroyImmediate(temporary);
                }
            }

            temporaryObjects.Clear();
            Draft = null;
        }

        public static string ToJson(UnityEngine.Object target)
        {
            return target == null ? string.Empty : EditorJsonUtility.ToJson(target, prettyPrint: true);
        }

        public static string Hash(string text)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty));
            return BitConverter.ToString(bytes).Replace("-", string.Empty).Substring(0, 12).ToLowerInvariant();
        }

        private UnityEngine.Object CloneForDraft(UnityEngine.Object source)
        {
            if (source == null)
            {
                return null;
            }

            if (source is EnemyBehaviorTreeDefinition tree)
            {
                return CloneBehaviorTreeForDraft(tree);
            }

            var clone = UnityEngine.Object.Instantiate(source);
            clone.name = $"{source.name}.Draft";
            clone.hideFlags = HideFlags.HideAndDontSave;
            temporaryObjects.Add(clone);
            return clone;
        }

        private EnemyBehaviorTreeDefinition CloneBehaviorTreeForDraft(EnemyBehaviorTreeDefinition source)
        {
            var clone = UnityEngine.Object.Instantiate(source);
            clone.name = $"{source.name}.Draft";
            clone.hideFlags = HideFlags.HideAndDontSave;
            temporaryObjects.Add(clone);

            var map = new Dictionary<EnemyBehaviorTreeNodeDefinition, EnemyBehaviorTreeNodeDefinition>();
            foreach (var sourceNode in source.Nodes)
            {
                if (sourceNode == null || map.ContainsKey(sourceNode))
                {
                    continue;
                }

                var cloneNode = UnityEngine.Object.Instantiate(sourceNode);
                cloneNode.name = $"{sourceNode.name}.Draft";
                cloneNode.hideFlags = HideFlags.HideAndDontSave;
                temporaryObjects.Add(cloneNode);
                map[sourceNode] = cloneNode;
            }

            foreach (var cloneNode in map.Values)
            {
                RemapNodeReferences(cloneNode, map);
            }

            var serialized = new SerializedObject(clone);
            serialized.FindProperty("rootNode").objectReferenceValue = source.RootNode != null && map.TryGetValue(source.RootNode, out var rootClone)
                ? rootClone
                : null;
            var nodes = serialized.FindProperty("nodes");
            nodes.arraySize = map.Count;
            var index = 0;
            foreach (var node in map.Values)
            {
                nodes.GetArrayElementAtIndex(index++).objectReferenceValue = node;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return clone;
        }

        private static void ApplyBehaviorTreeDraft(EnemyBehaviorTreeDefinition source, EnemyBehaviorTreeDefinition draft)
        {
            var path = AssetDatabase.GetAssetPath(source);
            var oldNodes = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<EnemyBehaviorTreeNodeDefinition>()
                .ToArray();
            var map = new Dictionary<EnemyBehaviorTreeNodeDefinition, EnemyBehaviorTreeNodeDefinition>();

            Undo.RegisterCompleteObjectUndo(source, "Apply Enemy Behavior Tree Draft");
            foreach (var draftNode in draft.Nodes)
            {
                if (draftNode == null || map.ContainsKey(draftNode))
                {
                    continue;
                }

                var newNode = ScriptableObject.CreateInstance(draftNode.GetType()) as EnemyBehaviorTreeNodeDefinition;
                if (newNode == null)
                {
                    continue;
                }

                EditorUtility.CopySerialized(draftNode, newNode);
                newNode.name = draftNode.name.Replace(".Draft", string.Empty);
                AssetDatabase.AddObjectToAsset(newNode, source);
                map[draftNode] = newNode;
            }

            foreach (var newNode in map.Values)
            {
                RemapNodeReferences(newNode, map);
                EditorUtility.SetDirty(newNode);
            }

            var sourceSerialized = new SerializedObject(source);
            var draftSerialized = new SerializedObject(draft);
            sourceSerialized.FindProperty("treeId").stringValue = draftSerialized.FindProperty("treeId").stringValue;
            sourceSerialized.FindProperty("displayName").stringValue = draftSerialized.FindProperty("displayName").stringValue;
            sourceSerialized.FindProperty("ownerId").stringValue = draftSerialized.FindProperty("ownerId").stringValue;
            sourceSerialized.FindProperty("bossMetadataOnly").boolValue = draftSerialized.FindProperty("bossMetadataOnly").boolValue;
            sourceSerialized.FindProperty("rootNode").objectReferenceValue = draft.RootNode != null && map.TryGetValue(draft.RootNode, out var mappedRoot)
                ? mappedRoot
                : null;
            var sourceNodes = sourceSerialized.FindProperty("nodes");
            sourceNodes.arraySize = map.Count;
            var index = 0;
            foreach (var node in map.Values)
            {
                sourceNodes.GetArrayElementAtIndex(index++).objectReferenceValue = node;
            }

            sourceSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(source);

            foreach (var oldNode in oldNodes)
            {
                if (oldNode != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldNode, allowDestroyingAssets: true);
                }
            }
        }

        internal void TrackTemporary(UnityEngine.Object temporary)
        {
            if (temporary != null && !temporaryObjects.Contains(temporary))
            {
                temporary.hideFlags = HideFlags.HideAndDontSave;
                temporaryObjects.Add(temporary);
            }
        }

        private static void RemapNodeReferences(
            UnityEngine.Object target,
            IReadOnlyDictionary<EnemyBehaviorTreeNodeDefinition, EnemyBehaviorTreeNodeDefinition> map)
        {
            if (target == null || map == null || map.Count == 0)
            {
                return;
            }

            var serialized = new SerializedObject(target);
            var property = serialized.GetIterator();
            var enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (property.objectReferenceValue is EnemyBehaviorTreeNodeDefinition oldNode &&
                    map.TryGetValue(oldNode, out var newNode))
                {
                    property.objectReferenceValue = newNode;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public static class EnemyAuthoringProtectionRegistryUtility
    {
        public const string RegistryPath = "Assets/_Hollow/Data/EnemyAuthoring/EnemyAuthoringProtectionRegistry.asset";

        public static EnemyAuthoringProtectionRegistry LoadOrCreate()
        {
            var registry = AssetDatabase.LoadAssetAtPath<EnemyAuthoringProtectionRegistry>(RegistryPath);
            if (registry != null)
            {
                return registry;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(RegistryPath) ?? "Assets/_Hollow/Data/EnemyAuthoring");
            registry = ScriptableObject.CreateInstance<EnemyAuthoringProtectionRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
            AssetDatabase.SaveAssets();
            return registry;
        }

        public static bool IsProtected(UnityEngine.Object asset)
        {
            var guid = GuidFor(asset);
            return !string.IsNullOrWhiteSpace(guid) && LoadOrCreate().ContainsGuid(guid);
        }

        public static void RecordProtected(UnityEngine.Object asset, string notes)
        {
            if (asset == null)
            {
                return;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid))
            {
                return;
            }

            var registry = LoadOrCreate();
            registry.Record(
                guid,
                path,
                OwnerIdFor(asset),
                DataKindFor(asset),
                EnemyAuthoringDraft.Hash(EnemyAuthoringDraft.ToJson(asset)),
                notes,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            EditorUtility.SetDirty(registry);
        }

        private static string GuidFor(UnityEngine.Object asset)
        {
            var path = asset != null ? AssetDatabase.GetAssetPath(asset) : string.Empty;
            return string.IsNullOrWhiteSpace(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }

        private static string OwnerIdFor(UnityEngine.Object asset)
        {
            return asset switch
            {
                EnemyDefinition enemy => enemy.SpawnKind,
                BossDefinition boss => boss.BossId,
                EnemyAttackProfileDefinition attack => attack.AttackId,
                EnemyActionProfileDefinition action => action.ActionId,
                EnemyBehaviorTreeDefinition tree => tree.OwnerId,
                EnemySpacingProfileDefinition spacing => spacing.OwnerSpawnKind,
                _ => asset != null ? asset.name : string.Empty
            };
        }

        private static string DataKindFor(UnityEngine.Object asset)
        {
            return asset switch
            {
                EnemyDefinition => "enemy",
                BossDefinition => "boss",
                EnemyAttackProfileDefinition => "attack",
                EnemyActionProfileDefinition => "action",
                EnemyBehaviorTreeDefinition => "behavior_tree",
                EnemySpacingProfileDefinition => "spacing",
                EnemyGuardProfileDefinition => "guard",
                _ => asset != null ? asset.GetType().Name : "unknown"
            };
        }
    }

    public static class EnemyAuthoringReportWriter
    {
        private const string ReportDirectory = "output/reports/enemy_authoring";

        public static string WriteApplyReport(UnityEngine.Object source, string path, string beforeJson, string afterJson, string notes)
        {
            Directory.CreateDirectory(ReportDirectory);
            var safeName = SanitizeFileName(source != null ? source.name : "enemy_authoring");
            var file = $"{ReportDirectory}/{DateTime.UtcNow:yyyyMMdd_HHmmss}_{safeName}.md";
            File.WriteAllText(file, $@"# Enemy Authoring Apply Report

- Asset: `{path}`
- Type: `{(source != null ? source.GetType().Name : "Unknown")}`
- Before hash: `{EnemyAuthoringDraft.Hash(beforeJson)}`
- After hash: `{EnemyAuthoringDraft.Hash(afterJson)}`
- Notes: {notes}

## Summary

{(string.Equals(beforeJson, afterJson, StringComparison.Ordinal) ? "No changes." : "Draft changes applied and asset marked as manually protected.")}
");
            AssetDatabase.Refresh();
            return file;
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var safe = new string((value ?? "asset").Select(character => invalid.Contains(character) ? '_' : character).ToArray());
            return string.IsNullOrWhiteSpace(safe) ? "asset" : safe;
        }
    }
}
