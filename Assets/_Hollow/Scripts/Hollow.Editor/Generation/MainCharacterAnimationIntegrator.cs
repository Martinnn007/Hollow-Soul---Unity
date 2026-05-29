using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class MainCharacterAnimationIntegrator
    {
        private const string IdleFbxPath = "Assets/MeshyImports/MainCharacter_001/Idle_11_20260506_124917/Meshy_AI_Grey_Sentinel_biped_Animation_Idle_11_withSkin.fbx";
        private const string WalkFbxPath = "Assets/MeshyImports/MainCharacter_001/Walking_20260506_124626/Meshy_AI_Grey_Sentinel_biped_Animation_Walking_withSkin.fbx";
        private const string RunFbxPath = "Assets/MeshyImports/Running_20260506_131917/Meshy_AI_Grey_Sentinel_biped_Animation_Running_withSkin.fbx";
        private const string RollFbxPath = "Assets/MeshyImports/Roll_Dodge_1_20260506_131201/Meshy_AI_Grey_Sentinel_biped_Animation_Roll_Dodge_1_withSkin.fbx";
        private const string SlashFbxPath = "Assets/MeshyImports/Left_Slash_20260506_131213/Meshy_AI_Grey_Sentinel_biped_Animation_Left_Slash_withSkin.fbx";
        private const string HitFbxPath = "Assets/MeshyImports/Hit_Reaction_1_20260506_131232/Meshy_AI_Grey_Sentinel_biped_Animation_Hit_Reaction_1_withSkin.fbx";
        private const string DeadFbxPath = "Assets/MeshyImports/Dead_20260506_131241/Meshy_AI_Grey_Sentinel_biped_Animation_Dead_withSkin.fbx";
        private const string IdleMaterialPath = "Assets/MeshyImports/MainCharacter_001/Idle_11_20260506_124917/Material_1.mat";
        private const string AlbedoTexturePath = "Assets/MeshyImports/MainCharacter_001/Idle_11_20260506_124917/Meshy_AI_Grey_Sentinel_biped_texture_0.png";
        private const string NormalTexturePath = "Assets/MeshyImports/MainCharacter_001/Idle_11_20260506_124917/Meshy_AI_Grey_Sentinel_biped_texture_0_normal.png";
        private const string MetallicTexturePath = "Assets/MeshyImports/MainCharacter_001/Idle_11_20260506_124917/Meshy_AI_Grey_Sentinel_biped_texture_0_metallic.png";
        private const string PlayerPrefabPath = "Assets/_Hollow/Prefabs/Player/PlayerCharacter.prefab";
        private const string PlayerControllerPath = "Assets/_Hollow/Art/Models/Characters/Player/MainCharacter_Player.controller";
        private const string RollInPlaceClipPath = "Assets/_Hollow/Art/Models/Characters/Player/MainCharacter_Roll_InPlace.anim";
        private const string CanonicalMaterialPath = "Assets/_Hollow/Art/Materials/ArtPass/AP_M_MainCharacter_GreySentinel.mat";
        private const string IdleClipName = "MainCharacter_Idle";
        private const string WalkClipName = "MainCharacter_Walk";
        private const string RunClipName = "MainCharacter_Run";
        private const string RollClipName = "MainCharacter_Roll";
        private const string SlashClipName = "MainCharacter_LeftSlash";
        private const string HitClipName = "MainCharacter_HitReaction";
        private const string DeadClipName = "MainCharacter_Dead";
        private const float RunStartMoveSpeedThreshold = 0.5f;
        private const float RunTransitionThreshold = RunStartMoveSpeedThreshold - 0.001f;
        private const float RollRootDriftStripThresholdMeters = 0.25f;
        private const string VisualRootName = "MainCharacter_VisualRoot";
        private const string ModelInstanceName = "MainCharacter_MeshyModel";
        private const string LegacyCapsuleName = "PlayerHeight_1_78m";
        private const string RightHandBoneName = "RightHand";
        private static readonly HashSet<string> RollRootLikeBindingNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Armature",
            "Hips",
            "Pelvis",
            "Root",
            "RootNode"
        };

        [MenuItem("Hollow/Art/Integrate Meshy Main Character")]
        public static void Integrate()
        {
            var idleClip = ConfigureAnimationImport(IdleFbxPath, IdleClipName, loop: true);
            var walkClip = ConfigureAnimationImport(WalkFbxPath, WalkClipName, loop: true);
            var runClip = ConfigureAnimationImport(RunFbxPath, RunClipName, loop: true);
            var importedRollClip = ConfigureAnimationImport(RollFbxPath, RollClipName, loop: false);
            var rollClip = CreateOrUpdateInPlaceRollClip(importedRollClip);
            var slashClip = ConfigureAnimationImport(SlashFbxPath, SlashClipName, loop: false);
            var hitClip = ConfigureAnimationImport(HitFbxPath, HitClipName, loop: false);
            var deadClip = ConfigureAnimationImport(DeadFbxPath, DeadClipName, loop: false);
            var material = CreateOrUpdateCanonicalMaterial();
            var controller = CreateAnimatorController(idleClip, walkClip, runClip, rollClip, slashClip, hitClip, deadClip);
            UpdatePlayerPrefab(controller, material, rollClip, slashClip, hitClip, deadClip);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Integrated Meshy main character idle/walk/run/action animations into PlayerCharacter.prefab.");
        }

        private static AnimationClip ConfigureAnimationImport(string path, string clipName, bool loop)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Missing Meshy animation FBX: {path}", path);
            }

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Asset is not a model import: {path}");
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.animationWrapMode = loop ? WrapMode.Loop : WrapMode.Once;

            var defaultClips = importer.defaultClipAnimations;
            if (defaultClips == null || defaultClips.Length == 0)
            {
                throw new InvalidOperationException($"Meshy FBX has no animation takes to configure: {path}");
            }

            var clip = defaultClips[0];
            clip.name = clipName;
            clip.loopTime = loop;
            clip.loopPose = loop;
            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = false;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = false;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalPositionXZ = false;

            importer.clipAnimations = new[] { clip };
            importer.SaveAndReimport();

            var importedClip = AssetDatabase
                .LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(asset => string.Equals(asset.name, clipName, StringComparison.Ordinal));
            if (importedClip == null)
            {
                throw new InvalidOperationException($"Failed to load configured animation clip {clipName} from {path}.");
            }

            return importedClip;
        }

        private static AnimationClip CreateOrUpdateInPlaceRollClip(AnimationClip sourceClip)
        {
            if (sourceClip == null)
            {
                throw new InvalidOperationException("Cannot generate an in-place roll clip from a null source clip.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(RollInPlaceClipPath) ?? string.Empty);
            var inPlaceClip = Object.Instantiate(sourceClip);
            inPlaceClip.name = RollClipName;
            var settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
            settings.loopTime = false;
            settings.loopBlend = false;
            settings.loopBlendPositionXZ = false;
            AnimationUtility.SetAnimationClipSettings(inPlaceClip, settings);

            var strippedBindings = new List<string>();
            foreach (var binding in AnimationUtility.GetCurveBindings(inPlaceClip))
            {
                if (!IsRootLikeRollPositionBinding(binding))
                {
                    continue;
                }

                var curve = AnimationUtility.GetEditorCurve(inPlaceClip, binding);
                if (curve == null || curve.length == 0)
                {
                    continue;
                }

                if (!HasSignificantRollDrift(curve))
                {
                    continue;
                }

                var initialValue = curve.Evaluate(0f);
                var lockedCurve = AnimationCurve.Constant(0f, Mathf.Max(0.01f, sourceClip.length), initialValue);
                AnimationUtility.SetEditorCurve(inPlaceClip, binding, lockedCurve);
                strippedBindings.Add($"{binding.path}.{binding.propertyName}");
            }

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(RollInPlaceClipPath) != null)
            {
                AssetDatabase.DeleteAsset(RollInPlaceClipPath);
            }

            AssetDatabase.CreateAsset(inPlaceClip, RollInPlaceClipPath);
            AssetDatabase.ImportAsset(RollInPlaceClipPath);
            var asset = AssetDatabase.LoadAssetAtPath<AnimationClip>(RollInPlaceClipPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Failed to create in-place roll clip at {RollInPlaceClipPath}.");
            }

            asset.name = RollClipName;
            EditorUtility.SetDirty(asset);
            if (strippedBindings.Count > 0)
            {
                Debug.Log($"Generated in-place roll by locking {strippedBindings.Count} drifting root-like curves: {string.Join(", ", strippedBindings)}");
            }

            return asset;
        }

        private static bool IsRootLikeRollPositionBinding(EditorCurveBinding binding)
        {
            if (binding.type != typeof(Transform))
            {
                return false;
            }

            if (binding.propertyName != "m_LocalPosition.x" &&
                binding.propertyName != "m_LocalPosition.y" &&
                binding.propertyName != "m_LocalPosition.z" &&
                binding.propertyName != "localPosition.x" &&
                binding.propertyName != "localPosition.y" &&
                binding.propertyName != "localPosition.z")
            {
                return false;
            }

            if (string.IsNullOrEmpty(binding.path))
            {
                return true;
            }

            var segments = binding.path.Split('/');
            var segment = segments[^1];
            var normalized = segment;
            var namespaceSeparator = normalized.LastIndexOf(':');
            if (namespaceSeparator >= 0 && namespaceSeparator < normalized.Length - 1)
            {
                normalized = normalized[(namespaceSeparator + 1)..];
            }

            return RollRootLikeBindingNames.Contains(normalized);
        }

        private static bool HasSignificantRollDrift(AnimationCurve curve)
        {
            if (curve == null || curve.length <= 1)
            {
                return false;
            }

            var first = curve.keys[0].value;
            var last = curve.keys[^1].value;
            var minimum = first;
            var maximum = first;
            foreach (var key in curve.keys)
            {
                minimum = Mathf.Min(minimum, key.value);
                maximum = Mathf.Max(maximum, key.value);
            }

            var range = maximum - minimum;
            var drift = Mathf.Abs(last - first);
            return Mathf.Max(range, drift) > RollRootDriftStripThresholdMeters;
        }

        private static Material CreateOrUpdateCanonicalMaterial()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CanonicalMaterialPath) ?? string.Empty);
            var sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(IdleMaterialPath);
            if (sourceMaterial == null)
            {
                throw new InvalidOperationException($"Missing Meshy source material: {IdleMaterialPath}");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(CanonicalMaterialPath);
            if (material == null)
            {
                material = new Material(sourceMaterial);
                AssetDatabase.CreateAsset(material, CanonicalMaterialPath);
            }
            else
            {
                EditorUtility.CopySerialized(sourceMaterial, material);
            }

            material.name = Path.GetFileNameWithoutExtension(CanonicalMaterialPath);
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoTexturePath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTexturePath);
            var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicTexturePath);
            AssignTexture(material, "_BaseMap", albedo);
            AssignTexture(material, "_MainTex", albedo);
            AssignTexture(material, "_BumpMap", normal);
            AssignTexture(material, "_MetallicGlossMap", metallic);
            if (normal != null)
            {
                material.EnableKeyword("_NORMALMAP");
                if (material.HasProperty("_BumpScale"))
                {
                    material.SetFloat("_BumpScale", 0.5f);
                }
            }

            if (metallic != null)
            {
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            material.DisableKeyword("_EMISSION");
            ClearTexture(material, "_EmissionMap");
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", Color.black);
            }

            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AssignTexture(Material material, string propertyName, Texture texture)
        {
            if (material != null && texture != null && material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void ClearTexture(Material material, string propertyName)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, null);
            }
        }

        private static AnimatorController CreateAnimatorController(
            AnimationClip idleClip,
            AnimationClip walkClip,
            AnimationClip runClip,
            AnimationClip rollClip,
            AnimationClip slashClip,
            AnimationClip hitClip,
            AnimationClip deadClip)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PlayerControllerPath) ?? string.Empty);
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(PlayerControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerControllerPath);
            controller.AddParameter(PlayerLocomotionAnimator.IsMovingParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(PlayerLocomotionAnimator.MoveSpeedParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(PlayerLocomotionAnimator.ActionSpeedParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(PlayerLocomotionAnimator.RollTriggerParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(PlayerLocomotionAnimator.SlashTriggerParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(PlayerLocomotionAnimator.HitTriggerParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(PlayerLocomotionAnimator.DeathTriggerParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(PlayerLocomotionAnimator.IsDeadParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(PlayerLocomotionAnimator.IsTargetLockedParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(PlayerLocomotionAnimator.LockedMoveXParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(PlayerLocomotionAnimator.LockedMoveYParameter, AnimatorControllerParameterType.Float);

            var stateMachine = controller.layers[0].stateMachine;
            var idleState = AddState(stateMachine, "Idle", idleClip, new Vector3(220f, 120f, 0f));
            stateMachine.defaultState = idleState;
            var walkState = AddState(stateMachine, "Walk", walkClip, new Vector3(500f, 120f, 0f));
            var runState = AddState(stateMachine, "Run", runClip, new Vector3(780f, 120f, 0f));
            var lockedState = AddState(
                stateMachine,
                "LockedLocomotion",
                CreateLockedLocomotionBlendTree(controller, idleClip, walkClip, runClip),
                new Vector3(500f, -80f, 0f));
            var rollState = AddActionState(stateMachine, "Roll", rollClip, new Vector3(220f, 320f, 0f));
            var slashState = AddActionState(stateMachine, "LeftSlash", slashClip, new Vector3(500f, 320f, 0f));
            var hitState = AddActionState(stateMachine, "HitReaction", hitClip, new Vector3(780f, 320f, 0f));
            var deadState = AddState(stateMachine, "Dead", deadClip, new Vector3(1060f, 120f, 0f));

            AddLocomotionTransitions(idleState, walkState, runState);
            AddLockedLocomotionTransitions(idleState, walkState, runState, lockedState);
            AddAnyStateTriggerTransition(stateMachine, deadState, PlayerLocomotionAnimator.DeathTriggerParameter, allowWhileDead: true, duration: 0.04f);
            AddAnyStateBoolTransition(stateMachine, deadState, PlayerLocomotionAnimator.IsDeadParameter, duration: 0.04f);
            AddAnyStateTriggerTransition(stateMachine, hitState, PlayerLocomotionAnimator.HitTriggerParameter, allowWhileDead: false, duration: 0.04f);
            AddAnyStateTriggerTransition(stateMachine, rollState, PlayerLocomotionAnimator.RollTriggerParameter, allowWhileDead: false, duration: 0.05f);
            AddAnyStateTriggerTransition(stateMachine, slashState, PlayerLocomotionAnimator.SlashTriggerParameter, allowWhileDead: false, duration: 0.04f);
            AddActionExitTransitions(rollState, idleState, walkState, runState, lockedState);
            AddActionExitTransitions(slashState, idleState, walkState, runState, lockedState);
            AddActionExitTransitions(hitState, idleState, walkState, runState, lockedState);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static BlendTree CreateLockedLocomotionBlendTree(
            AnimatorController controller,
            Motion idleClip,
            Motion walkClip,
            Motion runClip)
        {
            var tree = new BlendTree
            {
                name = "LockedLocomotionBlendTree",
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = PlayerLocomotionAnimator.LockedMoveXParameter,
                blendParameterY = PlayerLocomotionAnimator.LockedMoveYParameter,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, controller);

            tree.AddChild(idleClip, Vector2.zero);
            AddDirectionalChildren(tree, walkClip, 0.45f);
            AddDirectionalChildren(tree, runClip, 1f);
            return tree;
        }

        private static void AddDirectionalChildren(BlendTree tree, Motion motion, float magnitude)
        {
            tree.AddChild(motion, new Vector2(0f, magnitude));
            tree.AddChild(motion, new Vector2(0f, -magnitude));
            tree.AddChild(motion, new Vector2(magnitude, 0f));
            tree.AddChild(motion, new Vector2(-magnitude, 0f));

            var diagonal = magnitude * 0.70710677f;
            tree.AddChild(motion, new Vector2(diagonal, diagonal));
            tree.AddChild(motion, new Vector2(-diagonal, diagonal));
            tree.AddChild(motion, new Vector2(diagonal, -diagonal));
            tree.AddChild(motion, new Vector2(-diagonal, -diagonal));
        }

        private static AnimatorState AddState(AnimatorStateMachine stateMachine, string name, Motion motion, Vector3 position)
        {
            var state = stateMachine.AddState(name, position);
            state.motion = motion;
            state.writeDefaultValues = true;
            return state;
        }

        private static AnimatorState AddActionState(AnimatorStateMachine stateMachine, string name, Motion motion, Vector3 position)
        {
            var state = AddState(stateMachine, name, motion, position);
            state.speedParameter = PlayerLocomotionAnimator.ActionSpeedParameter;
            state.speedParameterActive = true;
            return state;
        }

        private static void AddLocomotionTransitions(AnimatorState idleState, AnimatorState walkState, AnimatorState runState)
        {
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.1f;
            idleToWalk.offset = 0f;
            idleToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            idleToWalk.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            idleToWalk.AddCondition(AnimatorConditionMode.Less, RunStartMoveSpeedThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);

            var idleToRun = idleState.AddTransition(runState);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.1f;
            idleToRun.offset = 0f;
            idleToRun.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            idleToRun.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            idleToRun.AddCondition(AnimatorConditionMode.Greater, RunTransitionThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);

            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.12f;
            walkToIdle.offset = 0f;
            walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsMovingParameter);

            var walkToRun = walkState.AddTransition(runState);
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.1f;
            walkToRun.offset = 0f;
            walkToRun.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            walkToRun.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, RunTransitionThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.12f;
            runToIdle.offset = 0f;
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsMovingParameter);

            var runToWalk = runState.AddTransition(walkState);
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.12f;
            runToWalk.offset = 0f;
            runToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            runToWalk.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            runToWalk.AddCondition(AnimatorConditionMode.Less, RunStartMoveSpeedThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);
        }

        private static void AddLockedLocomotionTransitions(
            AnimatorState idleState,
            AnimatorState walkState,
            AnimatorState runState,
            AnimatorState lockedState)
        {
            AddToLockedTransition(idleState, lockedState, 0.08f);
            AddToLockedTransition(walkState, lockedState, 0.08f);
            AddToLockedTransition(runState, lockedState, 0.08f);

            var lockedToIdle = lockedState.AddTransition(idleState);
            lockedToIdle.hasExitTime = false;
            lockedToIdle.duration = 0.1f;
            lockedToIdle.offset = 0f;
            lockedToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            lockedToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsMovingParameter);

            var lockedToWalk = lockedState.AddTransition(walkState);
            lockedToWalk.hasExitTime = false;
            lockedToWalk.duration = 0.1f;
            lockedToWalk.offset = 0f;
            lockedToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            lockedToWalk.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            lockedToWalk.AddCondition(AnimatorConditionMode.Less, RunStartMoveSpeedThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);

            var lockedToRun = lockedState.AddTransition(runState);
            lockedToRun.hasExitTime = false;
            lockedToRun.duration = 0.1f;
            lockedToRun.offset = 0f;
            lockedToRun.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            lockedToRun.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            lockedToRun.AddCondition(AnimatorConditionMode.Greater, RunTransitionThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);
        }

        private static void AddToLockedTransition(AnimatorState fromState, AnimatorState lockedState, float duration)
        {
            var transition = fromState.AddTransition(lockedState);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.offset = 0f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);
        }

        private static void AddAnyStateTriggerTransition(
            AnimatorStateMachine stateMachine,
            AnimatorState targetState,
            string triggerParameter,
            bool allowWhileDead,
            float duration)
        {
            var transition = stateMachine.AddAnyStateTransition(targetState);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.offset = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, triggerParameter);
            if (!allowWhileDead)
            {
                transition.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);
            }
        }

        private static void AddAnyStateBoolTransition(
            AnimatorStateMachine stateMachine,
            AnimatorState targetState,
            string boolParameter,
            float duration)
        {
            var transition = stateMachine.AddAnyStateTransition(targetState);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.offset = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, boolParameter);
        }

        private static void AddActionExitTransitions(
            AnimatorState actionState,
            AnimatorState idleState,
            AnimatorState walkState,
            AnimatorState runState,
            AnimatorState lockedState)
        {
            var toLocked = actionState.AddTransition(lockedState);
            toLocked.hasExitTime = true;
            toLocked.exitTime = 0.92f;
            toLocked.duration = 0.08f;
            toLocked.offset = 0f;
            toLocked.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            toLocked.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);

            var toRun = actionState.AddTransition(runState);
            toRun.hasExitTime = true;
            toRun.exitTime = 0.92f;
            toRun.duration = 0.08f;
            toRun.offset = 0f;
            toRun.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            toRun.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            toRun.AddCondition(AnimatorConditionMode.Greater, RunTransitionThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);
            toRun.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);

            var toWalk = actionState.AddTransition(walkState);
            toWalk.hasExitTime = true;
            toWalk.exitTime = 0.92f;
            toWalk.duration = 0.08f;
            toWalk.offset = 0f;
            toWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            toWalk.AddCondition(AnimatorConditionMode.Less, RunStartMoveSpeedThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);
            toWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);

            var toIdle = actionState.AddTransition(idleState);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 0.92f;
            toIdle.duration = 0.08f;
            toIdle.offset = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);
        }

        private static void UpdatePlayerPrefab(
            RuntimeAnimatorController controller,
            Material canonicalMaterial,
            AnimationClip rollClip,
            AnimationClip slashClip,
            AnimationClip hitClip,
            AnimationClip deadClip)
        {
            if (!File.Exists(PlayerPrefabPath))
            {
                throw new FileNotFoundException($"Missing player prefab: {PlayerPrefabPath}", PlayerPrefabPath);
            }

            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IdleFbxPath);
            if (modelPrefab == null)
            {
                throw new InvalidOperationException($"Could not load Meshy model prefab from {IdleFbxPath}.");
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                RemoveExistingVisuals(prefabRoot.transform);

                var visualRoot = new GameObject(VisualRootName);
                visualRoot.transform.SetParent(prefabRoot.transform, false);
                visualRoot.transform.localPosition = Vector3.zero;
                visualRoot.transform.localRotation = Quaternion.identity;
                visualRoot.transform.localScale = Vector3.one;

                var modelInstance = PrefabUtility.InstantiatePrefab(modelPrefab, visualRoot.transform) as GameObject;
                if (modelInstance == null)
                {
                    modelInstance = Object.Instantiate(modelPrefab, visualRoot.transform);
                }

                modelInstance.name = ModelInstanceName;
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one;

                StripGameplayComponentsFromVisual(modelInstance);
                EnsureRendererMaterials(modelInstance, canonicalMaterial);

                var animator = modelInstance.GetComponent<Animator>();
                var sourceAnimator = modelPrefab.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = modelInstance.AddComponent<Animator>();
                }

                if (sourceAnimator != null && sourceAnimator.avatar != null)
                {
                    animator.avatar = sourceAnimator.avatar;
                }

                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var meleeHandSocket = EnsureMeleeHandSocket(modelInstance.transform);
                var rangedHandSocket = EnsureSocket(
                    visualRoot.transform,
                    PlayerHeldWeaponVisualController.RangedHandSocketName,
                    PlayerHeldWeaponVisualController.DefaultRangedHandSocketLocalPosition,
                    PlayerHeldWeaponVisualController.DefaultRangedHandSocketLocalEuler,
                    PlayerHeldWeaponVisualController.DefaultRangedHandSocketLocalScale);
                var meleeHolsterSocket = EnsureSocket(
                    visualRoot.transform,
                    PlayerHeldWeaponVisualController.MeleeHolsterSocketName,
                    PlayerHeldWeaponVisualController.DefaultMeleeHolsterSocketLocalPosition,
                    PlayerHeldWeaponVisualController.DefaultMeleeHolsterSocketLocalEuler,
                    PlayerHeldWeaponVisualController.DefaultMeleeHolsterSocketLocalScale);
                var rangedHolsterSocket = EnsureSocket(
                    visualRoot.transform,
                    PlayerHeldWeaponVisualController.RangedHolsterSocketName,
                    PlayerHeldWeaponVisualController.DefaultRangedHolsterSocketLocalPosition,
                    PlayerHeldWeaponVisualController.DefaultRangedHolsterSocketLocalEuler,
                    PlayerHeldWeaponVisualController.DefaultRangedHolsterSocketLocalScale);
                var rangedMuzzleSocket = EnsureSocket(
                    rangedHandSocket,
                    PlayerHeldWeaponVisualController.RangedMuzzleSocketName,
                    PlayerWeaponVisualPosePolicy.MuzzleLocalPosition(),
                    Vector3.zero,
                    Vector3.one);

                var locomotionAnimator = prefabRoot.GetComponent<PlayerLocomotionAnimator>() ??
                    prefabRoot.AddComponent<PlayerLocomotionAnimator>();
                var aimLockController = prefabRoot.GetComponent<PlayerAimLockController>() ??
                    prefabRoot.AddComponent<PlayerAimLockController>();
                locomotionAnimator.Bind(animator, visualRoot.transform);
                locomotionAnimator.BindGameplay(
                    prefabRoot.GetComponent<PlayerWeaponController>(),
                    prefabRoot.GetComponent<CombatantHealth>(),
                    aimLockController);
                locomotionAnimator.Configure(0.05f, 720f, PlayerMovementController.DefaultSpeedMetersPerSecond, 1.5f);
                locomotionAnimator.ConfigureActionClips(
                    rollClip != null ? rollClip.length : PlayerWeaponController.RollDurationSeconds,
                    slashClip != null ? slashClip.length : 0.75f,
                    hitClip != null ? hitClip.length : 0.45f,
                    deadClip != null ? deadClip.length : 1.1f);
                EditorUtility.SetDirty(aimLockController);

                var heldWeaponVisual = prefabRoot.GetComponent<PlayerHeldWeaponVisualController>() ??
                    prefabRoot.AddComponent<PlayerHeldWeaponVisualController>();
                heldWeaponVisual.BindWeaponSockets(
                    meleeHandSocket,
                    rangedHandSocket,
                    meleeHolsterSocket,
                    rangedHolsterSocket,
                    rangedMuzzleSocket);
                EditorUtility.SetDirty(heldWeaponVisual);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void RemoveExistingVisuals(Transform root)
        {
            var targets = new List<GameObject>();
            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (child.name == LegacyCapsuleName || child.name == VisualRootName || child.name == ModelInstanceName)
                {
                    targets.Add(child.gameObject);
                    continue;
                }

                var marker = child.GetComponentInChildren<PresentationVisualMarker>(includeInactive: true);
                if (marker != null && marker.Role == PresentationPrefabRole.Player)
                {
                    targets.Add(child.gameObject);
                }
            }

            foreach (var target in targets.Distinct())
            {
                Object.DestroyImmediate(target);
            }
        }

        private static void StripGameplayComponentsFromVisual(GameObject visual)
        {
            foreach (var component in visual.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Object.DestroyImmediate(component);
            }

            foreach (var component in visual.GetComponentsInChildren<Rigidbody>(includeInactive: true))
            {
                Object.DestroyImmediate(component);
            }

            foreach (var component in visual.GetComponentsInChildren<Camera>(includeInactive: true))
            {
                Object.DestroyImmediate(component.gameObject);
            }

            foreach (var component in visual.GetComponentsInChildren<Light>(includeInactive: true))
            {
                Object.DestroyImmediate(component.gameObject);
            }

            foreach (var component in visual.GetComponentsInChildren<Animation>(includeInactive: true))
            {
                Object.DestroyImmediate(component);
            }
        }

        private static Transform EnsureMeleeHandSocket(Transform modelRoot)
        {
            var rightHand = FindDescendant(modelRoot, RightHandBoneName);
            if (rightHand == null)
            {
                throw new InvalidOperationException($"Meshy main character model does not contain a {RightHandBoneName} bone.");
            }

            var socket = rightHand.Find(PlayerHeldWeaponVisualController.MeleeHandSocketName);
            if (socket == null)
            {
                var socketObject = new GameObject(PlayerHeldWeaponVisualController.MeleeHandSocketName);
                socketObject.transform.SetParent(rightHand, false);
                socket = socketObject.transform;
            }

            socket.localPosition = PlayerHeldWeaponVisualController.DefaultMeleeSocketLocalPosition;
            socket.localRotation = Quaternion.Euler(PlayerHeldWeaponVisualController.DefaultMeleeSocketLocalEuler);
            socket.localScale = PlayerHeldWeaponVisualController.DefaultMeleeSocketLocalScale;
            return socket;
        }

        private static Transform EnsureSocket(
            Transform parent,
            string socketName,
            Vector3 localPosition,
            Vector3 localEuler,
            Vector3 localScale)
        {
            if (parent == null)
            {
                throw new InvalidOperationException($"Cannot create {socketName} without a parent transform.");
            }

            var socket = parent.Find(socketName);
            if (socket == null)
            {
                var socketObject = new GameObject(socketName);
                socketObject.transform.SetParent(parent, false);
                socket = socketObject.transform;
            }

            socket.localPosition = localPosition;
            socket.localRotation = Quaternion.Euler(localEuler);
            socket.localScale = localScale;
            return socket;
        }

        private static Transform FindDescendant(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static void EnsureRendererMaterials(GameObject visual, Material material)
        {
            if (material == null)
            {
                return;
            }

            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterials = new[] { material };
                    continue;
                }

                for (var index = 0; index < materials.Length; index++)
                {
                    materials[index] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }
    }
}
