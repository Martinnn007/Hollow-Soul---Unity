using System;
using Hollow.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.UI.MainMenu
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public sealed class MainMenuScreen : MonoBehaviour
    {
        private MainMenuController controller;
        private RectTransform rootPanel;
        private Font font;

        public void Build(MainMenuController controller)
        {
            this.controller = controller;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ConfigureCanvas();
            Rebuild();
        }

        public void Rebuild()
        {
            if (controller == null)
            {
                return;
            }

            ClearChildren();

            rootPanel = CreatePanel("Panel", transform, new Vector2(680f, 760f), new Color(0.04f, 0.05f, 0.08f, 0.92f));
            AddText(rootPanel, "Hollow Soul", 42, FontStyle.Bold, new Vector2(0f, 315f), new Vector2(620f, 64f));
            AddText(rootPanel, "Unity foundation menu - shared logic, platform-specific presentation", 16, FontStyle.Normal, new Vector2(0f, 270f), new Vector2(620f, 40f));

            if (controller.ViewModel.State == MainMenuState.Error)
            {
                AddText(rootPanel, controller.ViewModel.ErrorMessage, 18, FontStyle.Bold, new Vector2(0f, 225f), new Vector2(610f, 42f), new Color(1f, 0.45f, 0.45f));
            }

            BuildProfileCards();

            if (controller.ViewModel.SelectedProfile != null && !controller.ViewModel.SelectedProfile.IsEmpty)
            {
                AddText(rootPanel, $"Selected: {controller.ViewModel.SelectedProfile.DisplayName}", 18, FontStyle.Bold, new Vector2(0f, -110f), new Vector2(620f, 34f));
                AddButton(rootPanel, "Launch Windows", new Vector2(0f, -165f), controller.LaunchWindows);
                AddButton(rootPanel, "Launch Vision Pro Bounded", new Vector2(0f, -220f), controller.LaunchVisionOSBounded);
                AddButton(rootPanel, "Launch Vision Pro Immersive", new Vector2(0f, -275f), controller.LaunchVisionOSImmersive);
                AddButton(rootPanel, "Back To Profiles", new Vector2(0f, -330f), controller.BackToProfiles, new Color(0.22f, 0.25f, 0.33f));
            }
        }

        private void ConfigureCanvas()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void BuildProfileCards()
        {
            var cards = controller.ViewModel.ProfileCards;
            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                var x = -220f + index * 220f;
                var button = AddButton(rootPanel, card.Title, new Vector2(x, 110f), () => controller.SelectSlot(card.SlotIndex), new Color(0.13f, 0.20f, 0.36f), new Vector2(195f, 104f));
                AddText(button.transform as RectTransform, card.Subtitle, 13, FontStyle.Normal, new Vector2(0f, -26f), new Vector2(170f, 36f), new Color(0.82f, 0.88f, 1f));
            }
        }

        private Button AddButton(RectTransform parent, string label, Vector2 anchoredPosition, Action onClick, Color? color = null, Vector2? size = null)
        {
            var buttonRoot = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonRoot.transform.SetParent(parent, false);
            var rect = (RectTransform)buttonRoot.transform;
            rect.sizeDelta = size ?? new Vector2(360f, 44f);
            rect.anchoredPosition = anchoredPosition;

            var image = buttonRoot.GetComponent<Image>();
            image.color = color ?? new Color(0.85f, 0.55f, 0.12f);

            var button = buttonRoot.GetComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            AddText(rect, label, 15, FontStyle.Bold, Vector2.zero, rect.sizeDelta);
            return button;
        }

        private Text AddText(RectTransform parent, string text, int size, FontStyle style, Vector2 anchoredPosition, Vector2 sizeDelta, Color? color = null)
        {
            var textRoot = new GameObject(text, typeof(RectTransform), typeof(Text));
            textRoot.transform.SetParent(parent, false);
            var rect = (RectTransform)textRoot.transform;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;

            var label = textRoot.GetComponent<Text>();
            label.text = text;
            label.font = font;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color ?? Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 size, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = (RectTransform)panel.transform;
            rect.sizeDelta = size;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = color;
            return rect;
        }

        private void ClearChildren()
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                Destroy(transform.GetChild(index).gameObject);
            }
        }
    }
}
