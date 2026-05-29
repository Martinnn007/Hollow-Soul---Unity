using Hollow.Branches;
using Hollow.Core.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.UI.Shell
{
    [RequireComponent(typeof(Canvas))]
    public sealed class BranchMiniMapController : MonoBehaviour
    {
        private const float MapContentRotationDegrees = 45f;
        private const float MarkerCounterRotationDegrees = -45f;
        private const float MapContentScale = 1f;
        private const float DefaultMapCellStep = 34f;
        private const float DefaultMapCellSize = 33f;
        private const float DefaultMapCellGap = DefaultMapCellStep - DefaultMapCellSize;

        private BranchSessionController branchSessionController;
        private RectTransform mapPanel;
        private RectTransform shapeRoot;
        private RectTransform locationLabelRect;
        private Text locationLabelText;
        private Font font;
        private string lastSummary;
        private float nextModelRefreshTime;
        private float nextProviderSearchTime;

        public RectTransform MapPanel => mapPanel;

        public RectTransform ShapeRoot => shapeRoot;

        public Text LocationLabelText => locationLabelText;

        public string CurrentLocationLabel => locationLabelText != null ? locationLabelText.text : string.Empty;

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
                var now = Time.unscaledTime;
                if (now >= nextProviderSearchTime)
                {
                    branchSessionController = FindAnyObjectByType<BranchSessionController>();
                    nextProviderSearchTime = now + 0.5f;
                }
            }

            Refresh(force: false);
        }

        private void Refresh(bool force)
        {
            BuildIfNeeded();
            if (branchSessionController == null || mapPanel == null || shapeRoot == null)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (!force && now < nextModelRefreshTime)
            {
                RefreshLocationLabel();
                return;
            }

            M136PerformanceOperationCounters.ReportMiniMapModelBuild();
            var model = branchSessionController.CreateMiniMapModel();
            nextModelRefreshTime = now + M137PerformanceComfortPolicy.MiniMapModelMinRefreshIntervalSeconds;
            var summary = model.Summary();
            var activeSeed = branchSessionController.CurrentBranchSeed != 0
                ? branchSessionController.CurrentBranchSeed
                : branchSessionController.State?.Graph?.Seed ?? 0;
            var displayState = $"{summary}|{branchSessionController.RewardCounter.ClaimedRewards}|{branchSessionController.LastRewardMessage}|{branchSessionController.SaveStatus}|{activeSeed}|{branchSessionController.RunSeed}|{branchSessionController.WorldIndex}|{branchSessionController.WorldPhase}";
            if (!force && displayState == lastSummary)
            {
                RefreshLocationLabel();
                return;
            }

            lastSummary = displayState;
            RebuildShapeMap(model);
            RefreshLocationLabel();
        }

        public void RebuildShapeMap(BranchMiniMapModel model)
        {
            using (M137PerformanceProfilerMarkers.MiniMapRebuild.Auto())
            {
                M136PerformanceOperationCounters.ReportMiniMapRebuild();
                ClearShapeMap();
                if (model?.Nodes == null || shapeRoot == null)
                {
                    return;
                }

                var visibleNodes = new List<BranchMiniMapNode>();
                BranchMiniMapNode currentNode = null;
                for (var index = 0; index < model.Nodes.Count; index++)
                {
                    var node = model.Nodes[index];
                    if (node.IsRevealed)
                    {
                        visibleNodes.Add(node);
                    }

                    if (node.IsCurrent)
                    {
                        currentNode = node;
                    }
                }

                if (visibleNodes.Count == 0)
                {
                    return;
                }

                var layout = MiniMapLayout.Create(model.Nodes, shapeRoot.rect.size, currentNode);
                var contentRoot = CreateContentRoot(shapeRoot);
                foreach (var connection in model.Connections)
                {
                    DrawConnection(connection, contentRoot, layout);
                }

                foreach (var node in visibleNodes)
                {
                    DrawRoomNode(node, contentRoot, layout);
                }
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
            var footprintRoot = new GameObject($"MiniMapRoomFootprint_{node.Id.Value}", typeof(RectTransform)).GetComponent<RectTransform>();
            footprintRoot.transform.SetParent(root, false);
            StretchToParent(footprintRoot);

            var occupiedCells = node.OccupiedCells.ToHashSet();
            foreach (var cell in occupiedCells)
            {
                var cellObject = new GameObject($"MiniMapRoomFootprintFill_{node.Id.Value}_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image));
                cellObject.transform.SetParent(footprintRoot, false);
                var rect = (RectTransform)cellObject.transform;
                ConfigureMiniMapRect(rect, ExpandedCellCenterFor(cell, occupiedCells, layout));
                rect.sizeDelta = ExpandedCellSizeFor(cell, occupiedCells, layout);

                cellObject.GetComponent<Image>().color = FillColorFor(node);
            }

            foreach (var cell in occupiedCells)
            {
                DrawFootprintEdges(node, footprintRoot, layout, occupiedCells, cell);
            }

            if (node.HasPendingReward)
            {
                DrawOverlayDot(root, layout.PositionFor(FirstCell(node.OccupiedCells)), "MiniMapRewardDot", new Color(1f, 0.78f, 0.16f, 1f), 5f);
            }

            var marker = !string.IsNullOrWhiteSpace(node.DisplayLabel) ? node.DisplayLabel : MarkerFor(node.Role);
            if (!string.IsNullOrEmpty(marker))
            {
                DrawMarkerText(
                    root,
                    layout.CenterFor(node.OccupiedCells),
                    marker,
                    !string.IsNullOrWhiteSpace(node.DisplayLabel) ? Color.white : MarkerColorFor(node.Role),
                    marker.Length > 2 ? 9 : 11);
            }

            if (node.IsCurrent)
            {
                DrawDot(root, layout.CenterFor(node.OccupiedCells), "MiniMapCurrentPositionDot", new Color(0.2f, 1f, 0.35f, 1f), 5f);
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
                DrawMarkerText(root, midpoint, "L", new Color(1f, 0.72f, 0.25f, 1f), 9);
            }
        }

        private static RectTransform CreateContentRoot(RectTransform parent)
        {
            var contentRoot = new GameObject("BranchMiniMap.ContentRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            contentRoot.transform.SetParent(parent, false);
            StretchToParent(contentRoot);
            contentRoot.localEulerAngles = new Vector3(0f, 0f, MapContentRotationDegrees);
            contentRoot.localScale = Vector3.one * MapContentScale;
            return contentRoot;
        }

        private static Vector2Int FirstCell(IReadOnlyCollection<Vector2Int> cells)
        {
            if (cells == null)
            {
                return Vector2Int.zero;
            }

            foreach (var cell in cells)
            {
                return cell;
            }

            return Vector2Int.zero;
        }

        private void BuildIfNeeded()
        {
            if (mapPanel != null && shapeRoot != null && locationLabelText != null)
            {
                return;
            }

            font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mapPanel ??= AddMiniMapPanel();
            if (shapeRoot == null)
            {
                shapeRoot = new GameObject("BranchMiniMap.ShapeRoot", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
                shapeRoot.transform.SetParent(mapPanel, false);
                shapeRoot.anchorMin = Vector2.zero;
                shapeRoot.anchorMax = Vector2.one;
                shapeRoot.offsetMin = new Vector2(44f, 34f);
                shapeRoot.offsetMax = new Vector2(-44f, -34f);
            }

            locationLabelText ??= AddLocationLabel();
        }

        private RectTransform AddMiniMapPanel()
        {
            var panel = new GameObject("BranchMiniMap.MapPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-32f, -32f);
            panelRect.sizeDelta = new Vector2(420f, 250f);

            var image = panel.GetComponent<Image>();
            image.sprite = null;
            image.color = new Color(0.015f, 0.018f, 0.024f, 0.72f);
            image.preserveAspect = false;
            image.raycastTarget = false;
            return panelRect;
        }

        private Text AddLocationLabel()
        {
            var labelObject = new GameObject("BranchMiniMap.LocationLabel", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(transform, false);
            locationLabelRect = (RectTransform)labelObject.transform;
            locationLabelRect.anchorMin = new Vector2(1f, 1f);
            locationLabelRect.anchorMax = new Vector2(1f, 1f);
            locationLabelRect.pivot = new Vector2(1f, 1f);
            locationLabelRect.anchoredPosition = new Vector2(-32f, -290f);
            locationLabelRect.sizeDelta = new Vector2(420f, 24f);

            var text = labelObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 14;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.UpperRight;
            text.color = new Color(0.78f, 0.86f, 0.92f, 0.92f);
            text.raycastTarget = false;
            text.text = string.Empty;
            return text;
        }

        private void RefreshLocationLabel()
        {
            if (locationLabelText == null || branchSessionController == null)
            {
                return;
            }

            locationLabelText.text = branchSessionController.CreateLocationLabel();
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
                BranchRoomRole.CorruptedChest => new Color(0.78f, 0.24f, 0.88f, 0.95f),
                BranchRoomRole.Wave => new Color(0.28f, 0.74f, 1f, 0.95f),
                BranchRoomRole.SpecialEncounter => new Color(0.42f, 0.82f, 1f, 0.95f),
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
                BranchRoomRole.CorruptedChest => "!",
                BranchRoomRole.Wave => "W",
                BranchRoomRole.SpecialEncounter => "S",
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
                BranchRoomRole.CorruptedChest => new Color(1f, 0.48f, 1f, 1f),
                BranchRoomRole.Wave => new Color(0.55f, 0.9f, 1f, 1f),
                BranchRoomRole.SpecialEncounter => new Color(0.62f, 0.95f, 1f, 1f),
                BranchRoomRole.Treasure or BranchRoomRole.Reward => new Color(1f, 0.83f, 0.22f, 1f),
                _ => Color.white
            };
        }

        private void DrawOverlayDot(RectTransform root, Vector2 position, string name, Color color, float size)
        {
            DrawDot(root, position + new Vector2(5f, -5f), name, color, size);
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

        private void DrawMarkerText(RectTransform root, Vector2 position, string marker, Color color, int fontSize = 11)
        {
            var textObject = new GameObject($"MiniMapMarker_{marker}", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(root, false);
            var rect = (RectTransform)textObject.transform;
            ConfigureMiniMapRect(rect, position);
            rect.sizeDelta = new Vector2(Mathf.Max(20f, marker.Length * 9f), 18f);
            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.text = marker;
            rect.localEulerAngles = new Vector3(0f, 0f, MarkerCounterRotationDegrees);
        }

        private static void DrawFootprintEdges(
            BranchMiniMapNode node,
            RectTransform root,
            MiniMapLayout layout,
            HashSet<Vector2Int> occupiedCells,
            Vector2Int cell)
        {
            var color = OutlineColorFor(node);
            var thickness = node.IsCurrent ? 1.5f : 1f;
            var leftExpanded = occupiedCells.Contains(new Vector2Int(cell.x - 1, cell.y));
            var rightExpanded = occupiedCells.Contains(new Vector2Int(cell.x + 1, cell.y));
            var bottomExpanded = occupiedCells.Contains(new Vector2Int(cell.x, cell.y - 1));
            var topExpanded = occupiedCells.Contains(new Vector2Int(cell.x, cell.y + 1));

            if (!leftExpanded)
            {
                DrawEdge(root, layout.LeftEdgeCenterFor(cell, topExpanded, bottomExpanded), $"MiniMapRoomFootprintEdge_{node.Id.Value}_{cell.x}_{cell.y}_W", color, new Vector2(thickness, layout.CellSize + VerticalGap(topExpanded, bottomExpanded, layout)));
            }

            if (!rightExpanded)
            {
                DrawEdge(root, layout.RightEdgeCenterFor(cell, topExpanded, bottomExpanded), $"MiniMapRoomFootprintEdge_{node.Id.Value}_{cell.x}_{cell.y}_E", color, new Vector2(thickness, layout.CellSize + VerticalGap(topExpanded, bottomExpanded, layout)));
            }

            if (!topExpanded)
            {
                DrawEdge(root, layout.TopEdgeCenterFor(cell, leftExpanded, rightExpanded), $"MiniMapRoomFootprintEdge_{node.Id.Value}_{cell.x}_{cell.y}_N", color, new Vector2(layout.CellSize + HorizontalGap(leftExpanded, rightExpanded, layout), thickness));
            }

            if (!bottomExpanded)
            {
                DrawEdge(root, layout.BottomEdgeCenterFor(cell, leftExpanded, rightExpanded), $"MiniMapRoomFootprintEdge_{node.Id.Value}_{cell.x}_{cell.y}_S", color, new Vector2(layout.CellSize + HorizontalGap(leftExpanded, rightExpanded, layout), thickness));
            }
        }

        private static void DrawEdge(RectTransform root, Vector2 position, string name, Color color, Vector2 size)
        {
            var edgeObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            edgeObject.transform.SetParent(root, false);
            var rect = (RectTransform)edgeObject.transform;
            ConfigureMiniMapRect(rect, position);
            rect.sizeDelta = size;
            edgeObject.GetComponent<Image>().color = color;
        }

        private static Vector2 ExpandedCellCenterFor(Vector2Int cell, HashSet<Vector2Int> occupiedCells, MiniMapLayout layout)
        {
            var position = layout.PositionFor(cell);
            var leftExpand = occupiedCells.Contains(new Vector2Int(cell.x - 1, cell.y)) ? layout.HalfGap : 0f;
            var rightExpand = occupiedCells.Contains(new Vector2Int(cell.x + 1, cell.y)) ? layout.HalfGap : 0f;
            var bottomExpand = occupiedCells.Contains(new Vector2Int(cell.x, cell.y - 1)) ? layout.HalfGap : 0f;
            var topExpand = occupiedCells.Contains(new Vector2Int(cell.x, cell.y + 1)) ? layout.HalfGap : 0f;
            return new Vector2(position.x + (rightExpand - leftExpand) * 0.5f, position.y + (topExpand - bottomExpand) * 0.5f);
        }

        private static Vector2 ExpandedCellSizeFor(Vector2Int cell, HashSet<Vector2Int> occupiedCells, MiniMapLayout layout)
        {
            var horizontalGap = HorizontalGap(
                occupiedCells.Contains(new Vector2Int(cell.x - 1, cell.y)),
                occupiedCells.Contains(new Vector2Int(cell.x + 1, cell.y)),
                layout);
            var verticalGap = VerticalGap(
                occupiedCells.Contains(new Vector2Int(cell.x, cell.y + 1)),
                occupiedCells.Contains(new Vector2Int(cell.x, cell.y - 1)),
                layout);
            return new Vector2(layout.CellSize + horizontalGap, layout.CellSize + verticalGap);
        }

        private static float HorizontalGap(bool leftExpanded, bool rightExpanded, MiniMapLayout layout)
        {
            return (leftExpanded ? layout.HalfGap : 0f) + (rightExpanded ? layout.HalfGap : 0f);
        }

        private static float VerticalGap(bool topExpanded, bool bottomExpanded, MiniMapLayout layout)
        {
            return (topExpanded ? layout.HalfGap : 0f) + (bottomExpanded ? layout.HalfGap : 0f);
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
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

            public float HalfGap => Gap * 0.5f;

            public static MiniMapLayout Create(IReadOnlyCollection<BranchMiniMapNode> nodes, Vector2 availableSize, BranchMiniMapNode currentNode = null)
            {
                var cells = nodes?.SelectMany(node => node.OccupiedCells).ToArray() ?? System.Array.Empty<Vector2Int>();
                var focusCells = currentNode?.OccupiedCells?.ToArray();
                if (focusCells == null || focusCells.Length == 0)
                {
                    focusCells = cells;
                }

                var focusX = focusCells.Length > 0 ? focusCells.Average(cell => cell.x) : 0f;
                var focusY = focusCells.Length > 0 ? focusCells.Average(cell => cell.y) : 0f;
                var size = availableSize.x > 0f && availableSize.y > 0f ? availableSize : new Vector2(392f, 170f);
                return new MiniMapLayout(
                    (float)focusX,
                    (float)focusY,
                    size.x * 0.5f,
                    -size.y * 0.5f,
                    DefaultMapCellStep,
                    DefaultMapCellSize,
                    DefaultMapCellGap);
            }

            public Vector2 PositionFor(Vector2Int cell)
            {
                // The runtime branch grid stores north as decreasing Y, while the current
                // game camera reads the opposite way on screen. Flip only the minimap
                // presentation so traversal/generation data stays unchanged.
                return new Vector2(originX + (cell.x - focusX) * step, originY + (cell.y - focusY) * step);
            }

            public Vector2 LeftEdgeCenterFor(Vector2Int cell, bool topExpanded, bool bottomExpanded)
            {
                var position = PositionFor(cell);
                var topExpand = topExpanded ? HalfGap : 0f;
                var bottomExpand = bottomExpanded ? HalfGap : 0f;
                return new Vector2(position.x - CellSize * 0.5f, position.y + (topExpand - bottomExpand) * 0.5f);
            }

            public Vector2 RightEdgeCenterFor(Vector2Int cell, bool topExpanded, bool bottomExpanded)
            {
                var position = PositionFor(cell);
                var topExpand = topExpanded ? HalfGap : 0f;
                var bottomExpand = bottomExpanded ? HalfGap : 0f;
                return new Vector2(position.x + CellSize * 0.5f, position.y + (topExpand - bottomExpand) * 0.5f);
            }

            public Vector2 TopEdgeCenterFor(Vector2Int cell, bool leftExpanded, bool rightExpanded)
            {
                var position = PositionFor(cell);
                var leftExpand = leftExpanded ? HalfGap : 0f;
                var rightExpand = rightExpanded ? HalfGap : 0f;
                return new Vector2(position.x + (rightExpand - leftExpand) * 0.5f, position.y + CellSize * 0.5f);
            }

            public Vector2 BottomEdgeCenterFor(Vector2Int cell, bool leftExpanded, bool rightExpanded)
            {
                var position = PositionFor(cell);
                var leftExpand = leftExpanded ? HalfGap : 0f;
                var rightExpand = rightExpanded ? HalfGap : 0f;
                return new Vector2(position.x + (rightExpand - leftExpand) * 0.5f, position.y - CellSize * 0.5f);
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
