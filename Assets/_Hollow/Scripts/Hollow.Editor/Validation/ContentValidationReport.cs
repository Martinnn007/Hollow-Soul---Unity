using System.Collections.Generic;

namespace Hollow.Editor.Validation
{
    public sealed class ContentValidationReport
    {
        private readonly List<string> failures = new();
        private readonly List<string> warnings = new();

        public IReadOnlyList<string> Failures => failures;

        public IReadOnlyList<string> Warnings => warnings;

        public bool IsValid => failures.Count == 0;

        public void AddFailure(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                failures.Add(message);
            }
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                warnings.Add(message);
            }
        }
    }
}
