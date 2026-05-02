#!/usr/bin/env python3
from pathlib import Path
import shutil
import subprocess

from reportlab.lib import colors
from reportlab.lib.pagesizes import landscape, letter
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.platypus import Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle


ROOT = Path(__file__).resolve().parents[1]
DEFAULTS_PATH = ROOT / "Assets" / "_Hollow" / "Scripts" / "Hollow.Combat" / "EnemyAttackProfileDefaults.cs"
PDF_PATH = ROOT / "output" / "pdf" / "Hollow_M76_Enemy_Attack_Profiles.pdf"
PREVIEW_DIR = ROOT / "output" / "pdf" / "previews"

OWNER_LABELS = {
    "spawnEnemyNormal": "Normal Chaser",
    "spawnEnemyFlying": "Flying Chaser",
    "spawnEnemyFast": "Fast Chaser",
    "spawnEnemyHeavy": "Heavy Chaser",
    "spawnEnemyCharger": "Ash Charger",
    "spawnEnemyTurret": "Bone Turret",
    "spawnEnemySplitter": "Husk Splitter",
    "spawnEnemyBoss": "Stone Warden Spawn",
    "stone_warden": "Stone Warden",
    "splinter_saint": "Splinter Saint",
    "gravel_maw": "Gravel Maw",
    "cartouche_widow": "Cartouche Widow",
    "iron_reliquary": "Iron Reliquary",
    "mirror_husk": "Mirror Husk",
    "ash_comet": "Ash Comet",
    "choir_of_teeth": "Choir of Teeth",
    "rust_bishop": "Rust Bishop",
    "hollow_star_larva": "Hollow Star Larva",
}


def split_args(raw: str) -> list[str]:
    args: list[str] = []
    buf: list[str] = []
    in_string = False
    escaped = False
    parens = 0
    for ch in raw:
        if in_string:
            buf.append(ch)
            if escaped:
                escaped = False
            elif ch == "\\":
                escaped = True
            elif ch == '"':
                in_string = False
            continue

        if ch == '"':
            in_string = True
            buf.append(ch)
            continue
        if ch == "(":
            parens += 1
            buf.append(ch)
            continue
        if ch == ")":
            parens -= 1
            buf.append(ch)
            continue
        if ch == "," and parens == 0:
            args.append("".join(buf).strip())
            buf = []
            continue
        buf.append(ch)

    if buf:
        args.append("".join(buf).strip())
    return args


def string_value(token: str) -> str:
    token = token.strip()
    if token.startswith('"') and token.endswith('"'):
        return bytes(token[1:-1], "utf-8").decode("unicode_escape")
    return token


def float_value(token: str) -> float:
    return float(token.strip().rstrip("f"))


def enum_value(token: str) -> str:
    return token.strip().split(".")[-1]


def parse_specs() -> list[dict[str, object]]:
    specs: list[dict[str, object]] = []
    for line in DEFAULTS_PATH.read_text().splitlines():
        stripped = line.strip()
        if not (stripped.startswith("Enemy(") or stripped.startswith("Boss(")):
            continue

        is_boss = stripped.startswith("Boss(")
        body = stripped[stripped.find("(") + 1:stripped.rfind(")")]
        args = split_args(body)
        specs.append({
            "owner": string_value(args[0]),
            "is_boss": is_boss,
            "attack": string_value(args[1]),
            "display": string_value(args[2]),
            "runtime": enum_value(args[3]),
            "damage": int(float_value(args[4])),
            "cooldown": float_value(args[5]),
            "windup": float_value(args[6]),
            "active": float_value(args[7]),
            "range": float_value(args[8]),
            "count": int(float_value(args[9])),
            "speed": float_value(args[10]),
            "channel": enum_value(args[11]),
            "delivery": enum_value(args[12]),
            "element": enum_value(args[13]),
            "force": enum_value(args[14]),
            "threat": enum_value(args[15]),
            "knockback": float_value(args[16]),
            "guard": 0.35,
            "notes": string_value(args[17]),
        })
    return specs


def classification(spec: dict[str, object]) -> str:
    if spec["element"] == "None":
        return f"{spec['channel']} {spec['delivery']}"
    return f"{spec['channel']} {spec['delivery']} {spec['element']}"


def profile_rows(specs: list[dict[str, object]], *, boss: bool) -> list[list[str]]:
    rows = [["Owner", "Attack", "Runtime", "Type", "Force", "Threat", "Dmg", "KB", "Guard", "CD / Range", "Notes"]]
    for spec in [item for item in specs if item["is_boss"] is boss]:
        rows.append([
            OWNER_LABELS.get(str(spec["owner"]), str(spec["owner"])),
            str(spec["display"]),
            str(spec["runtime"]),
            classification(spec),
            str(spec["force"]),
            str(spec["threat"]),
            str(spec["damage"]),
            f"{float(spec['knockback']):.2f}m",
            f"x{float(spec['guard']):.2f}",
            f"{float(spec['cooldown']):.2f}s / {float(spec['range']):.2f}m",
            str(spec["notes"]),
        ])
    return rows


def build_pdf() -> None:
    specs = parse_specs()
    PDF_PATH.parent.mkdir(parents=True, exist_ok=True)
    styles = getSampleStyleSheet()
    styles.add(ParagraphStyle(
        name="Small",
        parent=styles["BodyText"],
        fontSize=7,
        leading=8.4,
        textColor=colors.HexColor("#333333"),
    ))
    styles.add(ParagraphStyle(
        name="Section",
        parent=styles["Heading2"],
        fontSize=13,
        leading=16,
        spaceBefore=10,
        spaceAfter=6,
        textColor=colors.HexColor("#20242b"),
    ))

    doc = SimpleDocTemplate(
        str(PDF_PATH),
        pagesize=landscape(letter),
        rightMargin=0.38 * inch,
        leftMargin=0.38 * inch,
        topMargin=0.42 * inch,
        bottomMargin=0.42 * inch,
        title="Hollow M76 Enemy Attack Profiles",
    )

    story = [
        Paragraph("Hollow M76: Enemy Attack Profiles + Impact Catalogue V1", styles["Title"]),
        Paragraph(
            "Bestiary-style attack catalogue for authored damage classification, force class, "
            "knockback, guard recoil, cooldown/range, and stability interaction. Runtime AI stays behavior-specific.",
            styles["BodyText"],
        ),
        Spacer(1, 0.10 * inch),
        Paragraph("Impact Contract", styles["Section"]),
        make_table([
            ["Topic", "Contract"],
            ["Attack Profiles", "Separate ScriptableObject assets are the source of truth."],
            ["Damage Type", "Uses DamageClassification: channel, delivery, element, force class."],
            ["knockback", "Each profile authors player knockback meters and guard recoil multiplier."],
            ["stability", "Existing ActiveStability thresholds reduce or cancel knockback after guard recoil."],
            ["Guard", "Perfect parry prevents recoil; blocked non-parry hits use reduced recoil."],
        ], [1.3 * inch, 8.8 * inch], font_size=8.2),
        Spacer(1, 0.10 * inch),
        Paragraph("Enemy Attack Profiles", styles["Section"]),
        make_table(profile_rows(specs, boss=False), profile_widths(), font_size=6.1),
        Spacer(1, 0.10 * inch),
        Paragraph("Boss Attack Profiles", styles["Section"]),
        make_table(profile_rows(specs, boss=True), profile_widths(), font_size=6.1),
        Spacer(1, 0.10 * inch),
        Paragraph("Compatibility", styles["Section"]),
        Paragraph(
            "No save schema change. Elemental resistance is intentionally deferred. Ash fire and Hollow Star cosmic "
            "attacks are authored now for future systems and catalogue clarity.",
            styles["BodyText"],
        ),
    ]
    doc.build(story)


def profile_widths() -> list[float]:
    return [
        0.95 * inch,
        1.05 * inch,
        0.72 * inch,
        1.15 * inch,
        0.55 * inch,
        0.72 * inch,
        0.35 * inch,
        0.45 * inch,
        0.45 * inch,
        0.82 * inch,
        2.9 * inch,
    ]


def make_table(rows: list[list[str]], widths: list[float], font_size: float) -> Table:
    table = Table(rows, colWidths=widths, repeatRows=1)
    table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#252a33")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, -1), font_size),
        ("LEADING", (0, 0), (-1, -1), font_size + 1.3),
        ("GRID", (0, 0), (-1, -1), 0.28, colors.HexColor("#c9ced8")),
        ("BACKGROUND", (0, 1), (-1, -1), colors.HexColor("#f7f8fa")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 3),
        ("RIGHTPADDING", (0, 0), (-1, -1), 3),
        ("TOPPADDING", (0, 0), (-1, -1), 2),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 2),
    ]))
    return table


def render_previews_if_available() -> None:
    pdftoppm = shutil.which("pdftoppm")
    if not pdftoppm:
        print(f"generated {PDF_PATH}; poppler preview skipped")
        return

    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    prefix = PREVIEW_DIR / "Hollow_M76_Enemy_Attack_Profiles"
    subprocess.run([pdftoppm, "-png", str(PDF_PATH), str(prefix)], check=True)
    print(f"generated {PDF_PATH}; previews written to {PREVIEW_DIR}")


def main() -> None:
    build_pdf()
    render_previews_if_available()


if __name__ == "__main__":
    main()
