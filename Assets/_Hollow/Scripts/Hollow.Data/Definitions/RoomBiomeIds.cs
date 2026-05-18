namespace Hollow.Data.Definitions
{
    public static class RoomBiomeIds
    {
        public const string HollowThreshold = "hollow_threshold";
        public const string VerdantRuins = "verdant_ruins";

        public static string Normalize(string biomeId)
        {
            return string.IsNullOrWhiteSpace(biomeId) ? HollowThreshold : biomeId.Trim();
        }

        public static bool Matches(string left, string right)
        {
            return Normalize(left) == Normalize(right);
        }
    }
}
