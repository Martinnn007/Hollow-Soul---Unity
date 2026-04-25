using System;

namespace Hollow.Persistence
{
    [Serializable]
    public readonly struct ProfileSlotId : IEquatable<ProfileSlotId>
    {
        public ProfileSlotId(int value)
        {
            if (value < 0 || value >= ProfileSlotConstants.MaxSlots)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Profile slot index is outside the supported slot range.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(ProfileSlotId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is ProfileSlotId other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => $"Slot {Value + 1}";

        public static bool operator ==(ProfileSlotId left, ProfileSlotId right) => left.Equals(right);

        public static bool operator !=(ProfileSlotId left, ProfileSlotId right) => !left.Equals(right);
    }
}
