using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.EnemyAuthoring;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.BehaviorTreeStudio
{
    public sealed class BehaviourTreeStudioWindow : EditorWindow
    {
        private readonly EnemyAuthoringDraft draft = new();
        private readonly List<EnemyBehaviorTreeDefinition> trees = new();
        private readonly List<EnemyBehaviorTreeTemplateDefinition> templates = new();
        private readonly List<UnityEngine.Object> owners = new();
        private readonly List<string> traceHistory = new();
        private Dictionary<EnemyBehaviorTreeNodeDefinition, Rect> nodeRects = new();
        private readonly BehaviourTreeStudioSyntheticContext sandboxContext = new();

        private UnityEngine.Object selectedSource;
        private UnityEngine.Object selectedOwner;
        private EnemyBehaviorTreeNodeDefinition selectedNode;
        private EnemyBehaviorTreeNodeDefinition connectParent;
        private EnemyBehaviorTreeNodeDefinition copiedNode;
        private EnemyBehaviorTreeTemplateDefinition selectedTemplate;
        private EnemyBehaviorTreeDefinition diffTarget;
        private BehaviourTreeStudioAnalysisResult validationResult;
        private BehaviourTreeStudioSyntheticResult sandboxResult;
        private Vector2 browserScroll;
        private Vector2 inspectorScroll;
        private Vector2 graphPan = new(40f, 40f);
        private Vector2 lastMouse;
        private float graphZoom = 1f;
        private bool draggingCanvas;
        private int selectedTab;
        private string search = string.Empty;
        private string applyNotes = "Manual Behaviour Tree Studio edit";

        [MenuItem("Hollow/Enemy Authoring/Behaviour Tree Studio")]
        public static void Open()
        {
            GetWindow<BehaviourTreeStudioWindow>("Behaviour Tree Studio");
        }

        public static void OpenTree(EnemyBehaviorTreeDefinition tree)
        {
            var window = GetWindow<BehaviourTreeStudioWindow>("Behaviour Tree Studio");
            window.LoadSource(tree);
            window.Focus();
        }

        private void OnEnable()
        {
            RefreshAssets();
            EditorApplication.update += TickLiveTrace;
        }

        private void OnDisable()
        {
            EditorApplication.update -= TickLiveTrace;
            draft.Dispose();
        }

        private void OnGUI()
        {
            DrawToolbar();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawBrowser();
                DrawMainPanel();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                selectedTab = GUILayout.Toolbar(selectedTab, BehaviourTreeStudioLocalization.Tabs, EditorStyles.toolbarButton, GUILayout.Width(520f));
                GUILayout.Space(8f);
                GUILayout.Label(BehaviourTreeStudioLocalization.T("Search", "Szukaj"), GUILayout.Width(48f));
                search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(220f));
                if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(24f)))
                {
                    search = string.Empty;
                }

                GUILayout.FlexibleSpace();
                var language = GUILayout.Toolbar(
                    (int)BehaviourTreeStudioLocalization.CurrentLanguage,
                    new[] { "EN", "PL" },
                    EditorStyles.toolbarButton,
                    GUILayout.Width(64f));
                if (language != (int)BehaviourTreeStudioLocalization.CurrentLanguage)
                {
                    BehaviourTreeStudioLocalization.CurrentLanguage = (EnemyAuthoringLanguage)language;
                }

                if (GUILayout.Button(Tr("Refresh", "Odśwież"), EditorStyles.toolbarButton, GUILayout.Width(74f)))
                {
                    RefreshAssets();
                }
            }
        }

        private void DrawBrowser()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(280f)))
            {
                EditorGUILayout.LabelField(Tr("Tree Browser", "Przeglądarka drzew"), EditorStyles.boldLabel);
                browserScroll = EditorGUILayout.BeginScrollView(browserScroll, GUI.skin.box);
                EditorGUILayout.LabelField(Tr("Runtime Trees", "Drzewa runtime"), EditorStyles.miniBoldLabel);
                foreach (var tree in trees)
                {
                    DrawSourceButton(tree, $"{tree.DisplayName} [{tree.OwnerId}]");
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Tr("Templates", "Szablony"), EditorStyles.miniBoldLabel);
                foreach (var template in templates)
                {
                    DrawSourceButton(template, $"{template.DisplayName} ({template.Role})");
                }

                EditorGUILayout.EndScrollView();

                if (GUILayout.Button(Tr("Generate / Refresh Templates", "Generuj / odśwież szablony")))
                {
                    BehaviourTreeStudioTemplateGenerator.GenerateAssets();
                    RefreshAssets();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(Tr("Ping Source", "Pokaż źródło")) && selectedSource != null)
                    {
                        EditorGUIUtility.PingObject(selectedSource);
                    }

                    if (GUILayout.Button(Tr("Discard Draft", "Odrzuć szkic")))
                    {
                        draft.Discard();
                        selectedNode = null;
                        RebuildLayout();
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Tr("Selected", "Wybrane"), EditorStyles.boldLabel);
                EditorGUILayout.ObjectField(selectedSource, typeof(ScriptableObject), false);
                EditorGUILayout.LabelField(Tr("Owner", "Właściciel"), selectedOwner != null ? selectedOwner.name : Tr("unknown", "nieznany"));
                EditorGUILayout.LabelField(Tr("Draft", "Szkic"), draft.IsDirty ? Tr("Dirty", "Zmieniony") : Tr("Clean", "Czysty"));
            }
        }

        private void DrawSourceButton(UnityEngine.Object source, string label)
        {
            if (source == null || !MatchesSearch(source, label))
            {
                return;
            }

            var selected = source == selectedSource;
            if (GUILayout.Toggle(selected, label, "Button") && !selected)
            {
                LoadSource(source);
            }
        }

        private void DrawMainPanel()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                DrawDraftHeader();
                switch (selectedTab)
                {
                    case 0:
                        DrawGraphTab();
                        break;
                    case 1:
                        DrawTemplatesTab();
                        break;
                    case 2:
                        DrawValidationTab();
                        break;
                    case 3:
                        DrawSandboxTab();
                        break;
                    case 4:
                        DrawLiveTraceTab();
                        break;
                    case 5:
                        DrawDiffTab();
                        break;
                }
            }
        }

        private void DrawDraftHeader()
        {
            var treeLike = draft.Draft;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(BehaviourTreeStudioGraphUtility.DisplayNameFor(treeLike), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                applyNotes = EditorGUILayout.TextField(applyNotes, GUILayout.Width(260f));
                using (new EditorGUI.DisabledScope(treeLike == null))
                {
                    if (GUILayout.Button(Tr("Validate", "Waliduj"), GUILayout.Width(86f)))
                    {
                        RunValidation();
                    }
                }

                using (new EditorGUI.DisabledScope(!draft.IsDirty || treeLike == null))
                {
                    if (GUILayout.Button(Tr("Apply Draft", "Zapisz szkic"), GUILayout.Width(100f)))
                    {
                        RunValidation();
                        if (validationResult == null || validationResult.IsValid)
                        {
                            draft.Apply(applyNotes);
                            RefreshAssets();
                        }
                    }
                }
            }
        }

        private void DrawGraphTab()
        {
            var treeLike = draft.Draft;
            if (treeLike == null)
            {
                EditorGUILayout.HelpBox(Tr("Select a behavior tree or template.", "Wybierz drzewo AI albo szablon."), MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawGraphToolbar(treeLike);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var graphRect = GUILayoutUtility.GetRect(600f, 720f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                DrawGraphCanvas(graphRect, treeLike);
                DrawNodeInspector(treeLike);
            }
        }

        private void DrawGraphToolbar(UnityEngine.Object treeLike)
        {
            if (GUILayout.Button("+ Selector", GUILayout.Width(86f)))
            {
                selectedNode = BehaviourTreeStudioGraphUtility.AddNode<EnemyBehaviorSelectorNodeDefinition>(treeLike, draft, "selector");
                RebuildLayout();
            }

            if (GUILayout.Button("+ Sequence", GUILayout.Width(92f)))
            {
                selectedNode = BehaviourTreeStudioGraphUtility.AddNode<EnemyBehaviorSequenceNodeDefinition>(treeLike, draft, "sequence");
                RebuildLayout();
            }

            if (GUILayout.Button("+ Weighted", GUILayout.Width(94f)))
            {
                selectedNode = BehaviourTreeStudioGraphUtility.AddNode<EnemyBehaviorWeightedSelectorNodeDefinition>(treeLike, draft, "weighted");
                RebuildLayout();
            }

            if (GUILayout.Button("+ Condition", GUILayout.Width(94f)))
            {
                selectedNode = BehaviourTreeStudioGraphUtility.AddNode<EnemyBehaviorConditionNodeDefinition>(treeLike, draft, "condition");
                RebuildLayout();
            }

            if (GUILayout.Button("+ Action", GUILayout.Width(82f)))
            {
                selectedNode = BehaviourTreeStudioGraphUtility.AddNode<EnemyBehaviorActionNodeDefinition>(treeLike, draft, "action");
                RebuildLayout();
            }

            GUILayout.Space(12f);
            if (GUILayout.Button(Tr("Auto Layout", "Auto układ"), GUILayout.Width(100f)))
            {
                RebuildLayout();
            }

            graphZoom = EditorGUILayout.Slider(graphZoom, 0.45f, 1.8f, GUILayout.Width(180f));
            if (GUILayout.Button(Tr("Frame", "Kadruj"), GUILayout.Width(70f)))
            {
                graphPan = new Vector2(40f, 40f);
            }
        }

        private void DrawGraphCanvas(Rect rect, UnityEngine.Object treeLike)
        {
            GUI.Box(rect, GUIContent.none);
            HandleCanvasInput(rect);
            if (nodeRects.Count == 0)
            {
                RebuildLayout();
            }

            var nodes = BehaviourTreeStudioGraphUtility.NodesFor(treeLike);
            var root = BehaviourTreeStudioGraphUtility.RootFor(treeLike);
            Handles.BeginGUI();
            DrawGrid(rect);
            foreach (var node in nodes)
            {
                if (node == null || !nodeRects.TryGetValue(node, out var local))
                {
                    continue;
                }

                foreach (var child in node.Children)
                {
                    if (child == null || !nodeRects.TryGetValue(child, out var childLocal))
                    {
                        continue;
                    }

                    var a = ToScreen(rect, local);
                    var b = ToScreen(rect, childLocal);
                    Handles.DrawBezier(
                        new Vector3(a.center.x, a.yMax, 0f),
                        new Vector3(b.center.x, b.yMin, 0f),
                        new Vector3(a.center.x, a.yMax + 40f, 0f),
                        new Vector3(b.center.x, b.yMin - 40f, 0f),
                        Color.gray,
                        null,
                        2f);
                }
            }
            Handles.EndGUI();

            foreach (var node in nodes)
            {
                if (node == null || !nodeRects.TryGetValue(node, out var local))
                {
                    continue;
                }

                var screen = ToScreen(rect, local);
                if (!rect.Overlaps(screen) || !MatchesSearch(node, BehaviourTreeStudioAnalysis.SummaryFor(node)))
                {
                    continue;
                }

                DrawNodeCard(screen, node, node == root);
            }

            DrawMiniMap(rect, nodes);
        }

        private void DrawMiniMap(Rect canvas, IReadOnlyList<EnemyBehaviorTreeNodeDefinition> nodes)
        {
            if (nodes == null || nodes.Count == 0 || nodeRects.Count == 0)
            {
                return;
            }

            var mapRect = new Rect(canvas.xMax - 174f, canvas.y + 12f, 158f, 104f);
            GUI.Box(mapRect, GUIContent.none, EditorStyles.helpBox);
            var bounds = Rect.MinMaxRect(
                nodeRects.Values.Min(rect => rect.xMin),
                nodeRects.Values.Min(rect => rect.yMin),
                nodeRects.Values.Max(rect => rect.xMax),
                nodeRects.Values.Max(rect => rect.yMax));
            var scale = Mathf.Min((mapRect.width - 16f) / Mathf.Max(1f, bounds.width), (mapRect.height - 16f) / Mathf.Max(1f, bounds.height));
            Handles.BeginGUI();
            foreach (var node in nodes)
            {
                if (node == null || !nodeRects.TryGetValue(node, out var local))
                {
                    continue;
                }

                var x = mapRect.x + 8f + (local.x - bounds.xMin) * scale;
                var y = mapRect.y + 8f + (local.y - bounds.yMin) * scale;
                var w = Mathf.Max(3f, local.width * scale);
                var h = Mathf.Max(3f, local.height * scale);
                Handles.color = selectedNode == node ? Color.cyan : ColorFor(node.Kind);
                Handles.DrawSolidRectangleWithOutline(new Rect(x, y, w, h), Handles.color, Color.black);
            }

            Handles.EndGUI();
            GUI.Label(new Rect(mapRect.x + 6f, mapRect.yMax - 18f, mapRect.width - 12f, 14f), "minimap", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawNodeCard(Rect rect, EnemyBehaviorTreeNodeDefinition node, bool isRoot)
        {
            var previousColor = GUI.color;
            var isSelected = selectedNode == node;
            var activeLive = IsLiveHighlighted(node);
            GUI.color = activeLive
                ? new Color(0.65f, 0.9f, 1f)
                : isSelected
                    ? new Color(0.85f, 0.95f, 1f)
                    : ColorFor(node.Kind);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUI.color = previousColor;

            var inner = new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, rect.height - 12f);
            GUI.Label(new Rect(inner.x, inner.y, inner.width, 18f), $"{(isRoot ? "ROOT " : string.Empty)}{node.NodeId}", EditorStyles.boldLabel);
            GUI.Label(new Rect(inner.x, inner.y + 20f, inner.width, 18f), node.Kind.ToString(), EditorStyles.miniBoldLabel);
            GUI.Label(new Rect(inner.x, inner.y + 42f, inner.width, 36f), BehaviourTreeStudioAnalysis.SummaryFor(node), EditorStyles.wordWrappedMiniLabel);
            if (node is EnemyBehaviorActionNodeDefinition action)
            {
                GUI.Label(new Rect(inner.x, inner.y + 78f, inner.width, 18f), BehaviourTreeStudioAnalysis.BadgeFor(action, OwnerActions(), OwnerAttacks()), EditorStyles.miniLabel);
            }

            var current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
            {
                selectedNode = node;
                GUI.FocusControl(null);
                current.Use();
            }
        }

        private void DrawNodeInspector(UnityEngine.Object treeLike)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(340f)))
            {
                inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll, GUI.skin.box);
                EditorGUILayout.LabelField(Tr("Node Inspector", "Inspektor węzła"), EditorStyles.boldLabel);
                if (selectedNode == null)
                {
                    EditorGUILayout.HelpBox(Tr("Select a graph node. Click-drag empty canvas to pan; use the zoom slider above.", "Wybierz węzeł grafu. Przeciągnij puste tło, aby przesuwać; zoom jest na górze."), MessageType.Info);
                    EditorGUILayout.EndScrollView();
                    return;
                }

                EditorGUILayout.LabelField(BehaviourTreeStudioAnalysis.SummaryFor(selectedNode), EditorStyles.wordWrappedLabel);
                if (selectedNode is EnemyBehaviorActionNodeDefinition actionNode)
                {
                    EditorGUILayout.HelpBox(BehaviourTreeStudioAnalysis.BadgeFor(actionNode, OwnerActions(), OwnerAttacks()), MessageType.None);
                }

                DrawSerializedObject(selectedNode);
                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(Tr("Set Root", "Ustaw root")))
                    {
                        BehaviourTreeStudioGraphUtility.SetRoot(treeLike, selectedNode);
                    }

                    if (GUILayout.Button(Tr("Copy", "Kopiuj")))
                    {
                        copiedNode = selectedNode;
                    }

                    using (new EditorGUI.DisabledScope(copiedNode == null))
                    {
                        if (GUILayout.Button(Tr("Paste", "Wklej")))
                        {
                            selectedNode = BehaviourTreeStudioGraphUtility.DuplicateNode(treeLike, draft, copiedNode);
                            RebuildLayout();
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(Tr("Duplicate", "Duplikuj")))
                    {
                        selectedNode = BehaviourTreeStudioGraphUtility.DuplicateNode(treeLike, draft, selectedNode);
                        RebuildLayout();
                    }

                    if (GUILayout.Button(Tr("Delete", "Usuń")))
                    {
                        BehaviourTreeStudioGraphUtility.RemoveNode(treeLike, selectedNode);
                        selectedNode = null;
                        RebuildLayout();
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Tr("Connect", "Połącz"), EditorStyles.boldLabel);
                connectParent = (EnemyBehaviorTreeNodeDefinition)EditorGUILayout.ObjectField(Tr("Parent", "Rodzic"), connectParent, typeof(EnemyBehaviorTreeNodeDefinition), false);
                if (GUILayout.Button(Tr("Use Selected As Parent", "Użyj zaznaczonego jako rodzica")))
                {
                    connectParent = selectedNode;
                }

                using (new EditorGUI.DisabledScope(connectParent == null || connectParent == selectedNode))
                {
                    if (GUILayout.Button(Tr("Connect Parent -> Selected", "Połącz rodzic -> zaznaczony")))
                    {
                        BehaviourTreeStudioGraphUtility.ConnectChild(connectParent, selectedNode);
                    }

                    if (GUILayout.Button(Tr("Disconnect Parent -> Selected", "Rozłącz rodzic -> zaznaczony")))
                    {
                        BehaviourTreeStudioGraphUtility.DisconnectChild(connectParent, selectedNode);
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Tr("Children", "Dzieci"), EditorStyles.boldLabel);
                foreach (var child in selectedNode.Children)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(child, typeof(EnemyBehaviorTreeNodeDefinition), false);
                        if (GUILayout.Button(Tr("Select", "Wybierz"), GUILayout.Width(64f)))
                        {
                            selectedNode = child;
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawTemplatesTab()
        {
            EditorGUILayout.LabelField(Tr("Global Templates", "Globalne szablony"), EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(360f)))
                {
                    foreach (var template in templates)
                    {
                        if (template == null)
                        {
                            continue;
                        }

                        if (GUILayout.Toggle(selectedTemplate == template, $"{template.DisplayName} ({template.Role})", "Button"))
                        {
                            selectedTemplate = template;
                        }
                    }
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    if (selectedTemplate == null)
                    {
                        EditorGUILayout.HelpBox(Tr("Select a template from the left.", "Wybierz szablon z lewej."), MessageType.Info);
                        return;
                    }

                    EditorGUILayout.ObjectField(selectedTemplate, typeof(EnemyBehaviorTreeTemplateDefinition), false);
                    EditorGUILayout.LabelField(Tr("Role", "Rola"), selectedTemplate.Role.ToString());
                    EditorGUILayout.LabelField(Tr("Recommended", "Rekomendowane"), $"{selectedTemplate.RecommendedBehaviorId} / {selectedTemplate.RecommendedDisposition} / {selectedTemplate.MinimumIntelligence}");
                    EditorGUILayout.HelpBox(selectedTemplate.Description, MessageType.None);
                    var templateValidation = BehaviourTreeStudioAnalysis.Validate(selectedTemplate);
                    DrawAnalysisResult(templateValidation);

                    using (new EditorGUI.DisabledScope(draft.Draft is not EnemyBehaviorTreeDefinition))
                    {
                        if (GUILayout.Button(Tr("Apply Template To Current Tree Draft", "Zastosuj szablon do aktualnego szkicu drzewa")))
                        {
                            BehaviourTreeStudioGraphUtility.ReplaceTreeDraftWithTemplate((EnemyBehaviorTreeDefinition)draft.Draft, selectedTemplate, draft);
                            selectedNode = BehaviourTreeStudioGraphUtility.RootFor(draft.Draft);
                            RebuildLayout();
                        }
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(Tr("Template Nodes", "Węzły szablonu"), EditorStyles.boldLabel);
                    foreach (var node in selectedTemplate.Nodes)
                    {
                        EditorGUILayout.LabelField(node.NodeId, BehaviourTreeStudioAnalysis.SummaryFor(node));
                    }
                }
            }
        }

        private void DrawValidationTab()
        {
            if (GUILayout.Button(Tr("Run Validation + Readability Analysis", "Uruchom walidację i analizę czytelności")))
            {
                RunValidation();
            }

            DrawAnalysisResult(validationResult);
        }

        private void DrawSandboxTab()
        {
            var treeLike = draft.Draft;
            if (treeLike == null)
            {
                EditorGUILayout.HelpBox(Tr("Select a tree or template first.", "Najpierw wybierz drzewo albo szablon."), MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(Tr("Room Sandbox Preview", "Podgląd sandbox pokoju"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Tr(
                "Editor-only synthetic room: move the dummy player by distance/state controls, then step evaluation. This previews tree decisions without dirtying gameplay assets.",
                "Editorowy syntetyczny pokój: ustaw dystans i stany dummy playera, potem wykonaj krok ewaluacji. To podgląda decyzje drzewa bez zmiany assetów gameplayu."), MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(320f)))
                {
                    sandboxContext.DistanceToPlayer = EditorGUILayout.Slider(Tr("Distance", "Dystans"), sandboxContext.DistanceToPlayer, 0f, 10f);
                    sandboxContext.Awareness = (EnemyAwarenessState)EditorGUILayout.EnumPopup(Tr("Awareness", "Świadomość"), sandboxContext.Awareness);
                    sandboxContext.Disposition = (EnemyInstinctDisposition)EditorGUILayout.EnumPopup(Tr("Disposition", "Dyspozycja"), sandboxContext.Disposition);
                    sandboxContext.Intelligence = (EnemyIntelligenceLevel)EditorGUILayout.EnumPopup(Tr("Intelligence", "Inteligencja"), sandboxContext.Intelligence);
                    sandboxContext.BehaviorId = (EnemyBehaviorId)EditorGUILayout.EnumPopup(Tr("Behavior", "Zachowanie"), sandboxContext.BehaviorId);
                    sandboxContext.IsIdle = EditorGUILayout.Toggle(Tr("Idle", "Idle"), sandboxContext.IsIdle);
                    sandboxContext.IsEndangered = EditorGUILayout.Toggle(Tr("Endangered", "Zagrożony"), sandboxContext.IsEndangered);
                    sandboxContext.ShouldSentinelEngage = EditorGUILayout.Toggle(Tr("Sentinel Engage", "Sentinel aktywny"), sandboxContext.ShouldSentinelEngage);
                    sandboxContext.CanStartMelee = EditorGUILayout.Toggle(Tr("Melee Budget", "Budżet melee"), sandboxContext.CanStartMelee);
                    sandboxContext.CanStartRanged = EditorGUILayout.Toggle(Tr("Ranged Budget", "Budżet ranged"), sandboxContext.CanStartRanged);
                    sandboxContext.CanStartCharge = EditorGUILayout.Toggle(Tr("Charge Budget", "Budżet charge"), sandboxContext.CanStartCharge);
                    sandboxContext.CanStartArea = EditorGUILayout.Toggle(Tr("Area Budget", "Budżet area"), sandboxContext.CanStartArea);
                    sandboxContext.CanStartGuard = EditorGUILayout.Toggle(Tr("Guard Ready", "Guard gotowy"), sandboxContext.CanStartGuard);
                    sandboxContext.CanStartCreatureMove = EditorGUILayout.Toggle(Tr("Move Action Ready", "Akcja ruchu gotowa"), sandboxContext.CanStartCreatureMove);
                    sandboxContext.CanStartCreatureSignal = EditorGUILayout.Toggle(Tr("Signal Ready", "Sygnał gotowy"), sandboxContext.CanStartCreatureSignal);

                    if (GUILayout.Button(Tr("Step Tree Tick", "Krok drzewa")))
                    {
                        sandboxContext.TimeSeconds += 0.25f;
                        sandboxResult = treeLike switch
                        {
                            EnemyBehaviorTreeDefinition tree => BehaviourTreeStudioAnalysis.EvaluateSynthetic(tree, sandboxContext),
                            EnemyBehaviorTreeTemplateDefinition template => BehaviourTreeStudioAnalysis.EvaluateSynthetic(template, sandboxContext),
                            _ => null
                        };
                    }

                    if (GUILayout.Button(Tr("Reset Sandbox", "Reset sandboxa")))
                    {
                        sandboxContext.TimeSeconds = 1f;
                        sandboxResult = null;
                    }
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawSandboxRoomPreview();
                    if (sandboxResult != null)
                    {
                        EditorGUILayout.LabelField(Tr("Result", "Wynik"), sandboxResult.Success ? Tr("Success", "Sukces") : Tr("Failed", "Niepowodzenie"));
                        EditorGUILayout.LabelField(Tr("Command", "Komenda"), $"{sandboxResult.Command.Kind} {sandboxResult.Command.ActionId}");
                        EditorGUILayout.LabelField(Tr("Reason", "Powód"), string.IsNullOrWhiteSpace(sandboxResult.FailureReason) ? sandboxResult.Command.Reason : sandboxResult.FailureReason);
                        EditorGUILayout.LabelField(Tr("Evaluated Path", "Ścieżka ewaluacji"), string.Join(" > ", sandboxResult.Path.Select(node => node.NodeId)));
                    }
                }
            }
        }

        private void DrawSandboxRoomPreview()
        {
            var rect = GUILayoutUtility.GetRect(360f, 220f, GUILayout.ExpandWidth(true));
            GUI.Box(rect, GUIContent.none);
            Handles.BeginGUI();
            var center = rect.center;
            var scale = Mathf.Min(rect.width, rect.height) / 12f;
            Handles.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            for (var i = -5; i <= 5; i++)
            {
                Handles.DrawLine(new Vector3(center.x + i * scale, rect.y + 8f), new Vector3(center.x + i * scale, rect.yMax - 8f));
                Handles.DrawLine(new Vector3(rect.x + 8f, center.y + i * scale), new Vector3(rect.xMax - 8f, center.y + i * scale));
            }

            Handles.color = Color.red;
            Handles.DrawSolidDisc(center, Vector3.forward, 6f);
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(center, Vector3.forward, sandboxContext.PreferredMin * scale);
            Handles.DrawWireDisc(center, Vector3.forward, sandboxContext.PreferredMax * scale);
            var player = center + new Vector2(0f, -sandboxContext.DistanceToPlayer * scale);
            Handles.color = Color.green;
            Handles.DrawSolidDisc(player, Vector3.forward, 6f);
            Handles.DrawLine(center, player);
            Handles.EndGUI();
            GUI.Label(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 18f), Tr("red enemy / green dummy player / cyan preferred envelope", "czerwony wróg / zielony dummy player / cyjan envelope dystansu"), EditorStyles.miniLabel);
        }

        private void DrawLiveTraceTab()
        {
            EditorGUILayout.LabelField(Tr("Play Mode Live Trace", "Live trace w Play Mode"), EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(Tr("Enter Play Mode and select an enemy instance to stream its AI blackboard.", "Wejdź w Play Mode i zaznacz instancję wroga, aby streamować blackboard AI."), MessageType.Info);
                return;
            }

            var selectedRuntime = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<EnemyRuntimeController>()
                : null;
            if (selectedRuntime == null)
            {
                EditorGUILayout.HelpBox(Tr("Select a runtime enemy GameObject.", "Zaznacz GameObject wroga runtime."), MessageType.Warning);
                return;
            }

            var blackboard = selectedRuntime.AiBlackboard;
            EditorGUILayout.ObjectField(selectedRuntime, typeof(EnemyRuntimeController), true);
            EditorGUILayout.TextArea(blackboard.Summary, GUILayout.MinHeight(120f));
            EditorGUILayout.LabelField(Tr("LOD", "LOD"), blackboard.LodTier.ToString());
            EditorGUILayout.LabelField(Tr("Tree Command", "Komenda drzewa"), blackboard.TreeCommand.ToString());
            EditorGUILayout.LabelField(Tr("Chosen Action", "Wybrana akcja"), blackboard.ChosenActionId);
            EditorGUILayout.LabelField(Tr("Path Status", "Status ścieżki"), blackboard.PathStatus.ToString());
            EditorGUILayout.LabelField(Tr("Cooldown/Fallback", "Cooldown/Fallback"), blackboard.CooldownReason);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Tr("Decision History", "Historia decyzji"), EditorStyles.boldLabel);
            foreach (var row in traceHistory.Skip(Mathf.Max(0, traceHistory.Count - 20)))
            {
                EditorGUILayout.LabelField(row, EditorStyles.miniLabel);
            }
        }

        private void DrawDiffTab()
        {
            EditorGUILayout.LabelField(Tr("Tree Diff", "Diff drzewa"), EditorStyles.boldLabel);
            diffTarget = (EnemyBehaviorTreeDefinition)EditorGUILayout.ObjectField(Tr("Compare To", "Porównaj z"), diffTarget, typeof(EnemyBehaviorTreeDefinition), false);
            if (draft.Draft is EnemyBehaviorTreeDefinition draftTree && diffTarget != null)
            {
                EditorGUILayout.HelpBox(BehaviourTreeStudioAnalysis.DiffSummary(diffTarget, draftTree), MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(Tr("Load a runtime tree draft and choose another runtime tree to compare node sets.", "Załaduj szkic drzewa runtime i wybierz drugie drzewo do porównania węzłów."), MessageType.Info);
            }
        }

        private void DrawAnalysisResult(BehaviourTreeStudioAnalysisResult result)
        {
            if (result == null)
            {
                EditorGUILayout.HelpBox(Tr("No analysis run yet.", "Analiza nie została jeszcze uruchomiona."), MessageType.Info);
                return;
            }

            foreach (var error in result.Errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            foreach (var warning in result.Warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            foreach (var note in result.ReadabilityNotes)
            {
                EditorGUILayout.HelpBox(note, MessageType.Info);
            }

            if (result.IsValid && result.Warnings.Count == 0)
            {
                EditorGUILayout.HelpBox(Tr("Validation passed.", "Walidacja przeszła pomyślnie."), MessageType.Info);
            }
        }

        private void RunValidation()
        {
            validationResult = draft.Draft switch
            {
                EnemyBehaviorTreeDefinition tree => BehaviourTreeStudioAnalysis.Validate(tree, OwnerActions(), tree.BossMetadataOnly),
                EnemyBehaviorTreeTemplateDefinition template => BehaviourTreeStudioAnalysis.Validate(template),
                _ => null
            };
        }

        private void LoadSource(UnityEngine.Object source)
        {
            selectedSource = source;
            draft.Load(source);
            selectedOwner = FindOwnerFor(source);
            selectedNode = BehaviourTreeStudioGraphUtility.RootFor(draft.Draft);
            selectedTemplate = source as EnemyBehaviorTreeTemplateDefinition;
            validationResult = null;
            sandboxResult = null;
            RebuildLayout();
        }

        private void RefreshAssets()
        {
            trees.Clear();
            templates.Clear();
            owners.Clear();
            trees.AddRange(FindAssets<EnemyBehaviorTreeDefinition>("t:EnemyBehaviorTreeDefinition", "Assets/_Hollow/Data"));
            templates.AddRange(FindAssets<EnemyBehaviorTreeTemplateDefinition>("t:EnemyBehaviorTreeTemplateDefinition", "Assets/_Hollow/Data"));
            owners.AddRange(FindAssets<EnemyDefinition>("t:EnemyDefinition", "Assets/_Hollow/Data/Enemies"));
            owners.AddRange(FindAssets<BossDefinition>("t:BossDefinition", "Assets/_Hollow/Data/Bosses"));
            trees.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal));
            templates.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal));
            if (selectedSource == null)
            {
                var first = trees.FirstOrDefault();
                if (first != null)
                {
                    LoadSource(first);
                }
            }
        }

        private UnityEngine.Object FindOwnerFor(UnityEngine.Object source)
        {
            if (source is not EnemyBehaviorTreeDefinition tree)
            {
                return null;
            }

            return owners.FirstOrDefault(owner => owner switch
            {
                EnemyDefinition enemy => enemy.BehaviorTree == tree,
                BossDefinition boss => boss.BehaviorTreeMetadata == tree,
                _ => false
            });
        }

        private IReadOnlyCollection<EnemyActionProfileDefinition> OwnerActions()
        {
            return selectedOwner switch
            {
                EnemyDefinition enemy => enemy.ActionProfiles,
                BossDefinition boss => boss.ActionProfiles,
                _ => Array.Empty<EnemyActionProfileDefinition>()
            };
        }

        private IReadOnlyCollection<EnemyAttackProfileDefinition> OwnerAttacks()
        {
            return selectedOwner switch
            {
                EnemyDefinition enemy => enemy.AttackProfiles,
                BossDefinition boss => boss.AttackProfiles,
                _ => Array.Empty<EnemyAttackProfileDefinition>()
            };
        }

        private void RebuildLayout()
        {
            nodeRects = BehaviourTreeStudioGraphUtility.AutoLayout(draft.Draft);
        }

        private Rect ToScreen(Rect canvas, Rect local)
        {
            return new Rect(
                canvas.x + graphPan.x + local.x * graphZoom,
                canvas.y + graphPan.y + local.y * graphZoom,
                local.width * graphZoom,
                local.height * graphZoom);
        }

        private void HandleCanvasInput(Rect rect)
        {
            var current = Event.current;
            if (!rect.Contains(current.mousePosition))
            {
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                var hitNode = nodeRects.Any(pair => ToScreen(rect, pair.Value).Contains(current.mousePosition));
                if (!hitNode)
                {
                    draggingCanvas = true;
                    lastMouse = current.mousePosition;
                    current.Use();
                }
            }

            if (current.type == EventType.MouseDrag && draggingCanvas)
            {
                graphPan += current.mousePosition - lastMouse;
                lastMouse = current.mousePosition;
                current.Use();
                Repaint();
            }

            if (current.type == EventType.MouseUp)
            {
                draggingCanvas = false;
            }

            if (current.type == EventType.ScrollWheel)
            {
                graphZoom = Mathf.Clamp(graphZoom - current.delta.y * 0.03f, 0.45f, 1.8f);
                current.Use();
            }
        }

        private void DrawGrid(Rect rect)
        {
            Handles.color = new Color(1f, 1f, 1f, 0.08f);
            var spacing = 48f * graphZoom;
            if (spacing < 12f)
            {
                return;
            }

            for (var x = rect.x + graphPan.x % spacing; x < rect.xMax; x += spacing)
            {
                Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.yMax));
            }

            for (var y = rect.y + graphPan.y % spacing; y < rect.yMax; y += spacing)
            {
                Handles.DrawLine(new Vector3(rect.x, y), new Vector3(rect.xMax, y));
            }
        }

        private bool MatchesSearch(UnityEngine.Object target, string extra)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            var needle = search.Trim();
            var haystack = $"{target?.name} {extra}".ToLowerInvariant();
            return haystack.Contains(needle.ToLowerInvariant());
        }

        private bool MatchesSearch(EnemyBehaviorTreeNodeDefinition node, string extra)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            var action = node as EnemyBehaviorActionNodeDefinition;
            var condition = node as EnemyBehaviorConditionNodeDefinition;
            var haystack = $"{node.NodeId} {node.Kind} {extra} {action?.ActionId} {action?.CommandKind} {condition?.ActionId} {condition?.Condition}".ToLowerInvariant();
            return haystack.Contains(search.Trim().ToLowerInvariant());
        }

        private bool IsLiveHighlighted(EnemyBehaviorTreeNodeDefinition node)
        {
            if (!EditorApplication.isPlaying || node == null)
            {
                return false;
            }

            var selectedRuntime = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<EnemyRuntimeController>()
                : null;
            if (selectedRuntime == null)
            {
                return false;
            }

            var blackboard = selectedRuntime.AiBlackboard;
            if (node is EnemyBehaviorActionNodeDefinition action)
            {
                return !string.IsNullOrWhiteSpace(blackboard.ChosenActionId) &&
                    string.Equals(action.ActionId, blackboard.ChosenActionId, StringComparison.Ordinal);
            }

            return false;
        }

        private void TickLiveTrace()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            var selectedRuntime = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<EnemyRuntimeController>()
                : null;
            if (selectedRuntime == null)
            {
                return;
            }

            var blackboard = selectedRuntime.AiBlackboard;
            var row = $"{DateTime.Now:HH:mm:ss.fff} {blackboard.LodTier} {blackboard.TreeCommand}->{blackboard.ChosenCommand} {blackboard.ChosenActionId} score {blackboard.ChosenScore:0.00}";
            if (traceHistory.Count == 0 || traceHistory[traceHistory.Count - 1] != row)
            {
                traceHistory.Add(row);
                while (traceHistory.Count > 40)
                {
                    traceHistory.RemoveAt(0);
                }
            }
        }

        private static Color ColorFor(EnemyBehaviorTreeNodeKind kind)
        {
            return kind switch
            {
                EnemyBehaviorTreeNodeKind.Selector => new Color(0.62f, 0.70f, 0.86f),
                EnemyBehaviorTreeNodeKind.Sequence => new Color(0.62f, 0.80f, 0.72f),
                EnemyBehaviorTreeNodeKind.WeightedSelector => new Color(0.82f, 0.74f, 0.58f),
                EnemyBehaviorTreeNodeKind.Condition => new Color(0.78f, 0.68f, 0.88f),
                EnemyBehaviorTreeNodeKind.Action => new Color(0.88f, 0.68f, 0.64f),
                _ => Color.white
            };
        }

        private static IReadOnlyList<T> FindAssets<T>(string filter, string folder)
            where T : UnityEngine.Object
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return Array.Empty<T>();
            }

            return AssetDatabase.FindAssets(filter, new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .ToArray();
        }

        private static void DrawSerializedObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            var serialized = new SerializedObject(target);
            var property = serialized.GetIterator();
            var enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyPath == "m_Script")
                {
                    continue;
                }

                EditorGUILayout.PropertyField(property, includeChildren: true);
            }

            serialized.ApplyModifiedProperties();
        }

        private static string Tr(string english, string polish)
        {
            return BehaviourTreeStudioLocalization.T(english, polish);
        }
    }
}
