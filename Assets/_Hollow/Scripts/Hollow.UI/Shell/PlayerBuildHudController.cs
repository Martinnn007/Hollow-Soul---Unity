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
        private const float AvatarSize = 118f;
        private const float HeartSize = 38f;
        private const float HeartPadding = 6f;
        private const float HeartSpacing = HeartSize + HeartPadding;
        private const float HeartRowSpacing = HeartSize + 4f;
        private const float HeartBottomInset = 8f;
        private const float HeartStartX = AvatarSize + 32f;
        private const int HeartsPerRow = 10;
        private const float StaminaFrameWidth = 360f;
        private const float StaminaFrameSourceWidth = 1307f;
        private const float StaminaFrameSourceHeight = 94f;
        private const float StaminaFillSourceWidth = 1107f;
        private const float StaminaFillSourceHeight = 31f;
        private const float StaminaGapBelowHearts = 10f;
        private const float StaminaPanelPadding = 20f;

        private BranchSessionController branchSessionController;
        private Text buildText;
        private RectTransform panelRect;
        private Image avatarImage;
        private RectTransform staminaBarRect;
        private Image staminaFrameImage;
        private Image staminaFillImage;
        private Sprite avatarSprite;
        private Sprite emptyHeartSprite;
        private Sprite fullHeartSprite;
        private Sprite staminaFrameSprite;
        private Sprite staminaFillSprite;
        private Font font;
        private int renderedMaxHealth = -1;
        private int renderedFullHeartCount = -1;
        private float renderedStaminaFillAmount = -1f;
        private readonly List<Image> heartImages = new List<Image>();

        public int RenderedHeartCount => heartImages.Count;
        public int RenderedFullHeartCount => renderedFullHeartCount;
        public float RenderedStaminaFillAmount => staminaFillImage != null ? staminaFillImage.fillAmount : 0f;
        public bool HasRenderedStaminaBar => staminaBarRect != null && staminaFrameImage != null && staminaFillImage != null;

        public void Bind(BranchSessionController controller)
        {
            branchSessionController = controller;
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
            if (branchSessionController == null)
            {
                branchSessionController = FindAnyObjectByType<BranchSessionController>();
            }

            Refresh(force: false);
        }

        public void Refresh(bool force)
        {
            BuildIfNeeded();
            if (branchSessionController == null || buildText == null)
            {
                return;
            }

            var model = branchSessionController.CreatePlayerBuildHudModel();
            RefreshFromModel(model);
        }

        public void RefreshFromModel(PlayerBuildHudModel model)
        {
            BuildIfNeeded();
            if (buildText != null)
            {
                buildText.text = model.BodyText;
            }

            RefreshHealth(model.CurrentHealth, model.MaxHealth);
            RefreshStamina(model.CurrentStamina, model.MaxStamina);
        }

        private void BuildIfNeeded()
        {
            if (panelRect != null && avatarImage != null && staminaFillImage != null)
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

        private void LoadSpritesIfNeeded()
        {
            avatarSprite ??= Resources.Load<Sprite>(AvatarSpriteResource);
            emptyHeartSprite ??= Resources.Load<Sprite>(EmptyHeartSpriteResource);
            fullHeartSprite ??= Resources.Load<Sprite>(FullHeartSpriteResource);
            staminaFrameSprite ??= Resources.Load<Sprite>(StaminaFrameSpriteResource);
            staminaFillSprite ??= Resources.Load<Sprite>(StaminaFillSpriteResource);
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

            var rowCount = Mathf.CeilToInt(maxHealth / (float)HeartsPerRow);
            var columnCount = Mathf.Min(maxHealth, HeartsPerRow);
            var bottomRowCenterY = -AvatarSize + HeartSize * 0.5f + HeartBottomInset;

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
            var bottomRowCenterY = -AvatarSize + HeartSize * 0.5f + HeartBottomInset;
            var staminaCenterY = bottomRowCenterY - HeartSize * 0.5f - StaminaGapBelowHearts - frameHeight * 0.5f;

            staminaBarRect.anchoredPosition = new Vector2(HeartStartX, staminaCenterY);
            staminaBarRect.sizeDelta = new Vector2(StaminaFrameWidth, frameHeight);
            var fillRect = (RectTransform)staminaFillImage.transform;
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(fillWidth, fillHeight);

            var heartRight = columnCount > 0
                ? HeartStartX + (columnCount - 1) * HeartSpacing + HeartSize * 0.5f
                : AvatarSize;
            var staminaRight = HeartStartX + StaminaFrameWidth;
            var panelWidth = Mathf.Max(heartRight, staminaRight) + StaminaPanelPadding;
            var topRowsHeight = normalizedMaxHealth > 0 ? (rowCount - 1) * HeartRowSpacing : 0f;
            var panelHeight = Mathf.Max(AvatarSize, -staminaCenterY + frameHeight * 0.5f + StaminaPanelPadding + topRowsHeight);
            panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        }
    }
}
