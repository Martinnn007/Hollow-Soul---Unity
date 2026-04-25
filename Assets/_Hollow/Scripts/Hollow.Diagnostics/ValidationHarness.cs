using System.Collections.Generic;

namespace Hollow.Diagnostics
{
    public sealed class ValidationReport
    {
        private readonly List<string> errors = new();

        public IReadOnlyList<string> Errors => errors;

        public bool Passed => errors.Count == 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                errors.Add(message);
            }
        }
    }

    public static class ValidationHarness
    {
        public static ValidationReport RunMilestone0Smoke()
        {
            return new ValidationReport();
        }
    }
}
