# M141-M150 Roadmap: From Build-Real Stability To Beta Slice

- Lock: `m141_m150_beta_slice_roadmap_v1`
- Target outcome: M150 reaches Internal Beta Slice Candidate, not full beta.
- Beta slice by M150: `yes`
- Full beta by M150: `no`

M150 is an internal beta-slice candidate, not a full beta promise. Full beta remains M151+ unless broader content, progression, UX, audio, settings, accessibility, crash reporting, and external-test readiness are also complete.

## M141: M140 Gate Closure And Truth Cleanup

Focus: Make M140 trustworthy and passing on macOS, with Windows artifact flow ready.

Work:
- Finish valid gameplay screenshots, render/FPS capture, missing-script failures, pool miss attribution, and M138/M139 integration.
- Re-run macOS Apple Silicon development and release-smoke gates.
- Document or import Windows player artifact requirements.

Pass gate: macOS M140 passes except Windows environment/artifact status; no fake gameplay screenshots; no hidden player-log script warnings.
Outcome: Build-real telemetry becomes the trusted source of performance and visual truth.
Dependency: M140 implemented and rerunnable.

## M142: Cold Miss And Pool Warm Closure

Focus: Remove branch/reward/boss cold-cache misses and post-warmup hard instantiates.

Work:
- Use M139/M140 miss-key reports to warm exact VFX, audio, projectile, pickup, reward, portal, and generated keys.
- Extend branch-load preload coverage for reward rooms, boss rooms, special rooms, and return traversal.
- Keep boss enemies unpooled unless a measured spike proves pooling is necessary.

Pass gate: Normal traversal after branch load has 0 cold misses and 0 runtime hard instantiates, except documented boss/unpooled exceptions.
Outcome: Branch traversal is seamless because runtime content is warm before gameplay reveal.
Dependency: M141 truth reports expose exact miss keys.

## M143: Projectile-Heavy Combat Performance Pass

Focus: Fix the first real performance cliff.

Work:
- Profile projectile-heavy stress separately from harness allocations.
- Optimize projectile collision queries, lifetime/update paths, ranged fire cadence, pooled reset, and VFX/audio spam.
- Add projectile counters for active projectiles, collision checks, hits, returns, pool misses, and projectile update ms.

Pass gate: Projectile-heavy M138/M140 scenario returns to stable 60 FPS p95 in trusted player capture.
Outcome: Projectile rooms become a known budgeted stress case instead of a frame cliff.
Dependency: M142 pool warming removes false-positive projectile misses.

## M144: AI/Nav Scale Finalization

Focus: Make crowded fights stable without making enemies feel asleep.

Work:
- Finalize central AI think budget and stagger policy.
- Tune LOD degradation for offscreen, far, waiting, and add enemies.
- Verify NavMesh solve budget, deferred path retry, avoidance tiers, and boss/add priority.

Pass gate: 30-enemy and boss-plus-adds scenarios have no synchronized AI/Nav spikes.
Outcome: Crowded combat scales predictably while visible threats remain responsive.
Dependency: M143 projectile pressure no longer hides AI/Nav cost.

## M145: Save/Load, Branch Restore, And Failure Recovery

Focus: Make the beta slice resilient.

Work:
- Validate fresh run, continue run, snapshot restore, branch abandon/re-enter, boss room restore, reward room restore, and next-branch transition.
- Add corrupted or old snapshot fallback behavior where needed.
- Ensure loading screens, input locks, cache invalidation, and pool ownership recover cleanly after failure.

Pass gate: No broken saves, no stuck loading/input state, no stale branch/pool state after restore.
Outcome: The slice survives interruption, restore, and branch transitions without developer repair.
Dependency: M141-M144 gates are stable enough to test restore behavior honestly.

## M146: Visual Readability And Render Budget Polish

Focus: Make the game look intentionally good under budget.

Work:
- Lock render profiles for macOS, Windows, and dev.
- Audit lighting after branch load, material first-use misses, shadows, projectile visibility, rewards, enemy silhouettes, HUD, and minimap contrast.
- Add screenshot review sheets from M140 scenarios.

Pass gate: Automated screenshots pass and manual visual review confirms combat, boss, rewards, rooms, HUD, and minimap read clearly.
Outcome: The beta slice looks deliberate while staying inside the render budget.
Dependency: M145 restore paths no longer create misleading visual states.

## M147: Beta Slice Content Lock

Focus: Define the exact playable beta-slice path.

Work:
- Choose the beta-slice branch/floor: biome, room types, enemy families, boss, reward room, special room, hub return, and next-branch handoff.
- Freeze content scope for the slice.
- Add content-lock validation for missing prefabs, materials, NavMesh, catalog entries, and reward definitions.

Pass gate: One deterministic beta-slice route is content complete and all required assets resolve in player builds.
Outcome: The team has one scoped path to polish instead of an expanding target.
Dependency: M146 confirms the selected content is visually readable.

## M148: Balance And Feel Pass

Focus: Make the slice fun, not just functional.

Work:
- Tune enemy HP/damage, player damage, weapon cadence, projectile density, rewards, coin/soul economy, chest risk/reward, and boss difficulty.
- Add deterministic balance smoke captures for normal, low-skill, and high-pressure routes.
- Preserve performance budgets while tuning.

Pass gate: Internal playtest checklist says the slice is readable, fair, paced, and worth replaying.
Outcome: The slice has a coherent difficulty and reward arc.
Dependency: M147 content scope is frozen.

## M149: QA Automation And Bug Triage Gate

Focus: Turn repeated checking into a routine.

Work:
- Add one-click Beta Slice QA Gate chaining compile, EditMode, PlayMode smoke, M138, M139 smoke, M140 macOS, and report summary.
- Create severity buckets: blocker, beta-slice blocker, polish, and later.
- Add a latest-report dashboard linking failures to artifacts, screenshots, and logs.

Pass gate: QA gate produces a clear pass/fail report with actionable failure reasons.
Outcome: Every candidate build has one obvious go/no-go report.
Dependency: M148 establishes what the QA gate must preserve.

## M150: Internal Beta Slice Candidate

Focus: Package a playable internal beta-slice build.

Work:
- Build macOS and Windows candidate artifacts.
- Include boot loading, branch loading, seamless traversal, boss room, reward room, save/continue, and return-to-hub.
- Produce release notes, known issues, QA checklist, and performance report.

Pass gate: Internal testers can play the slice from boot to boss/reward/return without developer intervention.
Outcome: Internal Beta Slice Candidate is ready; full beta remains a later M151+ expansion target.
Dependency: M149 QA gate is passing or has only accepted non-blocking known issues.

