using System;

namespace Hollow.Branches
{
    [Serializable]
    public readonly struct BranchRoomId : IEquatable<BranchRoomId>
    {
        public static readonly BranchRoomId Origin = new("origin");
        public static readonly BranchRoomId North = new("north");
        public static readonly BranchRoomId South = new("south");
        public static readonly BranchRoomId East = new("east");
        public static readonly BranchRoomId West = new("west");

        public BranchRoomId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? "unknown" : value;
        }

        public string Value { get; }

        public bool Equals(BranchRoomId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is BranchRoomId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value != null ? Value.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(BranchRoomId left, BranchRoomId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BranchRoomId left, BranchRoomId right)
        {
            return !left.Equals(right);
        }
    }
}
