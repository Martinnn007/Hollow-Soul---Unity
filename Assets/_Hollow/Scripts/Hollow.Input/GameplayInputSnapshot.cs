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
        {
            Move = Vector2.ClampMagnitude(move, 1f);
            Shoot = GameplayInputReader.CardinalizeShoot(shoot);
            InteractPressed = interactPressed;
            SwapWeaponPressed = swapWeaponPressed;
            LightAttackPressed = lightAttackPressed;
            HeavyAttackPressed = heavyAttackPressed;
        }

        public Vector2 Move { get; }

        public Vector2 Shoot { get; }

        public bool InteractPressed { get; }

        public bool SwapWeaponPressed { get; }

        public bool LightAttackPressed { get; }

        public bool HeavyAttackPressed { get; }

        public bool HasShoot => Shoot.sqrMagnitude > 0.001f;
    }
}
