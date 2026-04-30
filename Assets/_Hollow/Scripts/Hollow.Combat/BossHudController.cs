using UnityEngine;
using UnityEngine.UI;

namespace Hollow.Combat
{
    public sealed class BossHudController : MonoBehaviour
    {
        private RoomCombatController combatController;
        private RectTransform panel;
        private Text titleText;
        private Text statusText;
        private Image fillImage;
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

        private void Refresh()
        {
            if (panel == null)
            {
                return;
            }

            var boss = combatController != null ? combatController.ActiveBoss : null;
            var health = boss != null ? boss.Health : null;
            var visible = boss != null && health != null && health.IsAlive && boss.BossDefinition != null;
            panel.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            titleText.text = boss.BossDefinition.DisplayName;
            statusText.text = boss.BossStatusText;
            fillImage.fillAmount = health.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth);
        }

        private void BuildIfNeeded()
        {
            if (panel != null)
            {
                return;
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panel = AddPanel("BossHud.Panel", new Vector2(0f, 416f), new Vector2(620f, 72f));
            titleText = AddText("BossHud.Title", panel, new Vector2(0f, 18f), 20, TextAnchor.MiddleCenter, new Vector2(560f, 24f));
            var barBackground = AddPanel("BossHud.BarBackground", new Vector2(0f, -8f), new Vector2(520f, 16f), panel);
            var fillObject = new GameObject("BossHud.BarFill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(barBackground, false);
            var fillRect = (RectTransform)fillObject.transform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            fillImage = fillObject.GetComponent<Image>();
            fillImage.color = new Color(0.78f, 0.12f, 0.14f, 0.94f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            statusText = AddText("BossHud.Status", panel, new Vector2(0f, -28f), 12, TextAnchor.MiddleCenter, new Vector2(560f, 20f));
            panel.gameObject.SetActive(false);
        }

        private RectTransform AddPanel(string name, Vector2 anchoredPosition, Vector2 sizeDelta, Transform parentOverride = null)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parentOverride != null ? parentOverride : transform, false);
            var rect = (RectTransform)panelObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            var image = panelObject.GetComponent<Image>();
            image.color = new Color(0.03f, 0.025f, 0.02f, 0.74f);
            image.raycastTarget = false;
            return rect;
        }

        private Text AddText(string name, Transform parent, Vector2 anchoredPosition, int size, TextAnchor alignment, Vector2 sizeDelta)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
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
    }
}
