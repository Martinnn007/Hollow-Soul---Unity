using UnityEngine;
using UnityEngine.UI;

namespace Hollow.Combat
{
    public sealed class CombatHudController : MonoBehaviour
    {
        private RoomCombatController combatController;
        private Text playerHealthText;
        private Text enemyText;
        private Text roomStateText;
        private Text difficultyText;
        private Text archetypeText;
        private Text projectileText;
        private Text directorText;
        private Font font;

        public void Bind(RoomCombatController controller)
        {
            combatController = controller;
            BuildIfNeeded();
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (combatController == null || playerHealthText == null)
            {
                return;
            }

            var model = combatController.CreateHudModel();
            playerHealthText.text = $"HP {model.PlayerHealth}/{model.PlayerMaxHealth} | {model.DefenseSummary}";
            enemyText.text = $"Enemies {model.EnemiesRemaining}";
            roomStateText.text = model.StatusText;
            difficultyText.text = $"Tier {model.DifficultyName}";
            archetypeText.text = $"Types {model.ArchetypeSummary}";
            projectileText.text = model.ProjectileSummary;
            directorText.text = model.DirectorDebugLine;
            roomStateText.color = model.RoomState == RoomObjectiveState.Cleared ? new Color(0.25f, 1f, 0.45f) : Color.white;
        }

        private void BuildIfNeeded()
        {
            if (playerHealthText != null)
            {
                return;
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            playerHealthText = AddText("CombatHud.PlayerHealth", new Vector2(-760f, 470f), 24, TextAnchor.MiddleLeft);
            enemyText = AddText("CombatHud.Enemies", new Vector2(-760f, 430f), 24, TextAnchor.MiddleLeft);
            roomStateText = AddText("CombatHud.RoomState", new Vector2(-760f, 390f), 24, TextAnchor.MiddleLeft);
            difficultyText = AddText("CombatHud.Difficulty", new Vector2(-760f, 350f), 20, TextAnchor.MiddleLeft);
            archetypeText = AddText("CombatHud.Archetypes", new Vector2(-760f, 315f), 18, TextAnchor.MiddleLeft);
            projectileText = AddText("CombatHud.Projectiles", new Vector2(-760f, 285f), 16, TextAnchor.MiddleLeft);
            directorText = AddText("CombatHud.Director", new Vector2(-760f, 258f), 15, TextAnchor.MiddleLeft);
        }

        private Text AddText(string name, Vector2 anchoredPosition, int size, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(transform, false);
            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(360f, 42f);
            var label = textObject.GetComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }
    }
}
