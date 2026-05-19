using Hollow.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.Combat
{
    public sealed class CombatHudController : MonoBehaviour
    {
        private RoomCombatController combatController;
        private Text roomStateText;
        private Text debugText;
        private RectTransform debugPanel;
        private Font font;
        private RoomObjectiveState lastRoomState = (RoomObjectiveState)(-1);
        private float roomStateHideTime;

        public void Bind(RoomCombatController controller)
        {
            combatController = controller;
            BuildIfNeeded();
            Refresh();
        }

        private void Update()
        {
            if (GameplayInputReader.ReadDebugHudTogglePressed())
            {
                GameplayDebugHudState.Toggle();
            }

            Refresh();
        }

        public void Refresh()
        {
            if (combatController == null || roomStateText == null)
            {
                return;
            }

            var model = combatController.CreateHudModel();
            RefreshRoomStateBadge(model);
            if (debugPanel != null)
            {
                debugPanel.gameObject.SetActive(GameplayDebugHudState.IsVisible);
            }

            if (debugText != null)
            {
                debugText.text =
                    $"COMBAT DEBUG (F3)\n" +
                    $"HP {model.PlayerHealth}/{model.PlayerMaxHealth} | {model.DefenseSummary}\n" +
                    $"Enemies {model.EnemiesRemaining}\n" +
                    $"Tier {model.DifficultyName}\n" +
                    $"{model.RollDebugLine}\n" +
                    $"{model.RangedDrawDebugLine}\n" +
                    $"Types {model.ArchetypeSummary}\n" +
                    $"{model.ProjectileSummary}\n" +
                    $"{model.DirectorDebugLine}";
            }
        }

        private void BuildIfNeeded()
        {
            if (roomStateText != null)
            {
                return;
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            roomStateText = AddText("CombatHud.RoomState", new Vector2(0f, 472f), 22, TextAnchor.MiddleCenter, new Vector2(520f, 42f));
            roomStateText.gameObject.SetActive(false);
            debugPanel = AddPanel("CombatHud.DebugPanel", new Vector2(610f, -230f), new Vector2(430f, 190f));
            debugText = AddText("CombatHud.DebugText", Vector2.zero, 16, TextAnchor.UpperLeft, new Vector2(400f, 160f), debugPanel);
            debugPanel.gameObject.SetActive(GameplayDebugHudState.IsVisible);
        }

        private void RefreshRoomStateBadge(CombatHudModel model)
        {
            if (roomStateText == null)
            {
                return;
            }

            if (model.RoomState != lastRoomState)
            {
                lastRoomState = model.RoomState;
                roomStateText.text = model.StatusText;
                roomStateText.color = model.RoomState == RoomObjectiveState.Cleared
                    ? new Color(0.25f, 1f, 0.45f)
                    : Color.white;
                var shouldShow = model.RoomState == RoomObjectiveState.Cleared;
                roomStateText.gameObject.SetActive(shouldShow);
                roomStateHideTime = shouldShow && Application.isPlaying ? Time.unscaledTime + 2.25f : 0f;
            }

            if (roomStateText.gameObject.activeSelf && roomStateHideTime > 0f && Time.unscaledTime >= roomStateHideTime)
            {
                roomStateText.gameObject.SetActive(false);
                roomStateHideTime = 0f;
            }
        }

        private Text AddText(
            string name,
            Vector2 anchoredPosition,
            int size,
            TextAnchor alignment,
            Vector2 sizeDelta,
            Transform parentOverride = null)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parentOverride != null ? parentOverride : transform, false);
            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            var label = textObject.GetComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private RectTransform AddPanel(string name, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(transform, false);
            var rect = (RectTransform)panelObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            var image = panelObject.GetComponent<Image>();
            image.color = new Color(0.03f, 0.04f, 0.05f, 0.72f);
            image.raycastTarget = false;
            return rect;
        }
    }
}
