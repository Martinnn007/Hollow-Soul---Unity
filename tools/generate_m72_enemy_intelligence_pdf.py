#!/usr/bin/env python3
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.platypus import Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "output/pdf/Hollow_M72_Enemy_Intelligence_Catalogue.pdf"


def paragraph(text, style):
    return Paragraph(text, style)


def table(data, widths):
    result = Table(data, colWidths=widths, hAlign="LEFT")
    result.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#263238")),
                ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
                ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
                ("FONTNAME", (0, 1), (-1, -1), "Helvetica"),
                ("FONTSIZE", (0, 0), (-1, -1), 8.5),
                ("LEADING", (0, 0), (-1, -1), 10),
                ("GRID", (0, 0), (-1, -1), 0.25, colors.HexColor("#B0BEC5")),
                ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#F5F7F8")]),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("LEFTPADDING", (0, 0), (-1, -1), 5),
                ("RIGHTPADDING", (0, 0), (-1, -1), 5),
                ("TOPPADDING", (0, 0), (-1, -1), 4),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
            ]
        )
    )
    return result


def main():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)

    styles = getSampleStyleSheet()
    styles.add(
        ParagraphStyle(
            name="HollowTitle",
            parent=styles["Title"],
            fontName="Helvetica-Bold",
            fontSize=19,
            leading=23,
            textColor=colors.HexColor("#172026"),
            spaceAfter=10,
        )
    )
    styles.add(
        ParagraphStyle(
            name="HollowHeading",
            parent=styles["Heading2"],
            fontName="Helvetica-Bold",
            fontSize=12,
            leading=15,
            textColor=colors.HexColor("#263238"),
            spaceBefore=10,
            spaceAfter=5,
        )
    )
    styles.add(
        ParagraphStyle(
            name="HollowBody",
            parent=styles["BodyText"],
            fontName="Helvetica",
            fontSize=9,
            leading=12,
            textColor=colors.HexColor("#263238"),
            spaceAfter=6,
        )
    )

    story = [
        paragraph("M72: Enemy Intelligence + Instinct Disposition V1", styles["HollowTitle"]),
        paragraph(
            "Design contract for adding a 0-5 enemy intelligence scale and companion instinct disposition to Hollow's current roster. "
            "M72 is intentionally conservative: no pathfinding, no line of sight, no squad tactics, and no boss behavior changes.",
            styles["HollowBody"],
        ),
        paragraph("Intelligence Scale", styles["HollowHeading"]),
        table(
            [
                ["Value", "Label", "Runtime Intent"],
                ["0", "Instinctive", "Disposition-driven creature behavior."],
                ["1", "Simple", "Direct pressure with little adjustment."],
                ["2", "Basic", "Current direct combat with modest intent support."],
                ["3", "Trained", "Uses authored role cleanly, especially sentinel timing."],
                ["4", "Tactical", "Small priority bonus and cleaner spacing."],
                ["5", "Cunning", "Highest V1 priority bonus without bypassing pressure caps."],
            ],
            [0.55 * inch, 1.15 * inch, 5.1 * inch],
        ),
        paragraph("Disposition Definitions", styles["HollowHeading"]),
        table(
            [
                ["Disposition", "Definition"],
                ["prey", "Wanders or backs away until endangered."],
                ["predator", "Attacks directly without clever spacing at low intelligence."],
                ["sentinel", "Holds territory and attacks when approached."],
                ["mindless", "Uses simple direct or wandering pressure."],
            ],
            [1.2 * inch, 5.6 * inch],
        ),
        paragraph("Current Base Enemy Table", styles["HollowHeading"]),
        table(
            [
                ["Enemy", "Intelligence", "Disposition"],
                ["Normal Chaser", "1 Simple", "predator"],
                ["Flying Chaser", "0 Instinctive", "prey"],
                ["Fast Chaser", "1 Simple", "predator"],
                ["Heavy Chaser", "1 Simple", "mindless"],
                ["Ash Charger", "0 Instinctive", "predator"],
                ["Bone Turret", "3 Trained", "sentinel"],
                ["Husk Splitter", "2 Basic", "predator"],
                ["Stone Warden Spawn", "2 Basic", "sentinel"],
            ],
            [2.35 * inch, 1.45 * inch, 3 * inch],
        ),
        paragraph("Current Boss Metadata Table", styles["HollowHeading"]),
        table(
            [
                ["Boss", "Intelligence"],
                ["Stone Warden", "2 Basic"],
                ["Splinter Saint", "3 Trained"],
                ["Gravel Maw", "2 Basic"],
                ["Cartouche Widow", "5 Cunning"],
                ["Iron Reliquary", "4 Tactical"],
                ["Mirror Husk", "5 Cunning"],
                ["Ash Comet", "3 Trained"],
                ["Choir of Teeth", "4 Tactical"],
                ["Rust Bishop", "5 Cunning"],
                ["Hollow Star Larva", "5 Cunning"],
            ],
            [3.4 * inch, 3.4 * inch],
        ),
        paragraph("Runtime Behavior Effects", styles["HollowHeading"]),
        paragraph(
            "Instinctive prey backs away or wanders until endangered. Endangered means damaged in the last 3 seconds or kept very close briefly. "
            "Instinctive predators pressure directly. Sentinels hold territory until approached. Mindless enemies use direct or short wandering pressure. "
            "Tactical and Cunning enemies receive a small attack-priority bonus while the room budget still limits standard enemy attack starts.",
            styles["HollowBody"],
        ),
        paragraph("Save/Continue Compatibility", styles["HollowHeading"]),
        paragraph(
            "Encounter saves snapshot resolved intelligence and disposition values beside spawn kinds. Continue restores exact values by spawn index. "
            "Legacy saves without M72 fields fall back to current catalog defaults. Runtime instinct timers reset on Continue.",
            styles["HollowBody"],
        ),
        paragraph("Remaining Limitations And V3 Recommendations", styles["HollowHeading"]),
        paragraph(
            "M72 adds no pathfinding, line of sight, squad coordination, or boss behavior changes. V3 should revisit richer movement intents only after base combat readability and room-clear pacing are stable. "
            "Low-intelligence fleeing remains short and readable so room clears do not become hide-and-seek.",
            styles["HollowBody"],
        ),
    ]

    doc = SimpleDocTemplate(
        str(OUTPUT),
        pagesize=letter,
        rightMargin=0.55 * inch,
        leftMargin=0.55 * inch,
        topMargin=0.55 * inch,
        bottomMargin=0.55 * inch,
    )
    doc.build(story)
    print(OUTPUT)


if __name__ == "__main__":
    main()
