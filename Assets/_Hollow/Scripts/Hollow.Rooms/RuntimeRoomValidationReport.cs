using System.Collections.Generic;

namespace Hollow.Rooms
{
    public sealed class RuntimeRoomValidationReport
    {
        private readonly List<string> errors = new();

        public IReadOnlyList<string> Errors => errors;

        public bool IsValid => errors.Count == 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                errors.Add(message);
            }
        }

        public string Summary()
        {
            return IsValid ? "Runtime room placement valid" : string.Join("; ", errors);
        }
    }
}
