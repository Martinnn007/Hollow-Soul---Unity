# M107 AI Performance + LOD Lock Report

- AI diagnostics: `EnemyAiDebugOverlay.PerformanceStats`.
- Navigation diagnostics: `EnemyNavigationDebugOverlay.Stats`.
- Adaptive cadence entrypoint: `EnemyAiBrain.ResolveAdaptiveThinkIntervalForDiagnostics`.
- Runtime counters added: agent count, path pending count, behavior graph ticks, scorer calls, stuck agents, pressure penalties.
- Design rule: close/endangered/committed enemies keep fast updates; reduced/background enemies degrade gracefully under Arena swarm load.
- Docs: `Docs/Hollow_M107_AI_Performance_And_LOD_Lock.md`.
