# Hollow Enemy AI Tooling Recommendations

Snapshot date: May 2, 2026

## Decision

Buy or adopt pathfinding before buying a behavior authoring suite. Hollow's enemies already have a good data spine through `EnemyDefinition`, `EnemyBehaviorId`, intelligence, disposition, attack budgets, and room-local collision. The missing production multiplier is route planning around authored room obstacles, holes, and future line-of-sight/perception spaces.

Recommended stack:

1. **A* Pathfinding Project Pro** if we are willing to buy one paid AI tool.
2. **Unity AI Navigation** if we want a no-new-purchase first pass because `com.unity.ai.navigation` is already installed.
3. **Behavior Designer Pro** only after pathfinding, if we want long-term visual behavior trees.
4. **NodeCanvas** instead of Behavior Designer Pro if we prefer mature GameObject-first authoring, full source, BT/FSM flexibility, and fewer DOTS assumptions.

Codex can directly implement the shared adapters, movement drivers, room graph builders, behavior bindings, tests, and prefab/spawn glue after a package is imported. Paid Asset Store packages still have to be purchased and imported through Unity by the user. Visual graph tools may still need optional graph tuning, but Hollow should not require manual setup per enemy if we drive variants from existing enemy data.

## Current Hollow Baseline

- Unity version: `6000.4.1f1`.
- Installed relevant package: `com.unity.ai.navigation` `2.0.11`.
- Current combat movement: direct vector chase/retreat/hold inside `EnemyRuntimeController`, resolved through `RoomLocalCollision`.
- Current data model: `EnemyDefinition` covers archetype, behavior id, movement mode, intelligence, disposition, health, speed, range, charge/ranged data, body class, and split behavior.
- Known limitation from existing M72 documentation: no pathfinding, line of sight, squad coordination, or richer boss behavior.

## Ranked Tools

### 1. A* Pathfinding Project Pro

Snapshot: Unity Asset Store fallback lists version `5.4.6`, release date Jan 22, 2026, price `$140`, original Unity version `2021.3.45`.

Why it is worth using: It is the strongest long-term pathfinding upgrade. Official docs cover grid graphs, recast graphs, AIPath-style movement, graph updates, and Pro local avoidance using ORCA/RVO. Hollow's room data can map naturally to a grid graph, while recast remains available if rooms move toward more organic 3D geometry later.

Fit for Hollow: Excellent. It targets the actual missing layer: obstacle-aware paths, room graph reuse, dynamic graph updates, and local avoidance. It can sit under the existing enemy definitions rather than replacing the combat model.

Codex/direct setup: User purchases/imports. Codex can then build a `RoomNavigationBuilder`, an enemy navigation adapter, movement intent tests, and one prefab/spawn integration so all existing enemies inherit the behavior from `EnemyDefinition`.

Risks: Paid dependency. Needs import/API validation. It should be wrapped behind our own interface so Hollow can fall back to current movement or Unity AI Navigation.

Verdict: Best paid pick. If we buy one tool, buy this first.

### 2. Unity AI Navigation

Snapshot: Already installed locally as `com.unity.ai.navigation` `2.0.11`. Unity docs describe runtime/edit-time NavMesh building, dynamic obstacles, and links.

Why it is worth using: No new purchase, official Unity support, and enough functionality to prove obstacle-aware movement quickly.

Fit for Hollow: Good baseline. Runtime rooms are simple X/Z spaces with generated floor and obstacles, so a NavMeshSurface-based prototype is plausible.

Codex/direct setup: Codex can implement the runtime NavMesh builder, agent adapter, and shared enemy movement path without manual per-enemy configuration.

Risks: Less grid-native than Hollow's room data. It may need careful baking filters and runtime surface management for generated rooms. Local avoidance and per-agent tuning are less purpose-built than A* Pro for this specific use.

Verdict: Best no-cost prototype. Use this first if we are unsure about buying A* Pro.

### 3. Behavior Designer Pro

Snapshot: Asset Store fallback lists version `2.1.12`, release date Jan 28, 2026, price `$145`, original Unity version `2022.3.20`. Opsive docs describe GameObject and Entity tasks; requirements include Entities/Burst.

Why it is worth using: Strong long-term visual behavior-tree authoring with modern DOTS-backed traversal, shared variables, subtrees, debugging, and custom task support.

Fit for Hollow: Strong after pathfinding. It can express sentinel, prey, predator, charger, turret, and boss phase decisions more visibly than hard-coded branches.

Codex/direct setup: User purchases/imports. Codex can create shared tasks such as `MoveWithHollowNavigator`, `CanStartBudgetedAttack`, `WithinPreferredRange`, `HasLineOfSight`, and bind one shared tree to enemy definitions.

Risks: Adds DOTS/Entities dependency even for GameObject workflow. Opsive currently documents no WebGL support because Entities/Burst do not support WebGL. It does not solve pathfinding by itself.

Verdict: Best visual behavior-tree option for long-term power, but phase 3 rather than phase 1.

### 4. NodeCanvas

Snapshot: Marketplace/fallback data lists version `3.4.1`, release date Feb 20, 2026, list price `$120` with sale pricing sometimes visible. Official docs describe Behavior Trees, FSMs, Dialogue Trees, GraphOwner components, blackboards, subgraph variables, runtime debugging, full source, and A* Pathfinding Project integration.

Why it is worth using: Mature and flexible visual authoring. It is especially attractive for a GameObject-heavy Unity project with a bespoke runtime because full source and BT/FSM mixing make integration less opaque.

Fit for Hollow: Very good if we want readable designer-facing enemy logic without DOTS constraints. Good for bosses and hybrid state/behavior control.

Codex/direct setup: User purchases/imports. Codex can write reusable NodeCanvas tasks and map graph variables from `EnemyDefinition` so enemies do not need bespoke graphs.

Risks: Less future-looking than Behavior Designer Pro for massive agent counts. Graph freedom can invite one-off per-enemy authoring unless we enforce shared subgraphs and data binding.

Verdict: Best practical alternative to Behavior Designer Pro, especially if DOTS/WebGL risk matters.

### 5. Unity Behavior

Snapshot: Unity docs list `com.unity.behavior` `1.0.13` released for Unity Editor `6000.0`. It is an official graph-based behavior-tree package with reusable subgraphs, C# integration, prebuilt movement/detection/decision nodes, and play-mode debugging.

Why it is worth using: Official, no Asset Store purchase, and aligned with Unity's current graph tooling direction.

Fit for Hollow: Interesting, but not the first move. It could be a low-cost behavior authoring layer after navigation is solved.

Codex/direct setup: Codex can add the package to `manifest.json` if requested, create custom nodes, and bind shared behavior graphs to the existing enemy data.

Risks: Younger ecosystem than Behavior Designer Pro and NodeCanvas. It may require more custom nodes for Hollow's combat-specific budget/readability logic.

Verdict: Good official experiment. Not the top recommendation for production speed or proven long-term authoring yet.

### 6. GOAP v3 by CrashKonijn

Snapshot: Asset Store fallback lists GOAP v3 as free, version `3.1.1`, release date Dec 12, 2025; current docs show package install tag `3.1.2`. Official docs describe a multi-threaded GOAP setup with an agent, goals/actions, and debugging/visualization.

Why it is worth using: Powerful for systemic planning where enemies choose goals and action sequences from world state.

Fit for Hollow: Situational. It is a strong future boss/NPC planner, but it is not the first fix for obstacle-aware chasers or room combat readability.

Codex/direct setup: Codex can integrate the open-source package and author code-based goals/actions, but the actual goal model is game-design heavy.

Risks: GOAP adds planning complexity. Hollow's current roster mostly needs better movement and readable tactical variants, not full planning.

Verdict: Keep in reserve for bosses, companions, or systemic NPCs.

### 7. Utility Intelligence GO v3

Snapshot: Asset Store fallback lists version `3.1.3`, release date Apr 18, 2026, price `$125`, original Unity version `6000.0.71`.

Why it is worth using: Utility scoring is a good fit for decisions like hold range, retreat, pressure, pick target, use charge, or switch boss phase.

Fit for Hollow: Useful later as a high-level decision layer, especially for Cunning/Tactical enemies, but not a navigation solution.

Codex/direct setup: User purchases/imports. Codex can bind scoring inputs to `EnemyDefinition`, room state, player distance, recent damage, and attack budget.

Risks: Newer/smaller signal than Behavior Designer Pro or NodeCanvas. Scoring curves usually need hands-on tuning and playtest iteration.

Verdict: Interesting later. Do not buy before pathfinding.

### 8. Emerald AI 2025

Snapshot: Marketplace lists version `1.3.3`, release date Apr 8, 2026, list price `$60` with a `$30` sale shown in the snapshot, Unity `6000.0.21f1` compatibility.

Why it is worth using: Fast generic NPC/enemy setup for RPG, animal, humanoid, faction, patrol, and combat behaviors.

Fit for Hollow: Poor as the core AI layer. Hollow already has a bespoke room-combat model, readability states, attack budgets, boss runtime, spawn service, and data definitions. Emerald AI would likely replace or fight the custom systems rather than accelerate them.

Codex/direct setup: User purchases/imports. Codex could make adapters, but much of the value comes from Emerald's inspector-driven configuration, which increases per-enemy setup.

Risks: Integration overhead, duplicate combat logic, and likely manual tuning per enemy. Better for generic NPC projects than Hollow's tightly authored room combat.

Verdict: Do not use as the core Hollow enemy AI solution.

## Recommended Rollout

Phase 1 - Navigation first:

- If buying: import A* Pathfinding Project Pro and build a Hollow adapter.
- If not buying yet: prototype the same adapter against Unity AI Navigation.
- Generate room navigation from `RoomRuntimeRoot.CurrentLayout`, walkable tiles, obstacles, and interactive blockers.
- Preserve current enemy data, attack timing, readability states, and contact damage.

Phase 2 - Behavior abstraction:

- Extract direct movement decisions from `EnemyRuntimeController` into data-driven movement intents.
- Add optional perception hooks such as line of sight and threat memory.
- Keep attack budget and windup logic in Hollow code so production tuning remains deterministic and testable.

Phase 3 - Visual authoring only if needed:

- Add Behavior Designer Pro for maximum long-term behavior-tree power, or NodeCanvas if we prefer mature GameObject-first source access.
- Build one shared graph/subgraph per behavior family, parameterized by `EnemyDefinition`.
- Avoid bespoke graph setup per enemy unless a boss truly needs custom logic.

## Manual Setup Answer

Codex can take care of most integration work once assets exist in the project: shared components, adapters, custom graph tasks/nodes, prefab/spawn glue, ScriptableObject bindings, migration helpers, and tests.

The user still needs to manually purchase/import paid Asset Store packages. For visual graph tools, the user may also want to manually inspect or tune shared behavior graphs in Unity. The recommended architecture avoids per-enemy manual configuration by using existing `EnemyDefinition` data and shared prefabs/subgraphs.

## Source Links

- A* Pathfinding Project Pro Asset Store snapshot: https://assetstore-fallback.unity.com/packages/tools/behavior-ai/a-pathfinding-project-pro-87744
- A* Pathfinding Project graph types: https://arongranberg.com/astar/documentation/stable/graphtypes.html
- A* Pathfinding Project local avoidance: https://arongranberg.com/astar/documentation/stable/localavoidance.html
- Unity AI Navigation manual: https://docs.unity3d.com/ja/current/Manual/com.unity.ai.navigation.html
- Unity Behavior manual: https://docs.unity3d.com/kr/current/Manual/com.unity.behavior.html
- Behavior Designer Pro Asset Store snapshot: https://assetstore-fallback.unity.com/packages/tools/visual-scripting/behavior-designer-pro-dots-powered-behavior-trees-298743
- Behavior Designer Pro docs: https://opsive.com/support/documentation/behavior-designer-pro/
- Behavior Designer Pro requirements: https://opsive.com/support/documentation/behavior-designer-pro/requirements/
- NodeCanvas Asset Store snapshot: https://marketplace.unity.com/packages/tools/visual-scripting/nodecanvas-14914
- NodeCanvas official site: https://nodecanvas.paradoxnotion.com/
- GOAP v3 Asset Store snapshot: https://assetstore-fallback.unity.com/packages/tools/behavior-ai/goap-v3-302434
- GOAP v3 docs: https://goap.crashkonijn.com/readme/tutorial/gettingstarted
- Utility Intelligence GO v3 Asset Store snapshot: https://assetstore-fallback.unity.com/packages/tools/behavior-ai/utility-intelligence-go-v3-utility-ai-framework-308338
- Emerald AI 2025 Asset Store snapshot: https://marketplace.unity.com/packages/tools/behavior-ai/emerald-ai-2025-268519

