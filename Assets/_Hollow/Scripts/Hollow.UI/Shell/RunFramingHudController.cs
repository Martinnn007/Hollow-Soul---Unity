using Hollow.Branches;
using Hollow.Data.Definitions;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.UI.Shell
{
    [RequireComponent(typeof(Canvas))]
    public sealed class RunFramingHudController : MonoBehaviour
    {
        [SerializeField] private RunFramingCatalogDefinition catalog;

        private BranchSessionController branchSessionController;
        private RectTransform panel;
        private Text titleText;
        private Text phaseText;
        private Text messageText;
        private Font font;
        private string lastKey;

        public RunFramingCatalogDefinition Catalog => catalog;

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
            if (!force && snapshot.SummaryKey == lastKey)
            {
                return;
            }

            lastKey = snapshot.SummaryKey;
            titleText.text = snapshot.Title;
            phaseText.text = $"{snapshot.PhaseLabel} | {snapshot.SeedSummary}";
            messageText.text = $"{snapshot.Subtitle}\n{snapshot.Message}";
        }

        private void BuildIfNeeded()
        {
            if (panel != null && titleText != null && phaseText != null && messageText != null)
            {
                return;
            }

            font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panel = new GameObject("RunFramingHud.Panel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.transform.SetParent(transform, false);
            panel.anchorMin = new Vector2(0.5f, 1f);
            panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, -28f);
            panel.sizeDelta = new Vector2(720f, 112f);
            panel.GetComponent<Image>().color = new Color(0.03f, 0.025f, 0.02f, 0.42f);

            titleText = AddText("RunFramingHud.Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-28f, 24f), TextAnchor.UpperCenter, 18, FontStyle.Bold);
            phaseText = AddText("RunFramingHud.Phase", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(-28f, 20f), TextAnchor.UpperCenter, 13, FontStyle.Normal);
            messageText = AddText("RunFramingHud.Message", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-34f, 46f), TextAnchor.LowerCenter, 14, FontStyle.Normal);
        }

        private Text AddText(
            string name,
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
            textObject.transform.SetParent(panel, false);
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
