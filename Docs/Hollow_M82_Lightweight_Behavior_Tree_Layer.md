# M82: Lightweight Behavior Tree Layer V1

M82 moves normal enemy decisions into authored ScriptableObject behavior trees while keeping M80 attack execution authoritative. Trees choose only from idle; once an attack enters windup, active, or recovery, the runner stops replanning until the committed action finishes.

## Runtime Contract

- Non-boss enemies resolve an `EnemyBehaviorTreeDefinition` from authored assets or runtime defaults.
- The tree context includes awareness, intelligence, disposition, distance, preferred range bands, recent damage/endangered state, spawn index, current readability state, and room attack budget availability.
- Commands are intentionally small: hold, move, preferred-range movement, flee, wander, face player, start linked action, warning feint, or no-op.
- Attack commands only start from `EnemyReadabilityState.Idle`; M80 owns windup, active, and recovery after commitment.
- Existing melee and ranged/charge budgets remain authoritative. Tactical/Cunning intelligence only improves priority tie-breaking and does not increase total room pressure.
- Boss trees are metadata-only in M82 and are ignored by boss runtime behavior.

## Promoted Prototype Actions

| Enemy | Action | Runtime Kind | Damage | Purpose |
| --- | --- | --- | ---: | --- |
| Fast Chaser | `side_pounce` | MeleeLunge | 1 | Committed lateral pounce prototype. |
| Heavy Chaser | `stomp` | Area | 2 | Circular high-commitment punishable impact. |
| Rat | `warning_squeal` | Movement | 0 | Non-damaging territorial warning before bite pressure. |
| Spider | `side_hop_bite` | MeleeLunge | 1 | Quick side-hop bite chosen by deterministic weighted tree. |

## Runtime Enemy Trees

| Enemy | Tree | Primary Decisions |
| --- | --- | --- |
| Normal Chaser | `enemy_spawnEnemyNormal_m82_tree` | claw/bite commitment, then preferred-range pressure |
| Flying Chaser | `enemy_spawnEnemyFlying_m82_tree` | prey flee/wander until endangered or engaged |
| Fast Chaser | `enemy_spawnEnemyFast_m82_tree` | weighted side pounce, quick pounce, snap follow-up |
| Heavy Chaser | `enemy_spawnEnemyHeavy_m82_tree` | stomp, maul, shove, punishable pressure |
| Ash Charger | `enemy_spawnEnemyCharger_m82_tree` | charge first, close clash, direct pressure |
| Bone Turret | `enemy_spawnEnemyTurret_m82_tree` | stationary sentinel ranged budget |
| Husk Splitter | `enemy_spawnEnemySplitter_m82_tree` | splinter lunge or cleave |
| Spitting Pod | `enemy_spawnEnemySpittingPod_m82_tree` | stationary hearing-driven ballistic lob |
| Rat | `enemy_spawnEnemyRat_m82_tree` | warning squeal, bite, retreat when endangered |
| Spider | `enemy_spawnEnemySpider_m82_tree` | deterministic fight/flee, side-hop bite, hop, bite |

## Boss Metadata Trees

Boss definitions resolve metadata-only trees for documentation and M82 validation. Boss runtime remains unchanged.

| Boss | Tree | Runtime |
| --- | --- | --- |
| Stone Warden | `boss_stone_warden_m82_tree` | metadata-only, ignored by runtime |
| Splinter Saint | `boss_splinter_saint_m82_tree` | metadata-only, ignored by runtime |
| Gravel Maw | `boss_gravel_maw_m82_tree` | metadata-only, ignored by runtime |
| Cartouche Widow | `boss_cartouche_widow_m82_tree` | metadata-only, ignored by runtime |
| Iron Reliquary | `boss_iron_reliquary_m82_tree` | metadata-only, ignored by runtime |
| Mirror Husk | `boss_mirror_husk_m82_tree` | metadata-only, ignored by runtime |
| Ash Comet | `boss_ash_comet_m82_tree` | metadata-only, ignored by runtime |
| Choir of Teeth | `boss_choir_of_teeth_m82_tree` | metadata-only, ignored by runtime |
| Rust Bishop | `boss_rust_bishop_m82_tree` | metadata-only, ignored by runtime |
| Hollow Star Larva | `boss_hollow_star_larva_m82_tree` | metadata-only, ignored by runtime |

## Deferred

- No pathfinding, obstacle LOS, squad tactics, alert sharing, save schema changes, generic combo planner, or boss behavior rewrite is included.
- Future milestones can expand action selection with richer conditions and navigation adapters without replacing M80 committed attack windows.
