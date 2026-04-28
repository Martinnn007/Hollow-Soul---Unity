using System;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class MilestoneValidationExitPolicy
    {
        public static bool SuppressEditorExit { get; set; }

        public static bool ShouldExitForValidate()
        {
            if (!Application.isBatchMode || SuppressEditorExit)
            {
                return false;
            }

            foreach (var argument in Environment.GetCommandLineArgs())
            {
                if (string.Equals(argument, "-runTests", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
