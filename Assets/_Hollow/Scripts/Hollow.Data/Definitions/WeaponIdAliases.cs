namespace Hollow.Data.Definitions
{
    public static class WeaponIdAliases
    {
        public const string StarterPistolId = "starter_pistol";

        public static string Normalize(string weaponId)
        {
            return (weaponId ?? string.Empty).Trim() switch
            {
                "starter_bow" => StarterPistolId,
                "bone_bow" => "bone_pistol",
                "dragon_bow" => "dragon_pistol",
                var normalized => normalized
            };
        }

        public static string NormalizeDisplayName(string weaponId, string displayName)
        {
            return (weaponId ?? string.Empty).Trim() switch
            {
                "starter_bow" => "Basic Pistol",
                "bone_bow" => "Bone Pistol",
                "dragon_bow" => "Dragon Pistol",
                _ => displayName ?? string.Empty
            };
        }
    }
}
