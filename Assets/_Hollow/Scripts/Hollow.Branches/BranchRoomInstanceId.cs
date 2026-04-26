using System;

namespace Hollow.Branches
{
    [Serializable]
    public readonly struct BranchRoomInstanceId : IEquatable<BranchRoomInstanceId>
    {
        public static readonly BranchRoomInstanceId Unknown = new("unknown");

        public BranchRoomInstanceId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? "unknown" : value;
        }

        public string Value { get; }

        public bool Equals(BranchRoomInstanceId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is BranchRoomInstanceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value != null ? Value.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(BranchRoomInstanceId left, BranchRoomInstanceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BranchRoomInstanceId left, BranchRoomInstanceId right)
        {
            return !left.Equals(right);
        }
    }
}
