# M81: Enemy Action Profiles V2

M81 adds a data-only action-profile layer above M76 attack profiles. Existing attack profiles remain the source of damage, timing, poise, knockback, guard recoil, and impact classification. Runtime AI does not change in M81.

## Action Taxonomy

- **Body**: Body-only and creature pressure actions such as Bite, Pounce, Slam, and Shove.
- **Weapon**: Weapon-user actions for skeletons, knights, giants, duelists, and future humanoids.
- **Ranged**: Aimed weapon fire such as Arrow Shot, Musket Shot, and Cannon Shot.
- **Projectile**: Pattern pressure such as Spread Shot, Fan Shot, Radial Burst, and Falling Mark.
- **Magic**: Cast actions such as Beam, Curse Field, Ground Eruption, and Magic Counter.
- **Movement**: Repositioning such as Sidestep, Backstep, Teleport, Burrow, and Fly Strafe.
- **Defense**: Guard, Brace, Parry, Counter Stance, and other punishable defensive choices.
- **Summon**: Spawn, split, and add-management actions with room pressure budgets.
- **Hazard**: Area setup such as Acid Puddle, Fire Patch, Mine, and Falling Debris.
- **GhostSoul**: Ghost/soul behavior such as Phase, Possess, Soul Drain, Curse, Fear Pulse, and re-form.
- **BossScale**: Large arena-readable attacks such as Shockwave, Arena Hazard, and Desperation Burst.

## Current Roster Action Profiles

| Owner | Action | Category | Intent | Shape | Usage | Linked attack | Counterplay |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Normal Chaser | Claw Lunge | Body | Damage | ForwardArc | CurrentRuntime | claw_lunge | Dodge or block the active arc, then punish recovery. |
| Normal Chaser | Desperate Bite | Body | Damage | ForwardArc | CurrentRuntime | desperate_bite | Stay outside close bite range. |
| Normal Chaser | Claw Combo | Body | Pressure | ForwardArc | FutureCandidate | - | Future combo must expose a clear final recovery. |
| Flying Chaser | Panic Peck | Body | Damage | ForwardArc | CurrentRuntime | panic_peck | Endangered prey commits only briefly. |
| Flying Chaser | Dive Scratch | Body | Damage | ForwardArc | CurrentRuntime | dive_scratch | Sidestep the dive line. |
| Flying Chaser | Panic Retreat | Movement | Escape | Self | FutureCandidate | - | Read the retreat burst before re-engaging. |
| Fast Chaser | Quick Pounce | Body | Damage | ForwardArc | CurrentRuntime | quick_pounce | Fast but light; punish whiffs quickly. |
| Fast Chaser | Needle Rush | Body | Damage | ForwardArc | CurrentRuntime | needle_rush | Avoid being baited into the rush lane. |
| Fast Chaser | Evasive Skitter | Movement | Reposition | Self | FutureCandidate | - | Track landing rather than swinging early. |
| Heavy Chaser | Body Slam | Body | Damage | ForwardArc | CurrentRuntime | body_slam | High guard pressure, long recovery. |
| Heavy Chaser | Maul Lunge | Body | Damage | ForwardArc | CurrentRuntime | maul_lunge | Dodge late and punish commitment. |
| Heavy Chaser | Stomp | Body | Pressure | CircleArea | FutureCandidate | - | Future stomp needs a visible foot lift. |
| Ash Charger | Ash Charge | Body | Damage | Lane | CurrentRuntime | ash_charge | Move off the charge lane. |
| Ash Charger | Ember Clash | Body | Damage | ForwardArc | CurrentRuntime | ember_clash | Short close control hit. |
| Ash Charger | Fire Trail Charge | Hazard | HazardSetup | Lane | FutureCandidate | - | Do not chase through the future fire lane. |
| Bone Turret | Bone Dart | Projectile | Damage | Projectile | CurrentRuntime | bone_dart | Strafe the aimed shot. |
| Bone Turret | Rattle Volley | Projectile | Pressure | Projectile | CurrentRuntime | rattle_volley | Move through gaps and respect ranged budget. |
| Bone Turret | Aimed Bone Shot | Ranged | Damage | Projectile | FutureCandidate | - | Future ranged option stays stationary. |
| Husk Splitter | Husk Cleave | Body | Damage | ForwardArc | CurrentRuntime | husk_cleave | Dodge the cleave arc. |
| Husk Splitter | Death Split | Summon | Summon | CircleArea | CurrentRuntime | death_split | Room clear must account for split children. |
| Husk Splitter | Splinter Burst | Hazard | HazardSetup | Radial | FutureCandidate | - | Read radial gaps before committing. |
| Spitting Pod | Spit Lob | Projectile | Damage | Projectile | CurrentRuntime | spit_lob | Move before the ballistic landing point. |
| Spitting Pod | Seed Burst | Projectile | Pressure | Radial | FutureCandidate | - | Budgeted pod pressure with gaps. |
| Rat | Rat Bite | Body | Damage | ForwardArc | CurrentRuntime | rat_bite | Wait for territorial warning, then punish bite recovery. |
| Rat | Warning Squeal | Body | Feint | Cone | FutureCandidate | - | Non-damaging warning before attack selection. |
| Rat | Skitter Retreat | Movement | Escape | Self | FutureCandidate | - | Damage should make rats retreat readily. |
| Spider | Startle Hop | Body | Damage | ForwardArc | CurrentRuntime | startle_hop | Side-step the hop and punish recovery. |
| Spider | Close Bite | Body | Damage | ForwardArc | CurrentRuntime | close_bite | Do not stand inside bite range. |
| Spider | Panic Flee | Movement | Escape | Self | FutureCandidate | - | Fight-or-flight stays readable and capped. |

## Boss Action Profiles

| Owner | Action | Category | Intent | Shape | Usage | Linked attack | Counterplay |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Stone Warden | Stone Charge | BossScale | Damage | Lane | CurrentRuntime | stone_charge | Roll out of lane; punish recovery. |
| Stone Warden | Stone Shockwave | BossScale | Pressure | Radial | FutureCandidate | - | Radial wave needs safe ring timing. |
| Splinter Saint | Side-Hop Radial | BossScale | Pressure | Radial | CurrentRuntime | splinter_side_hop_radial | Move through radial gaps. |
| Splinter Saint | Splinter Dash Feint | BossScale | Feint | Lane | FutureCandidate | - | False dash should not deal damage. |
| Gravel Maw | Burrow Summon | BossScale | Summon | CircleArea | CurrentRuntime | gravel_burrow_summon | Pressure summon windows without losing room clear. |
| Gravel Maw | Gravel Emerge Bite | BossScale | Damage | ForwardArc | FutureCandidate | - | Ground tell before emerge attack. |
| Cartouche Widow | Falling Marks | BossScale | HazardSetup | Fan | CurrentRuntime | cartouche_falling_marks | Keep moving through target marks. |
| Cartouche Widow | Cartouche Mark Delay | BossScale | HazardSetup | TargetPoint | FutureCandidate | - | Delayed marks create dodge timing. |
| Iron Reliquary | Peek Shot | BossScale | Pressure | Fan | CurrentRuntime | iron_peek_shot | Punish reload/cover reset. |
| Iron Reliquary | Iron Bash Recover | BossScale | Interrupt | ForwardArc | FutureCandidate | - | Close bash punishes greedy pressure. |
| Mirror Husk | Mirror Chase Contact | BossScale | Damage | ForwardArc | CurrentRuntime | mirror_chase_contact | M79 keeps ordinary chase overlap harmless unless active. |
| Mirror Husk | Mirror Decoy | BossScale | Feint | Self | FutureCandidate | - | Misdirection needs strong readability. |
| Ash Comet | Comet Dash | BossScale | Damage | Lane | CurrentRuntime | ash_comet_dash | Move off dash line; fire identity remains data. |
| Ash Comet | Ash Fire Trail | BossScale | HazardSetup | Lane | FutureCandidate | - | Temporary hazard lane should leave safe routes. |
| Choir of Teeth | Rotating Hymn | BossScale | Pressure | Radial | CurrentRuntime | choir_rotating_hymn | Move with the rotating gap. |
| Choir of Teeth | Choir Silence Pulse | BossScale | Interrupt | Radial | FutureCandidate | - | Audio drop before pulse. |
| Rust Bishop | Rust Beam | BossScale | Pressure | Lane | CurrentRuntime | rust_beam | Strafe perpendicular to beam line. |
| Rust Bishop | Rust Hazard Minefield | BossScale | HazardSetup | HazardZone | FutureCandidate | - | Mines need arming tells. |
| Hollow Star Larva | Starfall | BossScale | Pressure | Fan | CurrentRuntime | larva_starfall | Read cosmic projectile fan. |
| Hollow Star Larva | Larva Void Pulse | BossScale | Pressure | Radial | FutureCandidate | - | Void pulse needs clear safe radius. |

## Counterplay And Scoring Contract

- Actions carry AI scoring metadata: min range, ideal range, max range, weight, pressure cost, cooldown group, minimum intelligence, allowed dispositions, minimum awareness, and facing requirements.
- Actions carry Dark Souls-style counterplay metadata: telegraph note, punishability rating, guard pressure rating, poise break note, parryable, blockable, dodgeable, and recovery punish note.
- Linked actions reference M76 attacks. Unlinked future actions and templates are explicitly non-damaging until an attack profile or behavior implementation exists.
- M82 can select from this layer without changing M81 runtime behavior.

## Reusable Future Action Templates

### Body

- **Bite**: Category `Body`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: rats, spiders, beasts, undead crows.
- **Claw**: Category `Body`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: rats, spiders, beasts, undead crows.
- **Peck**: Category `Body`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: rats, spiders, beasts, undead crows.
- **Pounce**: Category `Body`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: rats, spiders, beasts, undead crows.
- **Tail Swipe**: Category `Body`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: rats, spiders, beasts, undead crows.
- **Body Slam**: Category `Body`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: rats, spiders, beasts, undead crows.

### Weapon

- **Light Slash**: Category `Weapon`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: skeletons, knights, giants.
- **Heavy Slash**: Category `Weapon`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: skeletons, knights, giants.
- **Thrust**: Category `Weapon`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: skeletons, knights, giants.
- **Overhead Slash**: Category `Weapon`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: skeletons, knights, giants.
- **Sweep**: Category `Weapon`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: skeletons, knights, giants.
- **Shield Bash**: Category `Weapon`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: skeletons, knights, giants.

### Ranged

- **Arrow Shot**: Category `Ranged`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: archers, gunslingers, machines.
- **Arrow Volley**: Category `Ranged`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: archers, gunslingers, machines.
- **Aimed Shot**: Category `Ranged`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: archers, gunslingers, machines.
- **Thrown Knife**: Category `Ranged`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: archers, gunslingers, machines.
- **Pistol Shot**: Category `Ranged`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: archers, gunslingers, machines.
- **Cannon Shot**: Category `Ranged`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: archers, gunslingers, machines.

### Projectile

- **Slow Orb**: Category `Projectile`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: turrets, pods, wizards, bosses.
- **Fast Bolt**: Category `Projectile`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: turrets, pods, wizards, bosses.
- **Spread Shot**: Category `Projectile`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: turrets, pods, wizards, bosses.
- **Radial Burst**: Category `Projectile`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: turrets, pods, wizards, bosses.
- **Homing Shot**: Category `Projectile`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: turrets, pods, wizards, bosses.
- **Falling Mark**: Category `Projectile`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: turrets, pods, wizards, bosses.

### Magic

- **Beam**: Category `Magic`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: wizards, cultists, soul eaters.
- **Fire Trail**: Category `Magic`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: wizards, cultists, soul eaters.
- **Curse Field**: Category `Magic`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: wizards, cultists, soul eaters.
- **Ground Eruption**: Category `Magic`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: wizards, cultists, soul eaters.
- **Summoned Orb**: Category `Magic`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: wizards, cultists, soul eaters.
- **Magic Counter**: Category `Magic`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: wizards, cultists, soul eaters.

### Movement

- **Sidestep**: Category `Movement`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: creatures, knights, ghosts.
- **Backstep**: Category `Movement`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: creatures, knights, ghosts.
- **Roll**: Category `Movement`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: creatures, knights, ghosts.
- **Circle**: Category `Movement`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: creatures, knights, ghosts.
- **Teleport**: Category `Movement`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: creatures, knights, ghosts.
- **Burrow**: Category `Movement`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: creatures, knights, ghosts.

### Defense

- **Guard**: Category `Defense`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: knights, machines, bosses.
- **Brace**: Category `Defense`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: knights, machines, bosses.
- **Parry**: Category `Defense`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: knights, machines, bosses.
- **Evade**: Category `Defense`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: knights, machines, bosses.
- **Shield Wall**: Category `Defense`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: knights, machines, bosses.
- **Counter Stance**: Category `Defense`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: knights, machines, bosses.

### Summon

- **Summon Minion**: Category `Summon`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: necromancers, pods, bosses.
- **Summon Wave**: Category `Summon`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: necromancers, pods, bosses.
- **Raise Skeleton**: Category `Summon`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: necromancers, pods, bosses.
- **Spawn Trap**: Category `Summon`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: necromancers, pods, bosses.
- **Call Swarm**: Category `Summon`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: necromancers, pods, bosses.
- **Clone Split**: Category `Summon`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: necromancers, pods, bosses.

### Hazard

- **Spike Trap**: Category `Hazard`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: pods, machines, casters, bosses.
- **Acid Puddle**: Category `Hazard`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: pods, machines, casters, bosses.
- **Fire Patch**: Category `Hazard`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: pods, machines, casters, bosses.
- **Mine**: Category `Hazard`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: pods, machines, casters, bosses.
- **Falling Debris**: Category `Hazard`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: pods, machines, casters, bosses.
- **Closing Wall**: Category `Hazard`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: pods, machines, casters, bosses.

### GhostSoul

- **Phase**: Category `GhostSoul`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: ghosts, soul eaters, mirror enemies.
- **Possess**: Category `GhostSoul`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: ghosts, soul eaters, mirror enemies.
- **Soul Drain**: Category `GhostSoul`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: ghosts, soul eaters, mirror enemies.
- **Curse**: Category `GhostSoul`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: ghosts, soul eaters, mirror enemies.
- **Fear Pulse**: Category `GhostSoul`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: ghosts, soul eaters, mirror enemies.
- **Re-form**: Category `GhostSoul`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: ghosts, soul eaters, mirror enemies.

### BossScale

- **Shockwave**: Category `BossScale`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: bosses and giant enemies.
- **Arena Hazard**: Category `BossScale`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: bosses and giant enemies.
- **Multi-Stage Combo**: Category `BossScale`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: bosses and giant enemies.
- **Desperation Burst**: Category `BossScale`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: bosses and giant enemies.
- **Rotating Pattern**: Category `BossScale`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: bosses and giant enemies.
- **Boss Grab**: Category `BossScale`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: bosses and giant enemies.

## M82 Readiness

M81 deliberately stops at data, catalogue, validation, and behavior-tree readiness. M82 can layer selector/sequence logic over awareness, intelligence, disposition, cooldowns, range bands, and pressure budgets.
