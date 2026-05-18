using System.Globalization;
using Hollow.Input;
using Hollow.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Hollow.Diagnostics
{
    public sealed class VisionOSGameplayInputDiagnostics : MonoBehaviour
    {
        private const string HudCanvasName = "VisionOSInputDiagnosticsCanvas";
        private const string HudTextName = "VisionOSInputDiagnosticsText";
        private const float NonZeroMoveLogThreshold = 0.01f;

        [SerializeField] private bool forceEnabled;
        [SerializeField] private bool showHudInDevelopmentBuild = true;
        [SerializeField] private bool enablePointerMovePad;
        [SerializeField] private bool logDeviceSummaryOnStart = true;
        [SerializeField] private bool logFirstMovementSample = true;
        [SerializeField] private bool logGamepadInputEvents = true;
        [SerializeField] private float noInputSampleLogDelaySeconds = 2f;
        [SerializeField] private float noInputSampleLogIntervalSeconds = 3f;
        [SerializeField] private float hudUpdateIntervalSeconds = 0.2f;
        [SerializeField] private float gamepadEventLogIntervalSeconds = 1f;

        private Text hudText;
        private VisionOSMovePadControl movePadControl;
        private float nextHudUpdateTime;
        private float nextNoInputSampleLogTime;
        private float nextGamepadEventLogTime;
        private bool subscribedToDeviceChanges;
        private bool subscribedToInputEvents;
        private int gamepadEventCount;

        public string LastDeviceSummary { get; private set; }

        public string LastHudLine { get; private set; }

        public bool HasLoggedFirstMovement { get; private set; }

        public bool HasSeenGamepadInputEvent { get; private set; }

        public string LastGamepadEventSummary { get; private set; }

        private bool DiagnosticsEnabled => forceEnabled || Application.isEditor || Debug.isDebugBuild;

        private bool ShouldShowHud => DiagnosticsEnabled && showHudInDevelopmentBuild;

        private void OnEnable()
        {
            if (!DiagnosticsEnabled || subscribedToDeviceChanges)
            {
                return;
            }

            InputSystem.onDeviceChange += HandleDeviceChange;
            subscribedToDeviceChanges = true;
            InputSystem.onEvent += HandleInputEvent;
            subscribedToInputEvents = true;
        }

        private void OnDisable()
        {
            if (!subscribedToDeviceChanges)
            {
                return;
            }

            InputSystem.onDeviceChange -= HandleDeviceChange;
            subscribedToDeviceChanges = false;
            if (subscribedToInputEvents)
            {
                InputSystem.onEvent -= HandleInputEvent;
                subscribedToInputEvents = false;
            }
        }

        private void Start()
        {
            if (!DiagnosticsEnabled)
            {
                return;
            }

            LastDeviceSummary = BuildDeviceSummary();
            if (logDeviceSummaryOnStart)
            {
                Debug.Log($"VisionOS gameplay input devices: {LastDeviceSummary}");
            }

            nextNoInputSampleLogTime = Time.unscaledTime + Mathf.Max(0.25f, noInputSampleLogDelaySeconds);

            if (ShouldShowHud)
            {
                EnsureHud();
            }
        }

        private void Update()
        {
            if (!DiagnosticsEnabled)
            {
                return;
            }

            var pointerMove = movePadControl != null ? movePadControl.Move : Vector2.zero;
            GameplayInputReader.SetExternalMoveOverride(pointerMove);

            var gameplayRoot = ResolveGameplayRoot();
            var move = GameplayInputReader.ReadMoveForDiagnostics(gameplayRoot);
            if (Time.unscaledTime >= nextHudUpdateTime)
            {
                nextHudUpdateTime = Time.unscaledTime + Mathf.Max(0.05f, hudUpdateIntervalSeconds);
                LastHudLine = BuildHudLine(move);
                if (hudText != null)
                {
                    hudText.text = LastHudLine;
                }
            }

            if (logFirstMovementSample && !HasLoggedFirstMovement && move.sqrMagnitude > NonZeroMoveLogThreshold)
            {
                HasLoggedFirstMovement = true;
                LastHudLine = BuildHudLine(move);
                Debug.Log($"VisionOS gameplay input movement: {LastHudLine} | {BuildDeviceSummary()}");
            }

            if (!HasLoggedFirstMovement && Time.unscaledTime >= nextNoInputSampleLogTime)
            {
                nextNoInputSampleLogTime = Time.unscaledTime + Mathf.Max(0.5f, noInputSampleLogIntervalSeconds);
                Debug.Log($"VisionOS gameplay input sample: {GameplayInputReader.DescribeCurrentInputSamples(gameplayRoot)}");
            }
        }

        public string BuildDeviceSummary()
        {
            return GameplayInputReader.DescribeConnectedInputDevices();
        }

        public string BuildHudLine(Vector2 move)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Keyboard: {0} | Gamepad: {1} | Joystick: {2} | Move: {3:0.00}/{4:0.00}",
                GameplayInputReader.HasKeyboardDevice ? "yes" : "no",
                BuildGamepadStatus(),
                GameplayInputReader.HasJoystickDevice ? "yes" : "no",
                move.x,
                move.y);
        }

        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            LastDeviceSummary = BuildDeviceSummary();
            Debug.Log($"VisionOS gameplay input device change: {change} {device?.displayName ?? "unknown"} | {LastDeviceSummary}");
        }

        private void HandleInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (!DiagnosticsEnabled || !logGamepadInputEvents || !IsLikelyGamepadDevice(device))
            {
                return;
            }

            HasSeenGamepadInputEvent = true;
            gamepadEventCount++;
            LastGamepadEventSummary =
                $"device={device.displayName} layout={device.layout} type={eventPtr.type} size={eventPtr.sizeInBytes} count={gamepadEventCount}";

            if (Time.unscaledTime < nextGamepadEventLogTime)
            {
                return;
            }

            nextGamepadEventLogTime = Time.unscaledTime + Mathf.Max(0.1f, gamepadEventLogIntervalSeconds);
            Debug.Log($"VisionOS gameplay input event: {LastGamepadEventSummary} | {GameplayInputReader.DescribeCurrentInputSamples(ResolveGameplayRoot())}");
        }

        private string BuildGamepadStatus()
        {
            if (!GameplayInputReader.HasGamepadDevice)
            {
                return "none";
            }

            return HasSeenGamepadInputEvent ? "input seen" : "connected/no events";
        }

        private static bool IsLikelyGamepadDevice(InputDevice device)
        {
            if (device == null)
            {
                return false;
            }

            if (device is Gamepad)
            {
                return true;
            }

            var description = device.description;
            var identity = string.Concat(
                device.layout,
                " ",
                device.name,
                " ",
                device.displayName,
                " ",
                description.product,
                " ",
                description.manufacturer).ToLowerInvariant();

            return
                identity.Contains("gamepad") ||
                identity.Contains("controller") ||
                identity.Contains("dualshock") ||
                identity.Contains("dualsense") ||
                identity.Contains("xbox") ||
                identity.Contains("joystick");
        }

        private void EnsureHud()
        {
            if (hudText != null)
            {
                return;
            }

            var existingCanvas = transform.Find(HudCanvasName);
            var canvasObject = existingCanvas != null ? existingCanvas.gameObject : new GameObject(HudCanvasName, typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.layer = LayerMask.NameToLayer("UI");

            var canvas = canvasObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 200;
            if (enablePointerMovePad && canvasObject.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.localPosition = new Vector3(-0.55f, 0.34f, 0.56f);
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * 0.001f;
            canvasRect.sizeDelta = enablePointerMovePad ? new Vector2(860f, 260f) : new Vector2(860f, 80f);

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasObject.AddComponent<CanvasScaler>();
            }

            scaler.dynamicPixelsPerUnit = 16f;

            var textTransform = canvasObject.transform.Find(HudTextName);
            var textObject = textTransform != null ? textTransform.gameObject : new GameObject(HudTextName, typeof(RectTransform));
            textObject.transform.SetParent(canvasObject.transform, false);
            textObject.layer = canvasObject.layer;

            hudText = textObject.GetComponent<Text>();
            if (hudText == null)
            {
                hudText = textObject.AddComponent<Text>();
            }

            hudText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hudText.fontSize = 22;
            hudText.alignment = TextAnchor.MiddleLeft;
            hudText.color = new Color(0.75f, 0.95f, 1f, 0.92f);
            hudText.raycastTarget = false;

            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = enablePointerMovePad ? new Vector2(0f, 0.68f) : Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 0f);
            textRect.offsetMax = new Vector2(-12f, 0f);
            textRect.localScale = Vector3.one;

            if (enablePointerMovePad)
            {
                EnsureMovePad(canvasObject.transform);
            }

            LastHudLine = BuildHudLine(GameplayInputReader.ReadMoveForDiagnostics(ResolveGameplayRoot()));
            hudText.text = LastHudLine;
        }

        private Transform ResolveGameplayRoot()
        {
            var presentationRoot = FindAnyObjectByType<PlatformPresentationRoot>();
            return presentationRoot != null ? presentationRoot.transform : null;
        }

        private void EnsureMovePad(Transform parent)
        {
            const string padName = "VisionOSMovePad";
            const string knobName = "VisionOSMovePadKnob";

            var padTransform = parent.Find(padName);
            var padObject = padTransform != null ? padTransform.gameObject : new GameObject(padName, typeof(RectTransform), typeof(Image));
            padObject.transform.SetParent(parent, false);
            padObject.layer = parent.gameObject.layer;

            var padImage = padObject.GetComponent<Image>();
            padImage.color = new Color(0.12f, 0.35f, 0.5f, 0.32f);
            padImage.raycastTarget = true;

            var padRect = padObject.GetComponent<RectTransform>();
            padRect.anchorMin = new Vector2(0f, 0f);
            padRect.anchorMax = new Vector2(0f, 0f);
            padRect.pivot = new Vector2(0.5f, 0.5f);
            padRect.anchoredPosition = new Vector2(110f, 92f);
            padRect.sizeDelta = new Vector2(170f, 170f);
            padRect.localScale = Vector3.one;

            var knobTransform = padObject.transform.Find(knobName);
            var knobObject = knobTransform != null ? knobTransform.gameObject : new GameObject(knobName, typeof(RectTransform), typeof(Image));
            knobObject.transform.SetParent(padObject.transform, false);
            knobObject.layer = padObject.layer;

            var knobImage = knobObject.GetComponent<Image>();
            knobImage.color = new Color(0.75f, 0.95f, 1f, 0.78f);
            knobImage.raycastTarget = false;

            var knobRect = knobObject.GetComponent<RectTransform>();
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.anchoredPosition = Vector2.zero;
            knobRect.sizeDelta = new Vector2(44f, 44f);
            knobRect.localScale = Vector3.one;

            movePadControl = padObject.GetComponent<VisionOSMovePadControl>();
            if (movePadControl == null)
            {
                movePadControl = padObject.AddComponent<VisionOSMovePadControl>();
            }

            movePadControl.Bind(knobRect);
        }
    }

    internal sealed class VisionOSMovePadControl : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform rectTransform;
        private RectTransform knobTransform;

        public Vector2 Move { get; private set; }

        public void Bind(RectTransform knob)
        {
            rectTransform = GetComponent<RectTransform>();
            knobTransform = knob;
            UpdateKnob();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateMove(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateMove(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Move = Vector2.zero;
            UpdateKnob();
        }

        private void UpdateMove(PointerEventData eventData)
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    eventData.position,
                    eventData.pressEventCamera ?? eventData.enterEventCamera,
                    out var localPoint))
            {
                Move = Vector2.zero;
                UpdateKnob();
                return;
            }

            var rect = rectTransform.rect;
            var halfWidth = Mathf.Max(1f, rect.width * 0.5f);
            var halfHeight = Mathf.Max(1f, rect.height * 0.5f);
            Move = Vector2.ClampMagnitude(new Vector2(localPoint.x / halfWidth, localPoint.y / halfHeight), 1f);
            UpdateKnob();
        }

        private void UpdateKnob()
        {
            if (knobTransform == null)
            {
                return;
            }

            var rect = rectTransform != null ? rectTransform.rect : new Rect(0f, 0f, 170f, 170f);
            knobTransform.anchoredPosition = new Vector2(Move.x * rect.width * 0.32f, Move.y * rect.height * 0.32f);
        }
    }
}
