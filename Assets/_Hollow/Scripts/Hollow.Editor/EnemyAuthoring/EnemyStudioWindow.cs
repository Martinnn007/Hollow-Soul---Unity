using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.BehaviorTreeStudio;
using Hollow.Editor.CombatEncounterSimulator;
using Hollow.Editor.EnemyAiBrainStudio;
using Hollow.Editor.EnemyPreviewLab;
using Hollow.Presentation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.EnemyAuthoring
{
    public sealed class EnemyStudioWindow : EditorWindow
    {
        private readonly EnemyAuthoringDraft rootDraft = new();
        private readonly EnemyAuthoringDraft linkedDraft = new();
        private readonly List<UnityEngine.Object> roster = new();
        private int selectedPanel;
        private Vector2 scroll;
        private Vector2 rosterScroll;
        private UnityEngine.Object selectedSource;
        private UnityEngine.Object selectedLinkedSource;
        private EnemyBehaviorTreeNodeDefinition selectedTreeNode;
        private string applyNotes = "Manual Enemy Studio edit";
        private EnemyAuthoringValidationResult lastValidation;
        private bool showAdvancedSerialized;

        [MenuItem("Hollow/Enemy Authoring/Enemy Studio")]
        public static void Open()
        {
            GetWindow<EnemyStudioWindow>("Enemy Studio");
        }

        private void OnEnable()
        {
            RefreshRoster();
        }

        private void OnDisable()
        {
            rootDraft.Dispose();
            linkedDraft.Dispose();
        }

        private void OnGUI()
        {
            DrawToolbar();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawRosterSidebar();
                using (new EditorGUILayout.VerticalScope())
                {
                    scroll = EditorGUILayout.BeginScrollView(scroll);
                    switch (selectedPanel)
                    {
                        case 0:
                            DrawRosterPanel();
                            break;
                        case 1:
                            DrawStatsPanel();
                            break;
                        case 2:
                            DrawProfileListPanel<EnemyAttackProfileDefinition>("attackProfiles", "Attacks", "Ataki");
                            break;
                        case 3:
                            DrawProfileListPanel<EnemyActionProfileDefinition>("actionProfiles", "Actions", "Akcje");
                            break;
                        case 4:
                            DrawSpacingPanel();
                            break;
                        case 5:
                            DrawBehaviorTreePanel();
                            break;
                        case 6:
                            DrawVisualsPanel();
                            break;
                        case 7:
                            DrawLiveTuningPanel();
                            break;
                        case 8:
                            DrawValidationApplyPanel();
                            break;
                    }

                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                selectedPanel = GUILayout.Toolbar(selectedPanel, EnemyAuthoringLocalization.PanelLabels, EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();
                var language = GUILayout.Toolbar(
                    (int)EnemyAuthoringLocalization.CurrentLanguage,
                    new[] { "EN", "PL" },
                    EditorStyles.toolbarButton,
                    GUILayout.Width(64f));
                if (language != (int)EnemyAuthoringLocalization.CurrentLanguage)
                {
                    EnemyAuthoringLocalization.CurrentLanguage = (EnemyAuthoringLanguage)language;
                    Repaint();
                }

                if (GUILayout.Button(Tr("AI Brain", "Mózg AI"), EditorStyles.toolbarButton, GUILayout.Width(78f)))
                {
                    if (rootDraft.Draft is EnemyDefinition enemyDraft)
                    {
                        EnemyAiBrainStudioWindow.OpenEnemy(selectedSource as EnemyDefinition ?? enemyDraft);
                    }
                    else
                    {
                        EnemyAiBrainStudioWindow.Open();
                    }
                }

                if (GUILayout.Button(Tr("Preview Lab", "Preview Lab"), EditorStyles.toolbarButton, GUILayout.Width(94f)))
                {
                    if (rootDraft.Draft is EnemyDefinition enemyDraft)
                    {
                        EnemyPreviewLabSceneBuilder.OpenWithEnemy(selectedSource as EnemyDefinition ?? enemyDraft);
                    }
                    else
                    {
                        EnemyPreviewLabWindow.Open();
                    }
                }

                if (GUILayout.Button(Tr("Encounter Sim", "Symulacja"), EditorStyles.toolbarButton, GUILayout.Width(104f)))
                {
                    if (rootDraft.Draft is EnemyDefinition enemyDraft)
                    {
                        CombatEncounterSimulatorWindow.OpenWithEnemy(selectedSource as EnemyDefinition ?? enemyDraft);
                    }
                    else
                    {
                        CombatEncounterSimulatorWindow.Open();
                    }
                }

                if (GUILayout.Button(Tr("Refresh", "Odśwież"), EditorStyles.toolbarButton, GUILayout.Width(74f)))
                {
                    RefreshRoster();
                }
            }
        }

        private void DrawRosterSidebar()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(260f)))
            {
                EditorGUILayout.LabelField(Tr("Enemy/Boss Assets", "Assety wrogów/bossów"), EditorStyles.boldLabel);
                rosterScroll = EditorGUILayout.BeginScrollView(rosterScroll, GUI.skin.box);
                foreach (var asset in roster)
                {
                    if (asset == null)
                    {
                        continue;
                    }

                    var selected = asset == selectedSource;
                    var label = DisplayNameFor(asset);
                    var protectedLabel = EnemyAuthoringProtectionRegistryUtility.IsProtected(asset) ? " *" : string.Empty;
                    if (GUILayout.Toggle(selected, $"{label}{protectedLabel}", "Button"))
                    {
                        if (!selected)
                        {
                            SelectRoot(asset);
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.HelpBox(Tr(
                    "* means manually protected from generator overwrite.",
                    "* oznacza ręczną ochronę przed nadpisaniem przez generator."), MessageType.None);
            }
        }

        private void DrawRosterPanel()
        {
            EditorGUILayout.LabelField(Tr("Roster", "Lista"), EditorStyles.boldLabel);
            var next = EditorGUILayout.ObjectField(Tr("Selected Source", "Wybrane źródło"), selectedSource, typeof(ScriptableObject), false);
            if (next != selectedSource)
            {
                SelectRoot(next);
            }

            if (selectedSource == null)
            {
                EditorGUILayout.HelpBox(Tr("Select an enemy or boss asset from the left.", "Wybierz asset wroga lub bossa z listy po lewej."), MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(Tr("Asset Path", "Ścieżka assetu"), AssetDatabase.GetAssetPath(selectedSource));
            EditorGUILayout.LabelField(Tr("Draft State", "Stan szkicu"), rootDraft.IsDirty ? Tr("Dirty", "Zmieniony") : Tr("Clean", "Czysty"));
            if (GUILayout.Button(Tr("Ping Source Asset", "Pokaż asset źródłowy")))
            {
                EditorGUIUtility.PingObject(selectedSource);
            }

            if (GUILayout.Button(Tr("Discard Draft", "Odrzuć szkic")))
            {
                rootDraft.Discard();
                GUI.FocusControl(null);
            }
        }

        private void DrawStatsPanel()
        {
            EditorGUILayout.LabelField(Tr("Stats & Senses", "Staty i zmysły"), EditorStyles.boldLabel);
            var draft = rootDraft.Draft;
            if (draft == null)
            {
                DrawNoDraftHelp();
                return;
            }

            var fields = draft is EnemyDefinition
                ? new[]
                {
                    "spawnKind", "displayName", "archetypeId", "behaviorId", "movementMode",
                    "maxHealth", "speedMetersPerSecond", "radiusMeters", "bodyClass",
                    "intelligence", "disposition", "contactDamage", "contactCooldownSeconds",
                    "contactDamagePolicy", "passiveContactHazardType", "preferredRangeMinMeters", "preferredRangeMaxMeters",
                    "sightRadiusMeters", "sightAngleDegrees", "hearingRadiusMeters",
                    "hearingSensitivityMultiplier", "disturbanceEscalationThreshold", "investigationDurationSeconds",
                    "allyAlertSharingEnabled", "allyAlertRadiusMeters", "allyAlertCooldownSeconds", "allyAlertMinimumAwareness",
                    "lungeAttackEnabled", "lungeTriggerRangeMeters", "lungeWindupSeconds", "lungeActiveSeconds",
                    "lungeDistanceMeters", "lungeCooldownSeconds", "attackWindupScale", "attackActiveScale",
                    "attackRecoveryScale", "hitArcDegreesBonus", "poiseBreakThresholdOffset", "color"
                }
                : new[]
                {
                    "bossId", "displayName", "worldBand", "behaviorId", "maxHealth", "speedMetersPerSecond",
                    "radiusMeters", "visualScale", "bodyClass", "intelligence", "contactDamage",
                    "contactCooldownSeconds", "contactDamagePolicy", "passiveContactHazardType",
                    "sightRadiusMeters", "sightAngleDegrees", "hearingRadiusMeters", "projectileSpeedMetersPerSecond",
                    "debugColor", "arena", "phases", "attacks"
                };
            DrawSerializedFields(draft, fields);
            DrawAdvancedSerialized(draft);
        }

        private void DrawProfileListPanel<TProfile>(string listPropertyName, string englishTitle, string polishTitle)
            where TProfile : ScriptableObject
        {
            EditorGUILayout.LabelField(Tr(englishTitle, polishTitle), EditorStyles.boldLabel);
            var draft = rootDraft.Draft;
            if (draft == null)
            {
                DrawNoDraftHelp();
                return;
            }

            var serialized = new SerializedObject(draft);
            var list = serialized.FindProperty(listPropertyName);
            if (list == null)
            {
                EditorGUILayout.HelpBox(Tr("Selected asset does not expose this list.", "Wybrany asset nie ma tej listy."), MessageType.Info);
                return;
            }

            EditorGUILayout.PropertyField(list, includeChildren: true);
            serialized.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Tr("Linked Profile Draft", "Szkic profilu"), EditorStyles.boldLabel);
            for (var index = 0; index < list.arraySize; index++)
            {
                var profile = list.GetArrayElementAtIndex(index).objectReferenceValue as TProfile;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(profile, typeof(TProfile), false);
                    if (GUILayout.Button(Tr("Edit Draft", "Edytuj szkic"), GUILayout.Width(100f)) && profile != null)
                    {
                        SelectLinked(profile);
                    }
                }
            }

            DrawLinkedDraftEditor();
        }

        private void DrawSpacingPanel()
        {
            EditorGUILayout.LabelField(Tr("Spacing", "Dystans"), EditorStyles.boldLabel);
            var draft = rootDraft.Draft;
            if (draft == null)
            {
                DrawNoDraftHelp();
                return;
            }

            var propertyName = draft is BossDefinition ? "spacingProfileMetadata" : "spacingProfile";
            var profile = DrawObjectReferenceField<EnemySpacingProfileDefinition>(draft, propertyName, Tr("Spacing Profile", "Profil dystansu"));
            if (profile != null && GUILayout.Button(Tr("Edit Spacing Draft", "Edytuj szkic dystansu")))
            {
                SelectLinked(profile);
            }

            DrawLinkedDraftEditor();
        }

        private void DrawBehaviorTreePanel()
        {
            EditorGUILayout.LabelField(Tr("Behavior Tree", "Drzewo AI"), EditorStyles.boldLabel);
            var draft = rootDraft.Draft;
            if (draft == null)
            {
                DrawNoDraftHelp();
                return;
            }

            var propertyName = draft is BossDefinition ? "behaviorTreeMetadata" : "behaviorTree";
            var tree = DrawObjectReferenceField<EnemyBehaviorTreeDefinition>(draft, propertyName, Tr("Behavior Tree", "Drzewo AI"));
            if (tree != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(Tr("Open In Behaviour Tree Studio", "Otwórz w Behaviour Tree Studio")))
                    {
                        BehaviourTreeStudioWindow.OpenTree(tree);
                    }

                    if (GUILayout.Button(Tr("Edit Tree Draft Here", "Edytuj szkic tutaj")))
                    {
                        SelectLinked(tree);
                    }
                }
            }

            if (linkedDraft.Draft is not EnemyBehaviorTreeDefinition linkedTree)
            {
                DrawLinkedDraftEditor();
                return;
            }

            DrawTreeDraftEditor(linkedTree);
        }

        private void DrawVisualsPanel()
        {
            EditorGUILayout.LabelField(Tr("Visuals", "Wizualia"), EditorStyles.boldLabel);
            var draft = rootDraft.Draft;
            if (draft == null)
            {
                DrawNoDraftHelp();
                return;
            }

            DrawSerializedFields(draft, new[]
            {
                "presentationPrefabRoleOverrideEnabled", "presentationPrefabRoleOverride",
                "weaponPrefabRoleOverrideEnabled", "weaponPrefabRoleOverride",
                "offhandPrefabRoleOverrideEnabled", "offhandPrefabRoleOverride",
                "projectilePrefabRoleOverrideEnabled", "projectilePrefabRoleOverride",
                "vfxPrefabRoleOverrideEnabled", "vfxPrefabRoleOverride"
            });

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Tr("Resolved Preview Roles", "Rozwiązane role podglądu"), EditorStyles.boldLabel);
            if (draft is EnemyDefinition enemy)
            {
                DrawRolePreview(enemy.PresentationPrefabRole, Tr("Body", "Ciało"));
                if (enemy.HasWeaponPrefabRoleOverride) DrawRolePreview(enemy.WeaponPrefabRole, Tr("Weapon", "Broń"));
                if (enemy.HasOffhandPrefabRoleOverride) DrawRolePreview(enemy.OffhandPrefabRole, Tr("Offhand", "Druga ręka"));
                if (enemy.HasProjectilePrefabRoleOverride) DrawRolePreview(enemy.ProjectilePrefabRole, Tr("Projectile", "Pocisk"));
                if (enemy.HasVfxPrefabRoleOverride) DrawRolePreview(enemy.VfxPrefabRole, "VFX");
            }
            else if (draft is BossDefinition boss)
            {
                DrawRolePreview(boss.PresentationPrefabRole, Tr("Body", "Ciało"));
                if (boss.HasWeaponPrefabRoleOverride) DrawRolePreview(boss.WeaponPrefabRole, Tr("Weapon", "Broń"));
                if (boss.HasOffhandPrefabRoleOverride) DrawRolePreview(boss.OffhandPrefabRole, Tr("Offhand", "Druga ręka"));
                if (boss.HasProjectilePrefabRoleOverride) DrawRolePreview(boss.ProjectilePrefabRole, Tr("Projectile", "Pocisk"));
                if (boss.HasVfxPrefabRoleOverride) DrawRolePreview(boss.VfxPrefabRole, "VFX");
            }
        }

        private void DrawLiveTuningPanel()
        {
            EditorGUILayout.LabelField(Tr("Live Tuning", "Live tuning"), EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(Tr(
                    "Enter Play Mode to apply transient tuning to active non-boss enemies. This does not dirty assets.",
                    "Wejdź w Play Mode, aby zastosować tymczasowy tuning na aktywnych zwykłych wrogach. To nie zmienia assetów."), MessageType.Info);
                return;
            }

            if (rootDraft.Draft is not EnemyDefinition enemyDraft)
            {
                EditorGUILayout.HelpBox(Tr("Live tuning V1 supports non-boss enemy definitions.", "Live tuning V1 obsługuje definicje zwykłych wrogów."), MessageType.Warning);
                return;
            }

            var active = FindObjectsByType<EnemyRuntimeController>(FindObjectsInactive.Exclude)
                .Where(enemy => enemy != null && enemy.Definition != null && enemy.BossDefinition == null)
                .ToArray();
            var matching = active.Where(enemy => enemy.Definition.SpawnKind == enemyDraft.SpawnKind).ToArray();
            EditorGUILayout.LabelField(Tr("Active Enemies", "Aktywni wrogowie"), active.Length.ToString());
            EditorGUILayout.LabelField(Tr("Matching Spawn Kind", "Pasujące spawn kind"), matching.Length.ToString());
            using (new EditorGUI.DisabledScope(matching.Length == 0))
            {
                if (GUILayout.Button(Tr("Apply Draft To Matching Runtime Enemies", "Zastosuj szkic do pasujących wrogów runtime")))
                {
                    foreach (var runtime in matching)
                    {
                        runtime.ApplyDebugTuningOverride(enemyDraft);
                    }
                }
            }

            if (Selection.activeGameObject != null &&
                Selection.activeGameObject.TryGetComponent<EnemyRuntimeController>(out var selectedRuntime) &&
                selectedRuntime.BossDefinition == null)
            {
                if (GUILayout.Button(Tr("Apply Draft To Selected Enemy", "Zastosuj szkic do zaznaczonego wroga")))
                {
                    selectedRuntime.ApplyDebugTuningOverride(enemyDraft);
                }
            }
        }

        private void DrawValidationApplyPanel()
        {
            EditorGUILayout.LabelField(Tr("Validation & Apply", "Walidacja i zapis"), EditorStyles.boldLabel);
            applyNotes = EditorGUILayout.TextField(Tr("Apply Notes", "Notatka zapisu"), applyNotes);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Tr("Validate Draft", "Sprawdź szkic")))
                {
                    lastValidation = EnemyAuthoringValidator.Validate(rootDraft.Draft);
                }

                if (GUILayout.Button(Tr("Validate Linked Draft", "Sprawdź szkic profilu")))
                {
                    lastValidation = EnemyAuthoringValidator.Validate(linkedDraft.Draft);
                }
            }

            DrawValidationResult(lastValidation);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Tr("Apply", "Zapis"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Tr("Root Draft", "Główny szkic"), rootDraft.BuildDiffSummary());
            if (linkedDraft.Source != null)
            {
                EditorGUILayout.LabelField(Tr("Linked Draft", "Szkic profilu"), linkedDraft.BuildDiffSummary());
            }

            using (new EditorGUI.DisabledScope(rootDraft.Draft == null || !rootDraft.IsDirty))
            {
                if (GUILayout.Button(Tr("Apply Root Draft To Asset", "Zapisz główny szkic do assetu")))
                {
                    var validation = EnemyAuthoringValidator.Validate(rootDraft.Draft);
                    if (validation.IsValid)
                    {
                        rootDraft.Apply(applyNotes);
                        RefreshRoster();
                    }
                    lastValidation = validation;
                }
            }

            using (new EditorGUI.DisabledScope(linkedDraft.Draft == null || !linkedDraft.IsDirty))
            {
                if (GUILayout.Button(Tr("Apply Linked Draft To Asset", "Zapisz szkic profilu do assetu")))
                {
                    var validation = EnemyAuthoringValidator.Validate(linkedDraft.Draft);
                    if (validation.IsValid)
                    {
                        linkedDraft.Apply(applyNotes);
                    }
                    lastValidation = validation;
                }
            }
        }

        private void DrawLinkedDraftEditor()
        {
            if (selectedLinkedSource == null)
            {
                EditorGUILayout.HelpBox(Tr("Select a linked profile to edit its draft.", "Wybierz profil, aby edytować jego szkic."), MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(Tr("Linked Source", "Źródło profilu"), selectedLinkedSource.name);
            var draft = linkedDraft.Draft;
            if (draft == null)
            {
                return;
            }

            DrawSerializedObject(draft);
            EditorGUILayout.LabelField(Tr("Linked Draft State", "Stan szkicu profilu"), linkedDraft.IsDirty ? Tr("Dirty", "Zmieniony") : Tr("Clean", "Czysty"));
        }

        private void DrawTreeDraftEditor(EnemyBehaviorTreeDefinition tree)
        {
            EditorGUILayout.LabelField(Tr("Tree Draft", "Szkic drzewa"), tree.DisplayName, EditorStyles.boldLabel);
            DrawSerializedFields(tree, new[] { "treeId", "displayName", "ownerId", "bossMetadataOnly", "rootNode" });

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Selector")) AddTreeNode<EnemyBehaviorSelectorNodeDefinition>(tree, "selector");
                if (GUILayout.Button("+ Sequence")) AddTreeNode<EnemyBehaviorSequenceNodeDefinition>(tree, "sequence");
                if (GUILayout.Button("+ Weighted")) AddTreeNode<EnemyBehaviorWeightedSelectorNodeDefinition>(tree, "weighted");
                if (GUILayout.Button("+ Condition")) AddTreeNode<EnemyBehaviorConditionNodeDefinition>(tree, "condition");
                if (GUILayout.Button("+ Action")) AddTreeNode<EnemyBehaviorActionNodeDefinition>(tree, "action");
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(260f)))
                {
                    EditorGUILayout.LabelField(Tr("Nodes", "Węzły"), EditorStyles.boldLabel);
                    foreach (var node in tree.Nodes)
                    {
                        if (node == null)
                        {
                            continue;
                        }

                        if (GUILayout.Toggle(selectedTreeNode == node, $"{node.NodeId} ({node.Kind})", "Button"))
                        {
                            selectedTreeNode = node;
                        }
                    }

                    if (selectedTreeNode != null && GUILayout.Button(Tr("Remove Selected Node", "Usuń wybrany węzeł")))
                    {
                        RemoveTreeNode(tree, selectedTreeNode);
                        selectedTreeNode = null;
                    }
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    if (selectedTreeNode == null)
                    {
                        EditorGUILayout.HelpBox(Tr("Select a node to edit its fields and child links.", "Wybierz węzeł, aby edytować pola i połączenia."), MessageType.Info);
                    }
                    else
                    {
                        DrawSerializedObject(selectedTreeNode);
                    }
                }
            }
        }

        private void DrawValidationResult(EnemyAuthoringValidationResult result)
        {
            if (result == null)
            {
                EditorGUILayout.HelpBox(Tr("No validation run yet.", "Walidacja nie została jeszcze uruchomiona."), MessageType.Info);
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

            if (result.IsValid && result.Warnings.Count == 0)
            {
                EditorGUILayout.HelpBox(Tr("Validation passed.", "Walidacja przeszła pomyślnie."), MessageType.Info);
            }
        }

        private void SelectRoot(UnityEngine.Object asset)
        {
            selectedSource = asset;
            selectedLinkedSource = null;
            selectedTreeNode = null;
            linkedDraft.Dispose();
            rootDraft.Load(asset);
            lastValidation = null;
        }

        private void SelectLinked(UnityEngine.Object asset)
        {
            selectedLinkedSource = asset;
            selectedTreeNode = null;
            linkedDraft.Load(asset);
        }

        private void RefreshRoster()
        {
            roster.Clear();
            roster.AddRange(FindAssets<EnemyDefinition>("t:EnemyDefinition", "Assets/_Hollow/Data/Enemies"));
            roster.AddRange(FindAssets<BossDefinition>("t:BossDefinition", "Assets/_Hollow/Data/Bosses"));
            roster.Sort((left, right) => string.Compare(DisplayNameFor(left), DisplayNameFor(right), StringComparison.Ordinal));
            if (selectedSource == null && roster.Count > 0)
            {
                SelectRoot(roster[0]);
            }
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

        private void DrawSerializedFields(UnityEngine.Object target, IEnumerable<string> fields)
        {
            var serialized = new SerializedObject(target);
            foreach (var field in fields)
            {
                var property = serialized.FindProperty(field);
                if (property != null)
                {
                    EditorGUILayout.PropertyField(property, includeChildren: true);
                }
            }

            serialized.ApplyModifiedProperties();
        }

        private void DrawAdvancedSerialized(UnityEngine.Object target)
        {
            showAdvancedSerialized = EditorGUILayout.Foldout(showAdvancedSerialized, Tr("Advanced Raw Asset", "Zaawansowany asset raw"));
            if (showAdvancedSerialized)
            {
                DrawSerializedObject(target);
            }
        }

        private TAsset DrawObjectReferenceField<TAsset>(UnityEngine.Object owner, string propertyName, string label)
            where TAsset : UnityEngine.Object
        {
            var serialized = new SerializedObject(owner);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                return null;
            }

            EditorGUILayout.PropertyField(property, new GUIContent(label));
            serialized.ApplyModifiedProperties();
            return property.objectReferenceValue as TAsset;
        }

        private void DrawRolePreview(PresentationPrefabRole role, string label)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, role.ToString(), GUILayout.Width(260f));
                var prefab = PresentationPrefabResolver.Resolve(role);
                EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
            }
        }

        private void AddTreeNode<TNode>(EnemyBehaviorTreeDefinition tree, string prefix)
            where TNode : EnemyBehaviorTreeNodeDefinition
        {
            var node = ScriptableObject.CreateInstance<TNode>();
            node.ConfigureNodeId($"{prefix}_{tree.Nodes.Count + 1:00}");
            linkedDraft.TrackTemporary(node);
            var serialized = new SerializedObject(tree);
            var nodes = serialized.FindProperty("nodes");
            nodes.arraySize++;
            nodes.GetArrayElementAtIndex(nodes.arraySize - 1).objectReferenceValue = node;
            if (serialized.FindProperty("rootNode").objectReferenceValue == null)
            {
                serialized.FindProperty("rootNode").objectReferenceValue = node;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            selectedTreeNode = node;
        }

        private static void RemoveTreeNode(EnemyBehaviorTreeDefinition tree, EnemyBehaviorTreeNodeDefinition node)
        {
            var serialized = new SerializedObject(tree);
            var nodes = serialized.FindProperty("nodes");
            for (var index = nodes.arraySize - 1; index >= 0; index--)
            {
                if (nodes.GetArrayElementAtIndex(index).objectReferenceValue == node)
                {
                    nodes.DeleteArrayElementAtIndex(index);
                }
            }

            var root = serialized.FindProperty("rootNode");
            if (root.objectReferenceValue == node)
            {
                root.objectReferenceValue = null;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void DrawNoDraftHelp()
        {
            EditorGUILayout.HelpBox(Tr("Select an enemy or boss asset first.", "Najpierw wybierz asset wroga lub bossa."), MessageType.Info);
        }

        private static string DisplayNameFor(UnityEngine.Object asset)
        {
            return asset switch
            {
                EnemyDefinition enemy => $"{enemy.DisplayName} [{enemy.SpawnKind}]",
                BossDefinition boss => $"{boss.DisplayName} [{boss.BossId}]",
                _ => asset != null ? asset.name : "(none)"
            };
        }

        private static string Tr(string english, string polish)
        {
            return EnemyAuthoringLocalization.T(english, polish);
        }
    }
}
