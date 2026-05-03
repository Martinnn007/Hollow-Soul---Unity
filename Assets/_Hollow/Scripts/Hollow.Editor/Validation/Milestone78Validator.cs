using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone78Validator
    {
        private static readonly string[] RequiredText =
        {
            "Enemy Action Bible",
            "Bite",
            "Overhead Slash",
            "Arrow Volley",
            "Beam",
            "Teleport",
            "Soul Drain",
            "contact",
            "hazard",
            "behavior tree"
        };

        private static readonly string[] RequiredCoverage =
        {
            "body-only",
            "weapon-user",
            "ranged",
            "magic",
            "ghost/soul",
            "mechanical",
            "boss-scale"
        };

        [MenuItem("Hollow/Validation/Run Milestone 78 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateMarkdownContract(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 78 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateFiles(List<string> failures)
        {
            ExpectFile(Milestone78AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone78AssetGenerator.PdfPath, failures);
            ExpectFile(Milestone78AssetGenerator.ReportPath, failures);
            ExpectFile(Milestone78AssetGenerator.GeneratorScriptPath, failures);
            ExpectFile(Milestone78AssetGenerator.VerifyScriptPath, failures);
        }

        private static void ValidateMarkdownContract(List<string> failures)
        {
            if (!File.Exists(Milestone78AssetGenerator.DocsPath))
            {
                return;
            }

            var markdown = File.ReadAllText(Milestone78AssetGenerator.DocsPath);
            foreach (var required in RequiredText.Concat(RequiredCoverage))
            {
                if (markdown.IndexOf(required, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    failures.Add($"M78 action bible is missing required text `{required}`.");
                }
            }

            var actionCardCount = markdown
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Count(line => line.StartsWith("### ", StringComparison.Ordinal));
            if (actionCardCount < Milestone78AssetGenerator.MinimumActionCards ||
                actionCardCount > Milestone78AssetGenerator.MaximumActionCards)
            {
                failures.Add($"M78 action bible must contain {Milestone78AssetGenerator.MinimumActionCards}-{Milestone78AssetGenerator.MaximumActionCards} action cards; found {actionCardCount}.");
            }

            foreach (var category in new[] { "Body", "Weapon", "Ranged", "Projectile", "Magic", "Movement", "Defense", "Summon", "Hazard", "Ghost/Soul", "Mechanical", "Boss-Scale" })
            {
                if (markdown.IndexOf($"- Category: {category}", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    failures.Add($"M78 action bible is missing category `{category}`.");
                }
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M78 file: {path}");
            }
        }
    }
}
