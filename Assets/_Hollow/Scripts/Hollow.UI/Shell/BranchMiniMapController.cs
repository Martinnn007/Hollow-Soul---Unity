using Hollow.Branches;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.UI.Shell
{
    [RequireComponent(typeof(Canvas))]
    public sealed class BranchMiniMapController : MonoBehaviour
    {
        private BranchSessionController branchSessionController;
        private Text mapText;
        private Text economyText;
        private Text itemLogText;
        private Font font;
        private string lastSummary;

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

        private void Refresh(bool force)
        {
            BuildIfNeeded();
            if (branchSessionController == null || mapText == null || economyText == null || itemLogText == null)
            {
                return;
            }

            var model = branchSessionController.CreateMiniMapModel();
            var summary = model.Summary();
            var itemNames = branchSessionController.RunEconomy.CollectedRewards.Count == 0
                ? "None"
                : string.Join(", ", branchSessionController.RunEconomy.CollectedRewards.Select(record => record.DisplayName));
            var displayState = $"{summary}|{branchSessionController.RunEconomy.RunSouls}|{branchSessionController.BankedSouls}|{branchSessionController.RewardCounter.ClaimedRewards}|{itemNames}|{branchSessionController.LastRewardMessage}|{branchSessionController.SaveStatus}";
            if (!force && displayState == lastSummary)
            {
                return;
            }

            lastSummary = displayState;
            mapText.text = $"Branch Map\n{Format(model)}\nC current | X clear | R reward | V visited";
            economyText.text =
                $"Run Souls: {branchSessionController.RunEconomy.RunSouls}\nBanked: {branchSessionController.BankedSouls}\nRewards: {branchSessionController.RewardCounter.ClaimedRewards}/4\nSave: {branchSessionController.SaveStatus}";
            itemLogText.text = $"Latest\n{branchSessionController.LastRewardMessage}\n\nItems\n{itemNames}";
        }

        private static string Format(BranchMiniMapModel model)
        {
            if (model?.Nodes == null)
            {
                return "No branch";
            }

            if (model.Nodes.Count == 0)
            {
                return "No rooms";
            }

            var minX = model.Nodes.Min(node => node.Coordinate.x);
            var maxX = model.Nodes.Max(node => node.Coordinate.x);
            var minY = model.Nodes.Min(node => node.Coordinate.y);
            var maxY = model.Nodes.Max(node => node.Coordinate.y);
            var rows = Enumerable.Range(minY, maxY - minY + 1)
                .Select(y => string.Join(" ", Enumerable.Range(minX, maxX - minX + 1)
                    .Select(x => NodeTokenAt(model, new Vector2Int(x, y)))));
            return string.Join("\n", rows);
        }

        private static string NodeTokenAt(BranchMiniMapModel model, Vector2Int coordinate)
        {
            foreach (var node in model.Nodes)
            {
                if (node.Coordinate != coordinate)
                {
                    continue;
                }

                if (node.IsCurrent)
                {
                    return "[C]";
                }

                if (node.HasPendingReward)
                {
                    return "[R]";
                }

                if (node.IsCleared)
                {
                    return "[X]";
                }

                return node.IsVisited ? "[V]" : "[ ]";
            }

            return "   ";
        }

        private void BuildIfNeeded()
        {
            if (mapText != null && economyText != null && itemLogText != null)
            {
                return;
            }

            font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mapText = AddPanelText(
                "BranchMiniMap.MapPanel",
                "Branch Map\nWaiting...",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-32f, -32f),
                new Vector2(360f, 170f),
                TextAnchor.UpperRight,
                18);
            economyText = AddPanelText(
                "BranchMiniMap.EconomyPanel",
                "Run Souls: 0\nBanked: 0\nRewards: 0/4\nSave: Ready",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(32f, -315f),
                new Vector2(360f, 135f),
                TextAnchor.UpperLeft,
                18);
            itemLogText = AddPanelText(
                "BranchMiniMap.ItemLogPanel",
                "Latest\nNone\n\nItems\nNone",
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(32f, 32f),
                new Vector2(500f, 145f),
                TextAnchor.LowerLeft,
                16);
        }

        private Text AddPanelText(
            string panelName,
            string initialText,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            TextAnchor alignment,
            int fontSize)
        {
            var panel = new GameObject(panelName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = anchorMin;
            panelRect.anchorMax = anchorMax;
            panelRect.pivot = pivot;
            panelRect.anchoredPosition = anchoredPosition;
            panelRect.sizeDelta = size;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.42f);

            var textObject = new GameObject($"{panelName}.Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 10f);
            textRect.offsetMax = new Vector2(-14f, -10f);

            var label = textObject.GetComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = initialText;
            return label;
        }
    }
}
