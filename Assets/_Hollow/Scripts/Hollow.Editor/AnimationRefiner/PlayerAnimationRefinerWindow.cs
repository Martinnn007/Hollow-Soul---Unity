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
    public sealed class PlayerAnimationRefinerWindow : EditorWindow
    {
        public const string MenuPath = "Hollow/Animation/Player Animation Refiner";
        public const string PreviewScenePath = "Assets/_Hollow/Scenes/DeveloperLab/PlayerAnimationRefiner.unity";
        public const string ExportPath = "Assets/_Hollow/Data/AnimationProfiles/PlayerAnimationRefinerExport.json";
        public const string PreviewControllerPath = "Assets/_Hollow/Scenes/DeveloperLab/PlayerAnimationRefiner.controller";
        public const string PreviewPlaceholderClipPath = "Assets/_Hollow/Scenes/DeveloperLab/PlayerAnimationRefinerPlaceholder.anim";

        private const string PreviewRootName = "PlayerAnimationRefiner.PlayerPreview";
        private const string FloorName = "PlayerAnimationRefiner.FlatFloor";
        private const string CameraName = "PlayerAnimationRefiner.Camera";
        private const string LightName = "PlayerAnimationRefiner.DirectionalLight";
        private const string PreviewStateName = "PlayerAnimationRefinerPreview";
        private const string PreviewPlaceholderClipName = "PlayerAnimationRefinerPlaceholder";

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
            new("Melee Holstered", PlayerHeldWeaponVisualController.MeleeHolsterSocketName, PlayerHeldWeaponVisualController.HolsteredMeleeWeaponVisualName),
            new("Ranged In Hand", PlayerHeldWeaponVisualController.RangedHandSocketName, PlayerHeldWeaponVisualController.ActiveRangedWeaponVisualName),
            new("Ranged Holstered", PlayerHeldWeaponVisualController.RangedHolsterSocketName, PlayerHeldWeaponVisualController.HolsteredRangedWeaponVisualName),
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
            lastEditorTime = EditorApplication.timeSinceStartup;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            if (!EditorApplication.isPlayingOrWillChangePlaymode && AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
            }
        }

        private void OnGUI()
        {
            ResolveSceneReferences();
            DrawToolbar();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            try
            {
                DrawCameraControls();
                DrawProfileAndClipControls();
                DrawEquipmentControls();
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

        private void DrawExportControls()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Export Current Tuning JSON", GUILayout.Height(30f)))
            {
                var path = ExportCurrentTuning();
                Debug.Log($"Exported player animation refiner tuning: {path}");
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
            var transform = previewRoot != null ? ResolveEditableTransform(slot, socket, out targetKind, out _) : null;
            return new RefinerSlotExport
            {
                label = slot.Label,
                socketName = slot.SocketName,
                socketPath = socket != null ? TransformPath(previewRoot.transform, socket) : string.Empty,
                editTargetKind = targetKind,
                editTargetPath = transform != null ? TransformPath(previewRoot.transform, transform) : string.Empty,
                localPosition = transform != null ? transform.localPosition : Vector3.zero,
                localEuler = transform != null ? transform.localEulerAngles : Vector3.zero,
                localScale = transform != null ? transform.localScale : Vector3.one
            };
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

        private readonly struct EquipmentSocketTuning
        {
            public EquipmentSocketTuning(string label, string socketName, string wrapperName)
            {
                Label = label;
                SocketName = socketName;
                WrapperName = wrapperName;
            }

            public string Label { get; }

            public string SocketName { get; }

            public string WrapperName { get; }
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
            public Vector3 localPosition;
            public Vector3 localEuler;
            public Vector3 localScale;
        }
    }
}
