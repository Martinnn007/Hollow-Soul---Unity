namespace Hollow.Branches
{
    public static class DeveloperLabDefinition
    {
        public const string BranchId = "m55_developer_inspection_branch_v1";
        public const int Seed = 55001;
        public const int RoomCount = 10;
        public const string WideRoomAssetId = "combat_macro_wide_2x1";

        public static readonly string[] RoomAssetIds =
        {
            "developer_lab_01_environment_basics",
            "developer_lab_02_economy_sustain",
            "developer_lab_03_build_pickups",
            "developer_lab_04_enemy_gallery",
            "developer_lab_05_projectile_vfx_gallery",
            "developer_lab_06_hazard_physics_lane",
            "developer_lab_07_progression_props",
            "developer_lab_08_world1_boss_gallery",
            "developer_lab_09_world2_boss_gallery",
            "developer_lab_10_world3_boss_gallery"
        };

        public static readonly string[] RoomTitles =
        {
            "01 Environment Basics",
            "02 Economy + Sustain",
            "03 Build Pickups",
            "04 Enemy Gallery",
            "05 Projectile + VFX Gallery",
            "06 Live Hazard Lane",
            "07 Hub + Progression Props",
            "08 World 1 Boss Gallery",
            "09 World 2 Boss Gallery",
            "10 World 3 Boss Gallery"
        };
    }
}
