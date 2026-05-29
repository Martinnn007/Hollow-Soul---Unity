using Hollow.Branches;
using Hollow.Core.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.UI.Shell
{
    [RequireComponent(typeof(Canvas))]
    public sealed class PickupRevealController : MonoBehaviour
    {
        private const float VisibleSeconds = 4.25f;

        private BranchSessionController branchSessionController;
        private RectTransform panelRect;
        private Image panelImage;
        private Text cardText;
        private Text toastText;
        private Font font;
        private int displayedSequence;
        private float hideAtTime;
        private float nextRefreshTime;
        private float nextProviderSearchTime;

        public void Bind(BranchSessionController controller)
        {
            branchSessionController = controller;
            Refresh();
        }

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildIfNeeded();
            Hide();
        }

        private void Update()
        {
            if (branchSessionController == null)
            {
                var now = Time.unscaledTime;
                if (now >= nextProviderSearchTime)
                {
                    branchSessionController = FindAnyObjectByType<BranchSessionController>();
                    nextProviderSearchTime = now + 0.5f;
                }
            }

            if (Time.unscaledTime >= nextRefreshTime)
            {
                Refresh();
            }

            if (panelRect != null && panelRect.gameObject.activeSelf && Time.time > hideAtTime)
            {
                Hide();
            }
        }

        private void Refresh()
        {
            BuildIfNeeded();
            if (branchSessionController == null)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + M137PerformanceComfortPolicy.PickupRevealMinRefreshIntervalSeconds;
            var model = branchSessionController.LatestPickupReveal;
            if (model.IsEmpty || model.Sequence == displayedSequence)
            {
                return;
            }

            displayedSequence = model.Sequence;
            panelRect.gameObject.SetActive(true);
            cardText.text = model.BodyText;
            toastText.text = string.IsNullOrWhiteSpace(model.ToastText) ? model.Title : model.ToastText;
            panelImage.color = new Color(model.RarityColor.r * 0.35f, model.RarityColor.g * 0.35f, model.RarityColor.b * 0.35f, 0.84f);
            cardText.color = Color.white;
            toastText.color = model.RarityColor;
            hideAtTime = Time.time + VisibleSeconds;
        }

        private void Hide()
        {
            if (panelRect != null)
            {
                panelRect.gameObject.SetActive(false);
            }
        }

        private void BuildIfNeeded()
        {
            if (panelRect != null)
            {
                return;
            }

            font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var panel = new GameObject("PickupReveal.Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
            panel.transform.SetParent(transform, false);
            panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(1f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 0.5f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-36f, -40f);
            panelRect.sizeDelta = new Vector2(430f, 185f);
            panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.8f);
            var outline = panel.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.65f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            cardText = AddText("PickupReveal.CardText", panel.transform, new Vector2(18f, 44f), new Vector2(-18f, -14f), 18, TextAnchor.UpperLeft);
            toastText = AddText("PickupReveal.ToastText", panel.transform, new Vector2(18f, 10f), new Vector2(-18f, -136f), 15, TextAnchor.UpperLeft);
        }

        private Text AddText(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
