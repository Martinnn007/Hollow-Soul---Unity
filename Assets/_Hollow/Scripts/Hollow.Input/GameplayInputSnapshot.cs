using UnityEngine;

namespace Hollow.Input
{
    public readonly struct GameplayInputSnapshot
    {
        public GameplayInputSnapshot(Vector2 move, Vector2 shoot)
            : this(move, shoot, false)
        {
        }

        public GameplayInputSnapshot(Vector2 move, Vector2 shoot, bool interactPressed)
            : this(move, shoot, interactPressed, false, false, false)
        {
        }

        public GameplayInputSnapshot(
            Vector2 move,
            Vector2 shoot,
            bool interactPressed,
            bool swapWeaponPressed,
            bool lightAttackPressed,
            bool heavyAttackPressed)
            : this(move, shoot, interactPressed, swapWeaponPressed, lightAttackPressed, heavyAttackPressed, false, false)
        {
        }

        public GameplayInputSnapshot(
            Vector2 move,
            Vector2 shoot,
            bool interactPressed,
            bool swapWeaponPressed,
            bool lightAttackPressed,
            bool heavyAttackPressed,
            bool useActiveItemPressed,
            bool useConsumableCardPressed)
            : this(move, shoot, interactPressed, swapWeaponPressed, lightAttackPressed, heavyAttackPressed, useActiveItemPressed, useConsumableCardPressed, false)
        {
        }

        public GameplayInputSnapshot(
            Vector2 move,
            Vector2 shoot,
            bool interactPressed,
            bool swapWeaponPressed,
            bool lightAttackPressed,
            bool heavyAttackPressed,
            bool useActiveItemPressed,
            bool useConsumableCardPressed,
            bool guardHeld)
            : this(
                move,
                shoot,
                interactPressed,
                swapWeaponPressed,
                lightAttackPressed,
                heavyAttackPressed,
                useActiveItemPressed,
                useConsumableCardPressed,
                guardHeld,
                false)
        {
        }

        public GameplayInputSnapshot(
            Vector2 move,
            Vector2 shoot,
            bool interactPressed,
            bool swapWeaponPressed,
            bool lightAttackPressed,
            bool heavyAttackPressed,
            bool useActiveItemPressed,
            bool useConsumableCardPressed,
            bool guardHeld,
            bool pausePressed)
            : this(
                move,
                shoot,
                interactPressed,
                swapWeaponPressed,
                lightAttackPressed,
                heavyAttackPressed,
                useActiveItemPressed,
                useConsumableCardPressed,
                guardHeld,
                pausePressed,
                false)
        {
        }

        public GameplayInputSnapshot(
            Vector2 move,
            Vector2 shoot,
            bool interactPressed,
            bool swapWeaponPressed,
            bool lightAttackPressed,
            bool heavyAttackPressed,
            bool useActiveItemPressed,
            bool useConsumableCardPressed,
            bool guardHeld,
            bool pausePressed,
            bool rollPressed)
            : this(
                move,
                shoot,
                interactPressed,
                swapWeaponPressed,
                lightAttackPressed,
                heavyAttackPressed,
                useActiveItemPressed,
                useConsumableCardPressed,
                guardHeld,
                pausePressed,
                rollPressed,
                false)
        {
        }

        public GameplayInputSnapshot(
            Vector2 move,
            Vector2 shoot,
            bool interactPressed,
            bool swapWeaponPressed,
            bool lightAttackPressed,
            bool heavyAttackPressed,
            bool useActiveItemPressed,
            bool useConsumableCardPressed,
            bool guardHeld,
            bool pausePressed,
            bool rollPressed,
            bool lockTargetPressed)
            : this(
                move,
                shoot,
                interactPressed,
                swapWeaponPressed,
                lightAttackPressed,
                heavyAttackPressed,
                useActiveItemPressed,
                useConsumableCardPressed,
                guardHeld,
                pausePressed,
                rollPressed,
                lockTargetPressed,
                Vector2.zero,
                false)
        {
        }

        public GameplayInputSnapshot(
            Vector2 move,
            Vector2 shoot,
            bool interactPressed,
            bool swapWeaponPressed,
            bool lightAttackPressed,
            bool heavyAttackPressed,
            bool useActiveItemPressed,
            bool useConsumableCardPressed,
            bool guardHeld,
            bool pausePressed,
            bool rollPressed,
            bool lockTargetPressed,
            Vector2 pointerScreenPosition,
            bool hasPointerScreenPosition)
            : this(
                move,
                shoot,
                interactPressed,
                swapWeaponPressed,
                lightAttackPressed,
                heavyAttackPressed,
                useActiveItemPressed,
                useConsumableCardPressed,
                guardHeld,
                pausePressed,
                rollPressed,
                lockTargetPressed,
                pointerScreenPosition,
                hasPointerScreenPosition,
                false)
        {
        }

        public GameplayInputSnapshot(
            Vector2 move,
            Vector2 shoot,
            bool interactPressed,
            bool swapWeaponPressed,
            bool lightAttackPressed,
            bool heavyAttackPressed,
            bool useActiveItemPressed,
            bool useConsumableCardPressed,
            bool guardHeld,
            bool pausePressed,
            bool rollPressed,
            bool lockTargetPressed,
            Vector2 pointerScreenPosition,
            bool hasPointerScreenPosition,
            bool mouseAimIntent)
            : this(
                move,
                shoot,
                interactPressed,
                swapWeaponPressed,
                lightAttackPressed,
                heavyAttackPressed,
                useActiveItemPressed,
                useConsumableCardPressed,
                guardHeld,
                pausePressed,
                rollPressed,
                lockTargetPressed,
                pointerScreenPosition,
                hasPointerScreenPosition,
                mouseAimIntent,
                lightAttackPressed,
                false,
                heavyAttackPressed,
                false)
        {
        }

        public GameplayInputSnapshot(
            Vector2 move,
            Vector2 shoot,
            bool interactPressed,
            bool swapWeaponPressed,
            bool lightAttackPressed,
            bool heavyAttackPressed,
            bool useActiveItemPressed,
            bool useConsumableCardPressed,
            bool guardHeld,
            bool pausePressed,
            bool rollPressed,
            bool lockTargetPressed,
            Vector2 pointerScreenPosition,
            bool hasPointerScreenPosition,
            bool mouseAimIntent,
            bool lightAttackHeld,
            bool lightAttackReleased,
            bool heavyAttackHeld,
            bool heavyAttackReleased)
        {
            Move = Vector2.ClampMagnitude(move, 1f);
            Shoot = GameplayInputReader.NormalizeAimDirection(shoot);
            InteractPressed = interactPressed;
            SwapWeaponPressed = swapWeaponPressed;
            LightAttackPressed = lightAttackPressed;
            HeavyAttackPressed = heavyAttackPressed;
            UseActiveItemPressed = useActiveItemPressed;
            UseConsumableCardPressed = useConsumableCardPressed;
            GuardHeld = guardHeld;
            PausePressed = pausePressed;
            RollPressed = rollPressed;
            LockTargetPressed = lockTargetPressed;
            PointerScreenPosition = pointerScreenPosition;
            HasPointerScreenPosition = hasPointerScreenPosition;
            MouseAimIntent = mouseAimIntent;
            LightAttackHeld = lightAttackHeld;
            LightAttackReleased = lightAttackReleased;
            HeavyAttackHeld = heavyAttackHeld;
            HeavyAttackReleased = heavyAttackReleased;
        }

        public Vector2 Move { get; }

        public Vector2 Shoot { get; }

        public bool InteractPressed { get; }

        public bool SwapWeaponPressed { get; }

        public bool LightAttackPressed { get; }

        public bool HeavyAttackPressed { get; }

        public bool UseActiveItemPressed { get; }

        public bool UseConsumableCardPressed { get; }

        public bool GuardHeld { get; }

        public bool PausePressed { get; }

        public bool RollPressed { get; }

        public bool LockTargetPressed { get; }

        public Vector2 PointerScreenPosition { get; }

        public bool HasPointerScreenPosition { get; }

        public bool MouseAimIntent { get; }

        public bool LightAttackHeld { get; }

        public bool LightAttackReleased { get; }

        public bool HeavyAttackHeld { get; }

        public bool HeavyAttackReleased { get; }

        public bool HasShoot => Shoot.sqrMagnitude > 0.001f;
    }
}
