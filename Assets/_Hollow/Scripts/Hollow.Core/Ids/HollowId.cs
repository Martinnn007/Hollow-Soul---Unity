using System;

namespace Hollow.Core
{
    [Serializable]
    public readonly struct HollowId : IEquatable<HollowId>
    {
        public HollowId(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; }

        public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

        public bool Equals(HollowId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is HollowId other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value;

        public static bool operator ==(HollowId left, HollowId right) => left.Equals(right);

        public static bool operator !=(HollowId left, HollowId right) => !left.Equals(right);
    }
}
