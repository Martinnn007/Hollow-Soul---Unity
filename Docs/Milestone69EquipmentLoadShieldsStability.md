# M69: Equipment Load, Shields, Stability, And Attack Taxonomy V1

M69 adds a first equipment-load model and real shield equipment.

- Armor, shield, melee weapon, and ranged weapon each use `Light`, `Medium`, or `Heavy` load.
- Total load ranges from 4-12 and resolves into Light, Medium, or Heavy encumbrance.
- Medium/heavy load softly reduces speed and increases attack/guard stamina costs.
- Stability is a derived stat used for knockback resistance, not damage reduction.
- Starter or legacy saves fall back to `starter_buckler`.
- New shields: `starter_buckler`, `iron_kite_shield`, and `stone_wall_shield`.
- Damage now has composable channel/delivery/force/element classification metadata while preserving old `DamageThreatKind` behavior.
