using Hollow.Core.App;
using Hollow.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.UI.Shell
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public sealed class PlatformShellController : MonoBehaviour
    {
        [SerializeField] private HollowPlatformKind platformKind = HollowPlatformKind.WindowsStandard3D;

        public void Configure(HollowPlatformKind nextPlatformKind)
        {
            platformKind = nextPlatformKind;
        }

        private Font font;

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        public void ReturnToMainMenu()
        {
            HollowBootstrap.Instance?.AppStateMachine.TransitionTo(AppShellRoute.MainMenu);
            SceneLoaderService.LoadRouteAsync(AppShellRoute.MainMenu);
        }

        private void Build()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = platformKind == HollowPlatformKind.WindowsStandard3D ? RenderMode.ScreenSpaceOverlay : RenderMode.WorldSpace;
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                transform.localPosition = new Vector3(0f, 1.3f, 2.4f);
                transform.localScale = Vector3.one * 0.002f;
            }

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            AddText($"Loaded Shell: {platformKind}", new Vector2(0f, 80f), 28);
            AddText("M1 route target reached. Gameplay starts in M2.", new Vector2(0f, 35f), 16);
            AddButton("Back To Main Menu", new Vector2(0f, -40f));
        }

        private void AddText(string text, Vector2 anchoredPosition, int size)
        {
            var textRoot = new GameObject(text, typeof(RectTransform), typeof(Text));
            textRoot.transform.SetParent(transform, false);
            var rect = (RectTransform)textRoot.transform;
            rect.sizeDelta = new Vector2(680f, 44f);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            var label = textRoot.GetComponent<Text>();
            label.text = text;
            label.font = font;
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        private void AddButton(string label, Vector2 anchoredPosition)
        {
            var buttonRoot = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonRoot.transform.SetParent(transform, false);
            var rect = (RectTransform)buttonRoot.transform;
            rect.sizeDelta = new Vector2(320f, 48f);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            buttonRoot.GetComponent<Image>().color = new Color(0.85f, 0.55f, 0.12f);
            buttonRoot.GetComponent<Button>().onClick.AddListener(ReturnToMainMenu);
            AddText(label, anchoredPosition, 16);
        }
    }
}
