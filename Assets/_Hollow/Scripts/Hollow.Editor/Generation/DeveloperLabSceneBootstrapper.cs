using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    [InitializeOnLoad]
    public static class DeveloperLabSceneBootstrapper
    {
        private const string SessionKey = "Hollow.M66.DeveloperLabSceneBootstrapper.Attempted";

        static DeveloperLabSceneBootstrapper()
        {
            EditorApplication.delayCall += GenerateMissingScenesOnce;
        }

        private static void GenerateMissingScenesOnce()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += GenerateMissingScenesOnce;
                return;
            }

            SessionState.SetBool(SessionKey, true);
            if (DeveloperLabSceneGenerator.ScenePaths.Any(File.Exists))
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Developer Lab authoring scenes are missing. Leave Play Mode and run Hollow/Developer Lab/Generate Developer Lab Scenes.");
                return;
            }

            DeveloperLabSceneGenerator.GenerateScenes();
            Debug.Log("Created missing Developer Lab authoring scenes under Assets/_Hollow/Scenes/DeveloperLab. Use Hollow/Developer Lab/Export All Developer Lab Scenes after editing.");
        }
    }
}
