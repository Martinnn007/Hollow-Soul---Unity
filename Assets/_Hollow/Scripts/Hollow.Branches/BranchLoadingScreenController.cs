using UnityEngine;
using UnityEngine.UI;

namespace Hollow.Branches
{
    public sealed class BranchLoadingScreenController : MonoBehaviour
    {
        private const int SortingOrder = 6000;

        private Canvas canvas;
        private Image background;
        private Text titleText;
        private Text stageText;
        private Slider progressSlider;

        public bool IsVisible => gameObject.activeSelf;

        public string CurrentStage { get; private set; } = string.Empty;

        public float CurrentProgress01 { get; private set; }

        public static BranchLoadingScreenController Create(Transform parent)
        {
            var root = new GameObject("BranchLoadingScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            root.transform.SetParent(parent, false);
            var controller = root.AddComponent<BranchLoadingScreenController>();
            controller.Configure();
            root.SetActive(false);
            return controller;
        }

        public void Show(string title, string stage, float progress01)
        {
            Configure();
            gameObject.SetActive(true);
            titleText.text = string.IsNullOrWhiteSpace(title) ? "Loading" : title;
            SetStage(stage, progress01);
        }

        public void SetStage(string stage, float progress01)
        {
            Configure();
            CurrentStage = stage ?? string.Empty;
            CurrentProgress01 = Mathf.Clamp01(progress01);
            stageText.text = CurrentStage;
            progressSlider.value = CurrentProgress01;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            CurrentStage = string.Empty;
            CurrentProgress01 = 0f;
        }

        private void Configure()
        {
            canvas = GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            var scaler = GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var rect = GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            background = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            background.color = new Color(0.015f, 0.012f, 0.02f, 1f);

            titleText ??= CreateText("Title", new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(900f, 80f), 42, TextAnchor.MiddleCenter);
            stageText ??= CreateText("Stage", new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(1000f, 54f), 24, TextAnchor.MiddleCenter);
            progressSlider ??= CreateSlider();
        }

        private Text CreateText(string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(transform, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.92f, 0.9f, 0.86f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Slider CreateSlider()
        {
            var sliderObject = new GameObject("Progress", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(transform, false);
            var rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.38f);
            rect.anchorMax = new Vector2(0.5f, 0.38f);
            rect.sizeDelta = new Vector2(640f, 14f);
            rect.anchoredPosition = Vector2.zero;

            var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(sliderObject.transform, false);
            var backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            backgroundObject.GetComponent<Image>().color = new Color(0.16f, 0.15f, 0.18f, 1f);

            var fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaObject.transform.SetParent(sliderObject.transform, false);
            var fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(fillAreaObject.transform, false);
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillObject.GetComponent<Image>().color = new Color(0.8f, 0.64f, 0.28f, 1f);

            var slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.fillRect = fillRect;
            slider.targetGraphic = fillObject.GetComponent<Image>();
            return slider;
        }
    }
}
