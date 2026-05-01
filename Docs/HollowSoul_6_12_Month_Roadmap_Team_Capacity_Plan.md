# Hollow Soul: 6-12 Month Roadmap + Team Capacity Plan

Date: 2026-05-01  
Audience: Internal production planning for Martin, Rafal, and Patrycja  
Status: Working roadmap for beta vertical-slice planning  
Source alignment: `HollowSoul_GameDesignFoundation_GDD_V1.md`

## 1. Executive Summary

An indie team of 3 can realistically pull **Hollow Soul** into a strong beta vertical slice in 6-12 months, but only if the goal is clear: **a coherent playable beta slice, not a finished commercial roguelite**.

The strongest plan is a **9-month core roadmap**:

1. Stabilize the current prototype.
2. Build the Derelict Sanctuary spaceship hub.
3. Make melee/combat/rewards feel good.
4. Art-pass a small beta content subset.
5. Lock, test, and hand off.

The 6-month version is a minimum slice. The 12-month version is a stretch polish and expansion path. The biggest risk is scope creep: too many items, bosses, biomes, mechanics, and documents before the basic combat/reward/ship loop feels excellent.

## 2. Capacity Assumptions

This plan uses the agreed team split:

- Martin: 40 hours/week.
- Rafal: 20 hours/week.
- Patrycja: 20 hours/week.
- 1 workday = 8 hours.
- Total team capacity = 80 hours/week = 10 team-days/week.

### Weekly Capacity

| Person | Hours/Week | Days/Week | Team Share |
|---|---:|---:|---:|
| Martin | 40 | 5.0 | 50% |
| Rafal | 20 | 2.5 | 25% |
| Patrycja | 20 | 2.5 | 25% |
| Total | 80 | 10.0 | 100% |

### Horizon Capacity

| Horizon | Weeks | Team-Days | Martin Days | Rafal Days | Patrycja Days |
|---|---:|---:|---:|---:|---:|
| 6 months | 26 | 260 | 130 | 65 | 65 |
| 9 months | 39 | 390 | 195 | 97.5 | 97.5 |
| 12 months | 52 | 520 | 260 | 130 | 130 |

These numbers are theoretical available days, not guaranteed productive feature days. Real production should reserve time for bugs, communication, rework, learning, asset import issues, builds, and emotional weather. The practical planning assumption should be that **15-25% of capacity becomes overhead**, especially around integration and QA.

## 3. Team Roles

### Martin

Primary ownership:

- Unity implementation.
- Runtime systems.
- Combat, progression, save/load, branch flow.
- Technical design and integration.
- Build stability.
- Final production decisions.

Martin should avoid becoming the bottleneck for every content decision. The project needs Martin focused on the systems that only he can realistically implement right now.

### Rafal

Primary ownership:

- ArtPass assets.
- Asset scale, pivot, wrapper, material, and texture compliance.
- Player, enemies, rooms, portals, chests, coins, bosses, weapons, doors, and props.
- Visual polish.
- Developer Lab visual inspection.

Rafal's work becomes much faster if the ArtPass wrapper contract is strict and every asset can be checked in Developer Lab without guessing.

### Patrycja

Primary ownership:

- Narrative and worldbuilding.
- RPG-feel review.
- Task organization and production hygiene.
- Content catalogues.
- Decision logs.
- Playtest notes.
- Milestone acceptance checklists.

Patrycja should not be treated as "only writing lore." A strong hybrid role for her is **creative producer / narrative design support**: someone who helps the game think clearly and helps the team remember what was decided.

## 4. 9-Month Core Roadmap

## Phase 1: Stabilize The Foundation, Months 1-2

Goal: stop adding broad systems and make the existing prototype reliable, readable, and art-passable.

Key work:

- ArtPass wrapper calibration and asset intake QA.
- Developer Lab coverage lock.
- Combat input/controller reliability.
- Basic held weapon visuals and melee/ranged presentation.
- Chest, coin, pickup, portal, and basic prop visual correctness.
- Debug UI cleanup.
- Controls reliability.

Work allocation:

| Person | Days |
|---|---:|
| Martin | 38 |
| Rafal | 22 |
| Patrycja | 18 |
| Total | 78 |

Success criteria:

- Developer Lab shows every important visible entity.
- Art prefabs can be replaced safely without investigation each time.
- Keyboard and controller combat work reliably.
- No major debug overlays or broken fallback visuals appear in normal gameplay.
- Basic chest/coin/pickup visuals are trustworthy.

Main risks:

- Legacy systems and generated assets may still produce compile or validation churn.
- ArtPass issues can consume Martin's time if the wrapper contract is not strict enough.
- Combat may feel technically functional but not satisfying yet.

Recommended milestone mapping:

- M56: ArtPass Wrapper Calibration + Asset Intake QA.
- M57: Developer Lab Coverage Lock.
- M59: Combat Input + Controller Reliability Pass.
- M67 follow-up: Held Weapon Visuals polish.

## Phase 2: Ship Hub + Core Identity, Months 3-4

Goal: introduce the Derelict Sanctuary as the real meta-home.

Key work:

- Spaceship Meta Hub Greybox V1.
- Move or mirror New Run, Continue, Challenges, Developer Lab, and portals into the ship structure.
- First ship modules: Portal Engine, Challenge Terminal, Item Archive, Memory Chamber.
- Basic ship progression framing using souls.
- First memory fragments, terminal labels, and ship-room descriptions.

Work allocation:

| Person | Days |
|---|---:|
| Martin | 40 |
| Rafal | 18 |
| Patrycja | 22 |
| Total | 80 |

Success criteria:

- Player can start from the ship, enter a run, return, and understand why the ship matters.
- The ship feels like the long-term home, not just a menu replacement.
- Patrycja maintains an active content/narrative tracker for ship modules, NPC hooks, and memory fragments.
- The ship can be greybox, but it should already communicate the game's identity.

Main risks:

- Building the ship as a full base too early could explode scope.
- Too many modules could become menus with 3D walls around them.
- Narrative can become over-explained if memory fragments are too literal.

Recommended milestone mapping:

- M69: Spaceship Meta Hub Greybox V1.
- M71 early framing: Soul fuel and ship-power language.
- M76 first pass: Ship module unlock shape, but not a deep upgrade economy yet.

## Phase 3: Combat Feel + Reward Loop, Months 5-6

Goal: make the main 30-second and 5-minute loops satisfying.

Key work:

- Melee-first rebalance.
- Better hit feedback, enemy reactions, slash visuals, held weapon use, and attack readability.
- Soul economy and consumption UX.
- Chest, reward, shop, and item pacing.
- Sparse room rewards with meaningful treasure, boss, and shop moments.
- First cursed/optional-risk reward prototypes if time allows.

Work allocation:

| Person | Days |
|---|---:|
| Martin | 45 |
| Rafal | 15 |
| Patrycja | 18 |
| Total | 78 |

Success criteria:

- Slashing enemies feels good.
- Ranged attacks complement melee rather than replacing it.
- Souls feel important, visible, and readable.
- Rewards feel rare enough to matter.
- A 20-30 minute test run has understandable pacing.
- Shops feel useful but not mandatory.

Main risks:

- Combat can become numerically balanced but emotionally flat.
- Too many item effects can hide weak base combat.
- Souls can feel like another currency unless the UX and fiction are strong.

Recommended milestone mapping:

- M70: Combat Feel + Melee-First Rebalance.
- M71: Soul Economy + Consumption UX.
- M58 follow-up: Beta Reward Economy + Chest Balance.
- M77 small prototype: Secrets, Cursed Items, And Optional Risk V1, only if core feel is solid.

## Phase 4: World, Enemy, Boss Beta Subset, Months 7-8

Goal: stop trying to polish everything and choose the beta subset.

Key work:

- Pick 2-3 beta biome identities.
- Art pass for floors, walls, portals, doors, chests, coins, player, 3-5 enemies, and 2-3 bosses.
- Enemy readability pass.
- Boss beta subset polish: Stone Warden plus 2-3 others.
- Boss Lab V2.
- World and branch portal visual hints.

Work allocation:

| Person | Days |
|---|---:|
| Martin | 35 |
| Rafal | 30 |
| Patrycja | 15 |
| Total | 80 |

Success criteria:

- The beta content has visual identity, not greybox soup.
- Enemies are readable and distinct.
- At least 2-3 bosses feel intentionally designed.
- Portal and world choices begin to feel like adventure decisions.
- The team knows exactly which content is in beta and which content is deferred.

Main risks:

- Trying to art-pass all worlds and all bosses will break the schedule.
- Boss polish can become a black hole if every boss is treated equally.
- Biomes can become palette swaps unless material, lighting, props, enemies, and portal presentation are coordinated.

Recommended milestone mapping:

- M72: Art Direction Bible + Asset Scale Contract.
- M73: First Biome Identity Pass.
- M74: Enemy Identity + Readability Pass.
- M75: Boss Beta Subset Polish.

## Phase 5: Beta Lock + QA Handoff, Month 9

Goal: make the slice testable by people outside the implementation loop.

Key work:

- Beta content selection lock.
- Vertical slice beta lock gate.
- Save/continue, challenge, Developer Lab, Boss Lab, Room Designer, shop, chest, boss clear, and ship-return smoke checks.
- Known issues list.
- Controls sheet.
- Tester instructions.
- Updated GDD, roadmap, content catalogue, and QA PDFs.

Work allocation:

| Person | Days |
|---|---:|
| Martin | 37 |
| Rafal | 12 |
| Patrycja | 13 |
| Total | 62 |

Success criteria:

- Someone outside the implementation loop can launch, play, test, and report bugs.
- The build has a clear beginning, loop, boss moment, reward moment, and return-to-ship moment.
- Scope is frozen for beta except bug fixes and obvious polish.
- Reports say what is ready, what is placeholder, and what is known broken.

Main risks:

- The team may be tempted to add "one last feature" instead of fixing the slice.
- QA can reveal that earlier assumptions were too optimistic.
- Build/device issues can consume time late unless smoke checks are kept alive throughout the roadmap.

Recommended milestone mapping:

- M63: Beta Content Selection Lock.
- M64: Vertical Slice Beta Lock Gate.
- M65: Beta Handoff Build + QA Pack.
- M78: Vertical Slice Vision Lock + Team Handoff.

## 5. 6-Month Minimum Slice

If aiming for 6 months, cut aggressively.

Must keep:

- Stable combat.
- Ship hub greybox.
- One complete run loop.
- Sparse rewards, chests, coins, souls.
- 1-2 polished bosses.
- Developer Lab.
- Basic ArtPass for core objects.
- Save/continue.
- QA checklist.

Cut or delay:

- Large boss roster polish.
- Many biomes.
- Advanced cursed items.
- Deep NPC systems.
- Large item expansion.
- Multiple ship modules beyond basic portals, challenges, and archive.
- Steam-demo-level onboarding.

6-month capacity:

| Person | Days |
|---|---:|
| Martin | 130 |
| Rafal | 65 |
| Patrycja | 65 |
| Total | 260 |

Recommended 6-month target:

> A playable internal beta slice with ship start, one stable run loop, sparse rewards, 1-2 polished bosses, basic ArtPass, and enough QA documentation for trusted playtesters.

## 6. 12-Month Stretch

If the team sustains the pace for 12 months, months 10-12 should be used for polish and expansion, not random new systems.

Stretch goals:

- 3 polished biomes.
- 4-5 polished bosses.
- Stronger ship module unlocks.
- First recruitable NPC or entity.
- Better cursed/secret content.
- More production art replacements.
- Steam-demo-level onboarding and presentation.
- External playtest pass.

12-month capacity:

| Person | Days |
|---|---:|
| Martin | 260 |
| Rafal | 130 |
| Patrycja | 130 |
| Total | 520 |

Recommended 12-month target:

> A public-facing demo candidate, assuming the 9-month beta slice is stable and fun enough to deserve polish.

## 7. 9-Month Category Budget

Approximate budget: 390 team-days.

| Category | Team-Days | Main Owner | Notes |
|---|---:|---|---|
| Core Unity systems, stability, saves, progression | 85 | Martin | Highest risk; keep focused and avoid new large systems late. |
| Combat feel, weapons, enemies, bosses | 75 | Martin | Must become satisfying before item/boss expansion continues. |
| Ship hub and meta-progression | 55 | Martin + Patrycja | Greybox first, emotional clarity second, deep systems later. |
| ArtPass assets, scale QA, visual polish | 75 | Rafal | Needs strict intake contract and Developer Lab checks. |
| Rewards, economy, items, chests, shops | 35 | Martin + Patrycja | Tune meaning and pacing before expanding item volume. |
| Narrative, world identity, content writing | 30 | Patrycja | Memory fragments, NPC hooks, boss/world text, content names. |
| QA, docs, build handoff, task organization | 35 | Patrycja + Martin | Essential for making the beta testable. |
| Total | 390 | Team | 9-month theoretical team capacity. |

## 8. Per-Person 9-Month Allocation

### Martin - 195 Days

Recommended allocation:

- Core systems and integration: 55 days.
- Combat and enemy/boss implementation: 45 days.
- Ship hub and meta systems: 35 days.
- Rewards/economy/saves: 20 days.
- QA/build/debug/validation: 25 days.
- Planning and rework buffer: 15 days.

Guardrail:

- Martin should not spend too much time manually fixing asset scale/material/prefab issues once the ArtPass contract exists.

### Rafal - 97.5 Days

Recommended allocation:

- Asset pipeline and wrapper compliance: 15 days.
- Player, weapons, chest, coins, portals, doors, core props: 20 days.
- Enemy and boss visuals: 25 days.
- Floors, walls, rooms, biome material pass: 22.5 days.
- Visual polish, fixes, Developer Lab checks: 15 days.

Guardrail:

- Rafal should focus on a small beta art set first, not every possible enemy, boss, item, and biome.

### Patrycja - 97.5 Days

Recommended allocation:

- Production organization, task tracking, decision logs: 20 days.
- Narrative/worldbuilding and memory fragments: 20 days.
- Content catalogues: items, bosses, rooms, NPC hooks, ship modules: 18 days.
- RPG-feel review and playtest notes: 17 days.
- QA checklists, tester instructions, handoff docs: 17.5 days.
- Buffer and team communication: 5 days.

Guardrail:

- Patrycja should be given real ownership of organization and content clarity, not only occasional feedback requests.

## 9. Practical Production Rules

These rules make the roadmap survivable:

- Every month ends with a playable build or at least a playable test route.
- Developer Lab remains the truth for visual inspection.
- No new major system enters after Month 6 unless it replaces something worse.
- Beta content gets whitelisted; everything else is deferred.
- Art assets must pass scale/pivot/material/wrapper checks before integration.
- Combat feel beats item volume.
- Boss polish beats boss count.
- Ship clarity beats deep base-building.
- Souls must become meaningful before adding more currencies.
- Patrycja owns tracking enough that decisions stop disappearing into chat history.

## 10. Can This Team Pull It Off?

Yes, if the team accepts the actual target:

> A strong beta vertical slice that proves the fantasy, loop, combat feel, reward pacing, ship framing, and art direction.

No, if the target silently becomes:

> A finished roguelite with dozens of bosses, many characters, hundreds of items, full NPC systems, deep meta-progression, many biomes, and polished public release quality.

The project can become real in 6-12 months, but only by making the slice smaller, sharper, and more emotionally complete.

The recommended bet:

- 6 months: internal beta slice.
- 9 months: strong beta vertical slice with team handoff.
- 12 months: public demo candidate, only if the 9-month slice is stable and fun.

## 11. Immediate Next Actions

Next 2 weeks:

- Lock the beta-slice target in writing.
- Turn this roadmap into a task tracker.
- Assign ownership for Phase 1.
- Confirm the ArtPass asset acceptance checklist.
- Confirm the Developer Lab inspection list.
- Pick the first 1-2 bosses to polish.
- Create Patrycja's first content/admin tracker.
- Stop adding new broad systems until Phase 1 is stable.

Suggested first ownership split:

- Martin: Phase 1 technical stability and combat/input reliability.
- Rafal: ArtPass wrapper calibration and first asset set.
- Patrycja: roadmap tracker, decision log, content list, and first playtest checklist.
