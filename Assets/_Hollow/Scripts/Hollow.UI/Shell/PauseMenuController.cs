using System;
using Hollow.Core;
using Hollow.Input;
using Hollow.World;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.UI.Shell
{
    [RequireComponent(typeof(Canvas))]
    public sealed class PauseMenuController : MonoBehaviour
    {
        private const float PanelAlpha = 0.94f;

        private RectTransform root;
        private Font font;
        private GameSessionController gameSessionController;
        private PauseConfirmAction confirmAction;
        private float previousTimeScale = 1f;

        public PauseMenuState State { get; private set; } = PauseMenuState.Hidden;

        public bool IsVisible => State != PauseMenuState.Hidden;

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureRoot();
            HideRoot();
        }

        private void Update()
        {
            ResolveSessionController();
            if (!CanPauseGameplay())
            {
                return;
            }

            if (!GameplayInputReader.ReadPausePressed())
            {
                return;
            }

            if (State == PauseMenuState.Hidden)
            {
                ShowRoot();
            }
            else if (State == PauseMenuState.Root)
            {
                Resume();
            }
            else
            {
                ShowRoot();
            }
        }

        private void OnDestroy()
        {
            if (State != PauseMenuState.Hidden)
            {
                RestoreGameplay();
            }
        }

        public void Resume()
        {
            State = PauseMenuState.Hidden;
            confirmAction = PauseConfirmAction.None;
            ClearRoot();
            HideRoot();
            RestoreGameplay();
        }

        public void ShowRoot()
        {
            PauseGameplay();
            State = PauseMenuState.Root;
            confirmAction = PauseConfirmAction.None;
            ClearRoot();
            ShowRootObject();

            var panel = CreatePanel("Pause.Root", root, new Vector2(520f, 520f), new Color(0.04f, 0.05f, 0.08f, PanelAlpha));
            AddText(panel, "Paused", 34, FontStyle.Bold, new Vector2(0f, 195f), new Vector2(460f, 56f));
            AddText(panel, "Run state is frozen. UI remains active.", 14, FontStyle.Normal, new Vector2(0f, 155f), new Vector2(460f, 28f), new Color(0.76f, 0.83f, 0.92f));
            AddButton(panel, "Resume", new Vector2(0f, 92f), Resume, new Color(0.18f, 0.45f, 0.22f));
            AddButton(panel, "Restart", new Vector2(0f, 36f), () => ShowConfirm(PauseConfirmAction.Restart), new Color(0.60f, 0.36f, 0.12f));
            AddButton(panel, "Settings", new Vector2(0f, -20f), ShowSettings, new Color(0.20f, 0.34f, 0.58f));
            AddButton(panel, "Quit", new Vector2(0f, -76f), () => ShowConfirm(PauseConfirmAction.Quit), new Color(0.52f, 0.18f, 0.18f));
            AddText(panel, "Esc / Options resumes from this screen.", 12, FontStyle.Normal, new Vector2(0f, -175f), new Vector2(440f, 28f), new Color(0.68f, 0.72f, 0.80f));
        }

        public void ShowSettings()
        {
            PauseGameplay();
            State = PauseMenuState.Settings;
            ClearRoot();
            ShowRootObject();

            var panel = CreatePanel("Pause.Settings", root, new Vector2(620f, 560f), new Color(0.04f, 0.05f, 0.08f, PanelAlpha));
            AddText(panel, "Settings", 30, FontStyle.Bold, new Vector2(0f, 220f), new Vector2(540f, 52f));
            AddText(panel, "Audio, graphics, and accessibility are placeholders for now.", 13, FontStyle.Normal, new Vector2(0f, 180f), new Vector2(540f, 28f), new Color(0.76f, 0.83f, 0.92f));
            AddDisabledRow(panel, "Audio", "Placeholder");
            AddDisabledRow(panel, "Graphics", "Placeholder", -15f);
            AddDisabledRow(panel, "Accessibility", "Placeholder", -70f);
            AddButton(panel, "Controls", new Vector2(0f, -125f), ShowControls, new Color(0.20f, 0.44f, 0.70f));
            AddButton(panel, "Back", new Vector2(0f, -210f), ShowRoot, new Color(0.22f, 0.25f, 0.33f));
        }

        public void ShowControls()
        {
            PauseGameplay();
            State = PauseMenuState.Controls;
            ClearRoot();
            ShowRootObject();

            var panel = CreatePanel("Pause.Controls", root, new Vector2(900f, 660f), new Color(0.04f, 0.05f, 0.08f, PanelAlpha));
            AddText(panel, "Controls", 30, FontStyle.Bold, new Vector2(0f, 275f), new Vector2(820f, 50f));
            AddText(panel, "Normal gameplay layout. Room Designer controls are separate.", 13, FontStyle.Normal, new Vector2(0f, 238f), new Vector2(820f, 28f), new Color(0.76f, 0.83f, 0.92f));
            AddControlsColumn(panel, "Keyboard", new Vector2(-225f, 10f), new[]
            {
                "Move: WASD",
                "Aim: Arrow Keys",
                "Interact: E",
                "Swap Weapon: Tab",
                "Light Attack: J / Arrow Keys / Mouse Left",
                "Heavy Attack: K / Mouse Right",
                "Active Item: Q",
                "Consumable Card: F",
                "Guard: Shift",
                "Pause: Escape"
            });
            AddControlsColumn(panel, "DualShock 5", new Vector2(225f, 10f), new[]
            {
                "Move: Left Stick",
                "Aim: Right Stick",
                "Interact: Cross",
                "Swap Weapon: L1",
                "Light Attack: R1",
                "Heavy Attack: R2",
                "Active Item: Triangle",
                "Consumable Card: Square",
                "Guard: L2",
                "Pause: Options"
            });
            AddButton(panel, "Back", new Vector2(0f, -275f), ShowSettings, new Color(0.22f, 0.25f, 0.33f), new Vector2(300f, 42f));
        }

        private void ShowConfirm(PauseConfirmAction action)
        {
            PauseGameplay();
            State = PauseMenuState.Confirm;
            confirmAction = action;
            ClearRoot();
            ShowRootObject();

            var panel = CreatePanel("Pause.Confirm", root, new Vector2(650f, 410f), new Color(0.05f, 0.04f, 0.06f, PanelAlpha));
            var isRestart = action == PauseConfirmAction.Restart;
            var title = isRestart ? "Restart?" : "Quit?";
            var message = isRestart ? RestartMessage() : QuitMessage();
            AddText(panel, title, 30, FontStyle.Bold, new Vector2(0f, 135f), new Vector2(560f, 52f));
            AddText(panel, message, 15, FontStyle.Normal, new Vector2(0f, 45f), new Vector2(560f, 120f), new Color(0.90f, 0.88f, 0.82f));
            AddButton(panel, isRestart ? "Restart" : "Quit And Save", new Vector2(-150f, -110f), ConfirmAction, isRestart ? new Color(0.60f, 0.36f, 0.12f) : new Color(0.52f, 0.18f, 0.18f), new Vector2(250f, 44f));
            AddButton(panel, "Cancel", new Vector2(150f, -110f), ShowRoot, new Color(0.22f, 0.25f, 0.33f), new Vector2(250f, 44f));
        }

        private void ConfirmAction()
        {
            ResolveSessionController();
            var action = confirmAction;
            Resume();
            if (gameSessionController == null)
            {
                return;
            }

            if (action == PauseConfirmAction.Restart)
            {
                gameSessionController.RestartCurrentSession();
            }
            else if (action == PauseConfirmAction.Quit)
            {
                gameSessionController.QuitCurrentSessionToProfileMenu();
            }
        }

        private string RestartMessage()
        {
            return IsChallengeSession()
                ? "Restart this challenge from the beginning with its fixed curated seed and the same challenge loadout?"
                : "Restart this run from the beginning with the same character and a fresh random seed?";
        }

        private string QuitMessage()
        {
            return IsChallengeSession()
                ? "Save this challenge into the profile active-run slot and return to the profile menu. This can overwrite another saved run."
                : "Save the current run checkpoint and return to the profile menu. Continue can restore it later.";
        }

        private bool IsChallengeSession()
        {
            ResolveSessionController();
            return gameSessionController != null &&
                   (gameSessionController.SessionState?.SessionMode == RuntimeSessionMode.TransientChallenge ||
                    !string.IsNullOrWhiteSpace(gameSessionController.CurrentChallengeId));
        }

        private bool CanPauseGameplay()
        {
            ResolveSessionController();
            return gameSessionController != null &&
                   gameSessionController.SessionState?.SessionMode != RuntimeSessionMode.TransientRoomDesignerPlaytest;
        }

        private void PauseGameplay()
        {
            if (!GameplayPauseState.IsPaused)
            {
                previousTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            }

            GameplayPauseState.SetPaused(true);
            Time.timeScale = 0f;
        }

        private void RestoreGameplay()
        {
            GameplayPauseState.SetPaused(false);
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        }

        private void ResolveSessionController()
        {
            if (gameSessionController == null)
            {
                gameSessionController = FindFirstObjectByType<GameSessionController>();
            }
        }

        private void EnsureRoot()
        {
            if (root != null)
            {
                return;
            }

            var rootObject = new GameObject("PauseMenuRoot", typeof(RectTransform));
            rootObject.transform.SetParent(transform, false);
            root = (RectTransform)rootObject.transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        private void ShowRootObject()
        {
            EnsureRoot();
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
        }

        private void HideRoot()
        {
            EnsureRoot();
            root.gameObject.SetActive(false);
        }

        private void ClearRoot()
        {
            EnsureRoot();
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                DestroyRuntimeObject(root.GetChild(index).gameObject);
            }
        }

        private static void DestroyRuntimeObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void AddDisabledRow(RectTransform parent, string label, string status, float y = 40f)
        {
            var panel = CreatePanel($"Setting.{label}", parent, new Vector2(440f, 42f), new Color(0.11f, 0.12f, 0.16f, 0.90f));
            panel.anchoredPosition = new Vector2(0f, y);
            AddText(panel, label, 15, FontStyle.Bold, new Vector2(-120f, 0f), new Vector2(160f, 32f), new Color(0.86f, 0.90f, 1f));
            AddText(panel, status, 13, FontStyle.Normal, new Vector2(120f, 0f), new Vector2(160f, 32f), new Color(0.68f, 0.72f, 0.80f));
        }

        private void AddControlsColumn(RectTransform parent, string title, Vector2 anchoredPosition, string[] rows)
        {
            var panel = CreatePanel($"Controls.{title}", parent, new Vector2(380f, 405f), new Color(0.10f, 0.11f, 0.16f, 0.92f));
            panel.anchoredPosition = anchoredPosition;
            AddText(panel, title, 20, FontStyle.Bold, new Vector2(0f, 175f), new Vector2(330f, 34f), new Color(1f, 0.91f, 0.72f));
            for (var index = 0; index < rows.Length; index++)
            {
                AddText(panel, rows[index], 14, FontStyle.Normal, new Vector2(0f, 130f - index * 31f), new Vector2(330f, 28f), Color.white);
            }
        }

        private Button AddButton(RectTransform parent, string label, Vector2 anchoredPosition, Action onClick, Color color, Vector2? size = null)
        {
            var buttonRoot = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonRoot.transform.SetParent(parent, false);
            var rect = (RectTransform)buttonRoot.transform;
            rect.sizeDelta = size ?? new Vector2(320f, 44f);
            rect.anchoredPosition = anchoredPosition;
            buttonRoot.GetComponent<Image>().color = color;
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
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = color;
            return rect;
        }
    }
}
