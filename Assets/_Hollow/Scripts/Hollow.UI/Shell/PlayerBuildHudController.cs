using Hollow.Branches;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.UI.Shell
{
    [RequireComponent(typeof(Canvas))]
    public sealed class PlayerBuildHudController : MonoBehaviour
    {
        private BranchSessionController branchSessionController;
        private Text buildText;
        private Image panelBackground;
        private Font font;

        public void Bind(BranchSessionController controller)
        {
            branchSessionController = controller;
            Refresh(force: true);
        }

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildIfNeeded();
        }

        private void Update()
        {
            if (branchSessionController == null)
            {
                branchSessionController = FindAnyObjectByType<BranchSessionController>();
            }

            Refresh(force: false);
        }

        public void Refresh(bool force)
        {
            BuildIfNeeded();
            if (branchSessionController == null || buildText == null)
            {
                return;
            }

            var model = branchSessionController.CreatePlayerBuildHudModel();
            buildText.text = model.BodyText;
            if (panelBackground != null)
            {
                panelBackground.color = new Color(0.02f, 0.025f, 0.035f, 0.64f);
            }
        }

        private void BuildIfNeeded()
        {
            if (buildText != null)
            {
                return;
            }

            font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var panel = new GameObject("PlayerBuildHud.Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(0f, 0.5f);
            panelRect.anchorMax = new Vector2(0f, 0.5f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(28f, -40f);
            panelRect.sizeDelta = new Vector2(390f, 430f);
            panelBackground = panel.GetComponent<Image>();
            panelBackground.color = new Color(0.02f, 0.025f, 0.035f, 0.64f);

            var textObject = new GameObject("PlayerBuildHud.Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 14f);
            textRect.offsetMax = new Vector2(-16f, -14f);

            buildText = textObject.GetComponent<Text>();
            buildText.font = font;
            buildText.fontSize = 15;
            buildText.alignment = TextAnchor.UpperLeft;
            buildText.color = Color.white;
            buildText.raycastTarget = false;
            buildText.text = "BUILD\nWaiting...";
        }
    }
}
