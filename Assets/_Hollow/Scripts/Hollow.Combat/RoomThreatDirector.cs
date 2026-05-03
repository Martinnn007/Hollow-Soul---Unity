using System.Collections.Generic;
using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class RoomThreatDirector
    {
        private const float MeleeSoftCap = 2.6f;
        private const float RangedSoftCap = 3.2f;
        private const float AreaSoftCap = 1.6f;
        private const float ChargeSoftCap = 1.3f;

        private float meleePressure;
        private float rangedPressure;
        private float areaPressure;
        private float chargePressure;

        public float MeleePressure => meleePressure;

        public float RangedPressure => rangedPressure;

        public float AreaPressure => areaPressure;

        public float ChargePressure => chargePressure;

        public void Reset()
        {
            meleePressure = 0f;
            rangedPressure = 0f;
            areaPressure = 0f;
            chargePressure = 0f;
        }

        public void Tick(IEnumerable<EnemyRuntimeController> enemies)
        {
            Reset();
            if (enemies == null)
            {
                return;
            }

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive || enemy.BossDefinition != null)
                {
                    continue;
                }

                AddRuntimePressure(enemy);
            }
        }

        public float PressurePenaltyFor(
            EnemyRuntimeController enemy,
            EnemyActionProfileDefinition actionProfile,
            EnemyAttackProfileDefinition attackProfile)
        {
            if (enemy == null || actionProfile == null)
            {
                return 0f;
            }

            var lane = ResolveLane(actionProfile, attackProfile);
            var pressure = lane switch
            {
                ThreatLane.Melee => meleePressure,
                ThreatLane.Ranged => rangedPressure,
                ThreatLane.Area => areaPressure,
                ThreatLane.Charge => chargePressure,
                _ => 0f
            };
            var cap = lane switch
            {
                ThreatLane.Melee => MeleeSoftCap,
                ThreatLane.Ranged => RangedSoftCap,
                ThreatLane.Area => AreaSoftCap,
                ThreatLane.Charge => ChargeSoftCap,
                _ => 999f
            };
            if (pressure <= cap)
            {
                return 0f;
            }

            var overflow = pressure - cap;
            var pressureCost = Mathf.Max(0.25f, actionProfile.PressureCost);
            var intelligenceRelief = enemy.Intelligence is EnemyIntelligenceLevel.Tactical or EnemyIntelligenceLevel.Cunning ? 0.84f : 1f;
            return overflow * pressureCost * 0.72f * intelligenceRelief;
        }

        private void AddRuntimePressure(EnemyRuntimeController enemy)
        {
            var pressure = enemy.CurrentThreatPressureCost;
            if (pressure <= 0f)
            {
                return;
            }

            switch (enemy.CurrentThreatLane)
            {
                case ThreatLane.Melee:
                    meleePressure += pressure;
                    break;
                case ThreatLane.Ranged:
                    rangedPressure += pressure;
                    break;
                case ThreatLane.Area:
                    areaPressure += pressure;
                    break;
                case ThreatLane.Charge:
                    chargePressure += pressure;
                    break;
            }
        }

        public static ThreatLane ResolveLane(EnemyActionProfileDefinition actionProfile, EnemyAttackProfileDefinition attackProfile)
        {
            if (attackProfile != null)
            {
                return attackProfile.RuntimeKind switch
                {
                    EnemyAttackRuntimeKind.Charge => ThreatLane.Charge,
                    EnemyAttackRuntimeKind.Area => ThreatLane.Area,
                    EnemyAttackRuntimeKind.Projectile or EnemyAttackRuntimeKind.FanProjectile or EnemyAttackRuntimeKind.RadialProjectile or EnemyAttackRuntimeKind.Beam => ThreatLane.Ranged,
                    EnemyAttackRuntimeKind.MeleeLunge or EnemyAttackRuntimeKind.Contact or EnemyAttackRuntimeKind.WeaponMelee => ThreatLane.Melee,
                    _ => ThreatLane.Utility
                };
            }

            return actionProfile != null
                ? actionProfile.Category switch
                {
                    EnemyActionCategory.Ranged or EnemyActionCategory.Projectile or EnemyActionCategory.Magic => ThreatLane.Ranged,
                    EnemyActionCategory.Hazard or EnemyActionCategory.BossScale => ThreatLane.Area,
                    EnemyActionCategory.Body or EnemyActionCategory.Weapon => ThreatLane.Melee,
                    _ => ThreatLane.Utility
                }
                : ThreatLane.Utility;
        }
    }

    public enum ThreatLane
    {
        Utility = 0,
        Melee = 1,
        Ranged = 2,
        Area = 3,
        Charge = 4
    }
}
