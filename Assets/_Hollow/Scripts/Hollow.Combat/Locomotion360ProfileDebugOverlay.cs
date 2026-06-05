using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class Locomotion360ProfileDebugOverlay : MonoBehaviour
    {
        [SerializeField] private PlayerAnimationProfileCatalogDefinition profileCatalog;
        [SerializeField] private PlayerAnimationProfileController profileController;
        [SerializeField] private PlayerLocomotionAnimator locomotionAnimator;
        [SerializeField] private PlayerAnimationPoseCoordinator poseCoordinator;
        [SerializeField] private PlayerFootPlacementController footPlacement;
        [SerializeField] private PlayerWeaponController weaponController;
        [SerializeField] private Rect overlayRect = new(18f, 18f, 420f, 260f);

        private readonly PlayerAnimationProfileId[] profileOrder =
        {
            PlayerAnimationProfileId.UnarmedLocomotion,
            PlayerAnimationProfileId.SwordShieldCombat,
            PlayerAnimationProfileId.GreatSwordCombat,
            PlayerAnimationProfileId.RifleCombat,
            PlayerAnimationProfileId.PistolCombat
        };

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            ResolveReferences();
            for (var index = 0; index < profileOrder.Length; index++)
            {
                if (DebugKeyboardInput.NumberWasPressed(index + 1))
                {
                    SetDebugProfile(profileOrder[index]);
                }
            }

            if (DebugKeyboardInput.NumberWasPressed(0))
            {
                profileController?.ClearDebugProfileOverride();
            }
        }

        public void Configure(
            PlayerAnimationProfileCatalogDefinition nextProfileCatalog,
            PlayerAnimationProfileController nextProfileController,
            PlayerLocomotionAnimator nextLocomotionAnimator,
            PlayerAnimationPoseCoordinator nextPoseCoordinator,
            PlayerFootPlacementController nextFootPlacement,
            PlayerWeaponController nextWeaponController)
        {
            profileCatalog = nextProfileCatalog;
            profileController = nextProfileController;
            locomotionAnimator = nextLocomotionAnimator;
            poseCoordinator = nextPoseCoordinator;
            footPlacement = nextFootPlacement;
            weaponController = nextWeaponController;
        }

        private void OnGUI()
        {
            ResolveReferences();
            GUILayout.BeginArea(overlayRect, GUI.skin.box);
            GUILayout.Label("Locomotion 360 Profile Debug");
            DrawProfileButtons();

            var profile = profileController != null ? profileController.CurrentProfile : null;
            GUILayout.Label($"Profile: {(profile != null ? profile.ProfileName : "UnarmedLocomotionProfile (safe fallback)")}");
            GUILayout.Label($"Override: {(profileController != null && profileController.IsDebugOverrideEnabled ? "On" : "Off")}");
            GUILayout.Label($"Shield In Hand: {Bool(profileController != null && profileController.AllowsShieldInHand)}  Guard: {Bool(profileController != null && profileController.AllowsShieldGuard)}");
            GUILayout.Label($"Two-Handed: {Bool(profileController != null && profileController.RequiresTwoHandedWeapon)}  Ranged Aim: {Bool(profileController != null && profileController.UsesRangedAim)}");

            if (locomotionAnimator != null)
            {
                GUILayout.Label($"Move: {locomotionAnimator.RelativeMove.x:0.00}, {locomotionAnimator.RelativeMove.y:0.00}  Speed: {locomotionAnimator.PlanarSpeedMetersPerSecond:0.00}");
                GUILayout.Label($"Angle: {locomotionAnimator.MoveAngleDegrees:0}  Turn-In-Place: {Bool(locomotionAnimator.IsTurnInPlaceActive)}");
            }

            if (poseCoordinator != null || footPlacement != null)
            {
                var left = poseCoordinator != null ? poseCoordinator.LeftFootLockWeight : footPlacement != null ? footPlacement.LeftFootLockWeight : 0f;
                var right = poseCoordinator != null ? poseCoordinator.RightFootLockWeight : footPlacement != null ? footPlacement.RightFootLockWeight : 0f;
                var pelvis = poseCoordinator != null ? poseCoordinator.PelvisOffset : footPlacement != null ? footPlacement.PelvisOffset : 0f;
                GUILayout.Label($"Foot Locks L/R: {left:0.00}/{right:0.00}  Pelvis: {pelvis:0.000}");
            }

            if (weaponController != null)
            {
                GUILayout.Label($"Weapon Slot: {weaponController.ActiveWeaponSlot}  Weapon: {weaponController.ActiveWeaponDisplayName}");
            }

            GUILayout.EndArea();
        }

        private void DrawProfileButtons()
        {
            GUILayout.BeginHorizontal();
            foreach (var profileId in profileOrder)
            {
                if (GUILayout.Button(ButtonLabel(profileId), GUILayout.Height(24f)))
                {
                    SetDebugProfile(profileId);
                }
            }

            if (GUILayout.Button("Auto", GUILayout.Height(24f)))
            {
                profileController?.ClearDebugProfileOverride();
            }

            GUILayout.EndHorizontal();
        }

        private void SetDebugProfile(PlayerAnimationProfileId profileId)
        {
            ResolveReferences();
            var profile = profileCatalog != null ? profileCatalog.Resolve(profileId) : null;
            profileController?.SetDebugProfileOverride(profile);
        }

        private void ResolveReferences()
        {
            profileController ??= FindFirstObjectByType<PlayerAnimationProfileController>();
            if (profileCatalog == null && profileController != null)
            {
                profileCatalog = profileController.Catalog;
            }

            locomotionAnimator ??= FindFirstObjectByType<PlayerLocomotionAnimator>();
            poseCoordinator ??= FindFirstObjectByType<PlayerAnimationPoseCoordinator>();
            footPlacement ??= FindFirstObjectByType<PlayerFootPlacementController>();
            weaponController ??= FindFirstObjectByType<PlayerWeaponController>();
        }

        private static string ButtonLabel(PlayerAnimationProfileId profileId)
        {
            return profileId switch
            {
                PlayerAnimationProfileId.UnarmedLocomotion => "1 Unarmed",
                PlayerAnimationProfileId.SwordShieldCombat => "2 Sword",
                PlayerAnimationProfileId.GreatSwordCombat => "3 Great",
                PlayerAnimationProfileId.RifleCombat => "4 Rifle",
                PlayerAnimationProfileId.PistolCombat => "5 Pistol",
                _ => profileId.ToString()
            };
        }

        private static string Bool(bool value)
        {
            return value ? "Yes" : "No";
        }
    }
}
