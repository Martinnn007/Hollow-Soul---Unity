namespace Hollow.Branches
{
    public sealed class BranchConnection
    {
        public BranchConnection(BranchRoomId fromRoomId, BranchRoomId toRoomId, string fromDirection, string toDirection)
        {
            FromRoomId = fromRoomId;
            ToRoomId = toRoomId;
            FromDirection = fromDirection;
            ToDirection = toDirection;
        }

        public BranchRoomId FromRoomId { get; }

        public BranchRoomId ToRoomId { get; }

        public string FromDirection { get; }

        public string ToDirection { get; }
    }
}
