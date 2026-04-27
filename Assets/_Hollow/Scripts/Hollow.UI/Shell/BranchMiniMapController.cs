using Hollow.Branches;
using System.Collections.Generic;
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
        private RectTransform shapeRoot;
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
            if (branchSessionController == null || mapText == null || economyText == null || itemLogText == null || shapeRoot == null)
            {
                return;
            }

            var model = branchSessionController.CreateMiniMapModel();
            var summary = model.Summary();
            var itemNames = branchSessionController.RunEconomy.CollectedRewards.Count == 0
                ? "None"
                : string.Join(", ", branchSessionController.RunEconomy.CollectedRewards.Select(record => record.DisplayName));
            var activeSeed = branchSessionController.CurrentBranchSeed != 0
                ? branchSessionController.CurrentBranchSeed
                : branchSessionController.State?.Graph?.Seed ?? 0;
            var displayState = $"{summary}|{branchSessionController.RunEconomy.RunSouls}|{branchSessionController.RunEconomy.RunCoins}|{branchSessionController.BankedSouls}|{branchSessionController.RewardCounter.ClaimedRewards}|{itemNames}|{branchSessionController.LastRewardMessage}|{branchSessionController.SaveStatus}|{activeSeed}|{branchSessionController.RunSeed}|{branchSessionController.WorldIndex}|{branchSessionController.WorldPhase}|{branchSessionController.PlayerBuildHudSummary}";
            if (!force && displayState == lastSummary)
            {
                return;
            }

            lastSummary = displayState;
            mapText.text = "Branch Map\nBright: current | Gold: reward | Dark: nearby";
            RebuildShapeMap(model);
            economyText.text =
                $"Run Souls: {branchSessionController.RunEconomy.RunSouls}\nCoins: {branchSessionController.RunEconomy.RunCoins}\nBanked: {branchSessionController.BankedSouls}\nRewards: {branchSessionController.RewardCounter.ClaimedRewards}/4\nWorld: {branchSessionController.WorldIndex}\nRun Seed: {branchSessionController.RunSeed}\nBranch Seed: {activeSeed}\nSave: {branchSessionController.SaveStatus}\n{branchSessionController.PlayerBuildHudSummary}";
            itemLogText.text = $"Latest\n{branchSessionController.LastRewardMessage}\n\nItems\n{itemNames}";
        }

        public void RebuildShapeMap(BranchMiniMapModel model)
        {
            ClearShapeMap();
            if (model?.Nodes == null || shapeRoot == null)
            {
                return;
            }

            var visibleNodes = model.Nodes.Where(node => node.IsRevealed).ToList();
            if (visibleNodes.Count == 0)
            {
                return;
            }

            var currentNode = visibleNodes.FirstOrDefault(node => node.IsCurrent);
            var layout = MiniMapLayout.Create(visibleNodes, shapeRoot.rect.size, currentNode);
            foreach (var connection in model.Connections)
            {
                DrawConnection(connection, shapeRoot, layout);
            }

            foreach (var node in visibleNodes)
            {
                DrawRoomNode(node, shapeRoot, layout);
            }
        }

        public void ClearShapeMap()
        {
            if (shapeRoot == null)
            {
                return;
            }

            for (var index = shapeRoot.childCount - 1; index >= 0; index--)
            {
                var child = shapeRoot.GetChild(index).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        public void DrawRoomNode(BranchMiniMapNode node, RectTransform root, MiniMapLayout layout)
        {
            foreach (var cell in node.OccupiedCells)
            {
                var cellObject = new GameObject($"MiniMapRoomCell_{node.Id.Value}_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image), typeof(Outline));
                cellObject.transform.SetParent(root, false);
                var rect = (RectTransform)cellObject.transform;
                ConfigureMiniMapRect(rect, layout.PositionFor(cell));
                rect.sizeDelta = Vector2.one * layout.CellSize;

                cellObject.GetComponent<Image>().color = FillColorFor(node);
                var outline = cellObject.GetComponent<Outline>();
                outline.effectColor = OutlineColorFor(node);
                outline.effectDistance = node.IsCurrent ? new Vector2(3f, -3f) : new Vector2(1.5f, -1.5f);
            }

            if (node.HasPendingReward)
            {
                DrawOverlayDot(root, layout.PositionFor(node.OccupiedCells.First()), "MiniMapRewardDot", new Color(1f, 0.78f, 0.16f, 1f), 9f);
            }

            var marker = MarkerFor(node.Role);
            if (!string.IsNullOrEmpty(marker))
            {
                DrawMarkerText(root, layout.CenterFor(node.OccupiedCells), marker, MarkerColorFor(node.Role));
            }

            if (node.IsCurrent)
            {
                DrawDot(root, layout.CenterFor(node.OccupiedCells), "MiniMapCurrentPositionDot", new Color(0.2f, 1f, 0.35f, 1f), 8f);
            }
        }

        public void DrawConnection(BranchMiniMapConnectionVisual connection, RectTransform root, MiniMapLayout layout)
        {
            var from = layout.PositionFor(connection.FromCell);
            var to = layout.PositionFor(connection.ToCell);
            var midpoint = (from + to) * 0.5f;
            var delta = to - from;
            var horizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
            var connector = new GameObject($"MiniMapConnection_{connection.FromRoomId.Value}_{connection.ToRoomId.Value}", typeof(RectTransform), typeof(Image));
            connector.transform.SetParent(root, false);
            var rect = (RectTransform)connector.transform;
            ConfigureMiniMapRect(rect, midpoint);
            rect.sizeDelta = horizontal
                ? new Vector2(layout.Gap + 4f, 5f)
                : new Vector2(5f, layout.Gap + 4f);
            connector.GetComponent<Image>().color = connection.LockKind == BranchConnectionLockKind.BossKey
                ? new Color(0.96f, 0.55f, 0.12f, 0.95f)
                : new Color(0.68f, 0.66f, 0.58f, 0.85f);

            if (connection.LockKind == BranchConnectionLockKind.BossKey)
            {
                DrawMarkerText(root, midpoint, "L", new Color(1f, 0.72f, 0.25f, 1f), 12);
            }
        }

        private void BuildIfNeeded()
        {
            if (mapText != null && economyText != null && itemLogText != null && shapeRoot != null)
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
                new Vector2(420f, 250f),
                TextAnchor.UpperLeft,
                15);
            var mapPanel = (RectTransform)mapText.transform.parent;
            var mapTextRect = (RectTransform)mapText.transform;
            mapTextRect.anchorMin = new Vector2(0f, 1f);
            mapTextRect.anchorMax = new Vector2(1f, 1f);
            mapTextRect.pivot = new Vector2(0f, 1f);
            mapTextRect.anchoredPosition = new Vector2(0f, 0f);
            mapTextRect.sizeDelta = new Vector2(0f, 58f);
            shapeRoot = new GameObject("BranchMiniMap.ShapeRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            shapeRoot.transform.SetParent(mapPanel, false);
            shapeRoot.anchorMin = Vector2.zero;
            shapeRoot.anchorMax = Vector2.one;
            shapeRoot.offsetMin = new Vector2(14f, 14f);
            shapeRoot.offsetMax = new Vector2(-14f, -66f);
            economyText = AddPanelText(
                "BranchMiniMap.EconomyPanel",
                "Run Souls: 0\nCoins: 0\nBanked: 0\nRewards: 0/4\nWorld: 1\nRun Seed: 0\nBranch Seed: 0\nSave: Ready\nCharacter: Balanced\nWeapon: Ranged\nStamina: --",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(32f, -315f),
                new Vector2(390f, 250f),
                TextAnchor.UpperLeft,
                16);
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

        private static Color FillColorFor(BranchMiniMapNode node)
        {
            if (node.IsCurrent)
            {
                return new Color(0.88f, 0.84f, 0.7f, 0.98f);
            }

            if (!node.IsVisited && !node.IsCleared && !node.HasPendingReward)
            {
                return new Color(0.12f, 0.12f, 0.14f, 0.82f);
            }

            if (node.HasPendingReward)
            {
                return new Color(0.55f, 0.43f, 0.18f, 0.94f);
            }

            return node.IsCleared
                ? new Color(0.28f, 0.34f, 0.39f, 0.94f)
                : new Color(0.18f, 0.22f, 0.26f, 0.94f);
        }

        private static Color OutlineColorFor(BranchMiniMapNode node)
        {
            if (node.IsCurrent)
            {
                return new Color(0.95f, 0.92f, 0.72f, 1f);
            }

            return node.Role switch
            {
                BranchRoomRole.Boss => new Color(0.95f, 0.25f, 0.22f, 0.95f),
                BranchRoomRole.Secret => new Color(0.72f, 0.42f, 1f, 0.95f),
                BranchRoomRole.Treasure or BranchRoomRole.Reward => new Color(1f, 0.78f, 0.22f, 0.95f),
                _ => new Color(0.72f, 0.72f, 0.68f, 0.8f)
            };
        }

        private static string MarkerFor(BranchRoomRole role)
        {
            return role switch
            {
                BranchRoomRole.Boss => "B",
                BranchRoomRole.Secret => "?",
                BranchRoomRole.Treasure => "$",
                BranchRoomRole.Reward => "R",
                BranchRoomRole.Origin => "O",
                _ => string.Empty
            };
        }

        private static Color MarkerColorFor(BranchRoomRole role)
        {
            return role switch
            {
                BranchRoomRole.Boss => new Color(1f, 0.22f, 0.18f, 1f),
                BranchRoomRole.Secret => new Color(0.86f, 0.55f, 1f, 1f),
                BranchRoomRole.Treasure or BranchRoomRole.Reward => new Color(1f, 0.83f, 0.22f, 1f),
                _ => Color.white
            };
        }

        private void DrawOverlayDot(RectTransform root, Vector2 position, string name, Color color, float size)
        {
            DrawDot(root, position + new Vector2(8f, -8f), name, color, size);
        }

        private void DrawDot(RectTransform root, Vector2 position, string name, Color color, float size)
        {
            var dot = new GameObject(name, typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(root, false);
            var rect = (RectTransform)dot.transform;
            ConfigureMiniMapRect(rect, position);
            rect.sizeDelta = Vector2.one * size;
            dot.GetComponent<Image>().color = color;
        }

        private void DrawMarkerText(RectTransform root, Vector2 position, string marker, Color color, int fontSize = 15)
        {
            var textObject = new GameObject($"MiniMapMarker_{marker}", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(root, false);
            var rect = (RectTransform)textObject.transform;
            ConfigureMiniMapRect(rect, position);
            rect.sizeDelta = new Vector2(28f, 24f);
            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.text = marker;
        }

        private static void ConfigureMiniMapRect(RectTransform rect, Vector2 anchoredPosition)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
        }

        public readonly struct MiniMapLayout
        {
            private readonly float focusX;
            private readonly float focusY;
            private readonly float originX;
            private readonly float originY;
            private readonly float step;

            private MiniMapLayout(float focusX, float focusY, float originX, float originY, float step, float cellSize, float gap)
            {
                this.focusX = focusX;
                this.focusY = focusY;
                this.originX = originX;
                this.originY = originY;
                this.step = step;
                CellSize = cellSize;
                Gap = gap;
            }

            public float CellSize { get; }

            public float Gap { get; }

            public static MiniMapLayout Create(IReadOnlyCollection<BranchMiniMapNode> nodes, Vector2 availableSize, BranchMiniMapNode currentNode = null)
            {
                var cells = nodes.SelectMany(node => node.OccupiedCells).ToArray();
                var focusCells = currentNode?.OccupiedCells?.ToArray();
                if (focusCells == null || focusCells.Length == 0)
                {
                    focusCells = cells;
                }

                var focusX = focusCells.Average(cell => cell.x);
                var focusY = focusCells.Average(cell => cell.y);
                var radiusX = Mathf.Max(0.5f, cells.Max(cell => Mathf.Abs(cell.x - (float)focusX)) + 0.5f);
                var radiusY = Mathf.Max(0.5f, cells.Max(cell => Mathf.Abs(cell.y - (float)focusY)) + 0.5f);
                var columns = Mathf.Max(1f, radiusX * 2f);
                var rows = Mathf.Max(1f, radiusY * 2f);
                var size = availableSize.x > 0f && availableSize.y > 0f ? availableSize : new Vector2(392f, 170f);
                var step = Mathf.Clamp(Mathf.Min(size.x / columns, size.y / rows), 18f, 34f);
                var cellSize = Mathf.Max(12f, step - 4f);
                var gap = Mathf.Max(3f, step - cellSize);
                return new MiniMapLayout(
                    (float)focusX,
                    (float)focusY,
                    size.x * 0.5f,
                    -size.y * 0.5f,
                    step,
                    cellSize,
                    gap);
            }

            public Vector2 PositionFor(Vector2Int cell)
            {
                // The runtime branch grid stores north as decreasing Y, while the current
                // game camera reads the opposite way on screen. Flip only the minimap
                // presentation so traversal/generation data stays unchanged.
                return new Vector2(originX + (cell.x - focusX) * step, originY + (cell.y - focusY) * step);
            }

            public Vector2 CenterFor(IEnumerable<Vector2Int> cells)
            {
                var positions = cells.Select(PositionFor).ToArray();
                if (positions.Length == 0)
                {
                    return Vector2.zero;
                }

                return new Vector2(positions.Average(position => position.x), positions.Average(position => position.y));
            }
        }
    }
}
