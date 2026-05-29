using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.Core.App
{
    public enum BootLoadingScreenState
    {
        Loading = 0,
        Ready = 1,
        Failed = 2
    }

    public sealed class BootLoadingScreenController : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private Text studioLabel;
        private Text titleLabel;
        private Text stageLabel;
        private Text errorLabel;
        private Slider progressSlider;
        private BootLoadingScreenState state;

        public BootLoadingScreenState State => state;

        public string CurrentStage { get; private set; } = string.Empty;

        public float CurrentProgress01 { get; private set; }

        public static BootLoadingScreenController Create(Transform parent)
        {
            var root = new GameObject("BootLoadingScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(Image));
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var image = root.GetComponent<Image>();
            image.color = new Color(0.035f, 0.032f, 0.036f, 1f);

            var controller = root.AddComponent<BootLoadingScreenController>();
            controller.Build();
            root.SetActive(false);
            return controller;
        }

        public void Show(string studioName, string title, string stage)
        {
            gameObject.SetActive(true);
            state = BootLoadingScreenState.Loading;
            CurrentProgress01 = 0f;
            CurrentStage = stage ?? string.Empty;
            EnsureBuilt();
            canvasGroup.alpha = 1f;
            studioLabel.text = string.IsNullOrWhiteSpace(studioName) ? "Hollow Soul" : studioName;
            titleLabel.text = string.IsNullOrWhiteSpace(title) ? "Hollow Soul" : title;
            stageLabel.text = CurrentStage;
            errorLabel.gameObject.SetActive(false);
            progressSlider.value = 0f;
        }

        public void SetStage(string stage, float progress01)
        {
            EnsureBuilt();
            CurrentStage = stage ?? string.Empty;
            CurrentProgress01 = Mathf.Clamp01(progress01);
            stageLabel.text = CurrentStage;
            progressSlider.value = CurrentProgress01;
        }

        public void MarkReady(string stage = "Ready")
        {
            state = BootLoadingScreenState.Ready;
            SetStage(stage, 1f);
        }

        public void ShowFailure(string message)
        {
            EnsureBuilt();
            state = BootLoadingScreenState.Failed;
            CurrentStage = "Startup failed";
            stageLabel.text = CurrentStage;
            errorLabel.text = string.IsNullOrWhiteSpace(message)
                ? "Startup failed. Check the console for details."
                : message;
            errorLabel.gameObject.SetActive(true);
            progressSlider.value = Mathf.Max(progressSlider.value, 0.01f);
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
        }

        public IEnumerator FadeOut(float seconds)
        {
            EnsureBuilt();
            var duration = Mathf.Max(0f, seconds);
            if (duration <= 0f)
            {
                canvasGroup.alpha = 0f;
                gameObject.SetActive(false);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void Build()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            studioLabel = CreateText("StudioLabel", font, 22, FontStyle.Normal, new Vector2(0.5f, 0.58f), new Vector2(560f, 40f));
            studioLabel.color = new Color(0.72f, 0.68f, 0.62f, 1f);

            titleLabel = CreateText("TitleLabel", font, 54, FontStyle.Bold, new Vector2(0.5f, 0.51f), new Vector2(760f, 84f));
            titleLabel.color = new Color(0.94f, 0.9f, 0.82f, 1f);

            stageLabel = CreateText("StageLabel", font, 20, FontStyle.Normal, new Vector2(0.5f, 0.39f), new Vector2(760f, 42f));
            stageLabel.color = new Color(0.66f, 0.69f, 0.72f, 1f);

            errorLabel = CreateText("ErrorLabel", font, 19, FontStyle.Normal, new Vector2(0.5f, 0.31f), new Vector2(900f, 72f));
            errorLabel.color = new Color(0.95f, 0.44f, 0.38f, 1f);
            errorLabel.gameObject.SetActive(false);

            progressSlider = CreateSlider("Progress", new Vector2(0.5f, 0.35f), new Vector2(460f, 8f));
        }

        private Text CreateText(string name, Font font, int size, FontStyle style, Vector2 anchor, Vector2 sizeDelta)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(transform, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;

            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private Slider CreateSlider(string name, Vector2 anchor, Vector2 sizeDelta)
        {
            var sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(transform, false);
            var rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(sliderObject.transform, false);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.18f, 0.17f, 0.18f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = new Color(0.72f, 0.54f, 0.34f, 1f);

            var slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = background.GetComponent<Image>();
            return slider;
        }

        private void EnsureBuilt()
        {
            if (canvasGroup == null)
            {
                Build();
            }
        }
    }
}
