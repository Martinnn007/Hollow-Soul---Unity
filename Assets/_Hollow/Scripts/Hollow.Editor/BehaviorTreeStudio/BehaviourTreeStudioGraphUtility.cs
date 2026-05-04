using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.EnemyAuthoring;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.BehaviorTreeStudio
{
    public static class BehaviourTreeStudioGraphUtility
    {
        public static EnemyBehaviorTreeNodeDefinition RootFor(UnityEngine.Object treeLike)
        {
            return treeLike switch
            {
                EnemyBehaviorTreeDefinition tree => tree.RootNode,
                EnemyBehaviorTreeTemplateDefinition template => template.RootNode,
                _ => null
            };
        }

        public static IReadOnlyList<EnemyBehaviorTreeNodeDefinition> NodesFor(UnityEngine.Object treeLike)
        {
            return treeLike switch
            {
                EnemyBehaviorTreeDefinition tree => tree.Nodes,
                EnemyBehaviorTreeTemplateDefinition template => template.Nodes,
                _ => Array.Empty<EnemyBehaviorTreeNodeDefinition>()
            };
        }

        public static string DisplayNameFor(UnityEngine.Object treeLike)
        {
            return treeLike switch
            {
                EnemyBehaviorTreeDefinition tree => tree.DisplayName,
                EnemyBehaviorTreeTemplateDefinition template => template.DisplayName,
                _ => treeLike != null ? treeLike.name : "(none)"
            };
        }

        public static TNode AddNode<TNode>(UnityEngine.Object treeLike, EnemyAuthoringDraft draft, string prefix)
            where TNode : EnemyBehaviorTreeNodeDefinition
        {
            var node = ScriptableObject.CreateInstance<TNode>();
            var count = NodesFor(treeLike).Count;
            node.ConfigureNodeId($"{prefix}_{count + 1:00}");
            draft?.TrackTemporary(node);
            AddExistingNode(treeLike, node, setRootIfMissing: true);
            return node;
        }

        public static EnemyBehaviorTreeNodeDefinition DuplicateNode(UnityEngine.Object treeLike, EnemyAuthoringDraft draft, EnemyBehaviorTreeNodeDefinition source)
        {
            if (treeLike == null || source == null)
            {
                return null;
            }

            var clone = ScriptableObject.CreateInstance(source.GetType()) as EnemyBehaviorTreeNodeDefinition;
            if (clone == null)
            {
                return null;
            }

            EditorUtility.CopySerialized(source, clone);
            clone.ConfigureNodeId(UniqueNodeId(treeLike, $"{source.NodeId}_copy"));
            draft?.TrackTemporary(clone);
            ClearCompositeChildren(clone);
            AddExistingNode(treeLike, clone, setRootIfMissing: false);
            return clone;
        }

        public static void RemoveNode(UnityEngine.Object treeLike, EnemyBehaviorTreeNodeDefinition node)
        {
            if (treeLike == null || node == null)
            {
                return;
            }

            foreach (var candidate in NodesFor(treeLike))
            {
                DisconnectChild(candidate, node);
            }

            Undo.RegisterCompleteObjectUndo(treeLike, "Remove Behavior Tree Node");
            var serialized = new SerializedObject(treeLike);
            var nodes = serialized.FindProperty("nodes");
            for (var index = nodes.arraySize - 1; index >= 0; index--)
            {
                if (nodes.GetArrayElementAtIndex(index).objectReferenceValue == node)
                {
                    nodes.DeleteArrayElementAtIndex(index);
                }
            }

            var root = serialized.FindProperty("rootNode");
            if (root.objectReferenceValue == node)
            {
                root.objectReferenceValue = null;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetRoot(UnityEngine.Object treeLike, EnemyBehaviorTreeNodeDefinition node)
        {
            if (treeLike == null || node == null)
            {
                return;
            }

            Undo.RegisterCompleteObjectUndo(treeLike, "Set Behavior Tree Root");
            var serialized = new SerializedObject(treeLike);
            serialized.FindProperty("rootNode").objectReferenceValue = node;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static bool ConnectChild(EnemyBehaviorTreeNodeDefinition parent, EnemyBehaviorTreeNodeDefinition child)
        {
            if (parent == null || child == null || parent == child)
            {
                return false;
            }

            var serialized = new SerializedObject(parent);
            var children = serialized.FindProperty("children");
            if (children == null)
            {
                return false;
            }

            Undo.RegisterCompleteObjectUndo(parent, "Connect Behavior Tree Node");
            if (parent is EnemyBehaviorWeightedSelectorNodeDefinition)
            {
                for (var index = 0; index < children.arraySize; index++)
                {
                    if (children.GetArrayElementAtIndex(index).FindPropertyRelative("child").objectReferenceValue == child)
                    {
                        return false;
                    }
                }

                children.arraySize++;
                var row = children.GetArrayElementAtIndex(children.arraySize - 1);
                row.FindPropertyRelative("child").objectReferenceValue = child;
                row.FindPropertyRelative("weight").floatValue = 1f;
            }
            else
            {
                for (var index = 0; index < children.arraySize; index++)
                {
                    if (children.GetArrayElementAtIndex(index).objectReferenceValue == child)
                    {
                        return false;
                    }
                }

                children.arraySize++;
                children.GetArrayElementAtIndex(children.arraySize - 1).objectReferenceValue = child;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        public static bool DisconnectChild(EnemyBehaviorTreeNodeDefinition parent, EnemyBehaviorTreeNodeDefinition child)
        {
            if (parent == null || child == null)
            {
                return false;
            }

            var serialized = new SerializedObject(parent);
            var children = serialized.FindProperty("children");
            if (children == null)
            {
                return false;
            }

            Undo.RegisterCompleteObjectUndo(parent, "Disconnect Behavior Tree Node");
            var changed = false;
            for (var index = children.arraySize - 1; index >= 0; index--)
            {
                var reference = parent is EnemyBehaviorWeightedSelectorNodeDefinition
                    ? children.GetArrayElementAtIndex(index).FindPropertyRelative("child").objectReferenceValue
                    : children.GetArrayElementAtIndex(index).objectReferenceValue;
                if (reference != child)
                {
                    continue;
                }

                children.DeleteArrayElementAtIndex(index);
                changed = true;
            }

            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return changed;
        }

        public static void ReplaceTreeDraftWithTemplate(
            EnemyBehaviorTreeDefinition tree,
            EnemyBehaviorTreeTemplateDefinition template,
            EnemyAuthoringDraft draft)
        {
            if (tree == null || template == null)
            {
                return;
            }

            var map = CloneTemplateNodes(template, draft);
            foreach (var node in map.Values)
            {
                RemapNodeReferences(node, map);
            }

            var serialized = new SerializedObject(tree);
            Undo.RegisterCompleteObjectUndo(tree, "Apply Behavior Tree Template");
            serialized.FindProperty("rootNode").objectReferenceValue = template.RootNode != null && map.TryGetValue(template.RootNode, out var root)
                ? root
                : null;
            var nodes = serialized.FindProperty("nodes");
            nodes.arraySize = map.Count;
            var index = 0;
            foreach (var node in map.Values)
            {
                nodes.GetArrayElementAtIndex(index++).objectReferenceValue = node;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static Dictionary<EnemyBehaviorTreeNodeDefinition, Rect> AutoLayout(UnityEngine.Object treeLike)
        {
            var nodes = NodesFor(treeLike).Where(node => node != null).ToArray();
            var root = RootFor(treeLike);
            var positions = new Dictionary<EnemyBehaviorTreeNodeDefinition, Rect>();
            if (root == null)
            {
                for (var index = 0; index < nodes.Length; index++)
                {
                    positions[nodes[index]] = new Rect(index % 4 * 240f, index / 4 * 150f, 210f, 96f);
                }

                return positions;
            }

            var levels = new Dictionary<EnemyBehaviorTreeNodeDefinition, int>();
            var queue = new Queue<EnemyBehaviorTreeNodeDefinition>();
            queue.Enqueue(root);
            levels[root] = 0;
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                foreach (var child in node.Children)
                {
                    if (child == null || levels.ContainsKey(child))
                    {
                        continue;
                    }

                    levels[child] = levels[node] + 1;
                    queue.Enqueue(child);
                }
            }

            foreach (var missing in nodes.Where(node => !levels.ContainsKey(node)))
            {
                levels[missing] = levels.Count == 0 ? 0 : levels.Values.Max() + 1;
            }

            foreach (var group in levels.GroupBy(pair => pair.Value).OrderBy(group => group.Key))
            {
                var row = group.Select(pair => pair.Key).OrderBy(node => node.NodeId, StringComparer.Ordinal).ToArray();
                for (var index = 0; index < row.Length; index++)
                {
                    positions[row[index]] = new Rect(index * 250f, group.Key * 155f, 220f, 104f);
                }
            }

            return positions;
        }

        private static void AddExistingNode(UnityEngine.Object treeLike, EnemyBehaviorTreeNodeDefinition node, bool setRootIfMissing)
        {
            var serialized = new SerializedObject(treeLike);
            Undo.RegisterCompleteObjectUndo(treeLike, "Add Behavior Tree Node");
            var nodes = serialized.FindProperty("nodes");
            nodes.arraySize++;
            nodes.GetArrayElementAtIndex(nodes.arraySize - 1).objectReferenceValue = node;
            var root = serialized.FindProperty("rootNode");
            if (setRootIfMissing && root.objectReferenceValue == null)
            {
                root.objectReferenceValue = node;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string UniqueNodeId(UnityEngine.Object treeLike, string desired)
        {
            var existing = NodesFor(treeLike).Select(node => node.NodeId).ToHashSet(StringComparer.Ordinal);
            if (!existing.Contains(desired))
            {
                return desired;
            }

            for (var index = 2; index < 999; index++)
            {
                var candidate = $"{desired}_{index}";
                if (!existing.Contains(candidate))
                {
                    return candidate;
                }
            }

            return $"{desired}_{Guid.NewGuid():N}";
        }

        private static Dictionary<EnemyBehaviorTreeNodeDefinition, EnemyBehaviorTreeNodeDefinition> CloneTemplateNodes(
            EnemyBehaviorTreeTemplateDefinition template,
            EnemyAuthoringDraft draft)
        {
            var map = new Dictionary<EnemyBehaviorTreeNodeDefinition, EnemyBehaviorTreeNodeDefinition>();
            foreach (var sourceNode in template.Nodes)
            {
                var clone = ScriptableObject.CreateInstance(sourceNode.GetType()) as EnemyBehaviorTreeNodeDefinition;
                if (clone == null)
                {
                    continue;
                }

                EditorUtility.CopySerialized(sourceNode, clone);
                clone.name = sourceNode.name;
                draft?.TrackTemporary(clone);
                map[sourceNode] = clone;
            }

            return map;
        }

        private static void ClearCompositeChildren(EnemyBehaviorTreeNodeDefinition node)
        {
            if (node is not EnemyBehaviorSelectorNodeDefinition &&
                node is not EnemyBehaviorSequenceNodeDefinition &&
                node is not EnemyBehaviorWeightedSelectorNodeDefinition)
            {
                return;
            }

            var serialized = new SerializedObject(node);
            var children = serialized.FindProperty("children");
            if (children != null)
            {
                children.arraySize = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void RemapNodeReferences(
            UnityEngine.Object target,
            IReadOnlyDictionary<EnemyBehaviorTreeNodeDefinition, EnemyBehaviorTreeNodeDefinition> map)
        {
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
}
