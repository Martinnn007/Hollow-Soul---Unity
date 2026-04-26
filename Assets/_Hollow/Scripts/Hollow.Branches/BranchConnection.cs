namespace Hollow.Branches
{
    public sealed class BranchConnection
    {
        public BranchConnection(BranchRoomId fromRoomId, BranchRoomId toRoomId, string fromDirection, string toDirection)
            : this(fromRoomId, toRoomId, fromDirection, toDirection, string.Empty, string.Empty)
        {
        }

        public BranchConnection(
            BranchRoomId fromRoomId,
            BranchRoomId toRoomId,
            string fromDirection,
            string toDirection,
            string fromPortId,
            string toPortId)
            : this(fromRoomId, toRoomId, fromDirection, toDirection, fromPortId, toPortId, BranchConnectionLockKind.None)
        {
        }

        public BranchConnection(
            BranchRoomId fromRoomId,
            BranchRoomId toRoomId,
            string fromDirection,
            string toDirection,
            string fromPortId,
            string toPortId,
            BranchConnectionLockKind lockKind)
        {
            FromRoomId = fromRoomId;
            ToRoomId = toRoomId;
            FromDirection = fromDirection;
            ToDirection = toDirection;
            FromPortId = fromPortId ?? string.Empty;
            ToPortId = toPortId ?? string.Empty;
            LockKind = lockKind;
        }

        public BranchRoomId FromRoomId { get; }

        public BranchRoomId ToRoomId { get; }

        public string FromDirection { get; }

        public string ToDirection { get; }

        public string FromPortId { get; }

        public string ToPortId { get; }

        public BranchConnectionLockKind LockKind { get; private set; }

        public bool HasExplicitPorts => !string.IsNullOrWhiteSpace(FromPortId) && !string.IsNullOrWhiteSpace(ToPortId);

        public void SetLockKind(BranchConnectionLockKind lockKind)
        {
            LockKind = lockKind;
        }
    }
}
