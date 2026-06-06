using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hollow.Editor.AnimationRefiner
{
    public enum PlayerAnimationRefinerMode
    {
        EquipmentRefiner = 0,
        PositionRefiner = 1
    }

    public sealed class PlayerAnimationRefinerWindow : EditorWindow
    {
        public const string MenuPath = "Hollow/Animation/Player Animation Refiner";
        public const string PreviewScenePath = "Assets/_Hollow/Scenes/DeveloperLab/PlayerAnimationRefiner.unity";
        public const string ExportPath = "Assets/_Hollow/Data/AnimationProfiles/PlayerAnimationRefinerExport.json";
        public const string PositionExportPath = "Assets/_Hollow/Data/AnimationProfiles/PlayerPositionRefinerExport.json";
        public const string PreviewControllerPath = "Assets/_Hollow/Scenes/DeveloperLab/PlayerAnimationRefiner.controller";
        public const string PreviewPlaceholderClipPath = "Assets/_Hollow/Scenes/DeveloperLab/PlayerAnimationRefinerPlaceholder.anim";

        private const string PreviewRootName = "PlayerAnimationRefiner.PlayerPreview";
        private const string FloorName = "PlayerAnimationRefiner.FlatFloor";
        private const string CameraName = "PlayerAnimationRefiner.Camera";
        private const string LightName = "PlayerAnimationRefiner.DirectionalLight";
        private const string VisualRootName = "MainCharacter_VisualRoot";
        private const string PreviewStateName = "PlayerAnimationRefinerPreview";
        private const string PreviewPlaceholderClipName = "PlayerAnimationRefinerPlaceholder";
        private const string ScenarioSocketTargetKind = "Scenario Socket";
        private const float SuspiciousHolsteredVisualOffsetMeters = 1.25f;
        private const int SwordArcSegmentCount = 18;

        private static readonly PlayerAnimationProfileId[] ProfileOrder =
        {
            PlayerAnimationProfileId.UnarmedLocomotion,
            PlayerAnimationProfileId.SwordShieldCombat,
            PlayerAnimationProfileId.GreatSwordCombat,
            PlayerAnimationProfileId.RifleCombat,
            PlayerAnimationProfileId.PistolCombat
        };

        private static readonly EquipmentSocketTuning[] EquipmentSlots =
        {
            new("Melee In Hand", PlayerHeldWeaponVisualController.MeleeHandSocketName, PlayerHeldWeaponVisualController.ActiveMeleeWeaponVisualName),
            new(
                "Melee Holstered",
                PlayerHeldWeaponVisualController.MeleeHolsterSocketName,
                PlayerHeldWeaponVisualController.HolsteredMeleeWeaponVisualName,
                EquipmentEditTargetMode.ScenarioSocket),
            new("Ranged In Hand", PlayerHeldWeaponVisualController.RangedHandSocketName, PlayerHeldWeaponVisualController.ActiveRangedWeaponVisualName),
            new(
                "Ranged Holstered",
                PlayerHeldWeaponVisualController.RangedHolsterSocketName,
                PlayerHeldWeaponVisualController.HolsteredRangedWeaponVisualName,
                EquipmentEditTargetMode.ScenarioSocket),
            new("Shield Forearm", PlayerHeldWeaponVisualController.ShieldForearmSocketName, PlayerHeldWeaponVisualController.EquippedShieldVisualName),
            new("Shield Back", PlayerHeldWeaponVisualController.ShieldBackSocketName, PlayerHeldWeaponVisualController.EquippedShieldVisualName),
            new("Ranged Muzzle", PlayerHeldWeaponVisualController.RangedMuzzleSocketName, null)
        };

        private PlayerAnimationProfileCatalogDefinition catalog;
        private GameObject previewRoot;
        private Animator animator;
        private PlayerWeaponController weaponController;
        private PlayerAnimationProfileController profileController;
        private PlayerHeldWeaponVisualController heldWeaponVisual;
        private PlayerAnimationRefinerMode refinerMode = PlayerAnimationRefinerMode.EquipmentRefiner;
        private Vector2 scroll;
        private int selectedProfileIndex = 1;
        private int selectedClipIndex;
        private PlayerAnimationProfileId? lastAppliedPreviewProfileId;
        private string previewLoadoutDescription = "Preview loadout: --";
        private float clipTimeSeconds;
        private bool playing;
        private bool loop = true;
        private bool runtimeClipPaused;
        private bool disablePreviewGameplayControls = true;
        private double lastEditorTime;
        private float cameraYawDegrees = 25f;
        private float cameraPitchDegrees = 12f;
        private float cameraDistanceMeters = 3.4f;
        private float cameraTargetHeightMeters = 1f;
        private float positionNudgeStepMeters = 0.005f;
        private float rotationNudgeStepDegrees = 1f;
        private float scaleNudgeStep = 0.01f;
        private string samplingRootDescription = "Sampling root: --";
        private RuntimeAnimatorController runtimePreviewBaseController;
        private AnimatorOverrideController runtimePreviewOverrideController;
        private readonly Dictionary<string, RefinerTransformTuning> editedSlotTunings = new(StringComparer.Ordinal);
        private bool showBones;
        private bool showSockets = true;
        private bool showProjectileStart = true;
        private bool showSwordArc = true;
        private bool showGroundingHelpers = true;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<PlayerAnimationRefinerWindow>("Player Animation Refiner");
            window.minSize = new Vector2(440f, 560f);
            window.EnsurePreviewSceneAndInstance();
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            SceneView.duringSceneGui += DrawPositionRefinerSceneGui;
            lastEditorTime = EditorApplication.timeSinceStartup;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            SceneView.duringSceneGui -= DrawPositionRefinerSceneGui;
            if (!EditorApplication.isPlayingOrWillChangePlaymode && AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
            }
        }

        private void OnGUI()
        {
            ResolveSceneReferences();
            DrawToolbar();
            DrawModeSelector();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            try
            {
                DrawCameraControls();
                DrawProfileAndClipControls();
                if (refinerMode == PlayerAnimationRefinerMode.EquipmentRefiner)
                {
                    DrawEquipmentControls();
                }
                else
                {
                    DrawPositionRefinerControls();
                }

                DrawExportControls();
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            if (GUILayout.Button("Open Preview Scene", EditorStyles.toolbarButton))
            {
                EnsurePreviewSceneAndInstance();
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Frame Player", EditorStyles.toolbarButton))
            {
                FramePreviewPlayer();
            }

            if (GUILayout.Button("Frame Game Camera", EditorStyles.toolbarButton))
            {
                FrameGameCamera();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawModeSelector()
        {
            EditorGUILayout.Space(4f);
            var nextMode = (PlayerAnimationRefinerMode)GUILayout.Toolbar(
                (int)refinerMode,
                new[] { "Equipment Refiner", "Position Refiner" },
                GUILayout.Height(24f));
            if (nextMode != refinerMode)
            {
                refinerMode = nextMode;
                RepaintPreviewViews();
            }
        }

        private void DrawCameraControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview Camera", EditorStyles.boldLabel);
            cameraYawDegrees = EditorGUILayout.Slider("Yaw", cameraYawDegrees, -180f, 180f);
            cameraPitchDegrees = EditorGUILayout.Slider("Pitch", cameraPitchDegrees, -20f, 65f);
            cameraDistanceMeters = EditorGUILayout.Slider("Distance", cameraDistanceMeters, 1.2f, 8f);
            cameraTargetHeightMeters = EditorGUILayout.Slider("Target Height", cameraTargetHeightMeters, 0.2f, 2.2f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Front")) SetCameraPreset(0f, 10f, 3.2f);
            if (GUILayout.Button("Back")) SetCameraPreset(180f, 10f, 3.2f);
            if (GUILayout.Button("Left")) SetCameraPreset(-90f, 10f, 3.2f);
            if (GUILayout.Button("Right")) SetCameraPreset(90f, 10f, 3.2f);
            if (GUILayout.Button("3/4")) SetCameraPreset(35f, 14f, 3.4f);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Apply To Game Camera", GUILayout.Height(24f)))
            {
                FrameGameCamera();
            }

            EditorGUILayout.HelpBox(
                "Use Frame Game Camera to move the Play/Game camera onto the character. Scene view orbit still works normally with Alt/Option + mouse.",
                MessageType.None);
        }

        private void DrawProfileAndClipControls()
        {
            catalog ??= PlayerAnimationProfileAssetGenerator.LoadCatalog();
            if (catalog == null)
            {
                EditorGUILayout.HelpBox(
                    "Player animation profile catalog is missing. Run Hollow/Animation/Generate Player Animation Profiles first.",
                    MessageType.Warning);
                return;
            }

            var previousProfileIndex = selectedProfileIndex;
            var nextProfileIndex = GUILayout.Toolbar(
                Mathf.Clamp(selectedProfileIndex, 0, ProfileOrder.Length - 1),
                ProfileOrder.Select(ProfileTabLabel).ToArray());
            var profileChanged = nextProfileIndex != previousProfileIndex;
            selectedProfileIndex = nextProfileIndex;
            if (profileChanged)
            {
                selectedClipIndex = 0;
                clipTimeSeconds = 0f;
                playing = false;
            }

            var profile = ResolveSelectedProfile();
            ApplyPreviewProfile(profile, force: profileChanged);

            var clips = CollectClips(profile).ToArray();
            if (clips.Length == 0)
            {
                EditorGUILayout.HelpBox("The selected profile has no mapped clips to preview.", MessageType.Info);
                return;
            }

            selectedClipIndex = Mathf.Clamp(selectedClipIndex, 0, clips.Length - 1);
            var clipNames = clips.Select(clip => clip != null ? clip.name : "<missing>").ToArray();
            EditorGUI.BeginChangeCheck();
            selectedClipIndex = EditorGUILayout.Popup("Clip", selectedClipIndex, clipNames);
            var clipChanged = EditorGUI.EndChangeCheck();
            if (clipChanged)
            {
                clipTimeSeconds = 0f;
                playing = false;
                SampleSelectedClip();
            }

            var selectedClip = clips[selectedClipIndex];
            var clipLength = Mathf.Max(0.001f, selectedClip != null ? selectedClip.length : 1f);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox(
                    "Play Mode uses a dedicated refiner preview controller. The clip selector drives that controller, while gameplay movement/aim/attack input is disabled on the preview player.",
                    MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            if (EditorApplication.isPlaying)
            {
                if (GUILayout.Button(runtimeClipPaused ? "Resume" : "Pause", GUILayout.Width(90f)))
                {
                    runtimeClipPaused = !runtimeClipPaused;
                    PreviewSelectedClipInPlayMode(selectedClip);
                }

                if (GUILayout.Button("Restart", GUILayout.Width(90f)))
                {
                    clipTimeSeconds = 0f;
                    runtimeClipPaused = false;
                    PreviewSelectedClipInPlayMode(selectedClip);
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
                if (GUILayout.Button(playing ? "Pause" : "Play", GUILayout.Width(90f)))
                {
                    playing = !playing;
                    lastEditorTime = EditorApplication.timeSinceStartup;
                }

                if (GUILayout.Button("Restart", GUILayout.Width(90f)))
                {
                    clipTimeSeconds = 0f;
                    SampleSelectedClip();
                }
                EditorGUI.EndDisabledGroup();
            }

            loop = EditorGUILayout.ToggleLeft("Loop", loop, GUILayout.Width(80f));
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            clipTimeSeconds = EditorGUILayout.Slider("Time", clipTimeSeconds, 0f, clipLength);
            if (EditorGUI.EndChangeCheck())
            {
                playing = false;
                SampleSelectedClip();
            }

            EditorGUILayout.LabelField("Mode", MainCharacterAnimationIntegrator.DefaultAnimationSystemMode.ToString());
            EditorGUILayout.LabelField(previewLoadoutDescription);
            EditorGUILayout.LabelField(samplingRootDescription);
            EditorGUILayout.LabelField("Preview", PreviewScenePath);
            EditorGUI.BeginChangeCheck();
            disablePreviewGameplayControls = EditorGUILayout.ToggleLeft(
                "Disable gameplay controls/input in this preview scene",
                disablePreviewGameplayControls);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyPreviewRuntimeIsolation();
            }

            EditorGUILayout.HelpBox(
                "Orbit with the Scene view camera. Use Frame Game Camera to move the Game view camera. Socket changes are live on this preview instance only until you export the JSON tuning report.",
                MessageType.None);

            if (profileChanged)
            {
                SampleSelectedClip();
            }
        }

        private void DrawEquipmentControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Equipment Socket Refinement", EditorStyles.boldLabel);
            if (previewRoot == null)
            {
                EditorGUILayout.HelpBox("Open the preview scene to edit sockets.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Nudge Steps", EditorStyles.boldLabel);
            positionNudgeStepMeters = PositiveFloatField("Position", positionNudgeStepMeters);
            rotationNudgeStepDegrees = PositiveFloatField("Rotation", rotationNudgeStepDegrees);
            scaleNudgeStep = PositiveFloatField("Scale", scaleNudgeStep);
            EditorGUILayout.HelpBox(
                "Type exact XYZ values, or use the +/- buttons below each vector. The step values above control how much each button changes.",
                MessageType.None);
            EditorGUILayout.EndVertical();

            foreach (var slot in EquipmentSlots)
            {
                var socket = FindDescendant(previewRoot.transform, slot.SocketName);
                var transform = ResolveEditableTransform(slot, socket, out var targetKind, out var targetNote);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(slot.Label, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Socket", socket != null ? TransformPath(previewRoot.transform, socket) : $"Missing: {slot.SocketName}");
                EditorGUILayout.LabelField("Edit Target", transform != null ? TransformPath(previewRoot.transform, transform) : "Missing visible target");
                EditorGUILayout.LabelField("Target Type", targetKind);
                if (!string.IsNullOrEmpty(targetNote))
                {
                    EditorGUILayout.HelpBox(targetNote, MessageType.Info);
                }

                if (transform != null)
                {
                    EditorGUI.BeginChangeCheck();
                    var nextPosition = DrawVector3WithNudges("Local Position", transform.localPosition, positionNudgeStepMeters);
                    var nextEuler = DrawVector3WithNudges("Local Rotation", transform.localEulerAngles, rotationNudgeStepDegrees);
                    var nextScale = DrawVector3WithNudges("Local Scale", transform.localScale, scaleNudgeStep);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Reset Position")) nextPosition = Vector3.zero;
                    if (GUILayout.Button("Reset Rotation")) nextEuler = Vector3.zero;
                    if (GUILayout.Button("Reset Scale")) nextScale = Vector3.one;
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (!EditorApplication.isPlayingOrWillChangePlaymode)
                        {
                            Undo.RecordObject(transform, $"Refine {slot.Label}");
                        }

                        transform.localPosition = nextPosition;
                        transform.localRotation = Quaternion.Euler(nextEuler);
                        transform.localScale = SanitizeScale(nextScale);
                        editedSlotTunings[slot.Label] = new RefinerTransformTuning(
                            transform.localPosition,
                            transform.localEulerAngles,
                            transform.localScale,
                            transform == socket);
                        RepaintPreviewViews();
                        MarkSceneDirtyIfSafe(transform.gameObject.scene);
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawPositionRefinerControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Position Refiner", EditorStyles.boldLabel);
            if (previewRoot == null)
            {
                EditorGUILayout.HelpBox("Open the preview scene to edit the player visual position.", MessageType.Info);
                return;
            }

            var visualRoot = FindDescendant(previewRoot.transform, VisualRootName);
            if (visualRoot == null)
            {
                EditorGUILayout.HelpBox($"Missing visual root: {VisualRootName}", MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox(
                "This edits only the preview instance's master visual root. Export the JSON for review; this does not bake gameplay defaults.",
                MessageType.Info);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Master Visual Root Offset", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Target", TransformPath(previewRoot.transform, visualRoot));
            EditorGUI.BeginChangeCheck();
            var nextPosition = DrawVector3WithNudges("Local Position", visualRoot.localPosition, positionNudgeStepMeters);
            var nextEuler = DrawVector3WithNudges("Local Rotation", visualRoot.localEulerAngles, rotationNudgeStepDegrees);
            var nextScale = DrawVector3WithNudges("Local Scale", visualRoot.localScale, scaleNudgeStep);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Position")) nextPosition = Vector3.zero;
            if (GUILayout.Button("Reset Rotation")) nextEuler = Vector3.zero;
            if (GUILayout.Button("Reset Scale")) nextScale = Vector3.one;
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    Undo.RecordObject(visualRoot, "Refine Player Visual Root");
                }

                visualRoot.localPosition = nextPosition;
                visualRoot.localRotation = Quaternion.Euler(nextEuler);
                visualRoot.localScale = SanitizeScale(nextScale);
                RepaintPreviewViews();
                MarkSceneDirtyIfSafe(visualRoot.gameObject.scene);
            }
            EditorGUILayout.EndVertical();

            DrawPositionMetrics(visualRoot);
            DrawPositionVisualizerToggles();
        }

        private void DrawPositionMetrics(Transform visualRoot)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Current Measurements", EditorStyles.boldLabel);
            if (TryResolveBodyBounds(previewRoot, out var bodyBounds))
            {
                EditorGUILayout.LabelField("Body Bounds Center", FormatVector(bodyBounds.center));
                EditorGUILayout.LabelField("Body Bounds Size", FormatVector(bodyBounds.size));
                EditorGUILayout.LabelField("Body Min/Max Y", $"{bodyBounds.min.y:0.###} / {bodyBounds.max.y:0.###}");
                var targetMinY = ResolveGroundY() + ResolveGroundClearance();
                EditorGUILayout.LabelField("Predicted Grounding Offset Y", $"{targetMinY - bodyBounds.min.y:0.###}");
            }
            else
            {
                EditorGUILayout.LabelField("Body Bounds", "No visible body renderer resolved.");
            }

            var capsule = previewRoot.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                EditorGUILayout.LabelField(
                    "Capsule",
                    $"center={FormatVector(capsule.center)} radius={capsule.radius:0.###} height={capsule.height:0.###}");
            }

            if (TryResolveProjectilePreview(out var projectileOrigin, out var projectileDirection))
            {
                EditorGUILayout.LabelField("Projectile Start", FormatVector(projectileOrigin));
                EditorGUILayout.LabelField("Projectile Direction", FormatVector(projectileDirection));
            }
            else
            {
                EditorGUILayout.LabelField("Projectile Start", "No ranged muzzle resolved.");
            }

            var arc = BuildSwordArcSamples();
            EditorGUILayout.LabelField("Sword Arc Samples", arc.Length > 0 ? arc.Length.ToString() : "No melee socket resolved.");
            EditorGUILayout.EndVertical();
        }

        private void DrawPositionVisualizerToggles()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Visualizers", EditorStyles.boldLabel);
            showGroundingHelpers = EditorGUILayout.ToggleLeft("Show body/capsule/floor helpers", showGroundingHelpers);
            showSockets = EditorGUILayout.ToggleLeft("Show anchors and sockets", showSockets);
            showProjectileStart = EditorGUILayout.ToggleLeft("Show projectile start point", showProjectileStart);
            showSwordArc = EditorGUILayout.ToggleLeft("Show sword attack arc", showSwordArc);
            showBones = EditorGUILayout.ToggleLeft("Show character bones", showBones);
            EditorGUILayout.EndVertical();
        }

        private void DrawExportControls()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Export Equipment Tuning JSON", GUILayout.Height(28f)))
            {
                var path = ExportCurrentTuning();
                Debug.Log($"Exported player animation refiner tuning: {path}");
            }

            if (GUILayout.Button("Export Position Refiner JSON", GUILayout.Height(28f)))
            {
                var path = ExportPositionRefinerSnapshot(
                    previewRoot,
                    PositionExportPath,
                    ResolveSelectedProfile()?.ProfileId.ToString() ?? "<none>",
                    weaponController != null ? weaponController.ActiveWeaponSlot.ToString() : "<none>",
                    CurrentClip() != null ? AssetDatabase.GetAssetPath(CurrentClip()) : string.Empty,
                    clipTimeSeconds,
                    new PositionRefinerVisibilityFlags
                    {
                        showBones = showBones,
                        showSockets = showSockets,
                        showProjectileStart = showProjectileStart,
                        showSwordArc = showSwordArc,
                        showGroundingHelpers = showGroundingHelpers
                    });
                Debug.Log($"Exported player position refiner snapshot: {path}");
            }
        }

        private void OnEditorUpdate()
        {
            if (EditorApplication.isPlaying)
            {
                SyncRuntimePreviewClipTime();
                return;
            }

            if (!playing || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var now = EditorApplication.timeSinceStartup;
            var delta = Mathf.Max(0f, (float)(now - lastEditorTime));
            lastEditorTime = now;
            var clip = CurrentClip();
            if (clip == null)
            {
                return;
            }

            clipTimeSeconds += delta;
            if (clipTimeSeconds > clip.length)
            {
                if (loop)
                {
                    clipTimeSeconds %= Mathf.Max(0.001f, clip.length);
                }
                else
                {
                    clipTimeSeconds = clip.length;
                    playing = false;
                }
            }

            SampleSelectedClip();
            Repaint();
        }

        private void EnsurePreviewSceneAndInstance()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before opening or rebuilding the Player Animation Refiner scene.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(PreviewScenePath) ?? string.Empty);
            var scene = File.Exists(PreviewScenePath)
                ? EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsurePrimitiveFloor();
            EnsureCameraAndLight();
            EnsurePreviewControllerAsset();
            EnsurePreviewInstance();
            FrameGameCamera();
            FramePreviewPlayer();
            EditorSceneManager.SaveScene(scene, PreviewScenePath);
            AssetDatabase.Refresh();
        }

        private void EnsurePrimitiveFloor()
        {
            if (GameObject.Find(FloorName) != null)
            {
                return;
            }

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = FloorName;
            floor.transform.position = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(8f, 0.1f, 8f);
        }

        private void EnsureCameraAndLight()
        {
            if (GameObject.Find(CameraName) == null)
            {
                var cameraObject = new GameObject(CameraName);
                var camera = cameraObject.AddComponent<Camera>();
                cameraObject.transform.position = new Vector3(0f, 2.2f, -5.5f);
                cameraObject.transform.rotation = Quaternion.Euler(68f, 0f, 0f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.02f, 0.025f, 1f);
            }

            if (GameObject.Find(LightName) == null)
            {
                var lightObject = new GameObject(LightName);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        private static RuntimeAnimatorController EnsurePreviewControllerAsset()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreviewControllerPath) ?? string.Empty);
            var placeholderClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(PreviewPlaceholderClipPath);
            if (placeholderClip == null)
            {
                placeholderClip = new AnimationClip
                {
                    name = PreviewPlaceholderClipName,
                    frameRate = 30f
                };
                placeholderClip.SetCurve(string.Empty, typeof(Transform), "localPosition.x", AnimationCurve.Constant(0f, 0.033f, 0f));
                AssetDatabase.CreateAsset(placeholderClip, PreviewPlaceholderClipPath);
            }
            else if (placeholderClip.name != PreviewPlaceholderClipName)
            {
                placeholderClip.name = PreviewPlaceholderClipName;
                EditorUtility.SetDirty(placeholderClip);
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PreviewControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(PreviewControllerPath);
            }

            while (controller.layers.Length > 1)
            {
                controller.RemoveLayer(controller.layers.Length - 1);
            }

            var layer = controller.layers[0];
            layer.name = "Base Layer";
            layer.iKPass = false;
            layer.avatarMask = null;
            var stateMachine = layer.stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
            {
                if (childState.state != null && childState.state.name != PreviewStateName)
                {
                    stateMachine.RemoveState(childState.state);
                }
            }

            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate != null && candidate.name == PreviewStateName);
            if (state == null)
            {
                state = stateMachine.AddState(PreviewStateName);
            }

            state.motion = placeholderClip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            controller.layers = new[] { layer };
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private void ApplyPreviewAnimatorController()
        {
            if (animator == null)
            {
                return;
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PreviewControllerPath);
            if (controller == null && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                controller = EnsurePreviewControllerAsset();
            }

            if (controller == null)
            {
                return;
            }

            animator.applyRootMotion = false;
            if (EditorApplication.isPlaying)
            {
                if (animator.runtimeAnimatorController == null)
                {
                    animator.runtimeAnimatorController = controller;
                }

                return;
            }

            if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                animator.runtimeAnimatorController != controller)
            {
                animator.runtimeAnimatorController = controller;
                EditorUtility.SetDirty(animator);
            }
        }

        private void ApplyPreviewRuntimeIsolation()
        {
            if (previewRoot == null)
            {
                return;
            }

            SetPreviewComponentEnabled<PlayerMovementController>(!disablePreviewGameplayControls);
            SetPreviewComponentEnabled<PlayerWeaponController>(!disablePreviewGameplayControls);
            SetPreviewComponentEnabled<PlayerDefenseController>(!disablePreviewGameplayControls);
            SetPreviewComponentEnabled<PlayerAimLockController>(!disablePreviewGameplayControls);
            SetPreviewComponentEnabled<PlayerLocomotionAnimator>(!disablePreviewGameplayControls);
            SetPreviewComponentEnabled<PlayerAnimationPoseCoordinator>(false);
            SetPreviewComponentEnabled<PlayerFootPlacementController>(false);
            SetPreviewComponentEnabled<PlayerRangedHandPoseController>(false);
            SetPreviewComponentEnabled<PlayerShieldGuardPoseController>(false);

            foreach (var rigBuilder in previewRoot.GetComponentsInChildren<RigBuilder>(includeInactive: true))
            {
                if (rigBuilder != null && rigBuilder.enabled)
                {
                    rigBuilder.enabled = false;
                }
            }
        }

        private void SetPreviewComponentEnabled<T>(bool enabled) where T : Behaviour
        {
            if (previewRoot == null)
            {
                return;
            }

            foreach (var component in previewRoot.GetComponentsInChildren<T>(includeInactive: true))
            {
                if (component != null && component.enabled != enabled)
                {
                    component.enabled = enabled;
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        EditorUtility.SetDirty(component);
                    }
                }
            }
        }

        private void EnsurePreviewInstance()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerVisualAssemblyValidator.PlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Cannot open Player Animation Refiner because {PlayerVisualAssemblyValidator.PlayerPrefabPath} is missing.");
                return;
            }

            ResolveSceneReferences(unpackPreviewRoot: false);
            if (previewRoot != null)
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(previewRoot);
                if (source == null || source == prefab)
                {
                    EnsurePreviewRootIsEditable();
                    ResolveSceneReferences();
                    ApplyPreviewAnimatorController();
                    ApplyPreviewRuntimeIsolation();
                    ApplyPreviewProfile(ResolveSelectedProfile(), force: true);
                    RefreshEquipmentVisuals();
                    return;
                }

                DestroyImmediate(previewRoot);
                previewRoot = null;
            }

            previewRoot = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (previewRoot == null)
            {
                previewRoot = Object.Instantiate(prefab);
            }

            previewRoot.name = PreviewRootName;
            previewRoot.transform.position = Vector3.zero;
            previewRoot.transform.rotation = Quaternion.identity;
            EnsurePreviewRootIsEditable();
            ResolveSceneReferences();
            ApplyPreviewAnimatorController();
            ApplyPreviewRuntimeIsolation();
            ApplyPreviewProfile(ResolveSelectedProfile(), force: true);
            RefreshEquipmentVisuals();
        }

        private void ResolveSceneReferences(bool unpackPreviewRoot = true)
        {
            previewRoot = GameObject.Find(PreviewRootName);
            if (unpackPreviewRoot)
            {
                EnsurePreviewRootIsEditable();
            }

            animator = previewRoot != null ? previewRoot.GetComponentInChildren<Animator>(includeInactive: true) : null;
            profileController = previewRoot != null ? previewRoot.GetComponent<PlayerAnimationProfileController>() : null;
            weaponController = previewRoot != null ? previewRoot.GetComponent<PlayerWeaponController>() : null;
            heldWeaponVisual = previewRoot != null ? previewRoot.GetComponent<PlayerHeldWeaponVisualController>() : null;
            catalog ??= PlayerAnimationProfileAssetGenerator.LoadCatalog();
            ApplyPreviewRuntimeIsolation();
        }

        private void EnsurePreviewRootIsEditable()
        {
            if (previewRoot == null ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                !PrefabUtility.IsPartOfPrefabInstance(previewRoot))
            {
                return;
            }

            PrefabUtility.UnpackPrefabInstance(
                previewRoot,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            previewRoot.name = PreviewRootName;
            MarkSceneDirtyIfSafe(previewRoot.scene);
        }

        private void FramePreviewPlayer()
        {
            ResolveSceneReferences();
            if (previewRoot == null || SceneView.lastActiveSceneView == null)
            {
                return;
            }

            Selection.activeGameObject = previewRoot;
            SceneView.lastActiveSceneView.LookAt(
                previewRoot.transform.position + Vector3.up * 0.9f,
                Quaternion.Euler(25f, 35f, 0f),
                4.2f,
                true,
                false);
            SceneView.RepaintAll();
        }

        private void SetCameraPreset(float yawDegrees, float pitchDegrees, float distanceMeters)
        {
            cameraYawDegrees = yawDegrees;
            cameraPitchDegrees = pitchDegrees;
            cameraDistanceMeters = distanceMeters;
            FrameGameCamera();
        }

        private void FrameGameCamera()
        {
            ResolveSceneReferences();
            var camera = FindOrCreatePreviewCamera();
            if (previewRoot == null || camera == null)
            {
                return;
            }

            var target = previewRoot.transform.position + Vector3.up * cameraTargetHeightMeters;
            var orbit = Quaternion.Euler(cameraPitchDegrees, cameraYawDegrees, 0f);
            var cameraPosition = target + orbit * new Vector3(0f, 0f, -cameraDistanceMeters);
            camera.transform.position = cameraPosition;
            camera.transform.rotation = Quaternion.LookRotation((target - cameraPosition).normalized, Vector3.up);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 80f;

            MarkSceneDirtyIfSafe(camera.gameObject.scene);
            SceneView.RepaintAll();
        }

        private Camera FindOrCreatePreviewCamera()
        {
            var cameraObject = GameObject.Find(CameraName);
            if (cameraObject == null)
            {
                cameraObject = new GameObject(CameraName);
            }

            var camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.02f, 0.025f, 1f);
            if (Camera.main == null)
            {
                camera.tag = "MainCamera";
            }

            return camera;
        }

        private void ApplyPreviewProfile(PlayerAnimationProfileDefinition profile, bool force)
        {
            if (profileController == null || profile == null)
            {
                return;
            }

            var profileId = profile.ProfileId;
            if (force ||
                lastAppliedPreviewProfileId != profileId ||
                profileController.CurrentProfile != profile ||
                !profileController.IsDebugOverrideEnabled)
            {
                profileController.SetDebugProfileOverride(profile);
                ApplyPreviewLoadout(profileId);
                lastAppliedPreviewProfileId = profileId;
                RefreshEquipmentVisuals();
            }
        }

        private void ApplyPreviewLoadout(PlayerAnimationProfileId profileId)
        {
            if (weaponController == null)
            {
                previewLoadoutDescription = "Preview loadout: no PlayerWeaponController";
                return;
            }

            var weaponCatalog = weaponController.WeaponCatalog ??
                AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(Milestone27AssetGenerator.WeaponCatalogPath);
            if (weaponCatalog != null)
            {
                weaponController.ConfigureWeaponCatalog(weaponCatalog);
            }

            var meleeWeaponId = profileId == PlayerAnimationProfileId.GreatSwordCombat
                ? ResolveGreatSwordWeaponId(weaponCatalog)
                : "starter_blade";
            var rangedWeaponId = profileId == PlayerAnimationProfileId.RifleCombat
                ? ResolveRifleWeaponId(weaponCatalog)
                : ResolvePistolWeaponId(weaponCatalog);
            var activeSlot = profileId is PlayerAnimationProfileId.RifleCombat or PlayerAnimationProfileId.PistolCombat
                ? WeaponSlot.Ranged
                : WeaponSlot.Melee;

            weaponController.ConfigureBuildStats(
                nextCooldownMultiplier: 1f,
                nextRangedDamageBonus: 0,
                nextMeleeDamageBonus: 0,
                nextMaxStamina: Mathf.Max(1f, weaponController.MaxStamina),
                nextStaminaRegenPerSecond: 11f,
                nextMeleeWeaponId: meleeWeaponId,
                nextRangedWeaponId: rangedWeaponId,
                nextActiveWeaponSlot: activeSlot,
                nextCurrentStamina: Mathf.Max(1f, weaponController.CurrentStamina),
                nextWeaponCatalog: weaponCatalog);

            var rifleFallbackNote =
                profileId == PlayerAnimationProfileId.RifleCombat &&
                !ContainsToken(rangedWeaponId, "rifle") &&
                !ContainsToken(rangedWeaponId, "carbine")
                    ? " | rifle animation with current ranged visual fallback"
                    : string.Empty;
            previewLoadoutDescription =
                $"Preview loadout: {activeSlot} | melee={meleeWeaponId} | ranged={rangedWeaponId}{rifleFallbackNote}";
            RepaintPreviewViews();
        }

        private static string ResolveGreatSwordWeaponId(WeaponCatalogDefinition weaponCatalog)
        {
            var weapon = weaponCatalog?.WeaponsForSlot(WeaponSlot.Melee)
                .FirstOrDefault(candidate =>
                    candidate != null &&
                    (candidate.IsDoubleHandedForPresentation ||
                        ContainsToken(candidate.WeaponId, "great") ||
                        ContainsToken(candidate.DisplayName, "great") ||
                        ContainsToken(candidate.WeaponId, "cleaver") ||
                        ContainsToken(candidate.DisplayName, "cleaver") ||
                        ContainsToken(candidate.WeaponId, "two_handed") ||
                        ContainsToken(candidate.DisplayName, "two handed")));
            return weapon != null ? weapon.WeaponId : "iron_cleaver";
        }

        private static string ResolveRifleWeaponId(WeaponCatalogDefinition weaponCatalog)
        {
            var weapon = weaponCatalog?.WeaponsForSlot(WeaponSlot.Ranged)
                .FirstOrDefault(candidate =>
                    candidate != null &&
                    (ContainsToken(candidate.WeaponId, "rifle") ||
                        ContainsToken(candidate.DisplayName, "rifle") ||
                        ContainsToken(candidate.WeaponId, "carbine") ||
                        ContainsToken(candidate.DisplayName, "carbine")));
            return weapon != null ? weapon.WeaponId : ResolvePistolWeaponId(weaponCatalog);
        }

        private static string ResolvePistolWeaponId(WeaponCatalogDefinition weaponCatalog)
        {
            var weapon = weaponCatalog?.WeaponsForSlot(WeaponSlot.Ranged)
                .FirstOrDefault(candidate =>
                    candidate != null &&
                    candidate.Category == WeaponCategory.Gun &&
                    !ContainsToken(candidate.WeaponId, "rifle") &&
                    !ContainsToken(candidate.DisplayName, "rifle") &&
                    !ContainsToken(candidate.WeaponId, "carbine") &&
                    !ContainsToken(candidate.DisplayName, "carbine"));
            return weapon != null ? weapon.WeaponId : WeaponIdAliases.StarterPistolId;
        }

        private static bool ContainsToken(string value, string token)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SampleSelectedClip()
        {
            var clip = CurrentClip();
            if (EditorApplication.isPlaying)
            {
                PreviewSelectedClipInPlayMode(clip);
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                samplingRootDescription = "Sampling root: waiting for Play Mode transition";
                return;
            }

            ResolveSceneReferences();
            if (previewRoot == null || clip == null)
            {
                return;
            }

            var samplingRoot = ResolveClipSamplingRoot();
            if (samplingRoot == null)
            {
                samplingRootDescription = "Sampling root: missing Animator/model root";
                return;
            }

            if (!AnimationMode.InAnimationMode())
            {
                AnimationMode.StartAnimationMode();
            }

            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(samplingRoot, clip, Mathf.Clamp(clipTimeSeconds, 0f, clip.length));
            AnimationMode.EndSampling();
            samplingRootDescription = $"Sampling root: {TransformPath(previewRoot.transform, samplingRoot.transform)}";
            RefreshEquipmentVisuals();
            SceneView.RepaintAll();
        }

        private GameObject ResolveClipSamplingRoot()
        {
            if (animator != null)
            {
                return animator.gameObject;
            }

            return previewRoot;
        }

        private void PreviewSelectedClipInPlayMode(AnimationClip clip)
        {
            ResolveSceneReferences();
            ApplyPreviewAnimatorController();
            ApplyPreviewRuntimeIsolation();
            if (animator == null || clip == null)
            {
                return;
            }

            var baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PreviewControllerPath);
            if (baseController == null)
            {
                samplingRootDescription = "Runtime preview: missing refiner AnimatorController";
                return;
            }

            if (runtimePreviewOverrideController == null || runtimePreviewBaseController != baseController)
            {
                runtimePreviewBaseController = baseController;
                runtimePreviewOverrideController = new AnimatorOverrideController(baseController);
            }

            runtimePreviewOverrideController[PreviewPlaceholderClipName] = clip;
            if (animator.runtimeAnimatorController != runtimePreviewOverrideController)
            {
                animator.runtimeAnimatorController = runtimePreviewOverrideController;
            }

            animator.applyRootMotion = false;
            animator.speed = runtimeClipPaused ? 0f : 1f;
            var normalizedTime = clip.length > 0.001f ? Mathf.Clamp01(clipTimeSeconds / clip.length) : 0f;
            animator.Play(PreviewStateName, 0, normalizedTime);
            animator.Update(0f);
            samplingRootDescription = $"Runtime preview: {clip.name} on {TransformPath(previewRoot.transform, animator.transform)}";
            RefreshEquipmentVisuals();
            RepaintPreviewViews();
        }

        private void SyncRuntimePreviewClipTime()
        {
            ResolveSceneReferences();
            if (animator == null || runtimeClipPaused)
            {
                return;
            }

            var clip = CurrentClip();
            if (clip == null)
            {
                return;
            }

            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(PreviewStateName))
            {
                return;
            }

            var normalized = state.normalizedTime;
            if (loop)
            {
                normalized -= Mathf.Floor(normalized);
            }
            else
            {
                normalized = Mathf.Clamp01(normalized);
            }

            clipTimeSeconds = normalized * Mathf.Max(0.001f, clip.length);
            Repaint();
        }

        private void RefreshEquipmentVisuals()
        {
            if (heldWeaponVisual == null)
            {
                return;
            }

            var canRefreshVisualController = !PrefabUtility.IsPartOfPrefabInstance(heldWeaponVisual.gameObject);
            if (canRefreshVisualController)
            {
                heldWeaponVisual.RefreshAllEquipmentVisualTransforms();
            }

            ApplyStoredPreviewTunings();
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.SetDirty(heldWeaponVisual);
            }
        }

        private PlayerAnimationProfileDefinition ResolveSelectedProfile()
        {
            catalog ??= PlayerAnimationProfileAssetGenerator.LoadCatalog();
            if (catalog == null)
            {
                return null;
            }

            selectedProfileIndex = Mathf.Clamp(selectedProfileIndex, 0, ProfileOrder.Length - 1);
            return catalog.Resolve(ProfileOrder[selectedProfileIndex]);
        }

        private AnimationClip CurrentClip()
        {
            var clips = CollectClips(ResolveSelectedProfile()).ToArray();
            return clips.Length == 0 ? null : clips[Mathf.Clamp(selectedClipIndex, 0, clips.Length - 1)];
        }

        private static IEnumerable<AnimationClip> CollectClips(PlayerAnimationProfileDefinition profile)
        {
            if (profile == null)
            {
                yield break;
            }

            var seen = new HashSet<AnimationClip>();
            foreach (var clip in FlattenProfileClips(profile))
            {
                if (clip != null && seen.Add(clip))
                {
                    yield return clip;
                }
            }
        }

        private static IEnumerable<AnimationClip> FlattenProfileClips(PlayerAnimationProfileDefinition profile)
        {
            yield return profile.IdleClip;
            foreach (var direction in profile.DirectionalClips)
            {
                yield return direction.WalkClip;
                yield return direction.RunClip;
            }

            foreach (var clip in profile.StrafingClips) yield return clip;
            foreach (var clip in profile.TurnClips) yield return clip;
            foreach (var clip in profile.DrawClips) yield return clip;
            foreach (var clip in profile.SheatheClips) yield return clip;
            foreach (var clip in profile.AttackClips) yield return clip;
            foreach (var clip in profile.FireClips) yield return clip;
            foreach (var clip in profile.ShieldGuardClips) yield return clip;
            foreach (var clip in profile.WeaponBlockClips) yield return clip;
            foreach (var clip in profile.ImpactClips) yield return clip;
            foreach (var clip in profile.DeathClips) yield return clip;
            foreach (var clip in profile.JumpClips) yield return clip;
            foreach (var clip in profile.CrouchClips) yield return clip;
        }

        private string ExportCurrentTuning()
        {
            ResolveSceneReferences();
            Directory.CreateDirectory(Path.GetDirectoryName(ExportPath) ?? string.Empty);
            var profile = ResolveSelectedProfile();
            var clip = CurrentClip();
            var export = new RefinerExport
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                playerPrefabPath = PlayerVisualAssemblyValidator.PlayerPrefabPath,
                previewScenePath = PreviewScenePath,
                selectedProfile = profile != null ? profile.ProfileId.ToString() : "<none>",
                selectedWeaponSlot = weaponController != null ? weaponController.ActiveWeaponSlot.ToString() : "<none>",
                selectedClip = clip != null ? AssetDatabase.GetAssetPath(clip) : string.Empty,
                slots = EquipmentSlots
                    .Select(slot => BuildSlotExport(slot))
                    .ToArray()
            };

            var json = JsonUtility.ToJson(export, prettyPrint: true);
            File.WriteAllText(ExportPath, json);
            EditorGUIUtility.systemCopyBuffer = json;
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                AssetDatabase.ImportAsset(ExportPath);
            }

            return ExportPath;
        }

        private RefinerSlotExport BuildSlotExport(EquipmentSocketTuning slot)
        {
            var socket = previewRoot != null ? FindDescendant(previewRoot.transform, slot.SocketName) : null;
            var targetKind = "Missing";
            var targetNote = string.Empty;
            var transform = previewRoot != null ? ResolveEditableTransform(slot, socket, out targetKind, out targetNote) : null;
            var profile = ResolveSelectedProfile();
            var clip = CurrentClip();
            return new RefinerSlotExport
            {
                label = slot.Label,
                socketName = slot.SocketName,
                socketPath = socket != null ? TransformPath(previewRoot.transform, socket) : string.Empty,
                editTargetKind = targetKind,
                editTargetPath = transform != null ? TransformPath(previewRoot.transform, transform) : string.Empty,
                profileContext = profile != null ? profile.ProfileId.ToString() : "<none>",
                weaponSlotContext = weaponController != null ? weaponController.ActiveWeaponSlot.ToString() : "<none>",
                clipContext = clip != null ? AssetDatabase.GetAssetPath(clip) : string.Empty,
                notes = targetNote,
                localPosition = transform != null ? transform.localPosition : Vector3.zero,
                localEuler = transform != null ? transform.localEulerAngles : Vector3.zero,
                localScale = transform != null ? transform.localScale : Vector3.one
            };
        }

        public static string ExportPositionRefinerSnapshot(GameObject root, string outputPath)
        {
            return ExportPositionRefinerSnapshot(
                root,
                outputPath,
                "<none>",
                "<none>",
                string.Empty,
                0f,
                PositionRefinerVisibilityFlags.Default);
        }

        private static string ExportPositionRefinerSnapshot(
            GameObject root,
            string outputPath,
            string selectedProfile,
            string selectedWeaponSlot,
            string selectedClip,
            float selectedClipTimeSeconds,
            PositionRefinerVisibilityFlags visibility)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var export = BuildPositionExport(
                root,
                selectedProfile,
                selectedWeaponSlot,
                selectedClip,
                selectedClipTimeSeconds,
                visibility);
            var json = JsonUtility.ToJson(export, prettyPrint: true);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
            File.WriteAllText(outputPath, json);
            EditorGUIUtility.systemCopyBuffer = json;
            if (!EditorApplication.isPlayingOrWillChangePlaymode && outputPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                AssetDatabase.ImportAsset(outputPath);
            }

            return outputPath;
        }

        private static PositionRefinerExport BuildPositionExport(
            GameObject root,
            string selectedProfile,
            string selectedWeaponSlot,
            string selectedClip,
            float selectedClipTimeSeconds,
            PositionRefinerVisibilityFlags visibility)
        {
            var visualRoot = FindDescendant(root.transform, VisualRootName);
            TryResolveBodyBounds(root, out var bodyBounds);
            TryResolveProjectilePreview(root, out var projectileOrigin, out var projectileDirection);
            var swordArcSamples = BuildSwordArcSamples(root);
            var capsule = root.GetComponent<CapsuleCollider>();
            var grounding = root.GetComponent<SimpleFullBodyGroundingController>();
            var groundY = grounding != null && grounding.GroundReference != null ? grounding.GroundReference.position.y : 0f;
            var clearance = grounding != null ? grounding.GroundClearanceMeters : SimpleFullBodyGroundingController.DefaultGroundClearanceMeters;

            return new PositionRefinerExport
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                playerPrefabPath = PlayerVisualAssemblyValidator.PlayerPrefabPath,
                previewScenePath = PreviewScenePath,
                selectedProfile = selectedProfile ?? "<none>",
                selectedWeaponSlot = selectedWeaponSlot ?? "<none>",
                selectedClip = selectedClip ?? string.Empty,
                selectedClipTimeSeconds = Mathf.Max(0f, selectedClipTimeSeconds),
                masterVisualRootPath = visualRoot != null ? TransformPath(root.transform, visualRoot) : string.Empty,
                masterVisualRootLocalPosition = visualRoot != null ? visualRoot.localPosition : Vector3.zero,
                masterVisualRootLocalEuler = visualRoot != null ? visualRoot.localEulerAngles : Vector3.zero,
                masterVisualRootLocalScale = visualRoot != null ? visualRoot.localScale : Vector3.one,
                bodyBoundsCenter = bodyBounds.center,
                bodyBoundsSize = bodyBounds.size,
                bodyBoundsMinY = bodyBounds.min.y,
                bodyBoundsMaxY = bodyBounds.max.y,
                groundY = groundY,
                groundClearanceMeters = clearance,
                predictedGroundingOffsetY = bodyBounds.size.sqrMagnitude > 0.0001f
                    ? groundY + clearance - bodyBounds.min.y
                    : 0f,
                capsuleCenter = capsule != null ? capsule.center : Vector3.zero,
                capsuleRadius = capsule != null ? capsule.radius : 0f,
                capsuleHeight = capsule != null ? capsule.height : 0f,
                projectileOrigin = projectileOrigin,
                projectileDirection = projectileDirection,
                points = CollectPositionPoints(root).ToArray(),
                swordArcSamples = swordArcSamples,
                showBones = visibility.showBones,
                showSockets = visibility.showSockets,
                showProjectileStart = visibility.showProjectileStart,
                showSwordArc = visibility.showSwordArc,
                showGroundingHelpers = visibility.showGroundingHelpers
            };
        }

        private void DrawPositionRefinerSceneGui(SceneView sceneView)
        {
            if (refinerMode != PlayerAnimationRefinerMode.PositionRefiner || previewRoot == null)
            {
                return;
            }

            if (showGroundingHelpers)
            {
                DrawGroundingHelpers(previewRoot);
            }

            if (showSockets)
            {
                DrawPositionPoints(previewRoot);
            }

            if (showProjectileStart && TryResolveProjectilePreview(out var projectileOrigin, out var projectileDirection))
            {
                DrawPoint("Projectile Start", projectileOrigin, new Color(1f, 0.7f, 0.1f, 1f), 0.08f);
                Handles.color = new Color(1f, 0.7f, 0.1f, 0.9f);
                Handles.DrawAAPolyLine(4f, projectileOrigin, projectileOrigin + projectileDirection.normalized * 1.2f);
            }

            if (showSwordArc)
            {
                DrawSwordArc(BuildSwordArcSamples());
            }

            if (showBones)
            {
                DrawSkinnedBones(previewRoot);
            }
        }

        private static void DrawGroundingHelpers(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            if (TryResolveBodyBounds(root, out var bounds))
            {
                Handles.color = new Color(0.25f, 0.8f, 1f, 0.85f);
                Handles.DrawWireCube(bounds.center, bounds.size);
                DrawHorizontalLine("Body Min Y", bounds.min.y, new Color(0.25f, 0.8f, 1f, 0.85f));
                DrawHorizontalLine("Body Max Y", bounds.max.y, new Color(0.25f, 0.8f, 1f, 0.35f));
            }

            var grounding = root.GetComponent<SimpleFullBodyGroundingController>();
            var groundY = grounding != null && grounding.GroundReference != null ? grounding.GroundReference.position.y : 0f;
            var clearance = grounding != null ? grounding.GroundClearanceMeters : SimpleFullBodyGroundingController.DefaultGroundClearanceMeters;
            DrawHorizontalLine("Ground", groundY, new Color(0.55f, 1f, 0.45f, 0.8f));
            DrawHorizontalLine("Ground + Clearance", groundY + clearance, new Color(0.55f, 1f, 0.45f, 0.45f));

            var capsule = root.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                var center = root.transform.TransformPoint(capsule.center);
                Handles.color = new Color(1f, 1f, 1f, 0.55f);
                Handles.DrawWireDisc(center, Vector3.up, capsule.radius);
                Handles.DrawWireDisc(center + Vector3.up * (capsule.height * 0.5f - capsule.radius), Vector3.up, capsule.radius);
                Handles.DrawWireDisc(center - Vector3.up * (capsule.height * 0.5f - capsule.radius), Vector3.up, capsule.radius);
                Handles.Label(center + Vector3.up * (capsule.height * 0.5f + 0.08f), "Capsule");
            }
        }

        private static void DrawHorizontalLine(string label, float y, Color color)
        {
            Handles.color = color;
            var center = Vector3.up * y;
            Handles.DrawAAPolyLine(
                2f,
                center + new Vector3(-1.5f, 0f, -1.5f),
                center + new Vector3(1.5f, 0f, -1.5f),
                center + new Vector3(1.5f, 0f, 1.5f),
                center + new Vector3(-1.5f, 0f, 1.5f),
                center + new Vector3(-1.5f, 0f, -1.5f));
            Handles.Label(center + new Vector3(1.6f, 0f, 0f), label);
        }

        private static void DrawPositionPoints(GameObject root)
        {
            foreach (var point in CollectPositionPoints(root))
            {
                DrawPoint(point.label, point.worldPosition, point.kind switch
                {
                    "Root" => new Color(1f, 1f, 1f, 1f),
                    "Bone" => new Color(0.45f, 0.85f, 1f, 1f),
                    "Socket" => new Color(1f, 0.55f, 0.2f, 1f),
                    _ => new Color(0.85f, 0.85f, 0.85f, 1f)
                });
            }
        }

        private static void DrawPoint(string label, Vector3 position, Color color, float size = 0.045f)
        {
            Handles.color = color;
            Handles.SphereHandleCap(0, position, Quaternion.identity, size, EventType.Repaint);
            Handles.Label(position + Vector3.up * (size * 1.6f), label);
        }

        private void DrawSwordArc(Vector3[] samples)
        {
            if (samples == null || samples.Length < 2)
            {
                return;
            }

            Handles.color = new Color(1f, 0.25f, 0.25f, 0.9f);
            Handles.DrawAAPolyLine(4f, samples);
            for (var i = 0; i < samples.Length; i += Mathf.Max(1, samples.Length / 6))
            {
                Handles.SphereHandleCap(0, samples[i], Quaternion.identity, 0.035f, EventType.Repaint);
            }

            Handles.Label(samples[samples.Length / 2] + Vector3.up * 0.08f, "Sword Arc");
        }

        private static void DrawSkinnedBones(GameObject root)
        {
            var renderer = root.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true)
                .FirstOrDefault(candidate => candidate != null && candidate.bones != null && candidate.bones.Length > 0);
            if (renderer == null)
            {
                return;
            }

            var bones = new HashSet<Transform>(renderer.bones.Where(bone => bone != null));
            Handles.color = new Color(0.6f, 0.9f, 1f, 0.45f);
            foreach (var bone in bones)
            {
                if (bone.parent != null && bones.Contains(bone.parent))
                {
                    Handles.DrawAAPolyLine(2f, bone.parent.position, bone.position);
                }
            }
        }

        private bool TryResolveProjectilePreview(out Vector3 worldOrigin, out Vector3 worldDirection)
        {
            return TryResolveProjectilePreview(previewRoot, out worldOrigin, out worldDirection);
        }

        private static bool TryResolveProjectilePreview(GameObject root, out Vector3 worldOrigin, out Vector3 worldDirection)
        {
            worldOrigin = Vector3.zero;
            worldDirection = Vector3.forward;
            if (root == null)
            {
                return false;
            }

            var visual = root.GetComponent<PlayerHeldWeaponVisualController>();
            var weapon = root.GetComponent<PlayerWeaponController>();
            var aim = weapon != null ? weapon.LastAimDirection : PlanarDirectionFromTransform(root.transform);
            if (visual != null &&
                visual.TryResolveRangedMuzzlePose(aim, 0f, root.transform, out var localOrigin, out var localDirection))
            {
                worldOrigin = root.transform.TransformPoint(localOrigin);
                worldDirection = root.transform.TransformDirection(new Vector3(localDirection.x, 0f, localDirection.y));
                if (worldDirection.sqrMagnitude < 0.0001f)
                {
                    worldDirection = root.transform.forward;
                }

                worldDirection.y = 0f;
                worldDirection.Normalize();
                return true;
            }

            var muzzle = FindDescendant(root.transform, PlayerHeldWeaponVisualController.RangedMuzzleSocketName);
            if (muzzle == null)
            {
                return false;
            }

            worldOrigin = muzzle.position;
            worldDirection = muzzle.forward;
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
            {
                worldDirection = root.transform.forward;
            }

            worldDirection.Normalize();
            return true;
        }

        private Vector3[] BuildSwordArcSamples()
        {
            return BuildSwordArcSamples(previewRoot);
        }

        private static Vector3[] BuildSwordArcSamples(GameObject root)
        {
            if (root == null)
            {
                return Array.Empty<Vector3>();
            }

            var meleeSocket = FindDescendant(root.transform, PlayerHeldWeaponVisualController.MeleeHandSocketName);
            var activeMelee = FindDescendant(root.transform, PlayerHeldWeaponVisualController.ActiveMeleeWeaponVisualName);
            var originSource = activeMelee != null ? activeMelee : meleeSocket;
            if (originSource == null)
            {
                return Array.Empty<Vector3>();
            }

            ResolveMeleeAttackPreview(root, out var rangeMeters, out var arcDegrees);
            var forward = ResolveFacingDirection(root);
            var origin = originSource.position;
            origin.y = root.transform.position.y + CombatFeelTuning.MeleeHitHeightMeters;

            var samples = new Vector3[SwordArcSegmentCount + 1];
            var halfArc = Mathf.Clamp(arcDegrees, 1f, 360f) * 0.5f;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = samples.Length <= 1 ? 0.5f : i / (float)(samples.Length - 1);
                var angle = Mathf.Lerp(-halfArc, halfArc, t);
                var direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                samples[i] = origin + direction.normalized * Mathf.Max(0.1f, rangeMeters);
            }

            return samples;
        }

        private static void ResolveMeleeAttackPreview(GameObject root, out float rangeMeters, out float arcDegrees)
        {
            var attack = WeaponAttackDefinition.DefaultLight(WeaponSlot.Melee);
            var weapon = root.GetComponent<PlayerWeaponController>();
            if (weapon != null &&
                weapon.WeaponCatalog != null &&
                weapon.WeaponCatalog.TryGetWeapon(weapon.MeleeWeaponId, out var definition) &&
                definition != null)
            {
                attack = definition.LightAttack;
            }

            rangeMeters = attack.RangeMeters + (weapon != null ? weapon.MeleeRangeBonusMeters : 0f);
            arcDegrees = attack.HitArcDegrees;
        }

        private static IEnumerable<PositionPointExport> CollectPositionPoints(GameObject root)
        {
            if (root == null)
            {
                yield break;
            }

            foreach (var point in RootAndBonePoints(root))
            {
                yield return point;
            }

            foreach (var slot in EquipmentSlots)
            {
                var socket = FindDescendant(root.transform, slot.SocketName);
                if (socket != null)
                {
                    yield return BuildPoint(root.transform, slot.Label, "Socket", socket);
                }
            }
        }

        private static IEnumerable<PositionPointExport> RootAndBonePoints(GameObject root)
        {
            yield return BuildPoint(root.transform, "Player Root", "Root", root.transform);
            var visualRoot = FindDescendant(root.transform, VisualRootName);
            if (visualRoot != null)
            {
                yield return BuildPoint(root.transform, "Visual Root", "Root", visualRoot);
            }

            var animator = root.GetComponentInChildren<Animator>(includeInactive: true);
            if (animator != null)
            {
                yield return BuildPoint(root.transform, "Animator", "Root", animator.transform);
                foreach (var pair in HumanBonePoints(animator))
                {
                    yield return BuildPoint(root.transform, pair.label, "Bone", pair.transform);
                }
            }

            var hips = FindDescendant(root.transform, "mixamorig:Hips");
            if (hips != null && (animator == null || animator.GetBoneTransform(HumanBodyBones.Hips) != hips))
            {
                yield return BuildPoint(root.transform, "Hips", "Bone", hips);
            }
        }

        private static IEnumerable<(string label, Transform transform)> HumanBonePoints(Animator animator)
        {
            var bones = new[]
            {
                (label: "Hips", bone: HumanBodyBones.Hips),
                (label: "Right Hand", bone: HumanBodyBones.RightHand),
                (label: "Left Hand", bone: HumanBodyBones.LeftHand),
                (label: "Right Foot", bone: HumanBodyBones.RightFoot),
                (label: "Left Foot", bone: HumanBodyBones.LeftFoot)
            };

            foreach (var entry in bones)
            {
                var transform = animator.GetBoneTransform(entry.bone);
                if (transform != null)
                {
                    yield return (entry.label, transform);
                }
            }
        }

        private static PositionPointExport BuildPoint(Transform root, string label, string kind, Transform transform)
        {
            return new PositionPointExport
            {
                label = label,
                kind = kind,
                path = TransformPath(root, transform),
                localPosition = transform.localPosition,
                localEuler = transform.localEulerAngles,
                localScale = transform.localScale,
                worldPosition = transform.position,
                worldEuler = transform.rotation.eulerAngles
            };
        }

        private static bool TryResolveBodyBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            if (root == null)
            {
                return false;
            }

            var hasBounds = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(includeInactive: false))
            {
                if (renderer == null ||
                    !renderer.enabled ||
                    renderer.bounds.size.sqrMagnitude <= 0.0001f ||
                    IsUnderPresentationVisual(renderer.transform))
                {
                    continue;
                }

                bounds = hasBounds ? Encapsulate(bounds, renderer.bounds) : renderer.bounds;
                hasBounds = true;
            }

            return hasBounds;
        }

        private static Bounds Encapsulate(Bounds bounds, Bounds other)
        {
            bounds.Encapsulate(other.min);
            bounds.Encapsulate(other.max);
            return bounds;
        }

        private static bool IsUnderPresentationVisual(Transform transform)
        {
            var cursor = transform;
            while (cursor != null)
            {
                if (cursor.GetComponent<PresentationVisualMarker>() != null)
                {
                    return true;
                }

                cursor = cursor.parent;
            }

            return false;
        }

        private float ResolveGroundY()
        {
            var grounding = previewRoot != null ? previewRoot.GetComponent<SimpleFullBodyGroundingController>() : null;
            return grounding != null && grounding.GroundReference != null ? grounding.GroundReference.position.y : 0f;
        }

        private float ResolveGroundClearance()
        {
            var grounding = previewRoot != null ? previewRoot.GetComponent<SimpleFullBodyGroundingController>() : null;
            return grounding != null ? grounding.GroundClearanceMeters : SimpleFullBodyGroundingController.DefaultGroundClearanceMeters;
        }

        private static Vector3 ResolveFacingDirection(GameObject root)
        {
            var weapon = root.GetComponent<PlayerWeaponController>();
            if (weapon != null)
            {
                var aim = weapon.VisualAimDirection;
                if (aim.sqrMagnitude > 0.001f)
                {
                    return new Vector3(aim.x, 0f, aim.y).normalized;
                }
            }

            var visualRoot = FindDescendant(root.transform, VisualRootName);
            var forward = visualRoot != null ? visualRoot.forward : root.transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        }

        private static Vector2 PlanarDirectionFromTransform(Transform transform)
        {
            var forward = transform != null ? transform.forward : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            return new Vector2(forward.x, forward.z);
        }

        private static string FormatVector(Vector3 value)
        {
            return $"{value.x:0.###}, {value.y:0.###}, {value.z:0.###}";
        }

        private Transform ResolveEditableTransform(
            EquipmentSocketTuning slot,
            Transform socket,
            out string targetKind,
            out string note)
        {
            targetKind = "Socket";
            note = string.Empty;
            if (previewRoot == null)
            {
                return null;
            }

            if (slot.EditTargetMode == EquipmentEditTargetMode.ScenarioSocket)
            {
                targetKind = ScenarioSocketTargetKind;
                note = "This scenario edits the socket so placement remains stable across all animations for the active profile/loadout.";
                if (!string.IsNullOrEmpty(slot.WrapperName))
                {
                    var wrapper = FindDescendant(previewRoot.transform, slot.WrapperName);
                    var artPassRoot = FindPresentationVisualRoot(wrapper);
                    if (artPassRoot != null && artPassRoot.localPosition.magnitude > SuspiciousHolsteredVisualOffsetMeters)
                    {
                        note += $" Warning: the visible child offset is {artPassRoot.localPosition.magnitude:0.###}m, which is likely an animation-pose-specific offset. Regenerate the player or reset the child visual to defaults before exporting production tuning.";
                    }
                }

                return socket;
            }

            if (!string.IsNullOrEmpty(slot.WrapperName))
            {
                var wrapper = FindDescendant(previewRoot.transform, slot.WrapperName);
                if (wrapper != null)
                {
                    if (socket != null && !IsDescendantOf(wrapper, socket))
                    {
                        note = $"{slot.WrapperName} is currently parented to {TransformPath(previewRoot.transform, wrapper.parent)}, so this slot is not visible in the current weapon/profile state. Switch profile/slot/guard state to preview it live.";
                        targetKind = "Socket (inactive visual)";
                        return socket;
                    }

                    var artPassRoot = FindPresentationVisualRoot(wrapper);
                    targetKind = artPassRoot != null ? "Visible ArtPass Visual" : "Equipment Wrapper";
                    return artPassRoot != null ? artPassRoot : wrapper;
                }

                note = $"{slot.WrapperName} is not spawned in the current preview state. The socket can still be edited, but you will not see a model move until that visual is active.";
                targetKind = "Socket (missing visual)";
            }

            return socket;
        }

        private void ApplyStoredPreviewTunings()
        {
            if (previewRoot == null || editedSlotTunings.Count == 0)
            {
                return;
            }

            foreach (var slot in EquipmentSlots)
            {
                if (!editedSlotTunings.TryGetValue(slot.Label, out var tuning))
                {
                    continue;
                }

                var socket = FindDescendant(previewRoot.transform, slot.SocketName);
                var transform = tuning.AppliesToSocket
                    ? socket
                    : ResolveEditableTransform(slot, socket, out _, out _);
                if (transform == null)
                {
                    continue;
                }

                transform.localPosition = tuning.LocalPosition;
                transform.localRotation = Quaternion.Euler(tuning.LocalEuler);
                transform.localScale = SanitizeScale(tuning.LocalScale);
            }

            RepaintPreviewViews();
        }

        private static string ProfileTabLabel(PlayerAnimationProfileId profileId)
        {
            return profileId switch
            {
                PlayerAnimationProfileId.UnarmedLocomotion => "Unarmed",
                PlayerAnimationProfileId.SwordShieldCombat => "SwordShield",
                PlayerAnimationProfileId.GreatSwordCombat => "GreatSword",
                PlayerAnimationProfileId.RifleCombat => "Rifle",
                PlayerAnimationProfileId.PistolCombat => "Pistol",
                _ => profileId.ToString()
            };
        }

        private static Vector3 SanitizeScale(Vector3 scale)
        {
            return new Vector3(
                Mathf.Approximately(scale.x, 0f) ? 1f : scale.x,
                Mathf.Approximately(scale.y, 0f) ? 1f : scale.y,
                Mathf.Approximately(scale.z, 0f) ? 1f : scale.z);
        }

        private static Transform FindPresentationVisualRoot(Transform wrapper)
        {
            if (wrapper == null)
            {
                return null;
            }

            var marker = wrapper.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                .FirstOrDefault(candidate => candidate != null);
            return marker != null ? marker.transform : null;
        }

        private static bool IsDescendantOf(Transform candidate, Transform ancestor)
        {
            if (candidate == null || ancestor == null)
            {
                return false;
            }

            var cursor = candidate;
            while (cursor != null)
            {
                if (cursor == ancestor)
                {
                    return true;
                }

                cursor = cursor.parent;
            }

            return false;
        }

        private static void RepaintPreviewViews()
        {
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private static float PositiveFloatField(string label, float value)
        {
            return Mathf.Max(0.0001f, EditorGUILayout.FloatField(label, value));
        }

        private static Vector3 DrawVector3WithNudges(string label, Vector3 value, float step)
        {
            var next = EditorGUILayout.Vector3Field(label, value);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            next = AxisNudge("X", next, 0, step);
            next = AxisNudge("Y", next, 1, step);
            next = AxisNudge("Z", next, 2, step);
            EditorGUILayout.EndHorizontal();
            return next;
        }

        private static Vector3 AxisNudge(string label, Vector3 value, int axis, float step)
        {
            if (GUILayout.Button($"{label}-", GUILayout.Width(34f)))
            {
                value[axis] -= step;
            }

            if (GUILayout.Button($"{label}+", GUILayout.Width(34f)))
            {
                value[axis] += step;
            }

            return value;
        }

        private static void MarkSceneDirtyIfSafe(Scene scene)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private static Transform FindDescendant(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            foreach (var transform in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (transform != null && string.Equals(transform.name, targetName, StringComparison.Ordinal))
                {
                    return transform;
                }
            }

            return null;
        }

        private static string TransformPath(Transform root, Transform target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            var names = new Stack<string>();
            var cursor = target;
            while (cursor != null && cursor != root)
            {
                names.Push(cursor.name);
                cursor = cursor.parent;
            }

            if (root != null)
            {
                names.Push(root.name);
            }

            return string.Join("/", names);
        }

        private enum EquipmentEditTargetMode
        {
            AutoVisualOrSocket,
            ScenarioSocket
        }

        private readonly struct EquipmentSocketTuning
        {
            public EquipmentSocketTuning(
                string label,
                string socketName,
                string wrapperName,
                EquipmentEditTargetMode editTargetMode = EquipmentEditTargetMode.AutoVisualOrSocket)
            {
                Label = label;
                SocketName = socketName;
                WrapperName = wrapperName;
                EditTargetMode = editTargetMode;
            }

            public string Label { get; }

            public string SocketName { get; }

            public string WrapperName { get; }

            public EquipmentEditTargetMode EditTargetMode { get; }
        }

        private readonly struct RefinerTransformTuning
        {
            public RefinerTransformTuning(
                Vector3 localPosition,
                Vector3 localEuler,
                Vector3 localScale,
                bool appliesToSocket)
            {
                LocalPosition = localPosition;
                LocalEuler = localEuler;
                LocalScale = localScale;
                AppliesToSocket = appliesToSocket;
            }

            public Vector3 LocalPosition { get; }

            public Vector3 LocalEuler { get; }

            public Vector3 LocalScale { get; }

            public bool AppliesToSocket { get; }
        }

        [Serializable]
        private sealed class RefinerExport
        {
            public string generatedUtc;
            public string playerPrefabPath;
            public string previewScenePath;
            public string selectedProfile;
            public string selectedWeaponSlot;
            public string selectedClip;
            public RefinerSlotExport[] slots;
        }

        [Serializable]
        private sealed class RefinerSlotExport
        {
            public string label;
            public string socketName;
            public string socketPath;
            public string editTargetKind;
            public string editTargetPath;
            public string profileContext;
            public string weaponSlotContext;
            public string clipContext;
            public string notes;
            public Vector3 localPosition;
            public Vector3 localEuler;
            public Vector3 localScale;
        }

        private struct PositionRefinerVisibilityFlags
        {
            public bool showBones;
            public bool showSockets;
            public bool showProjectileStart;
            public bool showSwordArc;
            public bool showGroundingHelpers;

            public static PositionRefinerVisibilityFlags Default => new()
            {
                showBones = false,
                showSockets = true,
                showProjectileStart = true,
                showSwordArc = true,
                showGroundingHelpers = true
            };
        }

        [Serializable]
        private sealed class PositionRefinerExport
        {
            public string generatedUtc;
            public string playerPrefabPath;
            public string previewScenePath;
            public string selectedProfile;
            public string selectedWeaponSlot;
            public string selectedClip;
            public float selectedClipTimeSeconds;
            public string masterVisualRootPath;
            public Vector3 masterVisualRootLocalPosition;
            public Vector3 masterVisualRootLocalEuler;
            public Vector3 masterVisualRootLocalScale;
            public Vector3 bodyBoundsCenter;
            public Vector3 bodyBoundsSize;
            public float bodyBoundsMinY;
            public float bodyBoundsMaxY;
            public float groundY;
            public float groundClearanceMeters;
            public float predictedGroundingOffsetY;
            public Vector3 capsuleCenter;
            public float capsuleRadius;
            public float capsuleHeight;
            public Vector3 projectileOrigin;
            public Vector3 projectileDirection;
            public PositionPointExport[] points;
            public Vector3[] swordArcSamples;
            public bool showBones;
            public bool showSockets;
            public bool showProjectileStart;
            public bool showSwordArc;
            public bool showGroundingHelpers;
        }

        [Serializable]
        private sealed class PositionPointExport
        {
            public string label;
            public string kind;
            public string path;
            public Vector3 localPosition;
            public Vector3 localEuler;
            public Vector3 localScale;
            public Vector3 worldPosition;
            public Vector3 worldEuler;
        }
    }
}
