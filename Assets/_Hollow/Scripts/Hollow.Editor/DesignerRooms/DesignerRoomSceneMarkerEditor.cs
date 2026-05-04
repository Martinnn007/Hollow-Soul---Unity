using System;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.DesignerRooms
{
    [CustomEditor(typeof(DesignerRoomSceneMarker))]
    public sealed class DesignerRoomSceneMarkerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var marker = (DesignerRoomSceneMarker)target;
            EditorGUILayout.LabelField(DesignerRoomAuthoringLocalization.MarkerLabel(marker), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("markerId"), new GUIContent(T("Marker Id", "Id znacznika")));
            var markerKind = serializedObject.FindProperty("markerKind");
            DrawMarkerKindPopup(markerKind);
            DrawRuntimeKindPopup((DesignerRoomSceneMarkerKind)markerKind.enumValueIndex);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"), new GUIContent(T("Display Name Override", "Własna nazwa wyświetlana")));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showLabel"), new GUIContent(T("Show Scene Label", "Pokaż etykietę w scenie")));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("editableByDesigner"), new GUIContent(T("Editable By Designer", "Edytowalne dla designera")));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lockedLayer"), new GUIContent(T("Lock Layer", "Zablokuj warstwe")));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("previewRadiusMeters"), new GUIContent(T("Preview Radius", "Promien podgladu")));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("notes"), new GUIContent(T("Notes", "Notatki")));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(T("Source", "Źródło"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceRoomId"), new GUIContent(T("Source Room Id", "Id pokoju źródłowego")));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceRuntimePath"), new GUIContent(T("Source Runtime Path", "Ścieżka runtime źródła")));

            if ((DesignerRoomSceneMarkerKind)markerKind.enumValueIndex == DesignerRoomSceneMarkerKind.DoorPort)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(T("Door", "Drzwi"), EditorStyles.boldLabel);
                DrawStringPopup(T("Direction", "Kierunek"), serializedObject.FindProperty("doorDirection"), new[] { "north", "south", "east", "west" });
                DrawStringPopup(T("State", "Stan"), serializedObject.FindProperty("doorState"), DesignerRoomSceneAuthoringUtility.RuntimeKindsFor(DesignerRoomSceneMarkerKind.DoorPort));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("doorLaneIndex"), new GUIContent(T("Lane Index", "Numer wejscia")));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hostCellX"), new GUIContent(T("Host Cell X", "Komórka hosta X")));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hostCellZ"), new GUIContent(T("Host Cell Z", "Komórka hosta Z")));
            }

            serializedObject.ApplyModifiedProperties();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(T("Snap", "Przyciągnij")))
                {
                    DesignerRoomSceneAuthoringUtility.SnapMarker(marker);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
                }

                if (GUILayout.Button(T("Open Room Authoring", "Otworz Room Authoring")))
                {
                    DesignerRoomAuthoringWindow.Open();
                }
            }
        }

        private static void DrawMarkerKindPopup(SerializedProperty markerKind)
        {
            var values = (DesignerRoomSceneMarkerKind[])Enum.GetValues(typeof(DesignerRoomSceneMarkerKind));
            var selected = Mathf.Clamp(markerKind.enumValueIndex, 0, values.Length - 1);
            var labels = Array.ConvertAll(values, DesignerRoomAuthoringLocalization.MarkerKindLabel);
            markerKind.enumValueIndex = EditorGUILayout.Popup(T("Marker Kind", "Typ znacznika"), selected, labels);
        }

        private void DrawRuntimeKindPopup(DesignerRoomSceneMarkerKind markerKind)
        {
            var runtimeKind = serializedObject.FindProperty("runtimeKind");
            var options = DesignerRoomSceneAuthoringUtility.RuntimeKindsFor(markerKind);
            if (options.Length == 0)
            {
                EditorGUILayout.PropertyField(runtimeKind);
                return;
            }

            var current = runtimeKind.stringValue;
            var selected = Array.IndexOf(options, current);
            if (selected < 0)
            {
                selected = 0;
            }

            var labels = Array.ConvertAll(options, DesignerRoomAuthoringLocalization.DisplayNameForRuntimeKind);
            runtimeKind.stringValue = options[EditorGUILayout.Popup(T("Runtime Kind", "Typ runtime"), selected, labels)];
        }

        private static void DrawStringPopup(string label, SerializedProperty property, string[] options)
        {
            var selected = Array.IndexOf(options, property.stringValue);
            if (selected < 0)
            {
                selected = 0;
            }

            var labels = Array.ConvertAll(options, DesignerRoomAuthoringLocalization.OptionLabel);
            property.stringValue = options[EditorGUILayout.Popup(label, selected, labels)];
        }

        private static string T(string english, string polish)
        {
            return DesignerRoomAuthoringLocalization.T(english, polish);
        }
    }
}
