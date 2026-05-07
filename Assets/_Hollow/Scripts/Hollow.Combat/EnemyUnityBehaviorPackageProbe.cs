using Unity.Behavior;

namespace Hollow.Combat
{
    public static class EnemyUnityBehaviorPackageProbe
    {
        public const string PackageName = "com.unity.behavior";
        public const string RequiredVersion = "1.0.13";

        public static bool TypesAvailable => typeof(BehaviorGraphAgent) != null && typeof(BehaviorGraph) != null;

        public static string RuntimeAssemblyName => typeof(BehaviorGraphAgent).Assembly.GetName().Name;
    }
}
