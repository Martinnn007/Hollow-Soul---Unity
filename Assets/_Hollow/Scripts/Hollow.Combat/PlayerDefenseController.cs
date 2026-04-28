using Hollow.Entities;
using Hollow.Input;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class PlayerDefenseController : MonoBehaviour, IIncomingDamageModifier
    {
        public const int DefensePerPassiveDamageReduction = 2;
        public const int GuardDamageReduction = 1;
        public const float GuardDrainStaminaPerSecond = 6f;
        public const float GuardBlockStaminaCost = 10f;
        public const float GuardPushMeters = 0.65f;

        [SerializeField] private int defense;
        [SerializeField] private bool isGuarding;
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private PlayerWeaponController weaponController;

        public int Defense => defense;

        public bool IsGuarding => isGuarding;

        public int LastDamageReduction { get; private set; }

        public bool LastHitWasGuarded { get; private set; }

        public void Configure(int nextDefense)
        {
            defense = Mathf.Max(0, nextDefense);
            ResolveReferences();
        }

        public void Bind(RoomRuntimeRoot room)
        {
            roomRuntimeRoot = room;
            ResolveReferences();
        }

        private void Update()
        {
            var input = GameplayInputReader.ReadCurrent();
            Tick(input.GuardHeld, Time.deltaTime);
        }

        public void Tick(bool guardHeld, float deltaTime)
        {
            ResolveReferences();
            if (!guardHeld)
            {
                isGuarding = false;
                return;
            }

            var drainCost = GuardDrainStaminaPerSecond * Mathf.Max(0f, deltaTime);
            isGuarding = weaponController == null || weaponController.SpendStaminaForDefense(drainCost);
        }

        public int ModifyIncomingDamage(DamageRequest request, int currentAmount)
        {
            LastDamageReduction = 0;
            LastHitWasGuarded = false;
            if (currentAmount <= 0)
            {
                return 0;
            }

            var reducedAmount = currentAmount;
            var passiveReduction = Mathf.Min(currentAmount - 1, Mathf.FloorToInt(defense / (float)DefensePerPassiveDamageReduction));
            if (passiveReduction > 0)
            {
                reducedAmount -= passiveReduction;
                LastDamageReduction += passiveReduction;
            }

            if (isGuarding && SpendGuardBlockCost())
            {
                var beforeGuard = reducedAmount;
                reducedAmount = Mathf.Max(0, reducedAmount - GuardDamageReduction);
                LastDamageReduction += beforeGuard - reducedAmount;
                LastHitWasGuarded = true;
                PushSourceAway(request.Source);
            }

            return reducedAmount;
        }

        private bool SpendGuardBlockCost()
        {
            return weaponController == null || weaponController.SpendStaminaForDefense(GuardBlockStaminaCost);
        }

        private void PushSourceAway(GameObject source)
        {
            if (source == null)
            {
                return;
            }

            var enemy = source.GetComponentInParent<EnemyRuntimeController>();
            if (enemy == null || !enemy.IsAlive)
            {
                return;
            }

            var direction = enemy.transform.localPosition - transform.localPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector3.forward;
            }

            var desired = enemy.transform.localPosition + direction.normalized * GuardPushMeters;
            enemy.transform.localPosition = roomRuntimeRoot != null
                ? RoomLocalCollision.ResolveMove(roomRuntimeRoot, enemy.transform.localPosition, desired, enemy.RadiusMeters)
                : desired;
        }

        private void ResolveReferences()
        {
            if (weaponController == null)
            {
                weaponController = GetComponent<PlayerWeaponController>();
            }

            if (roomRuntimeRoot == null)
            {
                roomRuntimeRoot = GetComponentInParent<RoomRuntimeRoot>() ?? FindFirstObjectByType<RoomRuntimeRoot>();
            }
        }
    }
}
