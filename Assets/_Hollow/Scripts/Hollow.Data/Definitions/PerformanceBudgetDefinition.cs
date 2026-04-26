using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Performance Budget", fileName = "PerformanceBudget")]
    public sealed class PerformanceBudgetDefinition : HollowDefinition
    {
        [SerializeField] private PlatformPerformanceBudget[] budgets = Array.Empty<PlatformPerformanceBudget>();

        public PlatformPerformanceBudget[] Budgets => budgets;

        public void Configure(PlatformPerformanceBudget[] nextBudgets)
        {
            budgets = nextBudgets ?? Array.Empty<PlatformPerformanceBudget>();
        }

        public bool TryGetBudget(PlatformPresentationMode mode, out PlatformPerformanceBudget budget)
        {
            foreach (var candidate in budgets)
            {
                if (candidate.Mode == mode)
                {
                    budget = candidate;
                    return true;
                }
            }

            budget = default;
            return false;
        }
    }

    [Serializable]
    public struct PlatformPerformanceBudget
    {
        [SerializeField] private PlatformPresentationMode mode;
        [SerializeField] private int minimumTargetFrameRate;
        [SerializeField] private float maximumFrameTimeMs;
        [SerializeField] private float maximumRenderScale;
        [SerializeField] private int maximumVisibleEnemies;
        [SerializeField] private int maximumProjectiles;
        [SerializeField] private int maximumDrawCalls;
        [SerializeField, TextArea] private string notes;

        public PlatformPerformanceBudget(
            PlatformPresentationMode mode,
            int minimumTargetFrameRate,
            float maximumFrameTimeMs,
            float maximumRenderScale,
            int maximumVisibleEnemies,
            int maximumProjectiles,
            int maximumDrawCalls,
            string notes)
        {
            this.mode = mode;
            this.minimumTargetFrameRate = Mathf.Max(1, minimumTargetFrameRate);
            this.maximumFrameTimeMs = Mathf.Max(1f, maximumFrameTimeMs);
            this.maximumRenderScale = Mathf.Clamp(maximumRenderScale, 0.1f, 2f);
            this.maximumVisibleEnemies = Mathf.Max(1, maximumVisibleEnemies);
            this.maximumProjectiles = Mathf.Max(1, maximumProjectiles);
            this.maximumDrawCalls = Mathf.Max(1, maximumDrawCalls);
            this.notes = notes ?? string.Empty;
        }

        public PlatformPresentationMode Mode => mode;

        public int MinimumTargetFrameRate => minimumTargetFrameRate;

        public float MaximumFrameTimeMs => maximumFrameTimeMs;

        public float MaximumRenderScale => maximumRenderScale;

        public int MaximumVisibleEnemies => maximumVisibleEnemies;

        public int MaximumProjectiles => maximumProjectiles;

        public int MaximumDrawCalls => maximumDrawCalls;

        public string Notes => notes;
    }
}
