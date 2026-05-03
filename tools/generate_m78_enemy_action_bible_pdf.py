#!/usr/bin/env python3
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import shutil
import subprocess

from reportlab.lib import colors
from reportlab.lib.pagesizes import landscape, letter
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.platypus import (
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)


ROOT = Path(__file__).resolve().parents[1]
DOC_PATH = ROOT / "Docs/Hollow_M78_Enemy_Action_Bible.md"
REPORT_PATH = ROOT / "output/reports/m78_enemy_action_bible.md"
PDF_PATH = ROOT / "output/pdf/Hollow_M78_Enemy_Action_Bible.pdf"
PREVIEW_DIR = ROOT / "output/pdf/previews/m78"


@dataclass(frozen=True)
class ActionCard:
    name: str
    category: str
    users: str
    contact: str
    telegraph: str
    counterplay: str
    impact: str
    ai: str
    priority: str


CATEGORY_BUILD_NOTES = {
    "Body": "Body-only creatures can use this without carried weapons; active hit windows should replace passive touch damage.",
    "Weapon": "Weapon-user enemies need facing, reach, recovery, and readable weapon arcs.",
    "Ranged": "Ranged attackers need aim time, projectile ownership, reload/cooldown, and room pressure budgets.",
    "Projectile": "Pattern attacks need projectile shapes, spawn offsets, budget control, and clear safe lanes.",
    "Magic": "Caster actions need cast tells, element tags, interruption rules, and area readability.",
    "Movement": "Movement actions should reposition without dealing damage unless a linked attack window is active.",
    "Defense": "Defensive actions need guard state, stamina/stability interaction, and punishable recovery.",
    "Summon": "Summon actions need spawn budgets, telegraphed arrival points, and room-clear compatibility.",
    "Hazard": "Hazard bodies or fields are the main exception to harmless body contact.",
    "Ghost/Soul": "Intangible enemies need phase rules, contact exceptions, and anti-frustration cooldowns.",
    "Mechanical": "Machines can ignore organic tells, but still need audio/visual windups and reload windows.",
    "Boss-Scale": "Boss-scale actions need long tells, arena-safe spaces, and pressure caps.",
}

CATEGORY_TELEGRAPHS = {
    "Body": "Body crouch, head pullback, limb raise, or short inhale before the active frame.",
    "Weapon": "Weapon draw-back, shoulder turn, blade glint, and a recovery pose after the swing.",
    "Ranged": "Aim line, reload pose, barrel/bow lift, or hand throw windup before release.",
    "Projectile": "Spawner flash, muzzle point, arc marker, or pattern preview before projectiles move.",
    "Magic": "Cast circle, hand glow, chant pose, rune marker, or delayed ground tell.",
    "Movement": "Lean, dust, wing beat, vanish puff, or brief pause before repositioning.",
    "Defense": "Raised guard, braced feet, shield glow, or parry-ready posture.",
    "Summon": "Portal, ground crack, corpse twitch, or arrival marker before spawned units act.",
    "Hazard": "Persistent glow, spikes, flame, sludge, crackle, or pulsing warning edge.",
    "Ghost/Soul": "Fade, afterimage, soul trail, distortion, or cold glow before the effect lands.",
    "Mechanical": "Gear spin, pressure hiss, red lens, crank, or charge sound before firing.",
    "Boss-Scale": "Large animation lock, arena-wide cue, audio sting, and visible safe spaces.",
}

CATEGORY_COUNTERS = {
    "Body": "Step out of reach, circle behind, block light hits, or punish the recovery.",
    "Weapon": "Respect range, dodge through the active arc, block if stable, then punish recovery.",
    "Ranged": "Strafe, break aim timing, use cover when available, or punish reload.",
    "Projectile": "Read the pattern, move through safe lanes, block single heavy shots when stable.",
    "Magic": "Leave marked space, interrupt fragile casters, or hold guard for late projectiles.",
    "Movement": "Track the new position, avoid panic swings, and punish predictable landing recovery.",
    "Defense": "Bait guard, wait out parry, kick/bash, attack from flank, or use heavy attacks.",
    "Summon": "Pressure the summoner, clear adds quickly, and avoid arrival markers.",
    "Hazard": "Do not stand in the field; push, kite, or wait out the hazard duration.",
    "Ghost/Soul": "Watch phase cooldowns, dodge the reappear point, and punish after materialization.",
    "Mechanical": "Use reload windows, avoid telegraphed lanes, and punish immobile setups.",
    "Boss-Scale": "Move to safe zones, preserve stamina, and punish only after the full sequence ends.",
}


def impact(category: str, force: str, kb: str, delivery: str | None = None) -> str:
    delivery = delivery or {
        "Body": "Physical/Melee",
        "Weapon": "Physical/Melee",
        "Ranged": "Physical/Projectile",
        "Projectile": "Physical/Projectile",
        "Magic": "Elemental/Area",
        "Movement": "Physical/Melee",
        "Defense": "Physical/Melee",
        "Summon": "Physical/Area",
        "Hazard": "Environmental/Area",
        "Ghost/Soul": "NonPhysical/Area/Soul",
        "Mechanical": "Physical/Projectile",
        "Boss-Scale": "Mixed/Boss",
    }[category]
    return f"{delivery}; {force} force; suggested knockback {kb}."


def make(category: str, rows: list[tuple[str, str, str, str, str]]) -> list[ActionCard]:
    cards: list[ActionCard] = []
    for name, users, contact, force, priority in rows:
        cards.append(ActionCard(
            name=name,
            category=category,
            users=users,
            contact=contact,
            telegraph=CATEGORY_TELEGRAPHS[category],
            counterplay=CATEGORY_COUNTERS[category],
            impact=impact(category, force, knockback_for(force)),
            ai=f"{CATEGORY_BUILD_NOTES[category]} Suggested AI: {ai_hint(category, name)}",
            priority=priority,
        ))
    return cards


def knockback_for(force: str) -> str:
    return {
        "Light": "0.15-0.35m",
        "Medium": "0.35-0.65m",
        "Heavy": "0.65-0.95m",
        "Massive": "0.95-1.40m",
    }.get(force, "0.25-0.60m")


def ai_hint(category: str, name: str) -> str:
    lowered = name.lower()
    if category in {"Ranged", "Projectile", "Magic", "Mechanical"}:
        return "gate by awareness, range band, line preference later, and attack budget."
    if category == "Defense":
        return "use when pressured, recently hit, or protecting a ranged/caster role."
    if category == "Movement":
        return "use as a reposition branch before choosing the next attack."
    if category == "Summon":
        return "use sparse cooldowns and room population caps."
    if category == "Ghost/Soul":
        return "use phase or drain cooldowns so the player always gets punish windows."
    if "grab" in lowered or "drag" in lowered:
        return "requires explicit grab state, escape/cancel rules, and no passive contact damage."
    if "combo" in lowered:
        return "requires chained active windows with a readable stop point."
    return "score from distance, facing, intelligence, disposition, and recent player movement."


def action_cards() -> list[ActionCard]:
    cards: list[ActionCard] = []
    cards += make("Body", [
        ("Bite", "rats, spiders, beasts, undead crows, ghouls", "Active hit window", "Light", "M80"),
        ("Claw Swipe", "beasts, ghouls, demons, undead crows", "Active hit window", "Light", "M80"),
        ("Double Claw", "beasts, demons, fast undead", "Active hit window", "Medium", "M85"),
        ("Peck", "undead crows, birds, small flyers", "Active hit window", "Light", "M85"),
        ("Pounce", "rats, spiders, wolves, cats, beasts", "Active hit window", "Medium", "M80"),
        ("Leap Attack", "spiders, beasts, frog creatures, assassins", "Active hit window", "Medium", "M85"),
        ("Dive Attack", "undead crows, bats, flying demons", "Active hit window", "Medium", "M85"),
        ("Gore", "boars, horned beasts, demons", "Active hit window", "Heavy", "M85"),
        ("Tail Swipe", "beasts, dragons, lizards, scorpions", "Active hit window", "Medium", "M85"),
        ("Body Slam", "heavy beasts, giants, slimes, armored brutes", "Active hit window", "Heavy", "M80"),
        ("Belly Flop", "large beasts, giants, grotesque enemies", "Active hit window", "Heavy", "M85"),
        ("Stomp", "giants, trolls, bosses, heavy undead", "Active hit window", "Heavy", "M80"),
        ("Kick", "humanoids, beasts, skeletons", "Active hit window", "Medium", "M84"),
        ("Wing Buffet", "undead crows, harpies, winged beasts", "Active hit window", "Medium", "M85"),
        ("Headbutt", "skeletons, beasts, armored creatures", "Active hit window", "Medium", "M85"),
        ("Shoulder Check", "knights, brutes, giants, shield users", "Active hit window", "Medium", "M84"),
        ("Tentacle Lash", "sea creatures, horrors, soul eaters", "Active hit window", "Medium", "M87"),
        ("Tongue Lash", "frogs, leeches, mutants", "Active hit window", "Light", "M85"),
        ("Web Shot", "spiders, web casters", "Projectile", "Light", "M85"),
        ("Spit", "pods, insects, beasts, corrupted creatures", "Projectile", "Light", "M86"),
        ("Acid Spit", "slimes, insects, alchemic creatures", "Projectile or hazard", "Medium", "M87"),
        ("Burrow Emerge", "worms, moles, grave creatures", "Active hit window", "Medium", "M88"),
        ("Grab", "ghouls, giants, mimics, soul eaters", "Grab", "Heavy", "M80"),
        ("Drag", "leeches, ghosts, tentacle beasts", "Grab", "Medium", "M87"),
    ])
    cards += make("Weapon", [
        ("Light Slash", "skeletons, knights, bandits, cultists", "Active hit window", "Light", "M84"),
        ("Heavy Slash", "knights, giants, executioners", "Active hit window", "Heavy", "M84"),
        ("Overhead Slash", "skeletons, knights, giants, axe users", "Active hit window", "Heavy", "M84"),
        ("Thrust", "spear users, rapier enemies, soldiers", "Active hit window", "Medium", "M84"),
        ("Sweep", "halberd users, giants, scythe enemies", "Active hit window", "Medium", "M84"),
        ("Cleave", "axe users, heavy skeletons, brutes", "Active hit window", "Heavy", "M84"),
        ("Spear Jab", "spear skeletons, guards, hunters", "Active hit window", "Medium", "M84"),
        ("Lance Charge", "mounted enemies, knights, constructs", "Active hit window", "Heavy", "M86"),
        ("Shield Bash", "knights, guards, tower shield enemies", "Active hit window", "Medium", "M84"),
        ("Parry", "elite knights, duelists, skeleton captains", "Counter state", "Medium", "M84"),
        ("Riposte", "duelists, elite skeletons, assassins", "Active hit window", "Heavy", "M84"),
        ("Weapon Kick", "humanoid fighters, shield breakers", "Active hit window", "Medium", "M84"),
        ("Two-Hit Combo", "skeletons, knights, bandits", "Active hit window", "Medium", "M84"),
        ("Three-Hit Combo", "elite knights, bosses, assassins", "Active hit window", "Heavy", "M84"),
        ("Feint", "duelists, trickster enemies, ghosts with weapons", "No hit unless followed", "Light", "M82"),
        ("Spinning Attack", "dual-blade enemies, dancers, skeleton elites", "Active hit window", "Heavy", "M84"),
        ("Backhand Slash", "knights, giants, bosses", "Active hit window", "Medium", "M84"),
        ("Axe Hook", "axe enemies, butchers, giants", "Active hit window", "Medium", "M84"),
        ("Hammer Slam", "giants, clerics, stone soldiers", "Active hit window", "Heavy", "M84"),
        ("Mace Crush", "armored undead, clerics, brutes", "Active hit window", "Heavy", "M84"),
        ("Scythe Reap", "reapers, ghosts, cultists", "Active hit window", "Medium", "M87"),
        ("Dagger Flurry", "assassins, rats with knives, thieves", "Active hit window", "Light", "M84"),
        ("Whip Crack", "cultists, beast tamers, ghost hunters", "Active hit window", "Medium", "M84"),
        ("Thrown Weapon Followup", "bandits, skeletons, hunters", "Projectile", "Light", "M86"),
    ])
    cards += make("Ranged", [
        ("Arrow Shot", "archers, skeleton archers, hunters", "Projectile", "Light", "M86"),
        ("Arrow Volley", "archers, commanders, bosses", "Projectile pattern", "Medium", "M86"),
        ("Aimed Shot", "archers, gunslingers, elite hunters", "Projectile", "Medium", "M86"),
        ("Quick Shot", "archers, goblins, gunslingers", "Projectile", "Light", "M86"),
        ("Crossbow Bolt", "crossbow skeletons, soldiers", "Projectile", "Medium", "M86"),
        ("Thrown Knife", "assassins, thieves, cultists", "Projectile", "Light", "M86"),
        ("Thrown Spear", "spear soldiers, giants, hunters", "Projectile", "Medium", "M86"),
        ("Thrown Axe", "raiders, skeleton brutes, giants", "Projectile", "Medium", "M86"),
        ("Bomb Throw", "alchemists, goblins, machines", "Projectile or area", "Heavy", "M86"),
        ("Pistol Shot", "gunslingers, clockwork guards", "Projectile", "Medium", "M86"),
        ("Musket Shot", "riflemen, undead soldiers", "Projectile", "Heavy", "M86"),
        ("Shotgun Blast", "gunners, constructs, bosses", "Projectile fan", "Heavy", "M86"),
        ("Cannon Shot", "siege machines, giants, bosses", "Projectile or area", "Massive", "M86"),
        ("Trap Launch", "machines, wall traps, ambush enemies", "Projectile", "Medium", "M86"),
        ("Net Throw", "hunters, trappers, spiders, bandits", "Projectile control", "Light", "M86"),
        ("Harpoon Shot", "machines, fishers, sea horrors", "Projectile or grab", "Heavy", "M87"),
    ])
    cards += make("Projectile", [
        ("Slow Orb", "casters, bosses, soul enemies", "Projectile", "Light", "M86"),
        ("Fast Bolt", "wizards, turrets, ghosts", "Projectile", "Light", "M86"),
        ("Spread Shot", "casters, pods, mechanical enemies", "Projectile pattern", "Medium", "M86"),
        ("Fan Shot", "wizards, archers, bosses", "Projectile pattern", "Medium", "M86"),
        ("Radial Burst", "bosses, casters, exploding enemies", "Projectile pattern", "Medium", "M86"),
        ("Homing Shot", "ghosts, soul eaters, wizards", "Projectile", "Medium", "M87"),
        ("Boomerang Shot", "hunters, ghosts, machines", "Projectile", "Medium", "M86"),
        ("Bouncing Shot", "machines, trickster casters", "Projectile", "Light", "M86"),
        ("Splitting Shot", "casters, bosses, corrupted pods", "Projectile pattern", "Medium", "M87"),
        ("Delayed Shot", "wizards, traps, bosses", "Projectile", "Medium", "M87"),
        ("Ballistic Lob", "spitting pods, giants, artillery machines", "Projectile or area", "Medium", "M86"),
        ("Projectile Wall", "bosses, casters, mechanical gates", "Projectile pattern", "Medium", "M87"),
        ("Rotating Pattern", "bosses, machines, occult casters", "Projectile pattern", "Heavy", "M87"),
        ("Returning Shot", "ghosts, chakram users, machines", "Projectile", "Medium", "M86"),
    ])
    cards += make("Magic", [
        ("Beam", "wizards, machines, bosses, eye enemies", "Area or projectile lane", "Heavy", "M87"),
        ("Fireball", "fire casters, demons, dragons", "Projectile or area", "Medium", "M87"),
        ("Ice Spike", "frost casters, traps, ghosts", "Projectile or area", "Medium", "M87"),
        ("Lightning Strike", "storm casters, machines, bosses", "Area", "Heavy", "M87"),
        ("Falling Mark", "bosses, priests, star casters", "Area marker", "Medium", "M87"),
        ("Ground Eruption", "earth casters, bosses, burrowers", "Area", "Heavy", "M87"),
        ("Fire Trail", "demons, chargers, burning beasts", "Hazard trail", "Medium", "M87"),
        ("Curse Field", "witches, ghosts, soul eaters", "Area", "Medium", "M87"),
        ("Poison Cloud", "alchemists, insects, slimes", "Hazard area", "Light", "M87"),
        ("Gravity Pull", "cosmic enemies, bosses, machines", "Area control", "Medium", "M87"),
        ("Silence Pulse", "witch hunters, bosses, anti-magic enemies", "Area control", "Light", "M87"),
        ("Healing Chant", "priests, shamans, support casters", "No direct damage", "Light", "M87"),
        ("Shield Spell", "wizards, priests, elite enemies", "Defense state", "Light", "M87"),
        ("Teleport Strike", "warlocks, assassins, ghosts", "Movement plus active hit", "Heavy", "M87"),
    ])
    cards += make("Movement", [
        ("Sidestep", "duelists, spiders, knights, archers", "Harmless movement", "Light", "M82"),
        ("Backstep", "archers, rats, casters, duelists", "Harmless movement", "Light", "M82"),
        ("Roll", "bandits, knights, goblins", "Harmless movement", "Light", "M82"),
        ("Retreat", "prey, archers, casters, wounded enemies", "Harmless movement", "Light", "M82"),
        ("Circle", "wolves, knights, duelists, spiders", "Harmless movement", "Light", "M82"),
        ("Evade", "assassins, rats, ghosts, fast beasts", "Harmless movement", "Light", "M82"),
        ("Teleport", "wizards, ghosts, bosses", "Harmless movement unless paired", "Light", "M87"),
        ("Vanish", "ghosts, assassins, tricksters", "Harmless movement unless paired", "Light", "M87"),
        ("Burrow", "worms, spiders, grave creatures", "Harmless movement unless emerging", "Light", "M88"),
        ("Fly Strafe", "undead crows, bats, harpies", "Harmless movement", "Light", "M85"),
        ("Reposition", "all tactical enemies", "Harmless movement", "Light", "M82"),
        ("Close Distance", "melee enemies, beasts, knights", "Harmless movement", "Light", "M82"),
    ])
    cards += make("Defense", [
        ("Guard", "knights, skeletons, shield users", "Defense state", "Light", "M84"),
        ("Brace", "giants, machines, shield users", "Defense state", "Medium", "M84"),
        ("Shield Wall", "guards, constructs, commanders", "Defense state", "Medium", "M89"),
        ("Dodge Counter", "duelists, assassins, elite beasts", "Active hit after evade", "Medium", "M84"),
        ("Parry Counter", "elite knights, duelists", "Counter state", "Heavy", "M84"),
        ("Armor Harden", "stone beasts, machines, bosses", "Defense state", "Light", "M87"),
        ("Shell Hide", "turtles, insects, mimics", "Defense state", "Light", "M85"),
        ("Regenerate", "undead, slimes, occult enemies", "No direct damage", "Light", "M87"),
    ])
    cards += make("Summon", [
        ("Summon Minion", "wizards, necromancers, bosses", "Spawn event", "Light", "M87"),
        ("Summon Swarm", "spider queens, rat kings, bosses", "Spawn event", "Medium", "M87"),
        ("Raise Skeleton", "necromancers, grave enemies", "Spawn event", "Light", "M87"),
        ("Call Beast", "hunters, shamans, commanders", "Spawn event", "Medium", "M89"),
        ("Spawn Turret", "machines, engineers, bosses", "Spawn event", "Medium", "M87"),
        ("Create Clone", "mirror enemies, ghosts, bosses", "Spawn event", "Medium", "M87"),
        ("Portal Add", "warlocks, cosmic enemies, bosses", "Spawn event", "Medium", "M87"),
        ("Death Spawn", "splitters, parasites, necromancers", "Spawn event or area", "Medium", "M85"),
    ])
    cards += make("Hazard", [
        ("Spiked Body", "hedgehog beasts, spike traps, cursed armor", "Hazardous body", "Medium", "M79"),
        ("Burning Body", "fire slimes, demons, ash enemies", "Hazardous body", "Medium", "M79"),
        ("Acid Body", "slimes, insects, corrupted pods", "Hazardous body", "Medium", "M79"),
        ("Poison Aura", "toxic beasts, alchemic enemies", "Hazard area", "Light", "M87"),
        ("Static Aura", "machines, storm enemies", "Hazard area", "Medium", "M87"),
        ("Spike Patch", "traps, plant enemies, bosses", "Environmental", "Medium", "M87"),
        ("Flame Patch", "casters, demons, machines", "Environmental", "Medium", "M87"),
        ("Explode On Death", "bomb enemies, machines, cursed undead", "Area", "Heavy", "M85"),
    ])
    cards += make("Ghost/Soul", [
        ("Phase", "ghosts, wraiths, soul eaters", "Harmless movement", "Light", "M87"),
        ("Possess", "ghosts, parasites, occult bosses", "Grab or control", "Heavy", "M87"),
        ("Soul Drain", "soul eaters, ghosts, liches", "Active hit or beam", "Medium", "M87"),
        ("Curse Touch", "ghosts, cursed undead", "Active hit window", "Medium", "M87"),
        ("Fear Pulse", "wraiths, bosses, cursed idols", "Area control", "Light", "M87"),
        ("Decoy", "ghosts, mirror enemies, tricksters", "Spawn event", "Light", "M87"),
        ("Split", "ghosts, mirror husks, slimes", "Split event", "Medium", "M87"),
        ("Re-form", "ghosts, slimes, soul clusters", "Recovery event", "Light", "M87"),
        ("Pass-Through Attack", "ghosts, wraiths, phasing beasts", "Active hit window", "Medium", "M87"),
        ("Soul Projectile", "ghosts, liches, soul eaters", "Projectile", "Medium", "M87"),
        ("Life Link", "soul enemies, twin bosses", "Support state", "Light", "M87"),
        ("Haunt Zone", "ghosts, cursed rooms, bosses", "Hazard area", "Medium", "M87"),
    ])
    cards += make("Mechanical", [
        ("Saw Sweep", "machines, traps, clockwork soldiers", "Active hit window", "Heavy", "M86"),
        ("Gear Bite", "machines, mimics, constructs", "Active hit window", "Medium", "M86"),
        ("Steam Vent", "machines, traps, bosses", "Hazard area", "Medium", "M86"),
        ("Mine Drop", "machines, gunners, bosses", "Environmental", "Medium", "M86"),
        ("Laser Sweep", "machines, cosmic turrets, bosses", "Area lane", "Heavy", "M86"),
        ("Drill Charge", "machines, miners, constructs", "Active hit window", "Heavy", "M86"),
        ("Rocket Burst", "machines, gunners, bosses", "Projectile pattern", "Heavy", "M86"),
        ("Reload", "gunners, machines, turrets", "No direct damage", "Light", "M86"),
    ])
    cards += make("Boss-Scale", [
        ("Shockwave", "giants, stone bosses, hammer bosses", "Area", "Heavy", "M90"),
        ("Arena Hazard", "bosses, machines, casters", "Environmental", "Heavy", "M90"),
        ("Summon Wave", "bosses, necromancers, queens", "Spawn event", "Medium", "M90"),
        ("Multi-Stage Combo", "bosses, elite knights, demons", "Active hit windows", "Massive", "M90"),
        ("Desperation Burst", "bosses, soul enemies", "Projectile or area", "Massive", "M90"),
        ("Rotating Pattern", "bosses, machines, occult enemies", "Projectile pattern", "Heavy", "M90"),
        ("Phase Change Attack", "bosses, cosmic enemies", "Area or movement", "Heavy", "M90"),
        ("Arena Sweep", "dragons, giants, machines", "Area lane", "Massive", "M90"),
        ("Grab And Throw", "giants, demons, bosses", "Grab", "Massive", "M90"),
        ("Final Exhaustion", "bosses, elite enemies", "Recovery event", "Light", "M90"),
    ])
    return cards


ROADMAP = [
    ("M79 Contact Damage Rework V1", "Passive contact stops damaging except explicit hazardous bodies; bumping enemies disturbs or alerts but does not hurt."),
    ("M80 Active Hit Windows V1", "Attacks use readable windup, active, and recovery windows instead of proximity-only damage."),
    ("M81 Enemy Action Profiles V2", "Expand attack profiles to represent body, weapon, ranged, magic, movement, defense, and hazard actions."),
    ("M82 Lightweight Behavior Tree Layer V1", "Add simple selector/sequence behavior trees over current awareness, intelligence, disposition, cooldowns, and budgets."),
    ("M83 Noise + Disturbance V2", "Tune footsteps, attacks, proximity, and bump stimuli without adding full stealth UI."),
    ("M84 Weapon-User Enemies V1", "Skeletons, knights, and giants gain weapons, shields, swings, thrusts, and recovery windows."),
    ("M85 Creature Action Expansion V1", "Rats, spiders, birds, beasts, and body-only enemies gain richer fight/flee/action sets."),
    ("M86 Ranged + Firearm Enemies V1", "Archers, gunners, throwers, turrets, machines, and projectile pattern enemies."),
    ("M87 Magic/Ghost/Soul Enemies V1", "Casters, ghosts, soul eaters, phase movement, drain, curse, and area pressure."),
    ("M88 Navigation Adapter V1", "Add pathfinding or local-navigation milestones behind a wrapper, not as an immediate dependency."),
    ("M89 Limited Alert Sharing V1", "Selected enemies can wake nearby allies later; M78/M79 stay solo-enemy focused."),
    ("M90 Combat AI QA Lock", "Regression and feel pass across contact, attacks, weapon users, senses, movement, knockback, and bosses."),
]


def markdown_card(index: int, card: ActionCard) -> str:
    return "\n".join([
        f"### {index:03d}. {card.name}",
        "",
        f"- Category: {card.category}",
        f"- Best enemy users: {card.users}",
        f"- Contact policy: {card.contact}",
        f"- Telegraph/readability: {card.telegraph}",
        f"- Counterplay: {card.counterplay}",
        f"- Suggested impact: {card.impact}",
        f"- AI/build notes: {card.ai}",
        f"- Likely milestone priority: {card.priority}",
        "",
    ])


def write_markdown(cards: list[ActionCard]) -> None:
    DOC_PATH.parent.mkdir(parents=True, exist_ok=True)
    lines = [
        "# M78: Enemy Action Bible + Combat Behavior Roadmap",
        "",
        "M78 is a design-only enemy action bible. It defines a broad catalogue of future enemy attacks, actions, commands, and solo tactics without changing runtime behavior yet.",
        "",
        "The core combat direction is that most enemy body contact should not automatically damage the player. Contact should usually disturb, alert, bump, or reposition. Damage should come from explicit active hit windows, hazardous bodies, grabs, projectiles, area hazards, spells, weapons, traps, and boss-scale states.",
        "",
        "Coverage tags: body-only, weapon-user, ranged, magic, ghost/soul, mechanical, boss-scale.",
        "",
        "## Design Rules",
        "",
        "- Prefer plain action names over lore names.",
        "- Keep readable telegraphs, active windows, and recovery windows.",
        "- Reserve passive contact damage for explicit hazardous bodies such as spikes, fire, acid, curse, electricity, or crushing mass.",
        "- Bumping a normal enemy should disturb or alert it, not automatically hurt the player.",
        "- The player should be able to control how many enemies they wake through proximity, movement noise, and attacks, without adding full stealth UI in M78.",
        "- Future behavior tree work should sit on top of current awareness, intelligence, disposition, attack profiles, movement intent, and attack budgets.",
        "",
        "## Action Cards",
        "",
    ]
    for index, card in enumerate(cards, start=1):
        lines.append(markdown_card(index, card))

    lines += [
        "## Roadmap",
        "",
    ]
    for name, note in ROADMAP:
        lines.append(f"- {name}: {note}")

    lines += [
        "",
        "## M78 Compatibility",
        "",
        "- No runtime combat behavior changes.",
        "- No save schema changes.",
        "- No new enemy prefabs or encounters.",
        "- No pathfinding, line of sight, squad tactics, or boss runtime changes.",
        "- The PDF and Markdown are planning artifacts for M79+.",
        "",
    ]
    DOC_PATH.write_text("\n".join(lines), encoding="utf-8")


def write_report(cards: list[ActionCard]) -> None:
    REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    categories = sorted({card.category for card in cards})
    REPORT_PATH.write_text(
        "\n".join([
            "# M78 Enemy Action Bible Report",
            "",
            f"- Action cards: {len(cards)}.",
            f"- Categories: {', '.join(categories)}.",
            f"- Markdown: `{DOC_PATH.relative_to(ROOT)}`.",
            f"- PDF: `{PDF_PATH.relative_to(ROOT)}`.",
            "- Runtime changes: none.",
            "- Roadmap range: M79 through M90.",
            "- Core policy: normal body contact disturbs/alerts; only hazardous bodies passively damage.",
            "- AI direction: lightweight behavior tree layer over current awareness, intelligence, movement, profiles, and budgets.",
            "",
        ]),
        encoding="utf-8",
    )


def para(text: str, style: ParagraphStyle) -> Paragraph:
    escaped = (
        text.replace("&", "&amp;")
        .replace("<", "&lt;")
        .replace(">", "&gt;")
    )
    return Paragraph(escaped, style)


def card_flowable(index: int, card: ActionCard, styles: dict[str, ParagraphStyle]) -> list[Paragraph]:
    title = f"{index:03d}. {card.name}"
    return [
        para(title, styles["CardTitle"]),
        para(f"Category: {card.category}", styles["CardBody"]),
        para(f"Users: {card.users}", styles["CardBody"]),
        para(f"Contact: {card.contact}", styles["CardBody"]),
        para(f"Telegraph: {card.telegraph}", styles["CardBody"]),
        para(f"Counterplay: {card.counterplay}", styles["CardBody"]),
        para(f"Impact: {card.impact}", styles["CardBody"]),
        para(f"AI: {card.ai}", styles["CardBody"]),
        para(f"Priority: {card.priority}", styles["CardBody"]),
    ]


def build_pdf(cards: list[ActionCard]) -> None:
    PDF_PATH.parent.mkdir(parents=True, exist_ok=True)
    styles = getSampleStyleSheet()
    custom = {
        "Title": ParagraphStyle("M78Title", parent=styles["Title"], fontSize=18, leading=22, spaceAfter=8, textColor=colors.HexColor("#1f252d")),
        "Body": ParagraphStyle("M78Body", parent=styles["BodyText"], fontSize=8.5, leading=11),
        "Heading": ParagraphStyle("M78Heading", parent=styles["Heading2"], fontSize=12, leading=15, spaceBefore=8, spaceAfter=5, textColor=colors.HexColor("#1f252d")),
        "CardTitle": ParagraphStyle("CardTitle", parent=styles["Heading3"], fontSize=8.2, leading=9.4, spaceAfter=2, textColor=colors.HexColor("#121820")),
        "CardBody": ParagraphStyle("CardBody", parent=styles["BodyText"], fontSize=6.35, leading=7.45, spaceAfter=1, textColor=colors.HexColor("#263238")),
    }
    doc = SimpleDocTemplate(
        str(PDF_PATH),
        pagesize=landscape(letter),
        rightMargin=0.32 * inch,
        leftMargin=0.32 * inch,
        topMargin=0.34 * inch,
        bottomMargin=0.34 * inch,
        title="Hollow M78 Enemy Action Bible",
    )

    story = [
        para("M78: Enemy Action Bible + Combat Behavior Roadmap", custom["Title"]),
        para(
            "Card catalogue of universal enemy attacks and solo tactics for future combat milestones. "
            "M78 is design-only: normal body contact should disturb or alert, while damage comes from active attacks, hazardous bodies, projectiles, traps, grabs, spells, weapons, and boss states.",
            custom["Body"],
        ),
        Spacer(1, 0.08 * inch),
        Table([
            ["Action cards", str(len(cards))],
            ["Coverage", "body-only, weapon-user, ranged, magic, ghost/soul, mechanical, boss-scale"],
            ["AI direction", "lightweight behavior tree layer over awareness, intelligence, movement, profiles, and budgets"],
            ["Contact policy", "passive damage only for explicit hazardous bodies"],
        ], colWidths=[1.25 * inch, 8.4 * inch], style=TableStyle([
            ("BACKGROUND", (0, 0), (0, -1), colors.HexColor("#27313c")),
            ("TEXTCOLOR", (0, 0), (0, -1), colors.white),
            ("BACKGROUND", (1, 0), (1, -1), colors.HexColor("#f2f5f7")),
            ("GRID", (0, 0), (-1, -1), 0.25, colors.HexColor("#b9c3cc")),
            ("FONTSIZE", (0, 0), (-1, -1), 7.5),
            ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ])),
        Spacer(1, 0.1 * inch),
        para("Roadmap", custom["Heading"]),
    ]

    roadmap_rows = [["Milestone", "Purpose"]] + [[name, note] for name, note in ROADMAP]
    story.append(Table(roadmap_rows, colWidths=[2.25 * inch, 7.4 * inch], repeatRows=1, style=TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#27313c")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("BACKGROUND", (0, 1), (-1, -1), colors.HexColor("#f7f9fb")),
        ("GRID", (0, 0), (-1, -1), 0.25, colors.HexColor("#b9c3cc")),
        ("FONTSIZE", (0, 0), (-1, -1), 7.1),
        ("LEADING", (0, 0), (-1, -1), 8.4),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
    ])))
    story.append(PageBreak())
    story.append(para("Action Cards", custom["Heading"]))

    rows = []
    for i in range(0, len(cards), 2):
        left = card_flowable(i + 1, cards[i], custom)
        right = card_flowable(i + 2, cards[i + 1], custom) if i + 1 < len(cards) else ""
        rows.append([left, right])
    table = Table(rows, colWidths=[4.85 * inch, 4.85 * inch], splitByRow=True)
    table.setStyle(TableStyle([
        ("BOX", (0, 0), (-1, -1), 0.2, colors.HexColor("#d0d7de")),
        ("INNERGRID", (0, 0), (-1, -1), 0.2, colors.HexColor("#d0d7de")),
        ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#fbfcfd")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 5),
        ("RIGHTPADDING", (0, 0), (-1, -1), 5),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
    ]))
    story.append(table)
    doc.build(story)


def render_previews() -> None:
    pdftoppm = shutil.which("pdftoppm")
    if not pdftoppm:
        print("poppler preview skipped: pdftoppm not found")
        return
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    subprocess.run([pdftoppm, "-png", str(PDF_PATH), str(PREVIEW_DIR / "page")], check=True)
    print(f"rendered previews to {PREVIEW_DIR}")


def main() -> None:
    cards = action_cards()
    if not 120 <= len(cards) <= 180:
        raise SystemExit(f"M78 expected 120-180 action cards, got {len(cards)}")
    write_markdown(cards)
    write_report(cards)
    build_pdf(cards)
    render_previews()
    print(f"generated {PDF_PATH} cards={len(cards)}")


if __name__ == "__main__":
    main()
