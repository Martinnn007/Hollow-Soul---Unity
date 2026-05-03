using System.Diagnostics;
using System.IO;
using System.Linq;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone78EnemyActionBibleTests
    {
        [Test]
        public void CatalogueMarkdownExistsAndCarriesRequiredCoverage()
        {
            Assert.IsTrue(File.Exists(Milestone78AssetGenerator.DocsPath), Milestone78AssetGenerator.DocsPath);
            var markdown = File.ReadAllText(Milestone78AssetGenerator.DocsPath);
            StringAssert.Contains("Enemy Action Bible", markdown);
            StringAssert.Contains("Bite", markdown);
            StringAssert.Contains("Overhead Slash", markdown);
            StringAssert.Contains("Arrow Volley", markdown);
            StringAssert.Contains("Beam", markdown);
            StringAssert.Contains("Teleport", markdown);
            StringAssert.Contains("Soul Drain", markdown);
            StringAssert.Contains("behavior tree", markdown);
            StringAssert.Contains("body-only", markdown);
            StringAssert.Contains("weapon-user", markdown);
            StringAssert.Contains("ghost/soul", markdown);
            StringAssert.Contains("mechanical", markdown);
            StringAssert.Contains("boss-scale", markdown);

            var cardCount = markdown.Split('\n').Count(line => line.StartsWith("### "));
            Assert.GreaterOrEqual(cardCount, Milestone78AssetGenerator.MinimumActionCards);
            Assert.LessOrEqual(cardCount, Milestone78AssetGenerator.MaximumActionCards);
        }

        [Test]
        public void PdfExtractsRequiredTextAndValidatorPasses()
        {
            Assert.IsTrue(File.Exists(Milestone78AssetGenerator.PdfPath), Milestone78AssetGenerator.PdfPath);
            AssertPdfExtractsRequiredText();
            Assert.IsTrue(Milestone78Validator.Validate());
        }

        private static void AssertPdfExtractsRequiredText()
        {
            var scriptPath = Path.GetFullPath(Milestone78AssetGenerator.VerifyScriptPath);
            Assert.IsTrue(File.Exists(scriptPath), scriptPath);
            var startInfo = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            Assert.NotNull(process);
            if (!process.WaitForExit(15000))
            {
                process.Kill();
                Assert.Fail("Timed out while verifying the M78 PDF with pypdf.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            Debug.Log(output);
            Assert.AreEqual(0, process.ExitCode, error);
        }
    }
}
