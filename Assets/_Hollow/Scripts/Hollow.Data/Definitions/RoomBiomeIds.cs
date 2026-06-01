namespace Hollow.Data.Definitions
{
    public static class RoomBiomeIds
    {
        public const string HollowThreshold = "hollow_threshold";
        public const string VerdantRuins = "verdant_ruins";
        public const string CorruptedAshenShrine = "corrupted_ashen_shrine";
        public const string BeforeTeeth = "before_teeth";
        public const string SunkenCartouche = "sunken_cartouche";
        public const string RustChoir = "rust_choir";

        public static string Normalize(string biomeId)
        {
            return string.IsNullOrWhiteSpace(biomeId)
                ? HollowThreshold
                : biomeId.Trim().Replace('-', '_').ToLowerInvariant();
        }

        public static bool Matches(string left, string right)
        {
            return Normalize(left) == Normalize(right);
        }
    }
}
