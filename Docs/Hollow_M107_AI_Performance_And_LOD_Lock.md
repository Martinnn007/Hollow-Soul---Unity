# M107: AI Performance + LOD Lock

M107 stabilizes large Arena encounters by making enemy AI cost visible and adaptive. Hollow keeps Souls-like responsiveness for close, endangered, and committed enemies while stretching update cadence for reduced/background enemies under swarm or path pressure.

## Runtime Counters

- Active AI agents and Full/Reduced/Background LOD counts.
- NavMesh agent users, pending paths, stuck agents, invalid paths, solve timing, and fallback reasons.
- Unity Behavior graph ticks and emergency fallback ticks.
- Enemy action scorer calls and candidate counts.
- Room pressure lanes and scorer pressure penalties.

## Adaptive Cadence

- Full LOD enemies close to the player, endangered, or already committed keep their base think interval.
- Full LOD backliners receive only a small interval stretch under 20-40 enemy load.
- Reduced and Background enemies stretch more aggressively, reusing cached plans instead of constantly rescoring.
- The cap prevents visible dumbness: responsive threats stay fast, while far enemies hold, face, reposition, or reuse plans.

## Arena Profiling Contract

Arena smoke profiling should watch `EnemyNavigationDebugOverlay.DiagnosticsSummary` and `EnemyAiDebugOverlay.DiagnosticsSummary` while spawning 20-40 enemies. Healthy runs should show stable pending path counts, low stuck agents, bounded scorer calls, and pressure penalties instead of dogpiling.
