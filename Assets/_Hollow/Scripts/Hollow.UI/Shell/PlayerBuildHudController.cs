using System.Collections.Generic;
using Hollow.Branches;
using Hollow.Rewards;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.UI.Shell
{
    [RequireComponent(typeof(Canvas))]
    public sealed class PlayerBuildHudController : MonoBehaviour
    {
        private const string AvatarSpriteResource = "UI/Hud/CharacterAvatar";
        private const string EmptyHeartSpriteResource = "UI/Hud/HPContainerEmpty";
        private const string FullHeartSpriteResource = "UI/Hud/HPContainerFull";
        private const string StaminaFrameSpriteResource = "UI/Hud/StaminaBarFrame";
        private const string StaminaFillSpriteResource = "UI/Hud/StaminaBarFill";
        private const string SoulsIconSpriteResource = "UI/Hud/SoulsIcon";
        private const string CoinIconSpriteResource = "UI/Hud/CoinIcon";
        private const string GoldKeyIconSpriteResource = "UI/Hud/GoldKeyIcon";
        private const string BossKeyIconSpriteResource = "UI/Hud/BossKeyIcon";
        private const string MeleeDamageIconSpriteResource = "UI/Hud/MeleeDamageIcon";
        private const string MeleeSpeedIconSpriteResource = "UI/Hud/MeleeSpeedIcon";
        private const string RangedDamageIconSpriteResource = "UI/Hud/RangedDamageIcon";
        private const string RangedSpeedIconSpriteResource = "UI/Hud/RangedSpeedIcon";
        private const string RangeIconSpriteResource = "UI/Hud/RangeIcon";
        private const string DefenseIconSpriteResource = "UI/Hud/DefenseIcon";
        private const string MoveSpeedIconSpriteResource = "UI/Hud/MoveSpeedIcon";
        private const string KarmaIconSpriteResource = "UI/Hud/KarmaIcon";
        private const string WeaponIconSpriteResourcePrefix = "UI/Hud/Weapons/";
        private const string ActiveWeaponFallbackSpriteResource = "UI/Hud/Weapons/active_weapon_missing";
        private const float AvatarSize = 118f;
        private const float HeartSize = 38f;
        private const float HeartPadding = 6f;
        private const float HeartSpacing = HeartSize + HeartPadding;
        private const float HeartRowSpacing = HeartSize + 4f;
        private const float HeartStartX = AvatarSize + 32f;
        private const int HeartsPerRow = 10;
        private const float SoulsIconSize = 34f;
        private const float SoulsAmountGap = 8f;
        private const float SoulsAmountWidth = 124f;
        private const float SoulsAmountHeight = 34f;
        private const int SoulsAmountFontSize = 28;
        private const float SoulsGapAboveHearts = 7f;
        private const float CoinIconSize = 30f;
        private const float CoinAmountGap = 7f;
        private const float CoinAmountVisibleWidth = 24f;
        private const float CoinAmountWidth = 82f;
        private const float CoinAmountHeight = 30f;
        private const int CoinAmountFontSize = 24;
        private const float CoinGapBelowAvatar = 7f;
        private const float KeyGapBelowCoins = 6f;
        private const float StatGapBelowKeys = 9f;
        private const float StatIconSize = 22f;
        private const float StatIconGap = 5f;
        private const float StatValueWidth = 62f;
        private const float StatValueHeight = 22f;
        private const int StatValueFontSize = 18;
        private const float StatRowHeight = 24f;
        private const float StatColumnWidth = StatIconSize + StatIconGap + StatValueWidth;
        private const int StatRowCount = 8;
        private const float StaminaFrameWidth = 330f;
        private const float StaminaFrameSourceWidth = 1307f;
        private const float StaminaFrameSourceHeight = 94f;
        private const float StaminaFillSourceWidth = 1107f;
        private const float StaminaFillSourceHeight = 31f;
        private const float StaminaGapBelowHearts = 10f;
        private const float StaminaPanelPadding = 20f;
        private const float ActiveWeaponIconWidth = 108f;
        private const float ActiveWeaponIconHeight = 72f;
        private const float ActiveWeaponIconInset = 24f;

        private IPlayerBuildHudModelProvider hudModelProvider;
        private Text buildText;
        private RectTransform panelRect;
        private RectTransform activeWeaponIconRect;
        private Image avatarImage;
        private Image activeWeaponIconImage;
        private RectTransform staminaBarRect;
        private Image staminaFrameImage;
        private Image staminaFillImage;
        private RectTransform soulsIconRect;
        private RectTransform soulsAmountRect;
        private RectTransform coinsIconRect;
        private RectTransform coinsAmountRect;
        private RectTransform keysIconRect;
        private RectTransform keysAmountRect;
        private RectTransform statsBlockRect;
        private Image soulsIconImage;
        private Image coinsIconImage;
        private Image keysIconImage;
        private Text soulsAmountText;
        private Text coinsAmountText;
        private Text keysAmountText;
        private Sprite avatarSprite;
        private Sprite emptyHeartSprite;
        private Sprite fullHeartSprite;
        private Sprite staminaFrameSprite;
        private Sprite staminaFillSprite;
        private Sprite soulsIconSprite;
        private Sprite coinIconSprite;
        private Sprite goldKeyIconSprite;
        private Sprite bossKeyIconSprite;
        private Sprite meleeDamageIconSprite;
        private Sprite meleeSpeedIconSprite;
        private Sprite rangedDamageIconSprite;
        private Sprite rangedSpeedIconSprite;
        private Sprite rangeIconSprite;
        private Sprite defenseIconSprite;
        private Sprite moveSpeedIconSprite;
        private Sprite karmaIconSprite;
        private Sprite activeWeaponFallbackSprite;
        private Font font;
        private int renderedMaxHealth = -1;
        private int renderedFullHeartCount = -1;
        private int renderedSouls = int.MinValue;
        private int renderedCoins = int.MinValue;
        private int renderedKeys = int.MinValue;
        private bool renderedHasBossKey;
        private string renderedActiveWeaponId = string.Empty;
        private float renderedStaminaFillAmount = -1f;
        private readonly List<Image> heartImages = new List<Image>();
        private readonly List<Image> statIconImages = new List<Image>();
        private readonly List<Text> statValueTexts = new List<Text>();
        private readonly string[] renderedStatValues = new string[8];
        private readonly Dictionary<string, Sprite> weaponIconSprites = new Dictionary<string, Sprite>();

        public int RenderedHeartCount => heartImages.Count;
        public int RenderedFullHeartCount => renderedFullHeartCount;
        public int RenderedSouls => renderedSouls == int.MinValue ? 0 : renderedSouls;
        public int RenderedCoins => renderedCoins == int.MinValue ? 0 : renderedCoins;
        public int RenderedKeys => renderedKeys == int.MinValue ? 0 : renderedKeys;
        public bool RenderedHasBossKey => renderedHasBossKey;
        public float RenderedStaminaFillAmount => staminaFillImage != null ? staminaFillImage.fillAmount : 0f;
        public bool HasRenderedStaminaBar => staminaBarRect != null && staminaFrameImage != null && staminaFillImage != null;
        public bool HasRenderedSoulsCounter => soulsIconImage != null && soulsAmountText != null;
        public bool HasRenderedCoinsCounter => coinsIconImage != null && coinsAmountText != null;
        public bool HasRenderedKeysCounter => keysIconImage != null && keysAmountText != null;
        public bool HasRenderedStatsBlock => statsBlockRect != null && statIconImages.Count == 8 && statValueTexts.Count == 8;
        public bool HasRenderedActiveWeaponIcon => activeWeaponIconImage != null && activeWeaponIconRect != null && activeWeaponIconImage.enabled;
        public string RenderedActiveWeaponId => renderedActiveWeaponId ?? string.Empty;

        public void Bind(BranchSessionController controller)
        {
            Bind((IPlayerBuildHudModelProvider)controller);
        }

        public void Bind(IPlayerBuildHudModelProvider provider)
        {
            hudModelProvider = provider;
            Refresh(force: true);
        }

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            LoadSpritesIfNeeded();
            BuildIfNeeded();
        }

        private void Update()
        {
            hudModelProvider ??= FindHudModelProvider();

            Refresh(force: false);
        }

        public void Refresh(bool force)
        {
            BuildIfNeeded();
            if (hudModelProvider == null || buildText == null)
            {
                return;
            }

            var model = hudModelProvider.CreatePlayerBuildHudModel();
            RefreshFromModel(model);
        }

        private static IPlayerBuildHudModelProvider FindHudModelProvider()
        {
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour is IPlayerBuildHudModelProvider provider)
                {
                    return provider;
                }
            }

            return null;
        }

        public void RefreshFromModel(PlayerBuildHudModel model)
        {
            BuildIfNeeded();
            if (buildText != null)
            {
                buildText.text = model.BodyText;
            }

            RefreshHealth(model.CurrentHealth, model.MaxHealth);
            RefreshCoins(model.Coins);
            RefreshKeys(model.Keys, model.HasBossKey);
            RefreshSouls(model.Souls);
            RefreshStamina(model.CurrentStamina, model.MaxStamina);
            RefreshStats(model);
            RefreshActiveWeapon(model.ActiveWeaponId);
        }

        private void BuildIfNeeded()
        {
            if (panelRect != null &&
                avatarImage != null &&
                staminaFillImage != null &&
                soulsIconImage != null &&
                soulsAmountText != null &&
                coinsIconImage != null &&
                coinsAmountText != null &&
                keysIconImage != null &&
                keysAmountText != null &&
                statsBlockRect != null &&
                activeWeaponIconImage != null &&
                statIconImages.Count == 8 &&
                statValueTexts.Count == 8)
            {
                return;
            }

            LoadSpritesIfNeeded();
            font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var panel = new GameObject("PlayerBuildHud.Panel", typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(560f, 132f);

            var activeWeaponIconObject = new GameObject("PlayerBuildHud.ActiveWeaponIcon", typeof(RectTransform), typeof(Image));
            activeWeaponIconObject.transform.SetParent(transform, false);
            activeWeaponIconRect = (RectTransform)activeWeaponIconObject.transform;
            activeWeaponIconRect.anchorMin = Vector2.zero;
            activeWeaponIconRect.anchorMax = Vector2.zero;
            activeWeaponIconRect.pivot = Vector2.zero;
            activeWeaponIconRect.anchoredPosition = new Vector2(ActiveWeaponIconInset, ActiveWeaponIconInset);
            activeWeaponIconRect.sizeDelta = new Vector2(ActiveWeaponIconWidth, ActiveWeaponIconHeight);
            activeWeaponIconImage = activeWeaponIconObject.GetComponent<Image>();
            activeWeaponIconImage.sprite = activeWeaponFallbackSprite;
            activeWeaponIconImage.enabled = activeWeaponFallbackSprite != null;
            activeWeaponIconImage.preserveAspect = true;
            activeWeaponIconImage.raycastTarget = false;

            var avatarObject = new GameObject("PlayerBuildHud.Avatar", typeof(RectTransform), typeof(Image));
            avatarObject.transform.SetParent(panel.transform, false);
            var avatarRect = (RectTransform)avatarObject.transform;
            avatarRect.anchorMin = new Vector2(0f, 1f);
            avatarRect.anchorMax = new Vector2(0f, 1f);
            avatarRect.pivot = new Vector2(0f, 1f);
            avatarRect.anchoredPosition = Vector2.zero;
            avatarRect.sizeDelta = new Vector2(AvatarSize, AvatarSize);
            avatarImage = avatarObject.GetComponent<Image>();
            avatarImage.sprite = avatarSprite;
            avatarImage.preserveAspect = true;
            avatarImage.raycastTarget = false;

            var coinsIconObject = new GameObject("PlayerBuildHud.CoinsIcon", typeof(RectTransform), typeof(Image));
            coinsIconObject.transform.SetParent(panel.transform, false);
            coinsIconRect = (RectTransform)coinsIconObject.transform;
            coinsIconRect.anchorMin = new Vector2(0f, 1f);
            coinsIconRect.anchorMax = new Vector2(0f, 1f);
            coinsIconRect.pivot = new Vector2(0f, 0.5f);
            coinsIconRect.sizeDelta = new Vector2(CoinIconSize, CoinIconSize);
            coinsIconImage = coinsIconObject.GetComponent<Image>();
            coinsIconImage.sprite = coinIconSprite;
            coinsIconImage.preserveAspect = true;
            coinsIconImage.raycastTarget = false;

            var coinsAmountObject = new GameObject("PlayerBuildHud.CoinsAmount", typeof(RectTransform), typeof(Text));
            coinsAmountObject.transform.SetParent(panel.transform, false);
            coinsAmountRect = (RectTransform)coinsAmountObject.transform;
            coinsAmountRect.anchorMin = new Vector2(0f, 1f);
            coinsAmountRect.anchorMax = new Vector2(0f, 1f);
            coinsAmountRect.pivot = new Vector2(0f, 0.5f);
            coinsAmountRect.sizeDelta = new Vector2(CoinAmountWidth, CoinAmountHeight);
            coinsAmountText = coinsAmountObject.GetComponent<Text>();
            coinsAmountText.font = font;
            coinsAmountText.fontSize = CoinAmountFontSize;
            coinsAmountText.fontStyle = FontStyle.Bold;
            coinsAmountText.alignment = TextAnchor.MiddleLeft;
            coinsAmountText.color = new Color(1f, 0.86f, 0.34f, 1f);
            coinsAmountText.raycastTarget = false;
            coinsAmountText.text = "0";

            var keysIconObject = new GameObject("PlayerBuildHud.KeysIcon", typeof(RectTransform), typeof(Image));
            keysIconObject.transform.SetParent(panel.transform, false);
            keysIconRect = (RectTransform)keysIconObject.transform;
            keysIconRect.anchorMin = new Vector2(0f, 1f);
            keysIconRect.anchorMax = new Vector2(0f, 1f);
            keysIconRect.pivot = new Vector2(0f, 0.5f);
            keysIconRect.sizeDelta = new Vector2(CoinIconSize, CoinIconSize);
            keysIconImage = keysIconObject.GetComponent<Image>();
            keysIconImage.sprite = goldKeyIconSprite;
            keysIconImage.preserveAspect = true;
            keysIconImage.raycastTarget = false;

            var keysAmountObject = new GameObject("PlayerBuildHud.KeysAmount", typeof(RectTransform), typeof(Text));
            keysAmountObject.transform.SetParent(panel.transform, false);
            keysAmountRect = (RectTransform)keysAmountObject.transform;
            keysAmountRect.anchorMin = new Vector2(0f, 1f);
            keysAmountRect.anchorMax = new Vector2(0f, 1f);
            keysAmountRect.pivot = new Vector2(0f, 0.5f);
            keysAmountRect.sizeDelta = new Vector2(CoinAmountWidth, CoinAmountHeight);
            keysAmountText = keysAmountObject.GetComponent<Text>();
            keysAmountText.font = font;
            keysAmountText.fontSize = CoinAmountFontSize;
            keysAmountText.fontStyle = FontStyle.Bold;
            keysAmountText.alignment = TextAnchor.MiddleLeft;
            keysAmountText.color = new Color(1f, 0.88f, 0.42f, 1f);
            keysAmountText.raycastTarget = false;
            keysAmountText.text = "0";

            var statsBlockObject = new GameObject("PlayerBuildHud.StatsBlock", typeof(RectTransform));
            statsBlockObject.transform.SetParent(panel.transform, false);
            statsBlockRect = (RectTransform)statsBlockObject.transform;
            statsBlockRect.anchorMin = new Vector2(0f, 1f);
            statsBlockRect.anchorMax = new Vector2(0f, 1f);
            statsBlockRect.pivot = new Vector2(0f, 1f);
            statsBlockRect.sizeDelta = new Vector2(StatColumnWidth, StatRowHeight * StatRowCount);
            CreateStatRows(statsBlockObject.transform);

            var soulsIconObject = new GameObject("PlayerBuildHud.SoulsIcon", typeof(RectTransform), typeof(Image));
            soulsIconObject.transform.SetParent(panel.transform, false);
            soulsIconRect = (RectTransform)soulsIconObject.transform;
            soulsIconRect.anchorMin = new Vector2(0f, 1f);
            soulsIconRect.anchorMax = new Vector2(0f, 1f);
            soulsIconRect.pivot = new Vector2(0f, 0.5f);
            soulsIconRect.sizeDelta = new Vector2(SoulsIconSize, SoulsIconSize);
            soulsIconImage = soulsIconObject.GetComponent<Image>();
            soulsIconImage.sprite = soulsIconSprite;
            soulsIconImage.preserveAspect = true;
            soulsIconImage.raycastTarget = false;

            var soulsAmountObject = new GameObject("PlayerBuildHud.SoulsAmount", typeof(RectTransform), typeof(Text));
            soulsAmountObject.transform.SetParent(panel.transform, false);
            soulsAmountRect = (RectTransform)soulsAmountObject.transform;
            soulsAmountRect.anchorMin = new Vector2(0f, 1f);
            soulsAmountRect.anchorMax = new Vector2(0f, 1f);
            soulsAmountRect.pivot = new Vector2(0f, 0.5f);
            soulsAmountRect.sizeDelta = new Vector2(SoulsAmountWidth, SoulsAmountHeight);
            soulsAmountText = soulsAmountObject.GetComponent<Text>();
            soulsAmountText.font = font;
            soulsAmountText.fontSize = SoulsAmountFontSize;
            soulsAmountText.fontStyle = FontStyle.Bold;
            soulsAmountText.alignment = TextAnchor.MiddleLeft;
            soulsAmountText.color = new Color(0.78f, 0.94f, 1f, 1f);
            soulsAmountText.raycastTarget = false;
            soulsAmountText.text = "0";

            var staminaObject = new GameObject("PlayerBuildHud.StaminaBar", typeof(RectTransform));
            staminaObject.transform.SetParent(panel.transform, false);
            staminaBarRect = (RectTransform)staminaObject.transform;
            staminaBarRect.anchorMin = new Vector2(0f, 1f);
            staminaBarRect.anchorMax = new Vector2(0f, 1f);
            staminaBarRect.pivot = new Vector2(0f, 0.5f);

            var fillObject = new GameObject("PlayerBuildHud.StaminaFill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(staminaObject.transform, false);
            var fillRect = (RectTransform)fillObject.transform;
            fillRect.anchorMin = new Vector2(0.5f, 0.5f);
            fillRect.anchorMax = new Vector2(0.5f, 0.5f);
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            staminaFillImage = fillObject.GetComponent<Image>();
            staminaFillImage.sprite = staminaFillSprite;
            staminaFillImage.preserveAspect = true;
            staminaFillImage.raycastTarget = false;
            staminaFillImage.type = Image.Type.Filled;
            staminaFillImage.fillMethod = Image.FillMethod.Horizontal;
            staminaFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            staminaFillImage.fillAmount = 0f;

            var frameObject = new GameObject("PlayerBuildHud.StaminaFrame", typeof(RectTransform), typeof(Image));
            frameObject.transform.SetParent(staminaObject.transform, false);
            var frameRect = (RectTransform)frameObject.transform;
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            staminaFrameImage = frameObject.GetComponent<Image>();
            staminaFrameImage.sprite = staminaFrameSprite;
            staminaFrameImage.preserveAspect = true;
            staminaFrameImage.raycastTarget = false;
            fillObject.transform.SetAsLastSibling();

            var textObject = new GameObject("PlayerBuildHud.DebugText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            buildText = textObject.GetComponent<Text>();
            buildText.font = font;
            buildText.fontSize = 1;
            buildText.alignment = TextAnchor.UpperLeft;
            buildText.color = Color.clear;
            buildText.raycastTarget = false;
            buildText.text = "BUILD\nWaiting...";

            LayoutStaminaBar(renderedMaxHealth);
        }

        private void CreateStatRows(Transform parent)
        {
            statIconImages.Clear();
            statValueTexts.Clear();
            var rowNames = new[]
            {
                "MeleeDamage",
                "MeleeSpeed",
                "RangedDamage",
                "RangedSpeed",
                "Range",
                "Defense",
                "Speed",
                "Karma"
            };

            for (var i = 0; i < rowNames.Length; i++)
            {
                var rowObject = new GameObject($"PlayerBuildHud.Stat.{rowNames[i]}", typeof(RectTransform));
                rowObject.transform.SetParent(parent, false);
                var rowRect = (RectTransform)rowObject.transform;
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(0f, 1f);
                rowRect.pivot = new Vector2(0f, 0.5f);
                rowRect.anchoredPosition = new Vector2(0f, -i * StatRowHeight - StatRowHeight * 0.5f);
                rowRect.sizeDelta = new Vector2(StatColumnWidth, StatRowHeight);

                var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(rowObject.transform, false);
                var iconRect = (RectTransform)iconObject.transform;
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;
                iconRect.sizeDelta = new Vector2(StatIconSize, StatIconSize);
                var icon = iconObject.GetComponent<Image>();
                icon.sprite = GetStatIconSprite(i);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                statIconImages.Add(icon);

                var valueObject = new GameObject("Value", typeof(RectTransform), typeof(Text));
                valueObject.transform.SetParent(rowObject.transform, false);
                var valueRect = (RectTransform)valueObject.transform;
                valueRect.anchorMin = new Vector2(0f, 0.5f);
                valueRect.anchorMax = new Vector2(0f, 0.5f);
                valueRect.pivot = new Vector2(0f, 0.5f);
                valueRect.anchoredPosition = new Vector2(StatIconSize + StatIconGap, 0f);
                valueRect.sizeDelta = new Vector2(StatValueWidth, StatValueHeight);
                var value = valueObject.GetComponent<Text>();
                value.font = font;
                value.fontSize = StatValueFontSize;
                value.fontStyle = FontStyle.Bold;
                value.alignment = TextAnchor.MiddleLeft;
                value.color = new Color(0.86f, 0.95f, 1f, 1f);
                value.raycastTarget = false;
                value.text = "0";
                statValueTexts.Add(value);
            }
        }

        private Sprite GetStatIconSprite(int index)
        {
            return index switch
            {
                0 => meleeDamageIconSprite,
                1 => meleeSpeedIconSprite,
                2 => rangedDamageIconSprite,
                3 => rangedSpeedIconSprite,
                4 => rangeIconSprite,
                5 => defenseIconSprite,
                6 => moveSpeedIconSprite,
                7 => karmaIconSprite,
                _ => null
            };
        }

        private void LoadSpritesIfNeeded()
        {
            avatarSprite ??= Resources.Load<Sprite>(AvatarSpriteResource);
            emptyHeartSprite ??= Resources.Load<Sprite>(EmptyHeartSpriteResource);
            fullHeartSprite ??= Resources.Load<Sprite>(FullHeartSpriteResource);
            staminaFrameSprite ??= Resources.Load<Sprite>(StaminaFrameSpriteResource);
            staminaFillSprite ??= Resources.Load<Sprite>(StaminaFillSpriteResource);
            soulsIconSprite ??= Resources.Load<Sprite>(SoulsIconSpriteResource);
            coinIconSprite ??= Resources.Load<Sprite>(CoinIconSpriteResource);
            goldKeyIconSprite ??= Resources.Load<Sprite>(GoldKeyIconSpriteResource);
            bossKeyIconSprite ??= Resources.Load<Sprite>(BossKeyIconSpriteResource);
            meleeDamageIconSprite ??= Resources.Load<Sprite>(MeleeDamageIconSpriteResource);
            meleeSpeedIconSprite ??= Resources.Load<Sprite>(MeleeSpeedIconSpriteResource);
            rangedDamageIconSprite ??= Resources.Load<Sprite>(RangedDamageIconSpriteResource);
            rangedSpeedIconSprite ??= Resources.Load<Sprite>(RangedSpeedIconSpriteResource);
            rangeIconSprite ??= Resources.Load<Sprite>(RangeIconSpriteResource);
            defenseIconSprite ??= Resources.Load<Sprite>(DefenseIconSpriteResource);
            moveSpeedIconSprite ??= Resources.Load<Sprite>(MoveSpeedIconSpriteResource);
            karmaIconSprite ??= Resources.Load<Sprite>(KarmaIconSpriteResource);
            activeWeaponFallbackSprite ??= Resources.Load<Sprite>(ActiveWeaponFallbackSpriteResource);
        }

        private void RefreshHealth(int currentHealth, int maxHealth)
        {
            var normalizedMaxHealth = Mathf.Max(0, maxHealth);
            var normalizedCurrentHealth = Mathf.Clamp(currentHealth, 0, normalizedMaxHealth);
            if (renderedMaxHealth != normalizedMaxHealth)
            {
                RebuildHeartImages(normalizedMaxHealth);
            }

            if (renderedFullHeartCount == normalizedCurrentHealth)
            {
                return;
            }

            renderedFullHeartCount = normalizedCurrentHealth;
            for (var i = 0; i < heartImages.Count; i++)
            {
                var heart = heartImages[i];
                heart.sprite = i < normalizedCurrentHealth ? fullHeartSprite : emptyHeartSprite;
                heart.color = Color.white;
            }
        }

        private void RebuildHeartImages(int maxHealth)
        {
            renderedMaxHealth = maxHealth;
            renderedFullHeartCount = -1;
            for (var i = heartImages.Count - 1; i >= 0; i--)
            {
                var heart = heartImages[i];
                if (heart == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(heart.gameObject);
                }
                else
                {
                    DestroyImmediate(heart.gameObject);
                }
            }

            heartImages.Clear();
            if (maxHealth <= 0 || panelRect == null)
            {
                LayoutStaminaBar(maxHealth);
                return;
            }

            var bottomRowCenterY = CalculateBottomRowCenterY(maxHealth);

            for (var i = 0; i < maxHealth; i++)
            {
                var heartObject = new GameObject($"PlayerBuildHud.Heart_{i + 1:00}", typeof(RectTransform), typeof(Image));
                heartObject.transform.SetParent(panelRect, false);
                var heartRect = (RectTransform)heartObject.transform;
                heartRect.anchorMin = new Vector2(0f, 1f);
                heartRect.anchorMax = new Vector2(0f, 1f);
                heartRect.pivot = new Vector2(0.5f, 0.5f);
                var column = i % HeartsPerRow;
                var row = i / HeartsPerRow;
                heartRect.anchoredPosition = new Vector2(HeartStartX + column * HeartSpacing, bottomRowCenterY + row * HeartRowSpacing);
                heartRect.sizeDelta = new Vector2(HeartSize, HeartSize);

                var heartImage = heartObject.GetComponent<Image>();
                heartImage.sprite = emptyHeartSprite;
                heartImage.preserveAspect = true;
                heartImage.raycastTarget = false;
                heartImages.Add(heartImage);
            }

            LayoutStaminaBar(maxHealth);
        }

        private void RefreshCoins(int coins)
        {
            BuildIfNeeded();
            var normalizedCoins = Mathf.Max(0, coins);
            if (renderedCoins == normalizedCoins)
            {
                return;
            }

            renderedCoins = normalizedCoins;
            if (coinsAmountText != null)
            {
                coinsAmountText.text = normalizedCoins.ToString();
            }
        }

        private void RefreshSouls(int souls)
        {
            BuildIfNeeded();
            var normalizedSouls = Mathf.Max(0, souls);
            if (renderedSouls == normalizedSouls)
            {
                return;
            }

            renderedSouls = normalizedSouls;
            if (soulsAmountText != null)
            {
                soulsAmountText.text = normalizedSouls.ToString();
            }
        }

        private void RefreshKeys(int keys, bool hasBossKey)
        {
            BuildIfNeeded();
            var normalizedKeys = Mathf.Max(0, keys);
            var displayedKeys = hasBossKey ? 1 : normalizedKeys;
            if (renderedKeys == displayedKeys && renderedHasBossKey == hasBossKey)
            {
                return;
            }

            renderedKeys = displayedKeys;
            renderedHasBossKey = hasBossKey;
            if (keysAmountText != null)
            {
                keysAmountText.text = displayedKeys.ToString();
                keysAmountText.color = hasBossKey
                    ? new Color(0.9f, 0.66f, 1f, 1f)
                    : new Color(1f, 0.88f, 0.42f, 1f);
            }

            if (keysIconImage != null)
            {
                keysIconImage.sprite = hasBossKey && bossKeyIconSprite != null
                    ? bossKeyIconSprite
                    : goldKeyIconSprite;
            }
        }

        private void RefreshStats(PlayerBuildHudModel model)
        {
            BuildIfNeeded();
            if (statValueTexts.Count < 8)
            {
                return;
            }

            var values = new[]
            {
                $"{model.MeleeLightDamage}/{model.MeleeHeavyDamage}",
                $"{model.MeleeLightAttacksPerSecond:0.0}/s",
                $"{model.RangedLightDamage}/{model.RangedHeavyDamage}",
                $"{model.RangedLightAttacksPerSecond:0.0}/s",
                $"{model.EffectiveRangeMeters:0.0}m",
                model.Defense.ToString(),
                $"{model.MoveSpeedMetersPerSecond:0.0}m/s",
                FormatKarma(model.Karma)
            };

            for (var i = 0; i < values.Length; i++)
            {
                if (renderedStatValues[i] == values[i])
                {
                    continue;
                }

                renderedStatValues[i] = values[i];
                statValueTexts[i].text = values[i];
                statValueTexts[i].color = i == 7
                    ? KarmaTextColor(model.Karma)
                    : new Color(0.86f, 0.95f, 1f, 1f);
            }
        }

        private void RefreshActiveWeapon(string activeWeaponId)
        {
            BuildIfNeeded();
            if (activeWeaponIconImage == null)
            {
                return;
            }

            var normalizedWeaponId = string.IsNullOrWhiteSpace(activeWeaponId)
                ? "active_weapon_missing"
                : activeWeaponId.Trim();
            if (renderedActiveWeaponId == normalizedWeaponId && activeWeaponIconImage.sprite != null)
            {
                return;
            }

            renderedActiveWeaponId = normalizedWeaponId;
            var sprite = LoadWeaponIconSprite(normalizedWeaponId) ?? activeWeaponFallbackSprite;
            activeWeaponIconImage.sprite = sprite;
            activeWeaponIconImage.enabled = sprite != null;
            activeWeaponIconImage.preserveAspect = true;
        }

        private Sprite LoadWeaponIconSprite(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return null;
            }

            if (!weaponIconSprites.TryGetValue(weaponId, out var sprite))
            {
                sprite = Resources.Load<Sprite>($"{WeaponIconSpriteResourcePrefix}{weaponId}");
                weaponIconSprites[weaponId] = sprite;
            }

            return sprite;
        }

        private static string FormatKarma(int karma)
        {
            return karma > 0 ? $"+{karma}" : karma.ToString();
        }

        private static Color KarmaTextColor(int karma)
        {
            if (karma > 0)
            {
                return new Color(0.62f, 0.96f, 1f, 1f);
            }

            return karma < 0
                ? new Color(1f, 0.58f, 0.78f, 1f)
                : new Color(0.86f, 0.95f, 1f, 1f);
        }

        private void RefreshStamina(float currentStamina, float maxStamina)
        {
            BuildIfNeeded();
            LayoutStaminaBar(renderedMaxHealth);
            var fillAmount = maxStamina > 0f ? Mathf.Clamp01(currentStamina / maxStamina) : 0f;
            if (Mathf.Approximately(renderedStaminaFillAmount, fillAmount))
            {
                return;
            }

            renderedStaminaFillAmount = fillAmount;
            if (staminaFillImage != null)
            {
                staminaFillImage.fillAmount = fillAmount;
            }
        }

        private void LayoutStaminaBar(int maxHealth)
        {
            if (panelRect == null || staminaBarRect == null || staminaFillImage == null)
            {
                return;
            }

            var normalizedMaxHealth = Mathf.Max(0, maxHealth);
            var rowCount = Mathf.Max(1, Mathf.CeilToInt(normalizedMaxHealth / (float)HeartsPerRow));
            var columnCount = normalizedMaxHealth > 0 ? Mathf.Min(normalizedMaxHealth, HeartsPerRow) : 0;
            var frameHeight = StaminaFrameWidth * StaminaFrameSourceHeight / StaminaFrameSourceWidth;
            var fillWidth = StaminaFrameWidth * StaminaFillSourceWidth / StaminaFrameSourceWidth;
            var fillHeight = frameHeight * StaminaFillSourceHeight / StaminaFrameSourceHeight;
            var bottomRowCenterY = CalculateBottomRowCenterY(normalizedMaxHealth);
            var staminaCenterY = bottomRowCenterY - HeartSize * 0.5f - StaminaGapBelowHearts - frameHeight * 0.5f;
            var topHeartCenterY = bottomRowCenterY + Mathf.Max(0, rowCount - 1) * HeartRowSpacing;
            var soulsCenterY = topHeartCenterY + HeartSize * 0.5f + SoulsGapAboveHearts + SoulsIconSize * 0.5f;
            var heartLeft = HeartStartX - HeartSize * 0.5f;

            if (soulsIconRect != null)
            {
                soulsIconRect.anchoredPosition = new Vector2(heartLeft, soulsCenterY);
                soulsIconRect.sizeDelta = new Vector2(SoulsIconSize, SoulsIconSize);
            }

            if (soulsAmountRect != null)
            {
                soulsAmountRect.anchoredPosition = new Vector2(heartLeft + SoulsIconSize + SoulsAmountGap, soulsCenterY);
                soulsAmountRect.sizeDelta = new Vector2(SoulsAmountWidth, SoulsAmountHeight);
            }

            staminaBarRect.anchoredPosition = new Vector2(heartLeft, staminaCenterY);
            staminaBarRect.sizeDelta = new Vector2(StaminaFrameWidth, frameHeight);
            var fillRect = (RectTransform)staminaFillImage.transform;
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(fillWidth, fillHeight);

            var heartRight = columnCount > 0
                ? HeartStartX + (columnCount - 1) * HeartSpacing + HeartSize * 0.5f
                : AvatarSize;
            var staminaRight = heartLeft + StaminaFrameWidth;
            var soulsRight = heartLeft + SoulsIconSize + SoulsAmountGap + SoulsAmountWidth;
            var currencyRight = LayoutCurrencyCounters();
            var panelWidth = Mathf.Max(Mathf.Max(Mathf.Max(heartRight, staminaRight), soulsRight), currencyRight) + StaminaPanelPadding;
            var topRowsHeight = normalizedMaxHealth > 0 ? (rowCount - 1) * HeartRowSpacing : 0f;
            var currencyBottom = AvatarSize
                + CoinGapBelowAvatar
                + CoinIconSize
                + KeyGapBelowCoins
                + CoinIconSize
                + StatGapBelowKeys
                + StatRowHeight * StatRowCount;
            var staminaBottom = -staminaCenterY + frameHeight * 0.5f + topRowsHeight;
            var panelHeight = Mathf.Max(Mathf.Max(AvatarSize, currencyBottom), staminaBottom) + StaminaPanelPadding;
            panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        }

        private float LayoutCurrencyCounters()
        {
            var coinsCenterY = -AvatarSize - CoinGapBelowAvatar - CoinIconSize * 0.5f;
            var keysCenterY = coinsCenterY - CoinIconSize - KeyGapBelowCoins;
            var visibleCounterWidth = CoinIconSize + CoinAmountGap + CoinAmountVisibleWidth;
            var coinsStartX = (AvatarSize - visibleCounterWidth) * 0.5f;
            if (coinsIconRect != null)
            {
                coinsIconRect.anchoredPosition = new Vector2(coinsStartX, coinsCenterY);
                coinsIconRect.sizeDelta = new Vector2(CoinIconSize, CoinIconSize);
            }

            if (coinsAmountRect != null)
            {
                coinsAmountRect.anchoredPosition = new Vector2(coinsStartX + CoinIconSize + CoinAmountGap, coinsCenterY);
                coinsAmountRect.sizeDelta = new Vector2(CoinAmountWidth, CoinAmountHeight);
            }

            if (keysIconRect != null)
            {
                keysIconRect.anchoredPosition = new Vector2(coinsStartX, keysCenterY);
                keysIconRect.sizeDelta = new Vector2(CoinIconSize, CoinIconSize);
            }

            if (keysAmountRect != null)
            {
                keysAmountRect.anchoredPosition = new Vector2(coinsStartX + CoinIconSize + CoinAmountGap, keysCenterY);
                keysAmountRect.sizeDelta = new Vector2(CoinAmountWidth, CoinAmountHeight);
            }

            var statsRight = 0f;
            if (statsBlockRect != null)
            {
                var statsTopY = keysCenterY - CoinIconSize * 0.5f - StatGapBelowKeys;
                statsBlockRect.anchoredPosition = new Vector2(coinsStartX, statsTopY);
                statsBlockRect.sizeDelta = new Vector2(StatColumnWidth, StatRowHeight * StatRowCount);
                statsRight = coinsStartX + statsBlockRect.sizeDelta.x;
            }

            return Mathf.Max(coinsStartX + CoinIconSize + CoinAmountGap + CoinAmountWidth, statsRight);
        }

        private static float CalculateBottomRowCenterY(int maxHealth)
        {
            var normalizedMaxHealth = Mathf.Max(0, maxHealth);
            var rowCount = Mathf.Max(1, Mathf.CeilToInt(normalizedMaxHealth / (float)HeartsPerRow));
            var frameHeight = StaminaFrameWidth * StaminaFrameSourceHeight / StaminaFrameSourceWidth;
            var avatarCenterY = -AvatarSize * 0.5f;
            var soulsTopOffset = Mathf.Max(0, rowCount - 1) * HeartRowSpacing
                + HeartSize * 0.5f
                + SoulsGapAboveHearts
                + SoulsIconSize;
            var staminaBottomOffset = -HeartSize * 0.5f - StaminaGapBelowHearts - frameHeight;

            return avatarCenterY - (soulsTopOffset + staminaBottomOffset) * 0.5f;
        }
    }
}
