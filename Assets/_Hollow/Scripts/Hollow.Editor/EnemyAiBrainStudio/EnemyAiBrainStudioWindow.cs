using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.BehaviorTreeStudio;
using Hollow.Editor.EnemyAuthoring;
using Hollow.Editor.EnemyPreviewLab;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.EnemyAiBrainStudio
{
    public sealed class EnemyAiBrainStudioWindow : EditorWindow
    {
        private readonly EnemyAuthoringDraft enemyDraft = new();
        private readonly EnemyAuthoringDraft spacingDraft = new();
        private readonly List<EnemyDefinition> enemyAssets = new();
        private readonly List<EnemyDefinition> runtimeFallbackEnemies = new();
        private readonly List<EnemyAiBrainTemplateDefinition> templateAssets = new();
        private readonly List<EnemyAiBrainTemplateDefinition> runtimeTemplates = new();

        private EnemyDefinition selectedEnemySource;
        private EnemyAiBrainTemplateDefinition selectedTemplate;
        private EnemyAiBrainStudioValidationResult lastValidation;
        private Vector2 rosterScroll;
        private Vector2 mainScroll;
        private string search = string.Empty;
        private string applyNotes = "Manual Enemy AI Brain Studio edit";
        private int selectedTab;
        private float scoreDistance = 1.6f;
        private EnemyAwarenessState scoreAwareness = EnemyAwarenessState.Engaged;
        private EnemyInstinctDisposition scoreDisposition = EnemyInstinctDisposition.Predator;
        private EnemyIntelligenceLevel scoreIntelligence = EnemyIntelligenceLevel.Basic;
        private float scoreMeleePressure = 2.2f;
        private float scoreRangedPressure = 2.4f;
        private float scoreAreaPressure = 0.8f;
        private float scoreChargePressure = 0.6f;

        [MenuItem("Hollow/Enemy Authoring/Enemy AI Brain Studio")]
        public static void Open()
        {
            GetWindow<EnemyAiBrainStudioWindow>("Enemy AI Brain Studio");
        }

        public static void OpenEnemy(EnemyDefinition enemy)
        {
            var window = GetWindow<EnemyAiBrainStudioWindow>("Enemy AI Brain Studio");
            window.RefreshAssets();
            window.SelectEnemy(enemy);
            window.Focus();
        }

        private void OnEnable()
        {
            RefreshAssets();
        }

        private void OnDisable()
        {
            enemyDraft.Dispose();
            spacingDraft.Dispose();
            DestroyRuntimeObjects();
        }

        private void OnGUI()
        {
            DrawToolbar();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawRosterSidebar();
                using (new EditorGUILayout.VerticalScope())
                {
                    mainScroll = EditorGUILayout.BeginScrollView(mainScroll);
                    DrawSelectedHeader();
                    switch (selectedTab)
                    {
                        case 0:
                            DrawOverviewTab();
                            break;
                        case 1:
                            DrawIndividualTab();
                            break;
                        case 2:
                            DrawTemplatesTab();
                            break;
                        case 3:
                            DrawScoreLabTab();
                            break;
                        case 4:
                            DrawThreatLodTab();
                            break;
                        case 5:
                            DrawLiveTraceTab();
                            break;
                        case 6:
                            DrawValidationTab();
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
                selectedTab = GUILayout.Toolbar(selectedTab, EnemyAiBrainStudioLocalization.Tabs, EditorStyles.toolbarButton, GUILayout.Width(680f));
                GUILayout.Space(8f);
                GUILayout.Label(Tr("Search", "Szukaj"), GUILayout.Width(48f));
                search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(220f));
                if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(24f)))
                {
                    search = string.Empty;
                }

                GUILayout.FlexibleSpace();
                var language = GUILayout.Toolbar(
                    (int)EnemyAiBrainStudioLocalization.CurrentLanguage,
                    new[] { "EN", "PL" },
                    EditorStyles.toolbarButton,
                    GUILayout.Width(64f));
                if (language != (int)EnemyAiBrainStudioLocalization.CurrentLanguage)
                {
                    EnemyAiBrainStudioLocalization.CurrentLanguage = (EnemyAuthoringLanguage)language;
                    Repaint();
                }

                if (GUILayout.Button(Tr("Refresh", "Odśwież"), EditorStyles.toolbarButton, GUILayout.Width(74f)))
                {
                    RefreshAssets();
                }
            }
        }

        private void DrawRosterSidebar()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(310f)))
            {
                EditorGUILayout.LabelField(Tr("Enemy Brain Roster", "Lista AI wrogów"), EditorStyles.boldLabel);
                rosterScroll = EditorGUILayout.BeginScrollView(rosterScroll, GUI.skin.box);
                foreach (var enemy in AllEnemiesForDisplay())
                {
                    if (enemy == null || !MatchesSearch(enemy))
                    {
                        continue;
                    }

                    var role = EnemyAiBrainStudioAnalysis.SuggestRole(enemy);
                    var selected = enemy == selectedEnemySource;
                    var sourceMark = IsAsset(enemy) ? string.Empty : Tr(" (runtime fallback)", " (fallback runtime)");
                    var protectedMark = EnemyAuthoringProtectionRegistryUtility.IsProtected(enemy) ? " *" : string.Empty;
                    var label = $"{enemy.DisplayName}{protectedMark}\n{enemy.SpawnKind} | {role}{sourceMark}";
                    if (GUILayout.Toggle(selected, label, "Button") && !selected)
                    {
                        SelectEnemy(enemy);
                    }
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.HelpBox(Tr(
                    "* means protected from generator overwrite. Runtime fallback rows can be inspected but not applied to source assets.",
                    "* oznacza ochronę przed nadpisaniem generatora. Wiersze fallback runtime można oglądać, ale nie zapisywać do assetów."),
                    MessageType.None);
            }
        }

        private void DrawSelectedHeader()
        {
            if (SelectedDraft == null)
            {
                EditorGUILayout.HelpBox(Tr("Select an enemy from the roster.", "Wybierz wroga z listy."), MessageType.Info);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var enemy = SelectedDraft;
                EditorGUILayout.LabelField($"{enemy.DisplayName} [{enemy.SpawnKind}]", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(Tr("Role", "Rola"), EnemyAiBrainStudioAnalysis.SuggestRole(enemy).ToString(), GUILayout.Width(240f));
                    EditorGUILayout.LabelField(Tr("Intelligence", "Inteligencja"), enemy.Intelligence.ToString(), GUILayout.Width(220f));
                    EditorGUILayout.LabelField(Tr("Disposition", "Dyspozycja"), enemy.Disposition.ToString(), GUILayout.Width(220f));
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(Tr("Draft", "Szkic"), enemyDraft.IsDirty ? Tr("Dirty", "Zmieniony") : Tr("Clean", "Czysty"), GUILayout.Width(180f));
                    EditorGUILayout.LabelField(Tr("Spacing Draft", "Szkic dystansu"), spacingDraft.Draft == null ? Tr("None", "Brak") : spacingDraft.IsDirty ? Tr("Dirty", "Zmieniony") : Tr("Clean", "Czysty"), GUILayout.Width(220f));
                    if (GUILayout.Button(Tr("Open Enemy Studio", "Otwórz Enemy Studio"), GUILayout.Width(150f)))
                    {
                        EnemyStudioWindow.Open();
                    }

                    using (new EditorGUI.DisabledScope(enemy.BehaviorTree == null))
                    {
                        if (GUILayout.Button(Tr("Open Behaviour Tree", "Otwórz drzewo AI"), GUILayout.Width(170f)))
                        {
                            BehaviourTreeStudioWindow.OpenTree(enemy.BehaviorTree);
                        }
                    }

                    if (GUILayout.Button(Tr("Open Preview Lab", "Otwórz Preview Lab"), GUILayout.Width(150f)))
                    {
                        EnemyPreviewLabSceneBuilder.OpenWithEnemy(selectedEnemySource ?? enemy);
                    }

                    if (GUILayout.Button(Tr("Ping Source", "Pokaż źródło"), GUILayout.Width(110f)) && selectedEnemySource != null)
                    {
                        EditorGUIUtility.PingObject(selectedEnemySource);
                    }
                }
            }
        }

        private void DrawOverviewTab()
        {
            EditorGUILayout.LabelField(Tr("Global Brain Overview", "Globalny przegląd AI"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Tr(
                "This studio treats the enemy brain as a role contract: senses, awareness, scoring, spacing, commitment, pressure, and debug evidence. Runtime behavior still comes from existing behavior trees/action profiles.",
                "To studio traktuje AI wroga jako kontrakt roli: zmysły, świadomość, scoring, dystans, commitment, presję i diagnostykę. Runtime nadal korzysta z istniejących drzew zachowań/profili akcji."),
                MessageType.Info);

            var enemies = AllEnemiesForDisplay().Where(enemy => enemy != null).ToArray();
            DrawRoleCounts(enemies);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Tr("Roster Matrix", "Macierz wrogów"), EditorStyles.boldLabel);
            DrawMatrixHeader();
            foreach (var enemy in enemies.Where(MatchesSearch))
            {
                DrawMatrixRow(enemy);
            }
        }

        private void DrawIndividualTab()
        {
            EditorGUILayout.LabelField(Tr("Individual Enemy Brain", "Indywidualny mózg wroga"), EditorStyles.boldLabel);
            if (SelectedDraft == null)
            {
                DrawNoSelection();
                return;
            }

            using (new EditorGUI.DisabledScope(!IsAsset(selectedEnemySource)))
            {
                DrawSerializedFields(enemyDraft.Draft, new[]
                {
                    "displayName", "spawnKind", "behaviorId", "movementMode", "maxHealth",
                    "speedMetersPerSecond", "radiusMeters", "bodyClass", "intelligence", "disposition"
                }, Tr("Identity", "Tożsamość"));

                DrawSerializedFields(enemyDraft.Draft, new[]
                {
                    "sightRadiusMeters", "sightAngleDegrees", "hearingRadiusMeters",
                    "hearingSensitivityMultiplier", "disturbanceEscalationThreshold", "investigationDurationSeconds",
                    "allyAlertSharingEnabled", "allyAlertRadiusMeters", "allyAlertCooldownSeconds", "allyAlertMinimumAwareness"
                }, Tr("Senses, Disturbance, Alert Sharing", "Zmysły, zakłócenia, alarmowanie"));

                DrawSerializedFields(enemyDraft.Draft, new[]
                {
                    "attackWindupScale", "attackActiveScale", "attackRecoveryScale",
                    "hitArcDegreesBonus", "poiseBreakThresholdOffset", "behaviorTree", "spacingProfile"
                }, Tr("Commitment + Links", "Commitment + linki"));
            }

            EditorGUILayout.Space();
            DrawSpacingDraftPanel();
            DrawResolvedActionSummary(SelectedDraft);
        }

        private void DrawTemplatesTab()
        {
            EditorGUILayout.LabelField(Tr("Brain Templates", "Szablony mózgu"), EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Tr("Generate / Refresh Template Assets", "Generuj / odśwież assety szablonów")))
                {
                    EnemyAiBrainStudioTemplateGenerator.GenerateAssets();
                    RefreshAssets();
                }

                if (GUILayout.Button(Tr("Reload Templates", "Przeładuj szablony")))
                {
                    RefreshTemplates();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(310f)))
                {
                    EditorGUILayout.LabelField(Tr("Template Library", "Biblioteka szablonów"), EditorStyles.boldLabel);
                    foreach (var template in templateAssets.Concat(runtimeTemplates).Where(template => template != null))
                    {
                        var selected = template == selectedTemplate;
                        var fit = SelectedDraft != null && TemplateFits(template, SelectedDraft) ? " ✓" : string.Empty;
                        if (GUILayout.Toggle(selected, $"{template.DisplayName}{fit}\n{template.Role}", "Button") && !selected)
                        {
                            selectedTemplate = template;
                        }
                    }
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    if (selectedTemplate == null)
                    {
                        EditorGUILayout.HelpBox(Tr("Select a template to preview and apply it to the current draft.", "Wybierz szablon, aby podejrzeć i zastosować go do aktualnego szkicu."), MessageType.Info);
                        return;
                    }

                    DrawTemplateDetails(selectedTemplate);
                    using (new EditorGUI.DisabledScope(SelectedDraft == null || !IsAsset(selectedEnemySource)))
                    {
                        if (GUILayout.Button(Tr("Apply Template To Enemy Draft", "Zastosuj szablon do szkicu wroga")))
                        {
                            EnemyAiBrainStudioAnalysis.ApplyTemplateToEnemyDraft((EnemyDefinition)enemyDraft.Draft, selectedTemplate);
                            GUI.FocusControl(null);
                        }

                        using (new EditorGUI.DisabledScope(spacingDraft.Draft is not EnemySpacingProfileDefinition))
                        {
                            if (GUILayout.Button(Tr("Apply Template To Spacing Draft", "Zastosuj szablon do szkicu dystansu")))
                            {
                                EnemyAiBrainStudioAnalysis.ApplyTemplateToSpacingDraft((EnemySpacingProfileDefinition)spacingDraft.Draft, selectedTemplate);
                                GUI.FocusControl(null);
                            }
                        }
                    }
                }
            }
        }

        private void DrawScoreLabTab()
        {
            EditorGUILayout.LabelField(Tr("Deterministic Action Score Lab", "Laboratorium scoringu akcji"), EditorStyles.boldLabel);
            if (SelectedDraft == null)
            {
                DrawNoSelection();
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                scoreDistance = EditorGUILayout.Slider(Tr("Distance To Player", "Dystans do gracza"), scoreDistance, 0f, 10f);
                scoreAwareness = (EnemyAwarenessState)EditorGUILayout.EnumPopup(Tr("Awareness", "Świadomość"), scoreAwareness);
                scoreDisposition = (EnemyInstinctDisposition)EditorGUILayout.EnumPopup(Tr("Disposition", "Dyspozycja"), scoreDisposition);
                scoreIntelligence = (EnemyIntelligenceLevel)EditorGUILayout.EnumPopup(Tr("Intelligence", "Inteligencja"), scoreIntelligence);
                scoreMeleePressure = EditorGUILayout.Slider(Tr("Melee Pressure", "Presja melee"), scoreMeleePressure, 0f, 6f);
                scoreRangedPressure = EditorGUILayout.Slider(Tr("Ranged Pressure", "Presja ranged"), scoreRangedPressure, 0f, 6f);
                scoreAreaPressure = EditorGUILayout.Slider(Tr("Area Pressure", "Presja obszarowa"), scoreAreaPressure, 0f, 4f);
                scoreChargePressure = EditorGUILayout.Slider(Tr("Charge Pressure", "Presja szarży"), scoreChargePressure, 0f, 4f);
            }

            var previews = EnemyAiBrainStudioAnalysis.BuildActionPreview(
                SelectedDraft,
                scoreDistance,
                scoreAwareness,
                scoreDisposition,
                scoreIntelligence,
                scoreMeleePressure,
                scoreRangedPressure,
                scoreAreaPressure,
                scoreChargePressure);

            EditorGUILayout.LabelField(Tr("Top Runtime Actions", "Najlepsze akcje runtime"), EditorStyles.boldLabel);
            if (previews.Count == 0)
            {
                EditorGUILayout.HelpBox(Tr("No current runtime actions score for this context.", "Brak akcji runtime dla tego kontekstu."), MessageType.Warning);
                return;
            }

            DrawScoreHeader();
            foreach (var preview in previews.Take(12))
            {
                DrawScoreRow(preview);
            }
        }

        private void DrawThreatLodTab()
        {
            EditorGUILayout.LabelField(Tr("Threat Director + AI LOD", "Threat Director + AI LOD"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Tr(
                "AAA-facing rule: large swarms should look alive, but the room should still read like a duel with interruptions. Pressure caps reduce scores; they do not hard-disable brains.",
                "Reguła AAA: duże grupy mają wyglądać żywo, ale pokój nadal ma być czytelny jak pojedynek z przerwami. Limity presji obniżają scoring; nie wyłączają mózgów na sztywno."),
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawThreatLaneBox("Melee", "2.6", Tr("slashes, bites, lunges", "cięcia, ugryzienia, lunges"));
                DrawThreatLaneBox("Ranged", "3.2", Tr("arrows, shots, spells", "strzały, pociski, zaklęcia"));
                DrawThreatLaneBox("Area", "1.6", Tr("stomps, bursts, hazards", "stompy, wybuchy, hazardy"));
                DrawThreatLaneBox("Charge", "1.3", Tr("charges, dashes", "szarże, dashe"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Tr("LOD Readability Rules", "Reguły czytelności LOD"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Full: idle/attacking/endangered/within 5.5m. Reduced: alerted or within 10m. Background: far and low-threat; faces, holds, wanders, or reuses plans.", MessageType.None);
            DrawRoleCounts(AllEnemiesForDisplay().Where(enemy => enemy != null).ToArray());
        }

        private void DrawLiveTraceTab()
        {
            EditorGUILayout.LabelField(Tr("Play Mode Live Trace", "Live trace w Play Mode"), EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(Tr(
                    "Enter Play Mode and select an enemy instance to see its blackboard, path status, chosen action, pressure penalty, and top scorer rows.",
                    "Wejdź w Play Mode i zaznacz instancję wroga, aby zobaczyć blackboard, ścieżkę, wybraną akcję, karę presji i najlepsze wyniki scorera."),
                    MessageType.Info);
                return;
            }

            var overlayEnabled = EditorGUILayout.Toggle(Tr("Enable Runtime Blackboard Overlay", "Włącz overlay blackboard runtime"), EnemyAiDebugOverlay.BlackboardEnabled);
            EnemyAiDebugOverlay.SetBlackboardEnabled(overlayEnabled);
            EditorGUILayout.LabelField(Tr("Global Diagnostics", "Globalna diagnostyka"), EnemyAiDebugOverlay.DiagnosticsSummary);

            var runtime = ResolveSelectedRuntimeEnemy();
            if (runtime == null)
            {
                EditorGUILayout.HelpBox(Tr("No active matching runtime enemy found.", "Nie znaleziono aktywnej pasującej instancji wroga."), MessageType.Warning);
                return;
            }

            DrawRuntimeTrace(runtime);
        }

        private void DrawValidationTab()
        {
            EditorGUILayout.LabelField(Tr("Validation + Apply", "Walidacja + zapis"), EditorStyles.boldLabel);
            applyNotes = EditorGUILayout.TextField(Tr("Apply Notes", "Notatka zapisu"), applyNotes);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Tr("Validate Brain Draft", "Sprawdź szkic mózgu")))
                {
                    lastValidation = EnemyAiBrainStudioAnalysis.ValidateEnemy(SelectedDraft);
                }

                using (new EditorGUI.DisabledScope(selectedEnemySource == null || !IsAsset(selectedEnemySource) || enemyDraft.Draft == null || !enemyDraft.IsDirty))
                {
                    if (GUILayout.Button(Tr("Apply Enemy Draft", "Zapisz szkic wroga")))
                    {
                        var validation = EnemyAiBrainStudioAnalysis.ValidateEnemy((EnemyDefinition)enemyDraft.Draft);
                        if (validation.IsValid)
                        {
                            enemyDraft.Apply(applyNotes);
                            RefreshAssets();
                        }

                        lastValidation = validation;
                    }
                }

                using (new EditorGUI.DisabledScope(spacingDraft.Draft == null || !spacingDraft.IsDirty))
                {
                    if (GUILayout.Button(Tr("Apply Spacing Draft", "Zapisz szkic dystansu")))
                    {
                        spacingDraft.Apply(applyNotes);
                    }
                }
            }

            DrawValidationResult(lastValidation ?? EnemyAiBrainStudioAnalysis.ValidateEnemy(SelectedDraft));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Tr("Diff Summary", "Podsumowanie diffu"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(enemyDraft.BuildDiffSummary());
            if (spacingDraft.Source != null)
            {
                EditorGUILayout.LabelField(spacingDraft.BuildDiffSummary());
            }
        }

        private void DrawSpacingDraftPanel()
        {
            EditorGUILayout.LabelField(Tr("M91 Spacing Brain", "M91 mózg dystansu"), EditorStyles.boldLabel);
            if (spacingDraft.Draft is not EnemySpacingProfileDefinition spacing)
            {
                EditorGUILayout.HelpBox(Tr(
                    "This enemy is using a resolved fallback spacing profile. Add/assign a spacing asset in Enemy Studio for persistent spacing edits.",
                    "Ten wróg używa fallbackowego profilu dystansu. Dodaj/przypisz asset dystansu w Enemy Studio, aby zapisać edycje."),
                    MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(!IsAsset(spacingDraft.Source)))
            {
                DrawSerializedFields(spacing, new[]
                {
                    "displayName", "defaultIdealDistanceMeters", "defaultCloseToleranceMeters",
                    "defaultLongToleranceMeters", "closePressureBias", "retreatBurstSeconds",
                    "retreatReassessSeconds", "maxResetCountBeforeCommit",
                    "fallbackRecoveryMovementMode", "fallbackRecoveryDistanceMeters",
                    "fallbackRecoverySpeedMultiplier", "actionOverrides"
                }, Tr("Spacing Profile", "Profil dystansu"));
            }
        }

        private void DrawResolvedActionSummary(EnemyDefinition enemy)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Tr("Resolved Action Contract", "Rozwiązany kontrakt akcji"), EditorStyles.boldLabel);
            foreach (var action in enemy.ActionProfiles.Where(action => action != null && action.UsageState == EnemyActionUsageState.CurrentRuntime).Take(10))
            {
                var attack = enemy.AttackProfiles.FirstOrDefault(profile => profile != null && profile.AttackId == action.LinkedAttackId);
                var command = EnemyActionScorer.CommandKindFor(action, attack);
                var lane = RoomThreatDirector.ResolveLane(action, attack);
                EditorGUILayout.LabelField(
                    action.DisplayName,
                    $"{action.Intent} | {command} | {lane} | range {action.MinRangeMeters:0.0}-{Mathf.Max(action.MaxRangeMeters, attack != null ? attack.RangeMeters : 0f):0.0}m");
            }
        }

        private void DrawTemplateDetails(EnemyAiBrainTemplateDefinition template)
        {
            EditorGUILayout.LabelField(template.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Tr("Role", "Rola"), template.Role.ToString());
            EditorGUILayout.HelpBox(template.Description, MessageType.None);
            EditorGUILayout.LabelField(Tr("Target", "Cel"), $"{template.TargetIntelligence} / {template.TargetDisposition}");
            EditorGUILayout.LabelField(Tr("Senses", "Zmysły"), $"sight x{template.SightRadiusMultiplier:0.00} @ {(template.SightAngleDegrees < 0f ? "keep" : template.SightAngleDegrees.ToString("0"))}deg, hearing x{template.HearingRadiusMultiplier:0.00}, sensitivity x{template.HearingSensitivityMultiplier:0.00}");
            EditorGUILayout.LabelField(Tr("Commitment", "Commitment"), $"windup x{template.AttackWindupScale:0.00}, active x{template.AttackActiveScale:0.00}, recovery x{template.AttackRecoveryScale:0.00}, poise {template.PoiseBreakThresholdOffset:+#;-#;0}");
            EditorGUILayout.LabelField(Tr("Spacing", "Dystans"), $"ideal x{template.IdealDistanceMultiplier:0.00}, tolerance {template.CloseToleranceMeters:0.00}/{template.LongToleranceMeters:0.00}, resets {template.MaxResetCountBeforeCommit}");
            EditorGUILayout.LabelField(Tr("Recommended Behaviors", "Rekomendowane zachowania"), string.Join(", ", template.RecommendedBehaviors));
            EditorGUILayout.LabelField(Tr("Recommended Dispositions", "Rekomendowane dyspozycje"), string.Join(", ", template.RecommendedDispositions));
            if (!string.IsNullOrWhiteSpace(template.DesignerNotes))
            {
                EditorGUILayout.HelpBox(template.DesignerNotes, MessageType.Info);
            }
        }

        private void DrawValidationResult(EnemyAiBrainStudioValidationResult result)
        {
            if (result == null)
            {
                EditorGUILayout.HelpBox(Tr("No validation result yet.", "Brak wyniku walidacji."), MessageType.Info);
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

            foreach (var note in result.Notes)
            {
                EditorGUILayout.HelpBox(note, MessageType.None);
            }

            if (result.IsValid && result.Warnings.Count == 0)
            {
                EditorGUILayout.HelpBox(Tr("Brain contract looks clean.", "Kontrakt AI wygląda poprawnie."), MessageType.Info);
            }
        }

        private void DrawRuntimeTrace(EnemyRuntimeController runtime)
        {
            var blackboard = runtime.AiBlackboard;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.ObjectField(Tr("Runtime Enemy", "Instancja wroga"), runtime, typeof(EnemyRuntimeController), true);
                EditorGUILayout.LabelField(Tr("Definition", "Definicja"), runtime.Definition != null ? $"{runtime.Definition.DisplayName} [{runtime.Definition.SpawnKind}]" : "none");
                EditorGUILayout.LabelField(Tr("Blackboard", "Blackboard"), blackboard.Summary);
                EditorGUILayout.LabelField(Tr("LOD", "LOD"), blackboard.LodTier.ToString());
                EditorGUILayout.LabelField(Tr("Tree Command", "Komenda drzewa"), blackboard.TreeCommand.ToString());
                EditorGUILayout.LabelField(Tr("Chosen", "Wybrana"), $"{blackboard.ChosenCommand}:{blackboard.ChosenActionId}");
                EditorGUILayout.LabelField(Tr("Top Scores", "Top score"), blackboard.TopScores);
                EditorGUILayout.LabelField(Tr("Path", "Ścieżka"), $"{runtime.LastNavigationBackend} / {runtime.LastNavigationPathStatus} / {runtime.LastNavigationFallbackReason}");
                EditorGUILayout.LabelField(Tr("Waypoint", "Waypoint"), $"{runtime.LastNavigationNextWaypoint} ({runtime.LastNavigationWaypointCount})");
            }
        }

        private void DrawRoleCounts(IReadOnlyList<EnemyDefinition> enemies)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (EnemyAiBrainTemplateRole role in Enum.GetValues(typeof(EnemyAiBrainTemplateRole)))
                {
                    var count = enemies.Count(enemy => EnemyAiBrainStudioAnalysis.SuggestRole(enemy) == role);
                    if (count <= 0)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(150f)))
                    {
                        EditorGUILayout.LabelField(role.ToString(), EditorStyles.miniBoldLabel);
                        EditorGUILayout.LabelField(count.ToString());
                    }
                }
            }
        }

        private static void DrawMatrixHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Enemy", EditorStyles.boldLabel, GUILayout.Width(190f));
                EditorGUILayout.LabelField("Role", EditorStyles.boldLabel, GUILayout.Width(145f));
                EditorGUILayout.LabelField("Brain", EditorStyles.boldLabel, GUILayout.Width(170f));
                EditorGUILayout.LabelField("Senses", EditorStyles.boldLabel, GUILayout.Width(160f));
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel, GUILayout.Width(170f));
                EditorGUILayout.LabelField("Warnings", EditorStyles.boldLabel);
            }
        }

        private void DrawMatrixRow(EnemyDefinition enemy)
        {
            var result = EnemyAiBrainStudioAnalysis.ValidateEnemy(enemy);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{enemy.DisplayName}\n{enemy.SpawnKind}", GUILayout.Width(190f));
                EditorGUILayout.LabelField(EnemyAiBrainStudioAnalysis.SuggestRole(enemy).ToString(), GUILayout.Width(145f));
                EditorGUILayout.LabelField($"{enemy.Intelligence}/{enemy.Disposition}", GUILayout.Width(170f));
                EditorGUILayout.LabelField($"{enemy.SightRadiusMeters:0.0}m/{enemy.SightAngleDegrees:0}deg | H {enemy.HearingRadiusMeters:0.0}m", GUILayout.Width(160f));
                EditorGUILayout.LabelField($"{enemy.ActionProfiles.Count} actions | {enemy.AttackProfiles.Count} attacks", GUILayout.Width(170f));
                EditorGUILayout.LabelField($"{result.Errors.Count} errors, {result.Warnings.Count} warnings");
            }
        }

        private static void DrawScoreHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Action", EditorStyles.boldLabel, GUILayout.Width(210f));
                EditorGUILayout.LabelField("Command", EditorStyles.boldLabel, GUILayout.Width(160f));
                EditorGUILayout.LabelField("Score", EditorStyles.boldLabel, GUILayout.Width(70f));
                EditorGUILayout.LabelField("Reason", EditorStyles.boldLabel);
            }
        }

        private static void DrawScoreRow(EnemyAiBrainActionPreview preview)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(preview.ActionId, GUILayout.Width(210f));
                EditorGUILayout.LabelField(preview.CommandKind.ToString(), GUILayout.Width(160f));
                EditorGUILayout.LabelField(preview.Score.ToString("0.00"), GUILayout.Width(70f));
                EditorGUILayout.LabelField(preview.Reason);
            }
        }

        private static void DrawThreatLaneBox(string lane, string cap, string examples)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(150f)))
            {
                EditorGUILayout.LabelField(lane, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"soft cap {cap}");
                EditorGUILayout.LabelField(examples, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void SelectEnemy(EnemyDefinition enemy)
        {
            selectedEnemySource = enemy;
            lastValidation = null;
            enemyDraft.Load(IsAsset(enemy) ? enemy : null);
            if (enemyDraft.Draft == null && enemy != null)
            {
                enemyDraft.Load(enemy);
            }

            scoreDistance = enemy != null ? Mathf.Clamp(enemy.PreferredRangeMinMeters + 0.35f, 0.25f, 8f) : 1.6f;
            scoreAwareness = EnemyAwarenessState.Engaged;
            scoreDisposition = enemy != null ? enemy.Disposition : EnemyInstinctDisposition.Predator;
            scoreIntelligence = enemy != null ? enemy.Intelligence : EnemyIntelligenceLevel.Basic;
            LoadSpacingDraft(enemy);
        }

        private void LoadSpacingDraft(EnemyDefinition enemy)
        {
            spacingDraft.Dispose();
            var authoredSpacing = AuthoredSpacingProfileFor(enemy);
            if (authoredSpacing != null && IsAsset(authoredSpacing))
            {
                spacingDraft.Load(authoredSpacing);
            }
        }

        private void RefreshAssets()
        {
            RefreshEnemies();
            RefreshTemplates();
            if (selectedEnemySource == null)
            {
                SelectEnemy(AllEnemiesForDisplay().FirstOrDefault());
            }
            else
            {
                var replacement = AllEnemiesForDisplay().FirstOrDefault(enemy => enemy != null && enemy.SpawnKind == selectedEnemySource.SpawnKind);
                if (replacement != null && replacement != selectedEnemySource)
                {
                    SelectEnemy(replacement);
                }
            }
        }

        private void RefreshEnemies()
        {
            enemyAssets.Clear();
            runtimeFallbackEnemies.Clear();
            enemyAssets.AddRange(FindAssets<EnemyDefinition>("t:EnemyDefinition", "Assets/_Hollow/Data/Enemies"));
            enemyAssets.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal));
            if (enemyAssets.Count == 0)
            {
                runtimeFallbackEnemies.AddRange(EnemyCatalog.CreateRuntimeDefault().Definitions.Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss"));
            }
        }

        private void RefreshTemplates()
        {
            DestroyRuntimeTemplates();
            templateAssets.Clear();
            templateAssets.AddRange(FindAssets<EnemyAiBrainTemplateDefinition>("t:EnemyAiBrainTemplateDefinition", EnemyAiBrainStudioTemplateGenerator.TemplateFolder));
            templateAssets.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal));
            if (templateAssets.Count == 0)
            {
                runtimeTemplates.AddRange(EnemyAiBrainStudioTemplateGenerator.CreateRuntimeTemplates());
            }

            selectedTemplate = templateAssets.Concat(runtimeTemplates).FirstOrDefault(template => template != null && selectedTemplate != null && template.TemplateId == selectedTemplate.TemplateId)
                ?? templateAssets.Concat(runtimeTemplates).FirstOrDefault();
        }

        private IReadOnlyList<EnemyDefinition> AllEnemiesForDisplay()
        {
            return enemyAssets.Count > 0 ? enemyAssets : runtimeFallbackEnemies;
        }

        private EnemyDefinition SelectedDraft => enemyDraft.Draft as EnemyDefinition ?? selectedEnemySource;

        private EnemyRuntimeController ResolveSelectedRuntimeEnemy()
        {
            if (Selection.activeGameObject != null &&
                Selection.activeGameObject.TryGetComponent<EnemyRuntimeController>(out var selectedRuntime))
            {
                return selectedRuntime;
            }

            var spawnKind = SelectedDraft != null ? SelectedDraft.SpawnKind : string.Empty;
            return FindObjectsByType<EnemyRuntimeController>(FindObjectsInactive.Exclude)
                .Where(enemy => enemy != null && enemy.IsAlive && enemy.BossDefinition == null)
                .OrderBy(enemy => enemy.Definition != null && enemy.Definition.SpawnKind == spawnKind ? 0 : 1)
                .ThenBy(enemy => enemy.DistanceToPlayerMeters)
                .FirstOrDefault();
        }

        private static EnemySpacingProfileDefinition AuthoredSpacingProfileFor(EnemyDefinition enemy)
        {
            if (enemy == null || !IsAsset(enemy))
            {
                return null;
            }

            var serialized = new SerializedObject(enemy);
            return serialized.FindProperty("spacingProfile")?.objectReferenceValue as EnemySpacingProfileDefinition;
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

        private bool MatchesSearch(EnemyDefinition enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            var needle = search.Trim();
            return enemy.DisplayName.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   enemy.SpawnKind.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   enemy.BehaviorId.ToString().IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   enemy.Disposition.ToString().IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   EnemyAiBrainStudioAnalysis.SuggestRole(enemy).ToString().IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TemplateFits(EnemyAiBrainTemplateDefinition template, EnemyDefinition enemy)
        {
            if (template == null || enemy == null)
            {
                return false;
            }

            return template.Role == EnemyAiBrainStudioAnalysis.SuggestRole(enemy) ||
                   template.RecommendedBehaviors.Contains(enemy.BehaviorId) ||
                   template.RecommendedDispositions.Contains(enemy.Disposition);
        }

        private static void DrawSerializedFields(UnityEngine.Object target, IEnumerable<string> fields, string title)
        {
            if (target == null)
            {
                return;
            }

            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
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

        private void DestroyRuntimeObjects()
        {
            DestroyRuntimeTemplates();
            runtimeFallbackEnemies.Clear();
        }

        private void DestroyRuntimeTemplates()
        {
            foreach (var template in runtimeTemplates)
            {
                if (template != null)
                {
                    DestroyImmediate(template);
                }
            }

            runtimeTemplates.Clear();
        }

        private static bool IsAsset(UnityEngine.Object target)
        {
            return target != null && !string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(target));
        }

        private void DrawNoSelection()
        {
            EditorGUILayout.HelpBox(Tr("Select an enemy first.", "Najpierw wybierz wroga."), MessageType.Info);
        }

        private static string Tr(string english, string polish)
        {
            return EnemyAiBrainStudioLocalization.T(english, polish);
        }
    }
}
