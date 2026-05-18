using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.UI.MainMenu
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public sealed class VisionOSMainMenuScreen : MonoBehaviour
    {
        private const string PolySpatialUISortingLayerName = "PolySpatialUI";

        private enum MenuStep
        {
            SaveSlot,
            Mode,
            CharacterForRun,
            CharacterForArena,
            Challenge
        }

        private sealed class MenuButtonBinding
        {
            public RectTransform Rect;
            public Button Button;
            public CanvasGroup Group;
            public Text Label;
            public Text Detail;
        }

        private readonly List<MenuButtonBinding> saveSlotButtons = new();
        private readonly List<MenuButtonBinding> runCharacterButtons = new();
        private readonly List<MenuButtonBinding> arenaCharacterButtons = new();
        private readonly List<MenuButtonBinding> challengeButtons = new();

        private MainMenuController controller;
        private RectTransform rootPanel;
        private RectTransform saveSlotPanel;
        private RectTransform modePanel;
        private RectTransform runCharacterPanel;
        private RectTransform arenaCharacterPanel;
        private RectTransform challengePanel;
        private MenuButtonBinding continueRunButton;
        private MenuButtonBinding normalRunButton;
        private MenuButtonBinding challengesButton;
        private MenuButtonBinding arenaButton;
        private Text stepTitleLabel;
        private Text statusLabel;
        private Text profileSummaryLabel;
        private Font font;
        private MenuStep step = MenuStep.SaveSlot;
        private string statusMessage = "Choose a save slot.";

        public string CurrentStepName => step.ToString();

        public string CurrentStatusMessage => statusMessage;

        public void Build(MainMenuController nextController)
        {
            controller = nextController;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ConfigureCanvas();
            BuildStaticLayout();
            Rebuild();
        }

        public void Rebuild()
        {
            if (controller == null)
            {
                return;
            }

            ConfigureCanvas();
            if (rootPanel == null)
            {
                BuildStaticLayout();
            }

            if (controller.ViewModel.SelectedProfile == null || controller.ViewModel.State == MainMenuState.ProfileSelect)
            {
                step = MenuStep.SaveSlot;
            }

            RefreshSaveSlots();
            RefreshModeButtons();
            RefreshStepVisibility();
            LogActiveButtons();
        }

        private void BuildStaticLayout()
        {
            ClearChildrenImmediate();
            saveSlotButtons.Clear();
            runCharacterButtons.Clear();
            arenaCharacterButtons.Clear();
            challengeButtons.Clear();

            rootPanel = CreatePanel("VisionOSMainMenuPanel", transform, new Vector2(900f, 640f), new Color(0.035f, 0.045f, 0.065f, 0.94f));
            AddText(rootPanel, "Hollow Soul", 38, FontStyle.Bold, new Vector2(0f, 260f), new Vector2(820f, 52f), new Color(1f, 0.92f, 0.74f));
            stepTitleLabel = AddText(rootPanel, StepTitle(), 18, FontStyle.Bold, new Vector2(0f, 216f), new Vector2(820f, 34f), new Color(0.82f, 0.9f, 1f));
            statusLabel = AddText(rootPanel, statusMessage, 12, FontStyle.Normal, new Vector2(0f, -286f), new Vector2(820f, 28f), new Color(0.72f, 0.82f, 0.95f));

            saveSlotPanel = CreatePanel("SaveSlotPanel", rootPanel, new Vector2(850f, 360f), new Color(0f, 0f, 0f, 0f));
            modePanel = CreatePanel("ModePanel", rootPanel, new Vector2(850f, 420f), new Color(0f, 0f, 0f, 0f));
            runCharacterPanel = CreatePanel("RunCharacterPanel", rootPanel, new Vector2(850f, 420f), new Color(0f, 0f, 0f, 0f));
            arenaCharacterPanel = CreatePanel("ArenaCharacterPanel", rootPanel, new Vector2(850f, 420f), new Color(0f, 0f, 0f, 0f));
            challengePanel = CreatePanel("ChallengePanel", rootPanel, new Vector2(850f, 455f), new Color(0f, 0f, 0f, 0f));

            BuildSaveSlotControls();
            BuildModeControls();
            BuildCharacterControls(runCharacterPanel, runCharacterButtons, launchArena: false);
            BuildCharacterControls(arenaCharacterPanel, arenaCharacterButtons, launchArena: true);
            BuildChallengeControls();
        }

        private void BuildSaveSlotControls()
        {
            var cards = controller.ViewModel.ProfileCards;
            for (var index = 0; index < cards.Count; index++)
            {
                var slotIndex = index;
                var x = -280f + index * 280f;
                var binding = AddButton(saveSlotPanel, $"Profile {slotIndex + 1}", new Vector2(x, 68f), () => SelectSlot(slotIndex), new Color(0.12f, 0.22f, 0.38f), new Vector2(240f, 148f), stepName: MenuStep.SaveSlot.ToString());
                binding.Detail = AddText(binding.Rect, string.Empty, 14, FontStyle.Normal, new Vector2(0f, -32f), new Vector2(210f, 52f), new Color(0.82f, 0.9f, 1f));
                saveSlotButtons.Add(binding);
            }
        }

        private void BuildModeControls()
        {
            profileSummaryLabel = AddText(modePanel, string.Empty, 17, FontStyle.Bold, new Vector2(0f, 158f), new Vector2(760f, 32f));
            continueRunButton = AddButton(modePanel, "Continue Run", new Vector2(-260f, 86f), LaunchContinueRun, new Color(0.16f, 0.42f, 0.24f), new Vector2(320f, 92f), disableAfterActivation: true, stepName: MenuStep.Mode.ToString());
            normalRunButton = AddButton(modePanel, "Normal Run", new Vector2(260f, 86f), BeginNormalRunCharacterSelect, new Color(0.72f, 0.45f, 0.14f), new Vector2(320f, 92f), stepName: MenuStep.Mode.ToString());
            challengesButton = AddButton(modePanel, "Challenges", new Vector2(-260f, -40f), OpenChallenges, new Color(0.44f, 0.24f, 0.62f), new Vector2(320f, 92f), stepName: MenuStep.Mode.ToString());
            arenaButton = AddButton(modePanel, "Arena", new Vector2(260f, -40f), BeginArenaCharacterSelect, new Color(0.64f, 0.28f, 0.18f), new Vector2(320f, 92f), stepName: MenuStep.Mode.ToString());
            AddButton(modePanel, "Back", new Vector2(0f, -190f), BackToSlots, new Color(0.20f, 0.23f, 0.30f), new Vector2(240f, 54f), stepName: MenuStep.Mode.ToString());
        }

        private void BuildCharacterControls(RectTransform panel, List<MenuButtonBinding> bindings, bool launchArena)
        {
            var characters = controller.ViewModel.Characters.Where(character => character != null).ToArray();
            if (characters.Length == 0)
            {
                characters = CharacterCatalogDefinition.CreateRuntimeDefault().Characters.ToArray();
            }

            var startX = characters.Length == 1 ? 0f : -170f;
            var stepName = launchArena ? MenuStep.CharacterForArena.ToString() : MenuStep.CharacterForRun.ToString();
            for (var index = 0; index < characters.Length; index++)
            {
                var character = characters[index];
                var x = startX + index * 340f;
                var binding = AddButton(panel, character.DisplayName, new Vector2(x, 42f), () => LaunchCharacter(character.CharacterId, launchArena), CharacterColor(character.CharacterId), new Vector2(310f, 185f), disableAfterActivation: true, stepName: stepName);
                binding.Detail = AddText(binding.Rect, CharacterSummary(character), 13, FontStyle.Normal, new Vector2(0f, -38f), new Vector2(240f, 92f), new Color(0.9f, 0.92f, 1f));
                bindings.Add(binding);
            }

            AddButton(panel, "Back", new Vector2(0f, -190f), BackToModes, new Color(0.20f, 0.23f, 0.30f), new Vector2(240f, 54f), stepName: stepName);
        }

        private void BuildChallengeControls()
        {
            var challenges = controller.ViewModel.Challenges;
            for (var index = 0; index < challenges.Count; index++)
            {
                var challenge = challenges[index];
                var y = 120f - index * 58f;
                var panel = CreatePanel($"Challenge.{challenge.ChallengeId}", challengePanel, new Vector2(760f, 50f), new Color(0.10f, 0.08f, 0.15f, 0.95f));
                panel.anchoredPosition = new Vector2(0f, y);
                AddText(panel, challenge.DisplayName, 14, FontStyle.Bold, new Vector2(-265f, 9f), new Vector2(180f, 22f), new Color(1f, 0.92f, 0.74f));
                AddText(panel, $"Seed {challenge.FixedRunSeed} | {CharacterDisplayName(challenge.SelectedCharacterId)}", 11, FontStyle.Normal, new Vector2(-265f, -11f), new Vector2(180f, 20f), new Color(0.84f, 0.9f, 1f));
                AddText(panel, controller.ViewModel.ChallengeRecordSummary(challenge.ChallengeId), 11, FontStyle.Normal, new Vector2(-30f, 0f), new Vector2(240f, 34f), new Color(0.8f, 0.95f, 0.84f));
                challengeButtons.Add(AddButton(panel, "Launch", new Vector2(275f, 0f), () => LaunchChallenge(challenge.ChallengeId), new Color(0.44f, 0.24f, 0.62f), new Vector2(170f, 38f), disableAfterActivation: true, stepName: MenuStep.Challenge.ToString()));
            }

            AddButton(challengePanel, "Back", new Vector2(0f, -236f), BackToModes, new Color(0.20f, 0.23f, 0.30f), new Vector2(240f, 54f), stepName: MenuStep.Challenge.ToString());
        }

        private void RefreshSaveSlots()
        {
            var cards = controller.ViewModel.ProfileCards;
            for (var index = 0; index < saveSlotButtons.Count; index++)
            {
                var binding = saveSlotButtons[index];
                if (index >= cards.Count)
                {
                    SetButtonVisible(binding, visible: false);
                    continue;
                }

                var card = cards[index];
                binding.Rect.gameObject.name = card.Title;
                binding.Label.text = card.Title;
                if (binding.Detail != null)
                {
                    binding.Detail.text = card.Subtitle;
                }

                SetButtonVisible(binding, visible: true);
            }
        }

        private void RefreshModeButtons()
        {
            var selected = controller.ViewModel.SelectedProfile;
            var hasSelection = selected != null;
            if (profileSummaryLabel != null)
            {
                profileSummaryLabel.text = hasSelection ? $"{selected.DisplayName} | Souls {selected.BankedSouls}" : string.Empty;
            }

            var hasActiveRun = hasSelection && selected.HasActiveRun;
            SetButtonVisible(continueRunButton, hasActiveRun);
            if (normalRunButton != null)
            {
                normalRunButton.Rect.anchoredPosition = new Vector2(hasActiveRun ? 260f : -260f, 86f);
            }

            if (challengesButton != null)
            {
                challengesButton.Rect.anchoredPosition = new Vector2(hasActiveRun ? -260f : 260f, -40f);
            }

            if (arenaButton != null)
            {
                arenaButton.Rect.anchoredPosition = hasActiveRun ? new Vector2(260f, -40f) : new Vector2(0f, -40f);
            }
        }

        private void RefreshStepVisibility()
        {
            if (stepTitleLabel != null)
            {
                stepTitleLabel.text = StepTitle();
            }

            if (controller.ViewModel.State == MainMenuState.Error && !string.IsNullOrWhiteSpace(controller.ViewModel.ErrorMessage))
            {
                statusMessage = controller.ViewModel.ErrorMessage;
            }

            if (statusLabel != null)
            {
                statusLabel.text = statusMessage;
            }

            SetPanelVisible(saveSlotPanel, step == MenuStep.SaveSlot);
            SetPanelVisible(modePanel, step == MenuStep.Mode);
            SetPanelVisible(runCharacterPanel, step == MenuStep.CharacterForRun);
            SetPanelVisible(arenaCharacterPanel, step == MenuStep.CharacterForArena);
            SetPanelVisible(challengePanel, step == MenuStep.Challenge);
        }

        private string StepTitle()
        {
            return step switch
            {
                MenuStep.SaveSlot => "Choose Save Slot",
                MenuStep.Mode => "Choose Mode",
                MenuStep.CharacterForRun => "Choose Character",
                MenuStep.CharacterForArena => "Choose Arena Character",
                MenuStep.Challenge => "Choose Challenge",
                _ => string.Empty
            };
        }

        private void SelectSlot(int slotIndex)
        {
            SetStatus($"Selected save slot {slotIndex + 1}.");
            controller.ViewModel.SelectOrCreateSlot(slotIndex);
            step = controller.ViewModel.State == MainMenuState.Error ? MenuStep.SaveSlot : MenuStep.Mode;
            Rebuild();
        }

        private void BackToSlots()
        {
            SetStatus("Back to save slots.");
            controller.ViewModel.BackToProfiles();
            step = MenuStep.SaveSlot;
            Rebuild();
        }

        private void BackToModes()
        {
            SetStatus("Back to mode selection.");
            controller.ViewModel.BackFromChallenges();
            controller.ViewModel.BackFromCharacterSelect();
            step = MenuStep.Mode;
            Rebuild();
        }

        private void BeginNormalRunCharacterSelect()
        {
            SetStatus("Normal Run selected.");
            step = MenuStep.CharacterForRun;
            Rebuild();
        }

        private void BeginArenaCharacterSelect()
        {
            SetStatus("Arena selected.");
            step = MenuStep.CharacterForArena;
            Rebuild();
        }

        private void OpenChallenges()
        {
            SetStatus("Challenges selected.");
            controller.ViewModel.OpenChallenges();
            step = controller.ViewModel.State == MainMenuState.Error ? MenuStep.Mode : MenuStep.Challenge;
            Rebuild();
        }

        private void LaunchCharacter(string characterId, bool launchArena)
        {
            SetStatus(launchArena
                ? $"Launching Arena as {characterId}."
                : $"Launching Normal Run as {characterId}.");
            if (launchArena)
            {
                controller.LaunchArenaModeWithCharacter(characterId);
                RebuildIfErrored(MenuStep.CharacterForArena);
                return;
            }

            controller.ViewModel.BeginNewRun(controller.DefaultPlatformKind);
            if (RebuildIfErrored(MenuStep.Mode))
            {
                return;
            }

            controller.SelectCharacterAndLaunch(characterId);
            RebuildIfErrored(MenuStep.CharacterForRun);
        }

        private void LaunchContinueRun()
        {
            SetStatus("Launching Continue Run.");
            controller.LaunchDefaultContinue();
            RebuildIfErrored(MenuStep.Mode);
        }

        private void LaunchChallenge(string challengeId)
        {
            SetStatus($"Launching challenge {challengeId}.");
            controller.LaunchDefaultChallenge(challengeId);
            RebuildIfErrored(MenuStep.Challenge);
        }

        private bool RebuildIfErrored(MenuStep errorStep)
        {
            if (controller.ViewModel.State != MainMenuState.Error)
            {
                return false;
            }

            step = errorStep;
            SetStatus(controller.ViewModel.ErrorMessage);
            Rebuild();
            return true;
        }

        private string CharacterDisplayName(string characterId)
        {
            var character = controller.ViewModel.Characters.FirstOrDefault(candidate => candidate != null && candidate.CharacterId == characterId);
            return character != null ? character.DisplayName : characterId;
        }

        private static string CharacterSummary(CharacterDefinition character)
        {
            var stats = character.BaseStats;
            return $"{stats.MaxHealth} HP | {stats.SpeedMetersPerSecond:0.##} speed\n{character.StarterMeleeWeaponId}\n{character.StarterRangedWeaponId}";
        }

        private static Color CharacterColor(string characterId)
        {
            return characterId == "heavy"
                ? new Color(0.46f, 0.30f, 0.20f)
                : new Color(0.18f, 0.36f, 0.64f);
        }

        private void ConfigureCanvas()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            if (SortingLayer.layers.Any(layer => layer.name == PolySpatialUISortingLayerName))
            {
                canvas.sortingLayerName = PolySpatialUISortingLayerName;
            }

            canvas.sortingOrder = 20;
            transform.localPosition = new Vector3(0f, 1.35f, 2.05f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * 0.0018f;
            gameObject.layer = LayerMask.NameToLayer("UI");

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private MenuButtonBinding AddButton(RectTransform parent, string label, Vector2 anchoredPosition, Action onClick, Color color, Vector2? size = null, bool disableAfterActivation = false, string stepName = "")
        {
            var buttonRoot = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            buttonRoot.transform.SetParent(parent, false);
            buttonRoot.layer = gameObject.layer;
            var rect = (RectTransform)buttonRoot.transform;
            rect.sizeDelta = size ?? new Vector2(260f, 48f);
            rect.anchoredPosition = anchoredPosition;
            buttonRoot.GetComponent<Image>().color = color;
            var button = buttonRoot.GetComponent<Button>();
            var feedback = buttonRoot.AddComponent<VisionOSMenuButtonFeedback>();
            feedback.Configure(string.IsNullOrWhiteSpace(stepName) ? CurrentStepName : stepName, label, color, onClick, SetStatus, disableAfterActivation);
            var labelText = AddText(rect, label, 15, FontStyle.Bold, Vector2.zero, rect.sizeDelta);
            return new MenuButtonBinding
            {
                Rect = rect,
                Button = button,
                Group = buttonRoot.GetComponent<CanvasGroup>(),
                Label = labelText
            };
        }

        private Text AddText(RectTransform parent, string text, int size, FontStyle style, Vector2 anchoredPosition, Vector2 sizeDelta, Color? color = null)
        {
            var textRoot = new GameObject(string.IsNullOrWhiteSpace(text) ? "Text" : text, typeof(RectTransform), typeof(Text));
            textRoot.transform.SetParent(parent, false);
            textRoot.layer = parent.gameObject.layer;
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
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(parent, false);
            panel.layer = parent.gameObject.layer;
            var rect = (RectTransform)panel.transform;
            rect.sizeDelta = size;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            var image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static void SetPanelVisible(RectTransform panel, bool visible)
        {
            if (panel == null)
            {
                return;
            }

            var group = panel.GetComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private static void SetButtonVisible(MenuButtonBinding binding, bool visible)
        {
            if (binding == null)
            {
                return;
            }

            binding.Group.alpha = visible ? 1f : 0f;
            binding.Group.interactable = visible;
            binding.Group.blocksRaycasts = visible;
            binding.Button.interactable = visible;
        }

        private void ClearChildrenImmediate()
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                if (Application.isPlaying)
                {
                    Destroy(transform.GetChild(index).gameObject);
                }
                else
                {
                    DestroyImmediate(transform.GetChild(index).gameObject);
                }
            }
        }

        private void SetStatus(string message)
        {
            statusMessage = string.IsNullOrWhiteSpace(message) ? "Waiting for input." : message;
            if (statusLabel != null)
            {
                statusLabel.text = statusMessage;
            }
        }

        private void LogActiveButtons()
        {
            var labels = rootPanel.GetComponentsInChildren<Button>()
                .Where(button => button != null && button.IsInteractable())
                .Select(button => button.gameObject.name);
            Debug.Log($"VisionOS menu rebuilt: step={CurrentStepName} activeButtons={string.Join(",", labels)}");
        }
    }
}
