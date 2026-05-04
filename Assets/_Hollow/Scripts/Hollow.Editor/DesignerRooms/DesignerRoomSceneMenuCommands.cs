using System;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hollow.Editor.DesignerRooms
{
    public static class DesignerRoomSceneMenuCommands
    {
        [MenuItem("Hollow/Designer Rooms/Export Active DesignerRoom Scene")]
        public static void ExportActiveDesignerRoomScene()
        {
            try
            {
                var path = DesignerRoomSceneAuthoringUtility.ExportScene(SceneManager.GetActiveScene());
                AssetDatabase.Refresh();
                Debug.Log($"Exported DesignerRoom scene draft to {path}");
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message);
                EditorUtility.DisplayDialog("DesignerRoom Export Failed", exception.Message, "OK");
            }
        }

        [MenuItem("Hollow/Designer Rooms/Export All DesignerRooms")]
        public static void ExportAllDesignerRooms()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var activePath = SceneManager.GetActiveScene().path;
            var exported = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { DesignerRoomSceneAuthoringUtility.DesignerRoomsDirectory }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path);
                DesignerRoomSceneAuthoringUtility.ExportScene(scene);
                exported++;
            }

            if (!string.IsNullOrWhiteSpace(activePath))
            {
                EditorSceneManager.OpenScene(activePath);
            }

            AssetDatabase.Refresh();
            Debug.Log($"Exported {exported} DesignerRoom scene draft(s).");
        }

        [MenuItem("Hollow/Designer Rooms/Snap Selected")]
        public static void SnapSelected()
        {
            foreach (var gameObject in Selection.gameObjects)
            {
                var marker = gameObject.GetComponent<DesignerRoomSceneMarker>();
                if (marker == null)
                {
                    continue;
                }

                DesignerRoomSceneAuthoringUtility.SnapMarker(marker);
                EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
            }

            SceneView.RepaintAll();
        }

        [MenuItem("Hollow/Designer Rooms/Snap All In Active Scene")]
        public static void SnapAllInActiveScene()
        {
            DesignerRoomSceneAuthoringUtility.SnapAllInScene(SceneManager.GetActiveScene());
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            SceneView.RepaintAll();
        }

        [MenuItem("Hollow/Designer Rooms/Build Visual Preview")]
        public static void BuildVisualPreview()
        {
            try
            {
                DesignerRoomSceneVisualPreviewBuilder.BuildPreview(SceneManager.GetActiveScene());
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message);
                EditorUtility.DisplayDialog("DesignerRoom Preview Failed", exception.Message, "OK");
            }
        }

        [MenuItem("Hollow/Designer Rooms/Clear Visual Preview")]
        public static void ClearVisualPreview()
        {
            DesignerRoomSceneVisualPreviewBuilder.ClearPreview(SceneManager.GetActiveScene());
        }

        [MenuItem("Hollow/Designer Rooms/Diff Active Scene Against Source")]
        public static void DiffActiveSceneAgainstSource()
        {
            Debug.Log(DesignerRoomSceneAuthoringUtility.DiffAgainstSource(SceneManager.GetActiveScene()));
        }

        [MenuItem("Hollow/Designer Rooms/Refresh Active Scene From Source JSON")]
        public static void RefreshActiveSceneFromSourceJson()
        {
            var root = DesignerRoomSceneAuthoringUtility.FindRoomRoot(SceneManager.GetActiveScene());
            if (root == null)
            {
                EditorUtility.DisplayDialog("Refresh Failed", "Active scene is missing a DesignerRoom root marker.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Refresh Scene From Source JSON",
                    "This removes current editable markers and recreates them from the source runtime JSON.",
                    "Refresh",
                    "Cancel"))
            {
                return;
            }

            try
            {
                DesignerRoomSceneAuthoringUtility.RefreshSceneFromSource(root);
                EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message);
                EditorUtility.DisplayDialog("Refresh Failed", exception.Message, "OK");
            }
        }
    }
}
