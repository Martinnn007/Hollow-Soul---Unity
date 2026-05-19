using Hollow.Branches;
using Hollow.Data.Definitions;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.UI.Shell
{
    [RequireComponent(typeof(Canvas))]
    public sealed class RunFramingHudController : MonoBehaviour
    {
        private const float WorldToastDurationSeconds = 4.5f;
        private const float MainBannerDurationSeconds = 3.5f;

        [SerializeField] private RunFramingCatalogDefinition catalog;

        private BranchSessionController branchSessionController;
        private RectTransform panel;
        private RectTransform toastPanel;
        private Text titleText;
        private Text phaseText;
        private Text messageText;
        private Text toastTitleText;
        private Text toastMessageText;
        private Font font;
        private string lastKey;
        private string lastWorldIdentityId;
        private float toastHideTime;
        private float panelHideTime;

        public RunFramingCatalogDefinition Catalog => catalog;

        public bool IsWorldEntryToastVisible => toastPanel != null && toastPanel.gameObject.activeSelf;

        public string CurrentWorldEntryToastTitle => toastTitleText != null ? toastTitleText.text : string.Empty;

        public void Configure(RunFramingCatalogDefinition nextCatalog)
        {
            catalog = nextCatalog;
            if (Application.isPlaying)
            {
                Refresh(force: true);
            }
        }

        public void Bind(BranchSessionController controller)
        {
            branchSessionController = controller;
            if (Application.isPlaying)
            {
                Refresh(force: true);
            }
        }

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildIfNeeded();
        }

        private void Update()
        {
            if (branchSessionController == null)
            {
                branchSessionController = FindAnyObjectByType<BranchSessionController>();
            }

            Refresh(force: false);
            UpdateToastVisibility();
            UpdatePanelVisibility();
        }

        private void Refresh(bool force)
        {
            BuildIfNeeded();
            if (titleText == null || phaseText == null || messageText == null)
            {
                return;
            }

            var snapshot = branchSessionController != null
                ? branchSessionController.CreateRunFramingSnapshot(catalog)
                : RunFramingService.Create(catalog, 1, RunWorldPhase.Legacy, 0, 0, bossRoomActive: false);
            if (!string.IsNullOrWhiteSpace(snapshot.WorldIdentityId) &&
                (force || snapshot.WorldIdentityId != lastWorldIdentityId))
            {
                ShowWorldEntryToast(snapshot);
            }

            lastWorldIdentityId = snapshot.WorldIdentityId;
            if (!force && snapshot.SummaryKey == lastKey)
            {
                return;
            }

            lastKey = snapshot.SummaryKey;
            titleText.text = snapshot.Title;
            phaseText.text = $"{snapshot.PhaseLabel} | {snapshot.SeedSummary}";
            messageText.text = $"{snapshot.Subtitle}\n{snapshot.Message}";
            if (panel != null)
            {
                panel.gameObject.SetActive(true);
                panelHideTime = Application.isPlaying ? Time.unscaledTime + MainBannerDurationSeconds : 0f;
            }
        }

        private void ShowWorldEntryToast(RunFramingSnapshot snapshot)
        {
            BuildIfNeeded();
            if (toastPanel == null || toastTitleText == null || toastMessageText == null)
            {
                return;
            }

            toastTitleText.text = string.IsNullOrWhiteSpace(snapshot.WorldDisplayName) ? snapshot.Title : snapshot.WorldDisplayName;
            toastMessageText.text = snapshot.Message;
            toastPanel.gameObject.SetActive(true);
            toastHideTime = Application.isPlaying ? Time.unscaledTime + WorldToastDurationSeconds : 0f;
        }

        private void UpdateToastVisibility()
        {
            if (toastPanel == null || !toastPanel.gameObject.activeSelf || toastHideTime <= 0f)
            {
                return;
            }

            if (Time.unscaledTime >= toastHideTime)
            {
                toastPanel.gameObject.SetActive(false);
            }
        }

        private void UpdatePanelVisibility()
        {
            if (panel == null || !panel.gameObject.activeSelf || panelHideTime <= 0f)
            {
                return;
            }

            if (Time.unscaledTime >= panelHideTime)
            {
                panel.gameObject.SetActive(false);
                panelHideTime = 0f;
            }
        }

        private void BuildIfNeeded()
        {
            if (panel != null && toastPanel != null && titleText != null && phaseText != null && messageText != null && toastTitleText != null && toastMessageText != null)
            {
                return;
            }

            font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (panel == null)
            {
                panel = new GameObject("RunFramingHud.Panel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                panel.transform.SetParent(transform, false);
                panel.anchorMin = new Vector2(0.5f, 1f);
                panel.anchorMax = new Vector2(0.5f, 1f);
                panel.pivot = new Vector2(0.5f, 1f);
                panel.anchoredPosition = new Vector2(0f, -28f);
                panel.sizeDelta = new Vector2(720f, 112f);
                panel.GetComponent<Image>().color = new Color(0.03f, 0.025f, 0.02f, 0.42f);
            }

            titleText ??= AddText("RunFramingHud.Title", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-28f, 24f), TextAnchor.UpperCenter, 18, FontStyle.Bold);
            phaseText ??= AddText("RunFramingHud.Phase", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(-28f, 20f), TextAnchor.UpperCenter, 13, FontStyle.Normal);
            messageText ??= AddText("RunFramingHud.Message", panel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-34f, 46f), TextAnchor.LowerCenter, 14, FontStyle.Normal);

            if (toastPanel == null)
            {
                toastPanel = new GameObject("RunFramingHud.WorldEntryToast", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                toastPanel.transform.SetParent(transform, false);
                toastPanel.anchorMin = new Vector2(0.5f, 1f);
                toastPanel.anchorMax = new Vector2(0.5f, 1f);
                toastPanel.pivot = new Vector2(0.5f, 1f);
                toastPanel.anchoredPosition = new Vector2(0f, -150f);
                toastPanel.sizeDelta = new Vector2(560f, 82f);
                toastPanel.GetComponent<Image>().color = new Color(0.02f, 0.015f, 0.012f, 0.68f);
                toastPanel.gameObject.SetActive(false);
            }

            toastTitleText ??= AddText("RunFramingHud.WorldEntryTitle", toastPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-30f, 24f), TextAnchor.UpperCenter, 17, FontStyle.Bold);
            toastMessageText ??= AddText("RunFramingHud.WorldEntryMessage", toastPanel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(-36f, 42f), TextAnchor.LowerCenter, 13, FontStyle.Italic);
        }

        private Text AddText(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            TextAnchor alignment,
            int fontSize,
            FontStyle fontStyle)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
