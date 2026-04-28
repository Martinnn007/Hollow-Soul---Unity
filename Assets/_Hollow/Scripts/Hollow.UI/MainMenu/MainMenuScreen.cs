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

            if (controller.ViewModel.State == MainMenuState.CharacterSelect)
            {
                BuildCharacterSelect();
                return;
            }

            if (controller.ViewModel.State == MainMenuState.ChallengeSelect)
            {
                BuildChallengeSelect();
                return;
            }

            if (controller.ViewModel.SelectedProfile != null && !controller.ViewModel.SelectedProfile.IsEmpty)
            {
                var selected = controller.ViewModel.SelectedProfile;
                AddText(rootPanel, $"Selected: {selected.DisplayName} | Banked Souls: {selected.BankedSouls}", 18, FontStyle.Bold, new Vector2(0f, -95f), new Vector2(620f, 34f));
                if (selected.HasActiveRun)
                {
                    AddText(rootPanel, "Continue Active Run", 15, FontStyle.Bold, new Vector2(-190f, -138f), new Vector2(280f, 28f), new Color(0.88f, 1f, 0.82f));
                    AddButton(rootPanel, "Continue Windows", new Vector2(-190f, -175f), controller.LaunchContinueWindows, new Color(0.18f, 0.45f, 0.22f), new Vector2(300f, 40f));
                    AddButton(rootPanel, "Continue Bounded", new Vector2(-190f, -225f), controller.LaunchContinueVisionOSBounded, new Color(0.18f, 0.45f, 0.22f), new Vector2(300f, 40f));
                    AddButton(rootPanel, "Continue Immersive", new Vector2(-190f, -275f), controller.LaunchContinueVisionOSImmersive, new Color(0.18f, 0.45f, 0.22f), new Vector2(300f, 40f));
                }

                AddText(rootPanel, "New Run", 15, FontStyle.Bold, new Vector2(190f, -138f), new Vector2(280f, 28f), new Color(1f, 0.91f, 0.72f));
                AddButton(rootPanel, "New Windows", new Vector2(190f, -175f), controller.LaunchWindows, null, new Vector2(300f, 40f));
                AddButton(rootPanel, "New Bounded", new Vector2(190f, -225f), controller.LaunchVisionOSBounded, null, new Vector2(300f, 40f));
                AddButton(rootPanel, "New Immersive", new Vector2(190f, -275f), controller.LaunchVisionOSImmersive, null, new Vector2(300f, 40f));
                AddButton(rootPanel, "Challenges", new Vector2(-190f, -320f), controller.OpenChallenges, new Color(0.55f, 0.24f, 0.62f), new Vector2(300f, 40f));
                AddButton(rootPanel, "Room Designer", new Vector2(190f, -320f), controller.OpenRoomDesigner, new Color(0.25f, 0.44f, 0.78f), new Vector2(300f, 40f));
                AddButton(rootPanel, "Back To Profiles", new Vector2(0f, -365f), controller.BackToProfiles, new Color(0.22f, 0.25f, 0.33f));
            }
        }

        private void BuildCharacterSelect()
        {
            AddText(rootPanel, $"Choose Character for {controller.ViewModel.PendingNewRunPlatformKind}", 19, FontStyle.Bold, new Vector2(0f, -95f), new Vector2(620f, 34f), new Color(1f, 0.91f, 0.72f));

            var balanced = AddButton(rootPanel, "Balanced", new Vector2(-170f, -190f), controller.SelectBalancedCharacter, new Color(0.20f, 0.39f, 0.70f), new Vector2(260f, 150f));
            AddText(balanced.transform as RectTransform, "Steady Form\n6 HP | 4.0 speed\n+10 stamina, +1 regen", 13, FontStyle.Normal, new Vector2(0f, -25f), new Vector2(230f, 92f), new Color(0.86f, 0.92f, 1f));

            var heavy = AddButton(rootPanel, "Heavy", new Vector2(170f, -190f), controller.SelectHeavyCharacter, new Color(0.48f, 0.31f, 0.18f), new Vector2(260f, 150f));
            AddText(heavy.transform as RectTransform, "Crushing Grip\n9 HP | 3.15 speed\n2 defense, melee lean", 13, FontStyle.Normal, new Vector2(0f, -25f), new Vector2(230f, 92f), new Color(1f, 0.88f, 0.72f));

            AddButton(rootPanel, "Back", new Vector2(0f, -335f), controller.BackFromCharacterSelect, new Color(0.22f, 0.25f, 0.33f));
        }

        private void BuildChallengeSelect()
        {
            AddText(rootPanel, "Challenge Mode - fixed seeds, transient runs", 19, FontStyle.Bold, new Vector2(0f, -75f), new Vector2(620f, 34f), new Color(0.94f, 0.78f, 1f));
            var challenges = controller.ViewModel.Challenges;
            for (var index = 0; index < challenges.Count; index++)
            {
                var challenge = challenges[index];
                var y = -145f - index * 92f;
                var panel = CreatePanel($"Challenge.{challenge.ChallengeId}", rootPanel, new Vector2(600f, 78f), new Color(0.11f, 0.09f, 0.17f, 0.92f));
                panel.anchoredPosition = new Vector2(0f, y);
                AddText(panel, $"{challenge.DisplayName} | Seed {challenge.FixedRunSeed}", 16, FontStyle.Bold, new Vector2(-135f, 18f), new Vector2(310f, 28f), new Color(1f, 0.91f, 0.72f));
                AddText(panel, CompactRules(challenge.RulesSummary), 11, FontStyle.Normal, new Vector2(-135f, -17f), new Vector2(310f, 42f), new Color(0.84f, 0.83f, 0.94f));
                AddButton(panel, "Win", new Vector2(120f, 16f), () => controller.LaunchChallengeWindows(challenge.ChallengeId), new Color(0.55f, 0.24f, 0.62f), new Vector2(86f, 28f));
                AddButton(panel, "Bounded", new Vector2(215f, 16f), () => controller.LaunchChallengeVisionOSBounded(challenge.ChallengeId), new Color(0.55f, 0.24f, 0.62f), new Vector2(94f, 28f));
                AddButton(panel, "Imm", new Vector2(310f, 16f), () => controller.LaunchChallengeVisionOSImmersive(challenge.ChallengeId), new Color(0.55f, 0.24f, 0.62f), new Vector2(86f, 28f));
            }

            AddButton(rootPanel, "Back", new Vector2(0f, -365f), controller.BackFromChallenges, new Color(0.22f, 0.25f, 0.33f));
        }

        private static string CompactRules(string rules)
        {
            if (string.IsNullOrWhiteSpace(rules))
            {
                return "Fixed seed challenge.";
            }

            return rules.Length <= 96 ? rules : rules.Substring(0, 93) + "...";
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
