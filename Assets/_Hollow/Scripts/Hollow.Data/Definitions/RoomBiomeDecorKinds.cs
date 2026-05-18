namespace Hollow.Data.Definitions
{
    public static class RoomBiomeDecorKinds
    {
        public const string GrassTuft = "decorGrassTuft";
        public const string CrystalCluster = "decorCrystalCluster";
        public const string SmallTree = "decorSmallTree";
        public const string StoneRuin = "decorStoneRuin";

        public static string Normalize(string decorKind)
        {
            return string.IsNullOrWhiteSpace(decorKind) ? string.Empty : decorKind.Trim();
        }

        public static bool IsKnown(string decorKind)
        {
            return TryResolveDefaultPrefabRole(decorKind, out _);
        }

        public static bool TryResolveDefaultPrefabRole(string decorKind, out PresentationPrefabRole role)
        {
            switch (Normalize(decorKind))
            {
                case GrassTuft:
                    role = PresentationPrefabRole.DecorGrassTuft;
                    return true;
                case CrystalCluster:
                    role = PresentationPrefabRole.DecorCrystalCluster;
                    return true;
                case SmallTree:
                    role = PresentationPrefabRole.DecorSmallTree;
                    return true;
                case StoneRuin:
                    role = PresentationPrefabRole.DecorStoneRuin;
                    return true;
                default:
                    role = default;
                    return false;
            }
        }
    }
}
