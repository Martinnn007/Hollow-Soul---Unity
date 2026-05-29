using Hollow.Core.Diagnostics;
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
        private string lastStatusText = string.Empty;
        private CombatHudModel lastModel;
        private string lastDebugText = string.Empty;
        private bool hasLastModel;
        private bool lastDebugVisible;
        private float roomStateHideTime;
        private float nextRefreshTime;

        public void Bind(RoomCombatController controller)
        {
            combatController = controller;
            BuildIfNeeded();
            Refresh();
        }

        private void Update()
        {
            var forceRefresh = false;
            if (GameplayInputReader.ReadDebugHudTogglePressed())
            {
                GameplayDebugHudState.Toggle();
                forceRefresh = true;
            }

            Refresh(forceRefresh);
        }

        public void Refresh()
        {
            Refresh(force: true);
        }

        private void Refresh(bool force)
        {
            if (combatController == null || roomStateText == null)
            {
                return;
            }

            var now = Time.unscaledTime;
            var debugVisible = GameplayDebugHudState.IsVisible;
            if (!force &&
                hasLastModel &&
                debugVisible == lastDebugVisible &&
                now < nextRefreshTime)
            {
                HideExpiredRoomStateBadge(now);
                return;
            }

            var model = combatController.CreateHudModel();
            if (!force &&
                hasLastModel &&
                debugVisible == lastDebugVisible &&
                ModelsEquivalent(lastModel, model) &&
                now < nextRefreshTime)
            {
                HideExpiredRoomStateBadge(now);
                return;
            }

            using (M137PerformanceProfilerMarkers.CombatHudRefresh.Auto())
            {
                M136PerformanceOperationCounters.ReportCombatHudRefresh();
                RefreshRoomStateBadge(model, now);
                RefreshDebugPanel(model, debugVisible);
                lastModel = model;
                hasLastModel = true;
                lastDebugVisible = debugVisible;
                nextRefreshTime = now + M137PerformanceComfortPolicy.CombatHudMinRefreshIntervalSeconds;
            }
        }

        private void RefreshDebugPanel(CombatHudModel model, bool debugVisible)
        {
            if (debugPanel != null)
            {
                debugPanel.gameObject.SetActive(debugVisible);
            }

            if (debugText != null && debugVisible)
            {
                var debugLine =
                    $"COMBAT DEBUG (F3)\n" +
                    $"HP {model.PlayerHealth}/{model.PlayerMaxHealth} | {model.DefenseSummary}\n" +
                    $"Enemies {model.EnemiesRemaining}\n" +
                    $"Tier {model.DifficultyName}\n" +
                    $"{model.RollDebugLine}\n" +
                    $"{model.RangedDrawDebugLine}\n" +
                    $"Types {model.ArchetypeSummary}\n" +
                    $"{model.ProjectileSummary}\n" +
                    $"{model.DirectorDebugLine}";
                if (debugLine != lastDebugText)
                {
                    debugText.text = debugLine;
                    lastDebugText = debugLine;
                }
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

        private void RefreshRoomStateBadge(CombatHudModel model, float now)
        {
            if (roomStateText == null)
            {
                return;
            }

            if (model.RoomState != lastRoomState || model.StatusText != lastStatusText)
            {
                lastRoomState = model.RoomState;
                lastStatusText = model.StatusText;
                roomStateText.text = model.StatusText;
                roomStateText.color = model.RoomState == RoomObjectiveState.Cleared
                    ? new Color(0.25f, 1f, 0.45f)
                    : model.HasStatusOverride ? new Color(0.75f, 0.9f, 1f) : Color.white;
                var shouldShow = model.RoomState == RoomObjectiveState.Cleared || model.HasStatusOverride;
                roomStateText.gameObject.SetActive(shouldShow);
                roomStateHideTime = shouldShow && !model.HasStatusOverride && Application.isPlaying ? now + 2.25f : 0f;
            }

            HideExpiredRoomStateBadge(now);
        }

        private void HideExpiredRoomStateBadge(float now)
        {
            if (roomStateText != null && roomStateText.gameObject.activeSelf && roomStateHideTime > 0f && now >= roomStateHideTime)
            {
                roomStateText.gameObject.SetActive(false);
                roomStateHideTime = 0f;
            }
        }

        private static bool ModelsEquivalent(CombatHudModel left, CombatHudModel right)
        {
            return left.PlayerHealth == right.PlayerHealth &&
                left.PlayerMaxHealth == right.PlayerMaxHealth &&
                left.EnemiesRemaining == right.EnemiesRemaining &&
                left.RoomState == right.RoomState &&
                string.Equals(left.StatusOverride, right.StatusOverride, System.StringComparison.Ordinal) &&
                string.Equals(left.DifficultyName, right.DifficultyName, System.StringComparison.Ordinal) &&
                string.Equals(left.ArchetypeSummary, right.ArchetypeSummary, System.StringComparison.Ordinal) &&
                string.Equals(left.ProjectileSummary, right.ProjectileSummary, System.StringComparison.Ordinal) &&
                left.Defense == right.Defense &&
                left.IsGuarding == right.IsGuarding &&
                left.IsInParryWindow == right.IsInParryWindow &&
                left.LastGuardResult == right.LastGuardResult &&
                left.LastDamageReduction == right.LastDamageReduction &&
                string.Equals(left.DirectorDebugLine, right.DirectorDebugLine, System.StringComparison.Ordinal) &&
                string.Equals(left.RollDebugLine, right.RollDebugLine, System.StringComparison.Ordinal) &&
                string.Equals(left.RangedDrawDebugLine, right.RangedDrawDebugLine, System.StringComparison.Ordinal);
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
