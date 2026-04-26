using System;
using System.Collections.Generic;

namespace Hollow.Editor.Build
{
    [Serializable]
    public sealed class BuildArtifactManifest
    {
        public string manifestId = string.Empty;
        public string generatedAtUtc = string.Empty;
        public string prototypeVersion = string.Empty;
        public string unityVersion = string.Empty;
        public string gitBranch = string.Empty;
        public string gitCommit = string.Empty;
        public string buildTarget = string.Empty;
        public string buildResult = string.Empty;
        public string buildPath = string.Empty;
        public string auditResult = string.Empty;
        public string auditReportPath = string.Empty;
        public string addressablesProfile = string.Empty;
        public List<string> scenes = new();
    }
}
