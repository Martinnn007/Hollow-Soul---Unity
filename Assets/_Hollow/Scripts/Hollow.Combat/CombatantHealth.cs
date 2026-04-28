using System;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class CombatantHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 1;
        [SerializeField] private int currentHealth = 1;

        public int MaxHealth => maxHealth;

        public int CurrentHealth => currentHealth;

        public bool IsAlive => currentHealth > 0;

        public event Action<CombatantHealth> Damaged;

        public event Action<CombatantHealth> Died;

        public void Configure(int nextMaxHealth)
        {
            maxHealth = Mathf.Max(1, nextMaxHealth);
            currentHealth = maxHealth;
        }

        public void Restore(int nextMaxHealth, int nextCurrentHealth)
        {
            maxHealth = Mathf.Max(1, nextMaxHealth);
            currentHealth = Mathf.Clamp(nextCurrentHealth, 0, maxHealth);
        }

        public void SetMaxHealthPreservingCurrent(int nextMaxHealth, int healAmount)
        {
            var previousCurrent = currentHealth;
            maxHealth = Mathf.Max(1, nextMaxHealth);
            currentHealth = Mathf.Clamp(previousCurrent + Mathf.Max(0, healAmount), 0, maxHealth);
        }

        public bool ApplyDamage(DamageRequest request)
        {
            if (!IsAlive || request.Amount <= 0)
            {
                return false;
            }

            var amount = request.Amount;
            foreach (var behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour is IIncomingDamageModifier modifier && behaviour.isActiveAndEnabled)
                {
                    amount = Mathf.Max(0, modifier.ModifyIncomingDamage(request, amount));
                }
            }

            if (amount <= 0)
            {
                return false;
            }

            currentHealth = Mathf.Max(0, currentHealth - amount);
            Damaged?.Invoke(this);
            if (currentHealth == 0)
            {
                Died?.Invoke(this);
            }

            return true;
        }
    }
}
