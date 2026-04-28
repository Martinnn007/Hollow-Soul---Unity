using UnityEngine;

namespace Hollow.Rewards
{
    public readonly struct PickupRevealModel
    {
        public PickupRevealModel(
            int sequence,
            string title,
            string category,
            string effectText,
            string rarityText,
            string glyph,
            Color rarityColor,
            string replacementText,
            string toastText)
        {
            Sequence = sequence;
            Title = title ?? string.Empty;
            Category = category ?? string.Empty;
            EffectText = effectText ?? string.Empty;
            RarityText = rarityText ?? string.Empty;
            Glyph = glyph ?? string.Empty;
            RarityColor = rarityColor;
            ReplacementText = replacementText ?? string.Empty;
            ToastText = toastText ?? string.Empty;
        }

        public int Sequence { get; }

        public string Title { get; }

        public string Category { get; }

        public string EffectText { get; }

        public string RarityText { get; }

        public string Glyph { get; }

        public Color RarityColor { get; }

        public string ReplacementText { get; }

        public string ToastText { get; }

        public bool IsEmpty => Sequence <= 0 || string.IsNullOrWhiteSpace(Title);

        public string BodyText
        {
            get
            {
                var effect = string.IsNullOrWhiteSpace(EffectText) ? "Acquired" : EffectText;
                var rarity = string.IsNullOrWhiteSpace(RarityText) ? "Common" : RarityText;
                return string.IsNullOrWhiteSpace(ReplacementText)
                    ? $"{Glyph} {Title}\n{Category} | {rarity}\n{effect}"
                    : $"{Glyph} {Title}\n{Category} | {rarity}\n{effect}\n{ReplacementText}";
            }
        }

        public static PickupRevealModel Empty => new(0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, Color.white, string.Empty, string.Empty);

        public static PickupRevealModel Message(int sequence, string title, string toastText, Color color)
        {
            return new PickupRevealModel(sequence, title, "Status", toastText, "Info", "!", color, string.Empty, toastText);
        }
    }

}
