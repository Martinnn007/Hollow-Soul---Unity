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
                branchSessionController = FindFirstObjectByType<BranchSessionController>();
            }

            Refresh(force: false);
        }

        private void Refresh(bool force)
        {
            BuildIfNeeded();
            if (branchSessionController == null || mapText == null)
            {
                return;
            }

            var model = branchSessionController.CreateMiniMapModel();
            var summary = model.Summary();
            var itemNames = branchSessionController.RunEconomy.CollectedRewards.Count == 0
                ? "None"
                : string.Join(", ", branchSessionController.RunEconomy.CollectedRewards.Select(record => record.DisplayName));
            var displayState = $"{summary}|{branchSessionController.RunEconomy.RunSouls}|{itemNames}|{branchSessionController.LastRewardMessage}|{branchSessionController.SaveStatus}";
            if (!force && displayState == lastSummary)
            {
                return;
            }

            lastSummary = displayState;
            mapText.text =
                $"Branch Map\n{Format(model)}\nRun Souls: {branchSessionController.RunEconomy.RunSouls} | Banked: {branchSessionController.BankedSouls}\nRewards: {branchSessionController.RewardCounter.ClaimedRewards}/4\nLatest: {branchSessionController.LastRewardMessage}\nItems: {itemNames}\nSave: {branchSessionController.SaveStatus}";
        }

        private static string Format(BranchMiniMapModel model)
        {
            if (model?.Nodes == null)
            {
                return "No branch";
            }

            var north = NodeToken(model, BranchRoomId.North);
            var south = NodeToken(model, BranchRoomId.South);
            var east = NodeToken(model, BranchRoomId.East);
            var west = NodeToken(model, BranchRoomId.West);
            var origin = NodeToken(model, BranchRoomId.Origin);
            return $"  {north}\n{west} {origin} {east}\n  {south}";
        }

        private static string NodeToken(BranchMiniMapModel model, BranchRoomId id)
        {
            foreach (var node in model.Nodes)
            {
                if (node.Id != id)
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

            return "[?]";
        }

        private void BuildIfNeeded()
        {
            if (mapText != null)
            {
                return;
            }

            font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var textObject = new GameObject("BranchMiniMap.Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(transform, false);
            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(32f, -32f);
            rect.sizeDelta = new Vector2(520f, 300f);

            mapText = textObject.GetComponent<Text>();
            mapText.font = font;
            mapText.fontSize = 22;
            mapText.alignment = TextAnchor.UpperLeft;
            mapText.color = Color.white;
            mapText.raycastTarget = false;
            mapText.text = "Branch Map\nWaiting...";
        }
    }
}
