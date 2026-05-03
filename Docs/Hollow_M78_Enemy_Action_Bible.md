# M78: Enemy Action Bible + Combat Behavior Roadmap

M78 is a design-only enemy action bible. It defines a broad catalogue of future enemy attacks, actions, commands, and solo tactics without changing runtime behavior yet.

The core combat direction is that most enemy body contact should not automatically damage the player. Contact should usually disturb, alert, bump, or reposition. Damage should come from explicit active hit windows, hazardous bodies, grabs, projectiles, area hazards, spells, weapons, traps, and boss-scale states.

Coverage tags: body-only, weapon-user, ranged, magic, ghost/soul, mechanical, boss-scale.

## Design Rules

- Prefer plain action names over lore names.
- Keep readable telegraphs, active windows, and recovery windows.
- Reserve passive contact damage for explicit hazardous bodies such as spikes, fire, acid, curse, electricity, or crushing mass.
- Bumping a normal enemy should disturb or alert it, not automatically hurt the player.
- The player should be able to control how many enemies they wake through proximity, movement noise, and attacks, without adding full stealth UI in M78.
- Future behavior tree work should sit on top of current awareness, intelligence, disposition, attack profiles, movement intent, and attack budgets.

## Action Cards

### 001. Bite

- Category: Body
- Best enemy users: rats, spiders, beasts, undead crows, ghouls
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M80

### 002. Claw Swipe

- Category: Body
- Best enemy users: beasts, ghouls, demons, undead crows
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M80

### 003. Double Claw

- Category: Body
- Best enemy users: beasts, demons, fast undead
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M85

### 004. Peck

- Category: Body
- Best enemy users: undead crows, birds, small flyers
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M85

### 005. Pounce

- Category: Body
- Best enemy users: rats, spiders, wolves, cats, beasts
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M80

### 006. Leap Attack

- Category: Body
- Best enemy users: spiders, beasts, frog creatures, assassins
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M85

### 007. Dive Attack

- Category: Body
- Best enemy users: undead crows, bats, flying demons
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M85

### 008. Gore

- Category: Body
- Best enemy users: boars, horned beasts, demons
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M85

### 009. Tail Swipe

- Category: Body
- Best enemy users: beasts, dragons, lizards, scorpions
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M85

### 010. Body Slam

- Category: Body
- Best enemy users: heavy beasts, giants, slimes, armored brutes
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M80

### 011. Belly Flop

- Category: Body
- Best enemy users: large beasts, giants, grotesque enemies
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M85

### 012. Stomp

- Category: Body
- Best enemy users: giants, trolls, bosses, heavy undead
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M80

### 013. Kick

- Category: Body
- Best enemy users: humanoids, beasts, skeletons
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 014. Wing Buffet

- Category: Body
- Best enemy users: undead crows, harpies, winged beasts
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M85

### 015. Headbutt

- Category: Body
- Best enemy users: skeletons, beasts, armored creatures
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M85

### 016. Shoulder Check

- Category: Body
- Best enemy users: knights, brutes, giants, shield users
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 017. Tentacle Lash

- Category: Body
- Best enemy users: sea creatures, horrors, soul eaters
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M87

### 018. Tongue Lash

- Category: Body
- Best enemy users: frogs, leeches, mutants
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M85

### 019. Web Shot

- Category: Body
- Best enemy users: spiders, web casters
- Contact policy: Projectile
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M85

### 020. Spit

- Category: Body
- Best enemy users: pods, insects, beasts, corrupted creatures
- Contact policy: Projectile
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M86

### 021. Acid Spit

- Category: Body
- Best enemy users: slimes, insects, alchemic creatures
- Contact policy: Projectile or hazard
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M87

### 022. Burrow Emerge

- Category: Body
- Best enemy users: worms, moles, grave creatures
- Contact policy: Active hit window
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M88

### 023. Grab

- Category: Body
- Best enemy users: ghouls, giants, mimics, soul eaters
- Contact policy: Grab
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: requires explicit grab state, escape/cancel rules, and no passive contact damage.
- Likely milestone priority: M80

### 024. Drag

- Category: Body
- Best enemy users: leeches, ghosts, tentacle beasts
- Contact policy: Grab
- Telegraph/readability: Body crouch, head pullback, limb raise, or short inhale before the active frame.
- Counterplay: Step out of reach, circle behind, block light hits, or punish the recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage. Suggested AI: requires explicit grab state, escape/cancel rules, and no passive contact damage.
- Likely milestone priority: M87

### 025. Light Slash

- Category: Weapon
- Best enemy users: skeletons, knights, bandits, cultists
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 026. Heavy Slash

- Category: Weapon
- Best enemy users: knights, giants, executioners
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 027. Overhead Slash

- Category: Weapon
- Best enemy users: skeletons, knights, giants, axe users
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 028. Thrust

- Category: Weapon
- Best enemy users: spear users, rapier enemies, soldiers
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 029. Sweep

- Category: Weapon
- Best enemy users: halberd users, giants, scythe enemies
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 030. Cleave

- Category: Weapon
- Best enemy users: axe users, heavy skeletons, brutes
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 031. Spear Jab

- Category: Weapon
- Best enemy users: spear skeletons, guards, hunters
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 032. Lance Charge

- Category: Weapon
- Best enemy users: mounted enemies, knights, constructs
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M86

### 033. Shield Bash

- Category: Weapon
- Best enemy users: knights, guards, tower shield enemies
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 034. Parry

- Category: Weapon
- Best enemy users: elite knights, duelists, skeleton captains
- Contact policy: Counter state
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 035. Riposte

- Category: Weapon
- Best enemy users: duelists, elite skeletons, assassins
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 036. Weapon Kick

- Category: Weapon
- Best enemy users: humanoid fighters, shield breakers
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 037. Two-Hit Combo

- Category: Weapon
- Best enemy users: skeletons, knights, bandits
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: requires chained active windows with a readable stop point.
- Likely milestone priority: M84

### 038. Three-Hit Combo

- Category: Weapon
- Best enemy users: elite knights, bosses, assassins
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: requires chained active windows with a readable stop point.
- Likely milestone priority: M84

### 039. Feint

- Category: Weapon
- Best enemy users: duelists, trickster enemies, ghosts with weapons
- Contact policy: No hit unless followed
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M82

### 040. Spinning Attack

- Category: Weapon
- Best enemy users: dual-blade enemies, dancers, skeleton elites
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 041. Backhand Slash

- Category: Weapon
- Best enemy users: knights, giants, bosses
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 042. Axe Hook

- Category: Weapon
- Best enemy users: axe enemies, butchers, giants
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 043. Hammer Slam

- Category: Weapon
- Best enemy users: giants, clerics, stone soldiers
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 044. Mace Crush

- Category: Weapon
- Best enemy users: armored undead, clerics, brutes
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 045. Scythe Reap

- Category: Weapon
- Best enemy users: reapers, ghosts, cultists
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M87

### 046. Dagger Flurry

- Category: Weapon
- Best enemy users: assassins, rats with knives, thieves
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 047. Whip Crack

- Category: Weapon
- Best enemy users: cultists, beast tamers, ghost hunters
- Contact policy: Active hit window
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M84

### 048. Thrown Weapon Followup

- Category: Weapon
- Best enemy users: bandits, skeletons, hunters
- Contact policy: Projectile
- Telegraph/readability: Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.
- Counterplay: Respect range, dodge through the active arc, block if stable, then punish recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Weapon-user enemies need facing, reach, recovery, and readable weapon arcs. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M86

### 049. Arrow Shot

- Category: Ranged
- Best enemy users: archers, skeleton archers, hunters
- Contact policy: Projectile
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 050. Arrow Volley

- Category: Ranged
- Best enemy users: archers, commanders, bosses
- Contact policy: Projectile pattern
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 051. Aimed Shot

- Category: Ranged
- Best enemy users: archers, gunslingers, elite hunters
- Contact policy: Projectile
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 052. Quick Shot

- Category: Ranged
- Best enemy users: archers, goblins, gunslingers
- Contact policy: Projectile
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 053. Crossbow Bolt

- Category: Ranged
- Best enemy users: crossbow skeletons, soldiers
- Contact policy: Projectile
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 054. Thrown Knife

- Category: Ranged
- Best enemy users: assassins, thieves, cultists
- Contact policy: Projectile
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 055. Thrown Spear

- Category: Ranged
- Best enemy users: spear soldiers, giants, hunters
- Contact policy: Projectile
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 056. Thrown Axe

- Category: Ranged
- Best enemy users: raiders, skeleton brutes, giants
- Contact policy: Projectile
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 057. Bomb Throw

- Category: Ranged
- Best enemy users: alchemists, goblins, machines
- Contact policy: Projectile or area
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 058. Pistol Shot

- Category: Ranged
- Best enemy users: gunslingers, clockwork guards
- Contact policy: Projectile
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 059. Musket Shot

- Category: Ranged
- Best enemy users: riflemen, undead soldiers
- Contact policy: Projectile
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 060. Shotgun Blast

- Category: Ranged
- Best enemy users: gunners, constructs, bosses
- Contact policy: Projectile fan
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 061. Cannon Shot

- Category: Ranged
- Best enemy users: siege machines, giants, bosses
- Contact policy: Projectile or area
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Massive force; suggested knockback 0.95-1.40m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 062. Trap Launch

- Category: Ranged
- Best enemy users: machines, wall traps, ambush enemies
- Contact policy: Projectile
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 063. Net Throw

- Category: Ranged
- Best enemy users: hunters, trappers, spiders, bandits
- Contact policy: Projectile control
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 064. Harpoon Shot

- Category: Ranged
- Best enemy users: machines, fishers, sea horrors
- Contact policy: Projectile or grab
- Telegraph/readability: Aim line, reload pose, barrel/bow lift, or hand throw windup before release.
- Counterplay: Strafe, break aim timing, use cover when available, or punish reload.
- Suggested impact: Physical/Projectile; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 065. Slow Orb

- Category: Projectile
- Best enemy users: casters, bosses, soul enemies
- Contact policy: Projectile
- Telegraph/readability: Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.
- Counterplay: Read the pattern, move through safe lanes, block single heavy shots when stable.
- Suggested impact: Physical/Projectile; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 066. Fast Bolt

- Category: Projectile
- Best enemy users: wizards, turrets, ghosts
- Contact policy: Projectile
- Telegraph/readability: Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.
- Counterplay: Read the pattern, move through safe lanes, block single heavy shots when stable.
- Suggested impact: Physical/Projectile; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 067. Spread Shot

- Category: Projectile
- Best enemy users: casters, pods, mechanical enemies
- Contact policy: Projectile pattern
- Telegraph/readability: Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.
- Counterplay: Read the pattern, move through safe lanes, block single heavy shots when stable.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 068. Fan Shot

- Category: Projectile
- Best enemy users: wizards, archers, bosses
- Contact policy: Projectile pattern
- Telegraph/readability: Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.
- Counterplay: Read the pattern, move through safe lanes, block single heavy shots when stable.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 069. Radial Burst

- Category: Projectile
- Best enemy users: bosses, casters, exploding enemies
- Contact policy: Projectile pattern
- Telegraph/readability: Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.
- Counterplay: Read the pattern, move through safe lanes, block single heavy shots when stable.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 070. Homing Shot

- Category: Projectile
- Best enemy users: ghosts, soul eaters, wizards
- Contact policy: Projectile
- Telegraph/readability: Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.
- Counterplay: Read the pattern, move through safe lanes, block single heavy shots when stable.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 071. Boomerang Shot

- Category: Projectile
- Best enemy users: hunters, ghosts, machines
- Contact policy: Projectile
- Telegraph/readability: Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.
- Counterplay: Read the pattern, move through safe lanes, block single heavy shots when stable.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 072. Bouncing Shot

- Category: Projectile
- Best enemy users: machines, trickster casters
- Contact policy: Projectile
- Telegraph/readability: Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.
- Counterplay: Read the pattern, move through safe lanes, block single heavy shots when stable.
- Suggested impact: Physical/Projectile; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 073. Splitting Shot

- Category: Projectile
- Best enemy users: casters, bosses, corrupted pods
- Contact policy: Projectile pattern
- Telegraph/readability: Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.
- Counterplay: Read the pattern, move through safe lanes, block single heavy shots when stable.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 074. Delayed Shot

- Category: Projectile
- Best enemy users: wizards, traps, bosses
- Contact policy: Projectile
- Telegraph/readability: Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.
- Counterplay: Read the pattern, move through safe lanes, block single heavy shots when stable.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 075. Ballistic Lob

- Category: Projectile
- Best enemy users: spitting pods, giants, artillery machines
- Contact policy: Projectile or area
- Telegraph/readability: Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.
- Counterplay: Read the pattern, move through safe lanes, block single heavy shots when stable.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 076. Projectile Wall

- Category: Projectile
- Best enemy users: bosses, casters, mechanical gates
- Contact policy: Projectile pattern
- Telegraph/readability: Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.
- Counterplay: Read the pattern, move through safe lanes, block single heavy shots when stable.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 077. Rotating Pattern

- Category: Projectile
- Best enemy users: bosses, machines, occult casters
- Contact policy: Projectile pattern
- Telegraph/readability: Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.
- Counterplay: Read the pattern, move through safe lanes, block single heavy shots when stable.
- Suggested impact: Physical/Projectile; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 078. Returning Shot

- Category: Projectile
- Best enemy users: ghosts, chakram users, machines
- Contact policy: Projectile
- Telegraph/readability: Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.
- Counterplay: Read the pattern, move through safe lanes, block single heavy shots when stable.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 079. Beam

- Category: Magic
- Best enemy users: wizards, machines, bosses, eye enemies
- Contact policy: Area or projectile lane
- Telegraph/readability: Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.
- Counterplay: Leave marked space, interrupt fragile casters, or hold guard for late projectiles.
- Suggested impact: Elemental/Area; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Caster actions need cast tells, element tags, interruption rules, and area readability. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 080. Fireball

- Category: Magic
- Best enemy users: fire casters, demons, dragons
- Contact policy: Projectile or area
- Telegraph/readability: Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.
- Counterplay: Leave marked space, interrupt fragile casters, or hold guard for late projectiles.
- Suggested impact: Elemental/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Caster actions need cast tells, element tags, interruption rules, and area readability. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 081. Ice Spike

- Category: Magic
- Best enemy users: frost casters, traps, ghosts
- Contact policy: Projectile or area
- Telegraph/readability: Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.
- Counterplay: Leave marked space, interrupt fragile casters, or hold guard for late projectiles.
- Suggested impact: Elemental/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Caster actions need cast tells, element tags, interruption rules, and area readability. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 082. Lightning Strike

- Category: Magic
- Best enemy users: storm casters, machines, bosses
- Contact policy: Area
- Telegraph/readability: Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.
- Counterplay: Leave marked space, interrupt fragile casters, or hold guard for late projectiles.
- Suggested impact: Elemental/Area; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Caster actions need cast tells, element tags, interruption rules, and area readability. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 083. Falling Mark

- Category: Magic
- Best enemy users: bosses, priests, star casters
- Contact policy: Area marker
- Telegraph/readability: Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.
- Counterplay: Leave marked space, interrupt fragile casters, or hold guard for late projectiles.
- Suggested impact: Elemental/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Caster actions need cast tells, element tags, interruption rules, and area readability. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 084. Ground Eruption

- Category: Magic
- Best enemy users: earth casters, bosses, burrowers
- Contact policy: Area
- Telegraph/readability: Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.
- Counterplay: Leave marked space, interrupt fragile casters, or hold guard for late projectiles.
- Suggested impact: Elemental/Area; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Caster actions need cast tells, element tags, interruption rules, and area readability. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 085. Fire Trail

- Category: Magic
- Best enemy users: demons, chargers, burning beasts
- Contact policy: Hazard trail
- Telegraph/readability: Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.
- Counterplay: Leave marked space, interrupt fragile casters, or hold guard for late projectiles.
- Suggested impact: Elemental/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Caster actions need cast tells, element tags, interruption rules, and area readability. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 086. Curse Field

- Category: Magic
- Best enemy users: witches, ghosts, soul eaters
- Contact policy: Area
- Telegraph/readability: Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.
- Counterplay: Leave marked space, interrupt fragile casters, or hold guard for late projectiles.
- Suggested impact: Elemental/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Caster actions need cast tells, element tags, interruption rules, and area readability. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 087. Poison Cloud

- Category: Magic
- Best enemy users: alchemists, insects, slimes
- Contact policy: Hazard area
- Telegraph/readability: Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.
- Counterplay: Leave marked space, interrupt fragile casters, or hold guard for late projectiles.
- Suggested impact: Elemental/Area; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Caster actions need cast tells, element tags, interruption rules, and area readability. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 088. Gravity Pull

- Category: Magic
- Best enemy users: cosmic enemies, bosses, machines
- Contact policy: Area control
- Telegraph/readability: Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.
- Counterplay: Leave marked space, interrupt fragile casters, or hold guard for late projectiles.
- Suggested impact: Elemental/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Caster actions need cast tells, element tags, interruption rules, and area readability. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 089. Silence Pulse

- Category: Magic
- Best enemy users: witch hunters, bosses, anti-magic enemies
- Contact policy: Area control
- Telegraph/readability: Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.
- Counterplay: Leave marked space, interrupt fragile casters, or hold guard for late projectiles.
- Suggested impact: Elemental/Area; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Caster actions need cast tells, element tags, interruption rules, and area readability. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 090. Healing Chant

- Category: Magic
- Best enemy users: priests, shamans, support casters
- Contact policy: No direct damage
- Telegraph/readability: Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.
- Counterplay: Leave marked space, interrupt fragile casters, or hold guard for late projectiles.
- Suggested impact: Elemental/Area; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Caster actions need cast tells, element tags, interruption rules, and area readability. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 091. Shield Spell

- Category: Magic
- Best enemy users: wizards, priests, elite enemies
- Contact policy: Defense state
- Telegraph/readability: Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.
- Counterplay: Leave marked space, interrupt fragile casters, or hold guard for late projectiles.
- Suggested impact: Elemental/Area; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Caster actions need cast tells, element tags, interruption rules, and area readability. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 092. Teleport Strike

- Category: Magic
- Best enemy users: warlocks, assassins, ghosts
- Contact policy: Movement plus active hit
- Telegraph/readability: Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.
- Counterplay: Leave marked space, interrupt fragile casters, or hold guard for late projectiles.
- Suggested impact: Elemental/Area; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Caster actions need cast tells, element tags, interruption rules, and area readability. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M87

### 093. Sidestep

- Category: Movement
- Best enemy users: duelists, spiders, knights, archers
- Contact policy: Harmless movement
- Telegraph/readability: Lean, dust, wing beat, vanish puff, or brief pause before repositioning.
- Counterplay: Track the new position, avoid panic swings, and punish predictable landing recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Movement actions should reposition without dealing damage unless a linked attack window is active. Suggested AI: use as a reposition branch before choosing the next attack.
- Likely milestone priority: M82

### 094. Backstep

- Category: Movement
- Best enemy users: archers, rats, casters, duelists
- Contact policy: Harmless movement
- Telegraph/readability: Lean, dust, wing beat, vanish puff, or brief pause before repositioning.
- Counterplay: Track the new position, avoid panic swings, and punish predictable landing recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Movement actions should reposition without dealing damage unless a linked attack window is active. Suggested AI: use as a reposition branch before choosing the next attack.
- Likely milestone priority: M82

### 095. Roll

- Category: Movement
- Best enemy users: bandits, knights, goblins
- Contact policy: Harmless movement
- Telegraph/readability: Lean, dust, wing beat, vanish puff, or brief pause before repositioning.
- Counterplay: Track the new position, avoid panic swings, and punish predictable landing recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Movement actions should reposition without dealing damage unless a linked attack window is active. Suggested AI: use as a reposition branch before choosing the next attack.
- Likely milestone priority: M82

### 096. Retreat

- Category: Movement
- Best enemy users: prey, archers, casters, wounded enemies
- Contact policy: Harmless movement
- Telegraph/readability: Lean, dust, wing beat, vanish puff, or brief pause before repositioning.
- Counterplay: Track the new position, avoid panic swings, and punish predictable landing recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Movement actions should reposition without dealing damage unless a linked attack window is active. Suggested AI: use as a reposition branch before choosing the next attack.
- Likely milestone priority: M82

### 097. Circle

- Category: Movement
- Best enemy users: wolves, knights, duelists, spiders
- Contact policy: Harmless movement
- Telegraph/readability: Lean, dust, wing beat, vanish puff, or brief pause before repositioning.
- Counterplay: Track the new position, avoid panic swings, and punish predictable landing recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Movement actions should reposition without dealing damage unless a linked attack window is active. Suggested AI: use as a reposition branch before choosing the next attack.
- Likely milestone priority: M82

### 098. Evade

- Category: Movement
- Best enemy users: assassins, rats, ghosts, fast beasts
- Contact policy: Harmless movement
- Telegraph/readability: Lean, dust, wing beat, vanish puff, or brief pause before repositioning.
- Counterplay: Track the new position, avoid panic swings, and punish predictable landing recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Movement actions should reposition without dealing damage unless a linked attack window is active. Suggested AI: use as a reposition branch before choosing the next attack.
- Likely milestone priority: M82

### 099. Teleport

- Category: Movement
- Best enemy users: wizards, ghosts, bosses
- Contact policy: Harmless movement unless paired
- Telegraph/readability: Lean, dust, wing beat, vanish puff, or brief pause before repositioning.
- Counterplay: Track the new position, avoid panic swings, and punish predictable landing recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Movement actions should reposition without dealing damage unless a linked attack window is active. Suggested AI: use as a reposition branch before choosing the next attack.
- Likely milestone priority: M87

### 100. Vanish

- Category: Movement
- Best enemy users: ghosts, assassins, tricksters
- Contact policy: Harmless movement unless paired
- Telegraph/readability: Lean, dust, wing beat, vanish puff, or brief pause before repositioning.
- Counterplay: Track the new position, avoid panic swings, and punish predictable landing recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Movement actions should reposition without dealing damage unless a linked attack window is active. Suggested AI: use as a reposition branch before choosing the next attack.
- Likely milestone priority: M87

### 101. Burrow

- Category: Movement
- Best enemy users: worms, spiders, grave creatures
- Contact policy: Harmless movement unless emerging
- Telegraph/readability: Lean, dust, wing beat, vanish puff, or brief pause before repositioning.
- Counterplay: Track the new position, avoid panic swings, and punish predictable landing recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Movement actions should reposition without dealing damage unless a linked attack window is active. Suggested AI: use as a reposition branch before choosing the next attack.
- Likely milestone priority: M88

### 102. Fly Strafe

- Category: Movement
- Best enemy users: undead crows, bats, harpies
- Contact policy: Harmless movement
- Telegraph/readability: Lean, dust, wing beat, vanish puff, or brief pause before repositioning.
- Counterplay: Track the new position, avoid panic swings, and punish predictable landing recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Movement actions should reposition without dealing damage unless a linked attack window is active. Suggested AI: use as a reposition branch before choosing the next attack.
- Likely milestone priority: M85

### 103. Reposition

- Category: Movement
- Best enemy users: all tactical enemies
- Contact policy: Harmless movement
- Telegraph/readability: Lean, dust, wing beat, vanish puff, or brief pause before repositioning.
- Counterplay: Track the new position, avoid panic swings, and punish predictable landing recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Movement actions should reposition without dealing damage unless a linked attack window is active. Suggested AI: use as a reposition branch before choosing the next attack.
- Likely milestone priority: M82

### 104. Close Distance

- Category: Movement
- Best enemy users: melee enemies, beasts, knights
- Contact policy: Harmless movement
- Telegraph/readability: Lean, dust, wing beat, vanish puff, or brief pause before repositioning.
- Counterplay: Track the new position, avoid panic swings, and punish predictable landing recovery.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Movement actions should reposition without dealing damage unless a linked attack window is active. Suggested AI: use as a reposition branch before choosing the next attack.
- Likely milestone priority: M82

### 105. Guard

- Category: Defense
- Best enemy users: knights, skeletons, shield users
- Contact policy: Defense state
- Telegraph/readability: Raised guard, braced feet, shield glow, or parry-ready posture.
- Counterplay: Bait guard, wait out parry, kick/bash, attack from flank, or use heavy attacks.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Defensive actions need guard state, stamina/stability interaction, and punishable recovery. Suggested AI: use when pressured, recently hit, or protecting a ranged/caster role.
- Likely milestone priority: M84

### 106. Brace

- Category: Defense
- Best enemy users: giants, machines, shield users
- Contact policy: Defense state
- Telegraph/readability: Raised guard, braced feet, shield glow, or parry-ready posture.
- Counterplay: Bait guard, wait out parry, kick/bash, attack from flank, or use heavy attacks.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Defensive actions need guard state, stamina/stability interaction, and punishable recovery. Suggested AI: use when pressured, recently hit, or protecting a ranged/caster role.
- Likely milestone priority: M84

### 107. Shield Wall

- Category: Defense
- Best enemy users: guards, constructs, commanders
- Contact policy: Defense state
- Telegraph/readability: Raised guard, braced feet, shield glow, or parry-ready posture.
- Counterplay: Bait guard, wait out parry, kick/bash, attack from flank, or use heavy attacks.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Defensive actions need guard state, stamina/stability interaction, and punishable recovery. Suggested AI: use when pressured, recently hit, or protecting a ranged/caster role.
- Likely milestone priority: M89

### 108. Dodge Counter

- Category: Defense
- Best enemy users: duelists, assassins, elite beasts
- Contact policy: Active hit after evade
- Telegraph/readability: Raised guard, braced feet, shield glow, or parry-ready posture.
- Counterplay: Bait guard, wait out parry, kick/bash, attack from flank, or use heavy attacks.
- Suggested impact: Physical/Melee; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Defensive actions need guard state, stamina/stability interaction, and punishable recovery. Suggested AI: use when pressured, recently hit, or protecting a ranged/caster role.
- Likely milestone priority: M84

### 109. Parry Counter

- Category: Defense
- Best enemy users: elite knights, duelists
- Contact policy: Counter state
- Telegraph/readability: Raised guard, braced feet, shield glow, or parry-ready posture.
- Counterplay: Bait guard, wait out parry, kick/bash, attack from flank, or use heavy attacks.
- Suggested impact: Physical/Melee; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Defensive actions need guard state, stamina/stability interaction, and punishable recovery. Suggested AI: use when pressured, recently hit, or protecting a ranged/caster role.
- Likely milestone priority: M84

### 110. Armor Harden

- Category: Defense
- Best enemy users: stone beasts, machines, bosses
- Contact policy: Defense state
- Telegraph/readability: Raised guard, braced feet, shield glow, or parry-ready posture.
- Counterplay: Bait guard, wait out parry, kick/bash, attack from flank, or use heavy attacks.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Defensive actions need guard state, stamina/stability interaction, and punishable recovery. Suggested AI: use when pressured, recently hit, or protecting a ranged/caster role.
- Likely milestone priority: M87

### 111. Shell Hide

- Category: Defense
- Best enemy users: turtles, insects, mimics
- Contact policy: Defense state
- Telegraph/readability: Raised guard, braced feet, shield glow, or parry-ready posture.
- Counterplay: Bait guard, wait out parry, kick/bash, attack from flank, or use heavy attacks.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Defensive actions need guard state, stamina/stability interaction, and punishable recovery. Suggested AI: use when pressured, recently hit, or protecting a ranged/caster role.
- Likely milestone priority: M85

### 112. Regenerate

- Category: Defense
- Best enemy users: undead, slimes, occult enemies
- Contact policy: No direct damage
- Telegraph/readability: Raised guard, braced feet, shield glow, or parry-ready posture.
- Counterplay: Bait guard, wait out parry, kick/bash, attack from flank, or use heavy attacks.
- Suggested impact: Physical/Melee; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Defensive actions need guard state, stamina/stability interaction, and punishable recovery. Suggested AI: use when pressured, recently hit, or protecting a ranged/caster role.
- Likely milestone priority: M87

### 113. Summon Minion

- Category: Summon
- Best enemy users: wizards, necromancers, bosses
- Contact policy: Spawn event
- Telegraph/readability: Portal, ground crack, corpse twitch, or arrival marker before spawned units act.
- Counterplay: Pressure the summoner, clear adds quickly, and avoid arrival markers.
- Suggested impact: Physical/Area; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Summon actions need spawn budgets, telegraphed arrival points, and room-clear compatibility. Suggested AI: use sparse cooldowns and room population caps.
- Likely milestone priority: M87

### 114. Summon Swarm

- Category: Summon
- Best enemy users: spider queens, rat kings, bosses
- Contact policy: Spawn event
- Telegraph/readability: Portal, ground crack, corpse twitch, or arrival marker before spawned units act.
- Counterplay: Pressure the summoner, clear adds quickly, and avoid arrival markers.
- Suggested impact: Physical/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Summon actions need spawn budgets, telegraphed arrival points, and room-clear compatibility. Suggested AI: use sparse cooldowns and room population caps.
- Likely milestone priority: M87

### 115. Raise Skeleton

- Category: Summon
- Best enemy users: necromancers, grave enemies
- Contact policy: Spawn event
- Telegraph/readability: Portal, ground crack, corpse twitch, or arrival marker before spawned units act.
- Counterplay: Pressure the summoner, clear adds quickly, and avoid arrival markers.
- Suggested impact: Physical/Area; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Summon actions need spawn budgets, telegraphed arrival points, and room-clear compatibility. Suggested AI: use sparse cooldowns and room population caps.
- Likely milestone priority: M87

### 116. Call Beast

- Category: Summon
- Best enemy users: hunters, shamans, commanders
- Contact policy: Spawn event
- Telegraph/readability: Portal, ground crack, corpse twitch, or arrival marker before spawned units act.
- Counterplay: Pressure the summoner, clear adds quickly, and avoid arrival markers.
- Suggested impact: Physical/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Summon actions need spawn budgets, telegraphed arrival points, and room-clear compatibility. Suggested AI: use sparse cooldowns and room population caps.
- Likely milestone priority: M89

### 117. Spawn Turret

- Category: Summon
- Best enemy users: machines, engineers, bosses
- Contact policy: Spawn event
- Telegraph/readability: Portal, ground crack, corpse twitch, or arrival marker before spawned units act.
- Counterplay: Pressure the summoner, clear adds quickly, and avoid arrival markers.
- Suggested impact: Physical/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Summon actions need spawn budgets, telegraphed arrival points, and room-clear compatibility. Suggested AI: use sparse cooldowns and room population caps.
- Likely milestone priority: M87

### 118. Create Clone

- Category: Summon
- Best enemy users: mirror enemies, ghosts, bosses
- Contact policy: Spawn event
- Telegraph/readability: Portal, ground crack, corpse twitch, or arrival marker before spawned units act.
- Counterplay: Pressure the summoner, clear adds quickly, and avoid arrival markers.
- Suggested impact: Physical/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Summon actions need spawn budgets, telegraphed arrival points, and room-clear compatibility. Suggested AI: use sparse cooldowns and room population caps.
- Likely milestone priority: M87

### 119. Portal Add

- Category: Summon
- Best enemy users: warlocks, cosmic enemies, bosses
- Contact policy: Spawn event
- Telegraph/readability: Portal, ground crack, corpse twitch, or arrival marker before spawned units act.
- Counterplay: Pressure the summoner, clear adds quickly, and avoid arrival markers.
- Suggested impact: Physical/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Summon actions need spawn budgets, telegraphed arrival points, and room-clear compatibility. Suggested AI: use sparse cooldowns and room population caps.
- Likely milestone priority: M87

### 120. Death Spawn

- Category: Summon
- Best enemy users: splitters, parasites, necromancers
- Contact policy: Spawn event or area
- Telegraph/readability: Portal, ground crack, corpse twitch, or arrival marker before spawned units act.
- Counterplay: Pressure the summoner, clear adds quickly, and avoid arrival markers.
- Suggested impact: Physical/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Summon actions need spawn budgets, telegraphed arrival points, and room-clear compatibility. Suggested AI: use sparse cooldowns and room population caps.
- Likely milestone priority: M85

### 121. Spiked Body

- Category: Hazard
- Best enemy users: hedgehog beasts, spike traps, cursed armor
- Contact policy: Hazardous body
- Telegraph/readability: Persistent glow, spikes, flame, sludge, crackle, or pulsing warning edge.
- Counterplay: Do not stand in the field; push, kite, or wait out the hazard duration.
- Suggested impact: Environmental/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Hazard bodies or fields are the main exception to harmless body contact. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M79

### 122. Burning Body

- Category: Hazard
- Best enemy users: fire slimes, demons, ash enemies
- Contact policy: Hazardous body
- Telegraph/readability: Persistent glow, spikes, flame, sludge, crackle, or pulsing warning edge.
- Counterplay: Do not stand in the field; push, kite, or wait out the hazard duration.
- Suggested impact: Environmental/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Hazard bodies or fields are the main exception to harmless body contact. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M79

### 123. Acid Body

- Category: Hazard
- Best enemy users: slimes, insects, corrupted pods
- Contact policy: Hazardous body
- Telegraph/readability: Persistent glow, spikes, flame, sludge, crackle, or pulsing warning edge.
- Counterplay: Do not stand in the field; push, kite, or wait out the hazard duration.
- Suggested impact: Environmental/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Hazard bodies or fields are the main exception to harmless body contact. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M79

### 124. Poison Aura

- Category: Hazard
- Best enemy users: toxic beasts, alchemic enemies
- Contact policy: Hazard area
- Telegraph/readability: Persistent glow, spikes, flame, sludge, crackle, or pulsing warning edge.
- Counterplay: Do not stand in the field; push, kite, or wait out the hazard duration.
- Suggested impact: Environmental/Area; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Hazard bodies or fields are the main exception to harmless body contact. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M87

### 125. Static Aura

- Category: Hazard
- Best enemy users: machines, storm enemies
- Contact policy: Hazard area
- Telegraph/readability: Persistent glow, spikes, flame, sludge, crackle, or pulsing warning edge.
- Counterplay: Do not stand in the field; push, kite, or wait out the hazard duration.
- Suggested impact: Environmental/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Hazard bodies or fields are the main exception to harmless body contact. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M87

### 126. Spike Patch

- Category: Hazard
- Best enemy users: traps, plant enemies, bosses
- Contact policy: Environmental
- Telegraph/readability: Persistent glow, spikes, flame, sludge, crackle, or pulsing warning edge.
- Counterplay: Do not stand in the field; push, kite, or wait out the hazard duration.
- Suggested impact: Environmental/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Hazard bodies or fields are the main exception to harmless body contact. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M87

### 127. Flame Patch

- Category: Hazard
- Best enemy users: casters, demons, machines
- Contact policy: Environmental
- Telegraph/readability: Persistent glow, spikes, flame, sludge, crackle, or pulsing warning edge.
- Counterplay: Do not stand in the field; push, kite, or wait out the hazard duration.
- Suggested impact: Environmental/Area; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Hazard bodies or fields are the main exception to harmless body contact. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M87

### 128. Explode On Death

- Category: Hazard
- Best enemy users: bomb enemies, machines, cursed undead
- Contact policy: Area
- Telegraph/readability: Persistent glow, spikes, flame, sludge, crackle, or pulsing warning edge.
- Counterplay: Do not stand in the field; push, kite, or wait out the hazard duration.
- Suggested impact: Environmental/Area; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Hazard bodies or fields are the main exception to harmless body contact. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M85

### 129. Phase

- Category: Ghost/Soul
- Best enemy users: ghosts, wraiths, soul eaters
- Contact policy: Harmless movement
- Telegraph/readability: Fade, afterimage, soul trail, distortion, or cold glow before the effect lands.
- Counterplay: Watch phase cooldowns, dodge the reappear point, and punish after materialization.
- Suggested impact: NonPhysical/Area/Soul; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Intangible enemies need phase rules, contact exceptions, and anti-frustration cooldowns. Suggested AI: use phase or drain cooldowns so the player always gets punish windows.
- Likely milestone priority: M87

### 130. Possess

- Category: Ghost/Soul
- Best enemy users: ghosts, parasites, occult bosses
- Contact policy: Grab or control
- Telegraph/readability: Fade, afterimage, soul trail, distortion, or cold glow before the effect lands.
- Counterplay: Watch phase cooldowns, dodge the reappear point, and punish after materialization.
- Suggested impact: NonPhysical/Area/Soul; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Intangible enemies need phase rules, contact exceptions, and anti-frustration cooldowns. Suggested AI: use phase or drain cooldowns so the player always gets punish windows.
- Likely milestone priority: M87

### 131. Soul Drain

- Category: Ghost/Soul
- Best enemy users: soul eaters, ghosts, liches
- Contact policy: Active hit or beam
- Telegraph/readability: Fade, afterimage, soul trail, distortion, or cold glow before the effect lands.
- Counterplay: Watch phase cooldowns, dodge the reappear point, and punish after materialization.
- Suggested impact: NonPhysical/Area/Soul; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Intangible enemies need phase rules, contact exceptions, and anti-frustration cooldowns. Suggested AI: use phase or drain cooldowns so the player always gets punish windows.
- Likely milestone priority: M87

### 132. Curse Touch

- Category: Ghost/Soul
- Best enemy users: ghosts, cursed undead
- Contact policy: Active hit window
- Telegraph/readability: Fade, afterimage, soul trail, distortion, or cold glow before the effect lands.
- Counterplay: Watch phase cooldowns, dodge the reappear point, and punish after materialization.
- Suggested impact: NonPhysical/Area/Soul; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Intangible enemies need phase rules, contact exceptions, and anti-frustration cooldowns. Suggested AI: use phase or drain cooldowns so the player always gets punish windows.
- Likely milestone priority: M87

### 133. Fear Pulse

- Category: Ghost/Soul
- Best enemy users: wraiths, bosses, cursed idols
- Contact policy: Area control
- Telegraph/readability: Fade, afterimage, soul trail, distortion, or cold glow before the effect lands.
- Counterplay: Watch phase cooldowns, dodge the reappear point, and punish after materialization.
- Suggested impact: NonPhysical/Area/Soul; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Intangible enemies need phase rules, contact exceptions, and anti-frustration cooldowns. Suggested AI: use phase or drain cooldowns so the player always gets punish windows.
- Likely milestone priority: M87

### 134. Decoy

- Category: Ghost/Soul
- Best enemy users: ghosts, mirror enemies, tricksters
- Contact policy: Spawn event
- Telegraph/readability: Fade, afterimage, soul trail, distortion, or cold glow before the effect lands.
- Counterplay: Watch phase cooldowns, dodge the reappear point, and punish after materialization.
- Suggested impact: NonPhysical/Area/Soul; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Intangible enemies need phase rules, contact exceptions, and anti-frustration cooldowns. Suggested AI: use phase or drain cooldowns so the player always gets punish windows.
- Likely milestone priority: M87

### 135. Split

- Category: Ghost/Soul
- Best enemy users: ghosts, mirror husks, slimes
- Contact policy: Split event
- Telegraph/readability: Fade, afterimage, soul trail, distortion, or cold glow before the effect lands.
- Counterplay: Watch phase cooldowns, dodge the reappear point, and punish after materialization.
- Suggested impact: NonPhysical/Area/Soul; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Intangible enemies need phase rules, contact exceptions, and anti-frustration cooldowns. Suggested AI: use phase or drain cooldowns so the player always gets punish windows.
- Likely milestone priority: M87

### 136. Re-form

- Category: Ghost/Soul
- Best enemy users: ghosts, slimes, soul clusters
- Contact policy: Recovery event
- Telegraph/readability: Fade, afterimage, soul trail, distortion, or cold glow before the effect lands.
- Counterplay: Watch phase cooldowns, dodge the reappear point, and punish after materialization.
- Suggested impact: NonPhysical/Area/Soul; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Intangible enemies need phase rules, contact exceptions, and anti-frustration cooldowns. Suggested AI: use phase or drain cooldowns so the player always gets punish windows.
- Likely milestone priority: M87

### 137. Pass-Through Attack

- Category: Ghost/Soul
- Best enemy users: ghosts, wraiths, phasing beasts
- Contact policy: Active hit window
- Telegraph/readability: Fade, afterimage, soul trail, distortion, or cold glow before the effect lands.
- Counterplay: Watch phase cooldowns, dodge the reappear point, and punish after materialization.
- Suggested impact: NonPhysical/Area/Soul; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Intangible enemies need phase rules, contact exceptions, and anti-frustration cooldowns. Suggested AI: use phase or drain cooldowns so the player always gets punish windows.
- Likely milestone priority: M87

### 138. Soul Projectile

- Category: Ghost/Soul
- Best enemy users: ghosts, liches, soul eaters
- Contact policy: Projectile
- Telegraph/readability: Fade, afterimage, soul trail, distortion, or cold glow before the effect lands.
- Counterplay: Watch phase cooldowns, dodge the reappear point, and punish after materialization.
- Suggested impact: NonPhysical/Area/Soul; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Intangible enemies need phase rules, contact exceptions, and anti-frustration cooldowns. Suggested AI: use phase or drain cooldowns so the player always gets punish windows.
- Likely milestone priority: M87

### 139. Life Link

- Category: Ghost/Soul
- Best enemy users: soul enemies, twin bosses
- Contact policy: Support state
- Telegraph/readability: Fade, afterimage, soul trail, distortion, or cold glow before the effect lands.
- Counterplay: Watch phase cooldowns, dodge the reappear point, and punish after materialization.
- Suggested impact: NonPhysical/Area/Soul; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Intangible enemies need phase rules, contact exceptions, and anti-frustration cooldowns. Suggested AI: use phase or drain cooldowns so the player always gets punish windows.
- Likely milestone priority: M87

### 140. Haunt Zone

- Category: Ghost/Soul
- Best enemy users: ghosts, cursed rooms, bosses
- Contact policy: Hazard area
- Telegraph/readability: Fade, afterimage, soul trail, distortion, or cold glow before the effect lands.
- Counterplay: Watch phase cooldowns, dodge the reappear point, and punish after materialization.
- Suggested impact: NonPhysical/Area/Soul; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Intangible enemies need phase rules, contact exceptions, and anti-frustration cooldowns. Suggested AI: use phase or drain cooldowns so the player always gets punish windows.
- Likely milestone priority: M87

### 141. Saw Sweep

- Category: Mechanical
- Best enemy users: machines, traps, clockwork soldiers
- Contact policy: Active hit window
- Telegraph/readability: Gear spin, pressure hiss, red lens, crank, or charge sound before firing.
- Counterplay: Use reload windows, avoid telegraphed lanes, and punish immobile setups.
- Suggested impact: Physical/Projectile; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Machines can ignore organic tells, but still need audio/visual windups and reload windows. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 142. Gear Bite

- Category: Mechanical
- Best enemy users: machines, mimics, constructs
- Contact policy: Active hit window
- Telegraph/readability: Gear spin, pressure hiss, red lens, crank, or charge sound before firing.
- Counterplay: Use reload windows, avoid telegraphed lanes, and punish immobile setups.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Machines can ignore organic tells, but still need audio/visual windups and reload windows. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 143. Steam Vent

- Category: Mechanical
- Best enemy users: machines, traps, bosses
- Contact policy: Hazard area
- Telegraph/readability: Gear spin, pressure hiss, red lens, crank, or charge sound before firing.
- Counterplay: Use reload windows, avoid telegraphed lanes, and punish immobile setups.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Machines can ignore organic tells, but still need audio/visual windups and reload windows. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 144. Mine Drop

- Category: Mechanical
- Best enemy users: machines, gunners, bosses
- Contact policy: Environmental
- Telegraph/readability: Gear spin, pressure hiss, red lens, crank, or charge sound before firing.
- Counterplay: Use reload windows, avoid telegraphed lanes, and punish immobile setups.
- Suggested impact: Physical/Projectile; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Machines can ignore organic tells, but still need audio/visual windups and reload windows. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 145. Laser Sweep

- Category: Mechanical
- Best enemy users: machines, cosmic turrets, bosses
- Contact policy: Area lane
- Telegraph/readability: Gear spin, pressure hiss, red lens, crank, or charge sound before firing.
- Counterplay: Use reload windows, avoid telegraphed lanes, and punish immobile setups.
- Suggested impact: Physical/Projectile; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Machines can ignore organic tells, but still need audio/visual windups and reload windows. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 146. Drill Charge

- Category: Mechanical
- Best enemy users: machines, miners, constructs
- Contact policy: Active hit window
- Telegraph/readability: Gear spin, pressure hiss, red lens, crank, or charge sound before firing.
- Counterplay: Use reload windows, avoid telegraphed lanes, and punish immobile setups.
- Suggested impact: Physical/Projectile; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Machines can ignore organic tells, but still need audio/visual windups and reload windows. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 147. Rocket Burst

- Category: Mechanical
- Best enemy users: machines, gunners, bosses
- Contact policy: Projectile pattern
- Telegraph/readability: Gear spin, pressure hiss, red lens, crank, or charge sound before firing.
- Counterplay: Use reload windows, avoid telegraphed lanes, and punish immobile setups.
- Suggested impact: Physical/Projectile; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Machines can ignore organic tells, but still need audio/visual windups and reload windows. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 148. Reload

- Category: Mechanical
- Best enemy users: gunners, machines, turrets
- Contact policy: No direct damage
- Telegraph/readability: Gear spin, pressure hiss, red lens, crank, or charge sound before firing.
- Counterplay: Use reload windows, avoid telegraphed lanes, and punish immobile setups.
- Suggested impact: Physical/Projectile; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Machines can ignore organic tells, but still need audio/visual windups and reload windows. Suggested AI: gate by awareness, range band, line preference later, and attack budget.
- Likely milestone priority: M86

### 149. Shockwave

- Category: Boss-Scale
- Best enemy users: giants, stone bosses, hammer bosses
- Contact policy: Area
- Telegraph/readability: Large animation lock, arena-wide cue, audio sting, and visible safe spaces.
- Counterplay: Move to safe zones, preserve stamina, and punish only after the full sequence ends.
- Suggested impact: Mixed/Boss; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Boss-scale actions need long tells, arena-safe spaces, and pressure caps. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M90

### 150. Arena Hazard

- Category: Boss-Scale
- Best enemy users: bosses, machines, casters
- Contact policy: Environmental
- Telegraph/readability: Large animation lock, arena-wide cue, audio sting, and visible safe spaces.
- Counterplay: Move to safe zones, preserve stamina, and punish only after the full sequence ends.
- Suggested impact: Mixed/Boss; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Boss-scale actions need long tells, arena-safe spaces, and pressure caps. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M90

### 151. Summon Wave

- Category: Boss-Scale
- Best enemy users: bosses, necromancers, queens
- Contact policy: Spawn event
- Telegraph/readability: Large animation lock, arena-wide cue, audio sting, and visible safe spaces.
- Counterplay: Move to safe zones, preserve stamina, and punish only after the full sequence ends.
- Suggested impact: Mixed/Boss; Medium force; suggested knockback 0.35-0.65m.
- AI/build notes: Boss-scale actions need long tells, arena-safe spaces, and pressure caps. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M90

### 152. Multi-Stage Combo

- Category: Boss-Scale
- Best enemy users: bosses, elite knights, demons
- Contact policy: Active hit windows
- Telegraph/readability: Large animation lock, arena-wide cue, audio sting, and visible safe spaces.
- Counterplay: Move to safe zones, preserve stamina, and punish only after the full sequence ends.
- Suggested impact: Mixed/Boss; Massive force; suggested knockback 0.95-1.40m.
- AI/build notes: Boss-scale actions need long tells, arena-safe spaces, and pressure caps. Suggested AI: requires chained active windows with a readable stop point.
- Likely milestone priority: M90

### 153. Desperation Burst

- Category: Boss-Scale
- Best enemy users: bosses, soul enemies
- Contact policy: Projectile or area
- Telegraph/readability: Large animation lock, arena-wide cue, audio sting, and visible safe spaces.
- Counterplay: Move to safe zones, preserve stamina, and punish only after the full sequence ends.
- Suggested impact: Mixed/Boss; Massive force; suggested knockback 0.95-1.40m.
- AI/build notes: Boss-scale actions need long tells, arena-safe spaces, and pressure caps. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M90

### 154. Rotating Pattern

- Category: Boss-Scale
- Best enemy users: bosses, machines, occult enemies
- Contact policy: Projectile pattern
- Telegraph/readability: Large animation lock, arena-wide cue, audio sting, and visible safe spaces.
- Counterplay: Move to safe zones, preserve stamina, and punish only after the full sequence ends.
- Suggested impact: Mixed/Boss; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Boss-scale actions need long tells, arena-safe spaces, and pressure caps. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M90

### 155. Phase Change Attack

- Category: Boss-Scale
- Best enemy users: bosses, cosmic enemies
- Contact policy: Area or movement
- Telegraph/readability: Large animation lock, arena-wide cue, audio sting, and visible safe spaces.
- Counterplay: Move to safe zones, preserve stamina, and punish only after the full sequence ends.
- Suggested impact: Mixed/Boss; Heavy force; suggested knockback 0.65-0.95m.
- AI/build notes: Boss-scale actions need long tells, arena-safe spaces, and pressure caps. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M90

### 156. Arena Sweep

- Category: Boss-Scale
- Best enemy users: dragons, giants, machines
- Contact policy: Area lane
- Telegraph/readability: Large animation lock, arena-wide cue, audio sting, and visible safe spaces.
- Counterplay: Move to safe zones, preserve stamina, and punish only after the full sequence ends.
- Suggested impact: Mixed/Boss; Massive force; suggested knockback 0.95-1.40m.
- AI/build notes: Boss-scale actions need long tells, arena-safe spaces, and pressure caps. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M90

### 157. Grab And Throw

- Category: Boss-Scale
- Best enemy users: giants, demons, bosses
- Contact policy: Grab
- Telegraph/readability: Large animation lock, arena-wide cue, audio sting, and visible safe spaces.
- Counterplay: Move to safe zones, preserve stamina, and punish only after the full sequence ends.
- Suggested impact: Mixed/Boss; Massive force; suggested knockback 0.95-1.40m.
- AI/build notes: Boss-scale actions need long tells, arena-safe spaces, and pressure caps. Suggested AI: requires explicit grab state, escape/cancel rules, and no passive contact damage.
- Likely milestone priority: M90

### 158. Final Exhaustion

- Category: Boss-Scale
- Best enemy users: bosses, elite enemies
- Contact policy: Recovery event
- Telegraph/readability: Large animation lock, arena-wide cue, audio sting, and visible safe spaces.
- Counterplay: Move to safe zones, preserve stamina, and punish only after the full sequence ends.
- Suggested impact: Mixed/Boss; Light force; suggested knockback 0.15-0.35m.
- AI/build notes: Boss-scale actions need long tells, arena-safe spaces, and pressure caps. Suggested AI: score from distance, facing, intelligence, disposition, and recent player movement.
- Likely milestone priority: M90

## Roadmap

- M79 Contact Damage Rework V1: Passive contact stops damaging except explicit hazardous bodies; bumping enemies disturbs or alerts but does not hurt.
- M80 Active Hit Windows V1: Attacks use readable windup, active, and recovery windows instead of proximity-only damage.
- M81 Enemy Action Profiles V2: Expand attack profiles to represent body, weapon, ranged, magic, movement, defense, and hazard actions.
- M82 Lightweight Behavior Tree Layer V1: Add simple selector/sequence behavior trees over current awareness, intelligence, disposition, cooldowns, and budgets.
- M83 Noise + Disturbance V2: Tune footsteps, attacks, proximity, and bump stimuli without adding full stealth UI.
- M84 Weapon-User Enemies V1: Skeletons, knights, and giants gain weapons, shields, swings, thrusts, and recovery windows.
- M85 Creature Action Expansion V1: Rats, spiders, birds, beasts, and body-only enemies gain richer fight/flee/action sets.
- M86 Ranged + Firearm Enemies V1: Archers, gunners, throwers, turrets, machines, and projectile pattern enemies.
- M87 Magic/Ghost/Soul Enemies V1: Casters, ghosts, soul eaters, phase movement, drain, curse, and area pressure.
- M88 Navigation Adapter V1: Add pathfinding or local-navigation milestones behind a wrapper, not as an immediate dependency.
- M89 Limited Alert Sharing V1: Selected enemies can wake nearby allies later; M78/M79 stay solo-enemy focused.
- M90 Combat AI QA Lock: Regression and feel pass across contact, attacks, weapon users, senses, movement, knockback, and bosses.

## M78 Compatibility

- No runtime combat behavior changes.
- No save schema changes.
- No new enemy prefabs or encounters.
- No pathfinding, line of sight, squad tactics, or boss runtime changes.
- The PDF and Markdown are planning artifacts for M79+.
