using System.Collections.Generic;
using Hollow.Entities;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class ArenaModeScoreTracker
    {
        private readonly HashSet<CombatantHealth> boundEnemyHealth = new();
        private readonly HashSet<CombatantHealth> playerDamagedEnemyHealth = new();
        private PlaceholderPlayerController player;
        private float startTimeSeconds;
        private int damageDealt;
        private int kills;
        private int waveClears;

        public int DamageDealt => damageDealt;

        public int Kills => kills;

        public int WaveClears => waveClears;

        public float TimeSurvivedSeconds => Mathf.Max(0f, Time.time - startTimeSeconds);

        public int Score => Mathf.RoundToInt(damageDealt * 10f + kills * 100f + TimeSurvivedSeconds * 5f + waveClears * 250f);

        public void Reset(PlaceholderPlayerController nextPlayer)
        {
            foreach (var health in boundEnemyHealth)
            {
                if (health == null)
                {
                    continue;
                }

                health.DamageApplied -= OnEnemyDamageApplied;
                health.Died -= OnEnemyDied;
            }

            boundEnemyHealth.Clear();
            playerDamagedEnemyHealth.Clear();
            player = nextPlayer;
            startTimeSeconds = Time.time;
            damageDealt = 0;
            kills = 0;
            waveClears = 0;
        }

        public void BindEnemies(IEnumerable<EnemyRuntimeController> enemies)
        {
            foreach (var enemy in enemies ?? System.Array.Empty<EnemyRuntimeController>())
            {
                if (enemy?.Health == null || !boundEnemyHealth.Add(enemy.Health))
                {
                    continue;
                }

                enemy.Health.DamageApplied += OnEnemyDamageApplied;
                enemy.Health.Died += OnEnemyDied;
            }
        }

        public void RecordWaveClear()
        {
            waveClears++;
        }

        private void OnEnemyDamageApplied(CombatantHealth health, DamageRequest request, int appliedAmount)
        {
            if (appliedAmount <= 0 || !IsPlayerSource(request.Source))
            {
                return;
            }

            damageDealt += appliedAmount;
            playerDamagedEnemyHealth.Add(health);
        }

        private void OnEnemyDied(CombatantHealth health)
        {
            if (playerDamagedEnemyHealth.Remove(health))
            {
                kills++;
            }
        }

        private bool IsPlayerSource(GameObject source)
        {
            if (source == null || player == null)
            {
                return true;
            }

            if (source == player.gameObject || source.GetComponent<PlaceholderPlayerController>() != null)
            {
                return true;
            }

            var parent = source.transform;
            while (parent != null)
            {
                if (parent.gameObject == player.gameObject || parent.GetComponent<PlaceholderPlayerController>() != null)
                {
                    return true;
                }

                parent = parent.parent;
            }

            return source.GetComponent<PlayerWeaponController>() != null;
        }
    }
}
