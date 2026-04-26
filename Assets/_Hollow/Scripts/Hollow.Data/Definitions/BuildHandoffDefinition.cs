using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Build Handoff", fileName = "BuildHandoff")]
    public sealed class BuildHandoffDefinition : HollowDefinition
    {
        [SerializeField] private string prototypeVersion = "M11 Prototype Lock";
        [SerializeField] private string unityVersion = string.Empty;
        [SerializeField] private string lastVerifiedMilestone = "M11";
        [SerializeField] private string[] requiredScenes = Array.Empty<string>();
        [SerializeField] private string[] validationCommands = Array.Empty<string>();
        [SerializeField, TextArea] private string[] handoffNotes = Array.Empty<string>();

        public string PrototypeVersion => prototypeVersion;

        public string UnityVersion => unityVersion;

        public string LastVerifiedMilestone => lastVerifiedMilestone;

        public string[] RequiredScenes => requiredScenes;

        public string[] ValidationCommands => validationCommands;

        public string[] HandoffNotes => handoffNotes;

        public void Configure(
            string nextPrototypeVersion,
            string nextUnityVersion,
            string nextLastVerifiedMilestone,
            string[] nextRequiredScenes,
            string[] nextValidationCommands,
            string[] nextHandoffNotes)
        {
            prototypeVersion = nextPrototypeVersion ?? string.Empty;
            unityVersion = nextUnityVersion ?? string.Empty;
            lastVerifiedMilestone = nextLastVerifiedMilestone ?? string.Empty;
            requiredScenes = nextRequiredScenes ?? Array.Empty<string>();
            validationCommands = nextValidationCommands ?? Array.Empty<string>();
            handoffNotes = nextHandoffNotes ?? Array.Empty<string>();
        }
    }
}
