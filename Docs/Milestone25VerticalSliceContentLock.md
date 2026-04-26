# Milestone 25: Vertical Slice Content Lock

M25 turns the current prototype into a repeatable vertical-slice handoff. It does not add new gameplay systems. Instead, it pins one complete branch path and validates that the content, presentation, platform checklist, and handoff reports are ready for review.

## Locked Slice

- Branch identity: `m20_branch_features_v1`
- Locked seed: `15001`
- Room pool: M13 macro fixtures are required; `DesignerApproved` rooms are optional and additive.
- Route: New Run through combat/reward rooms, visible secret, boss-key pickup, boss-key locked boss door, boss clear, inter-branch hub, shop-card purchase, and three next-branch portal choices.
- Art bar: required ArtPass presentation roles, VFX cues, and audio cues must resolve from catalog bindings without prototype visual fallback.
- Platform bar: Windows, Vision Pro bounded, and Vision Pro immersive use equal manual QA checklist coverage. Physical Vision Pro signing/deploy may remain environment-blocked.

## Generated Assets And Reports

- Lock asset: `Assets/_Hollow/Data/VerticalSlice/VerticalSliceLock_M25.asset`
- Latest JSON report: `output/reports/latest_vertical_slice_lock.json`
- Latest Markdown report: `output/reports/latest_vertical_slice_lock.md`
- PDF handoff: `output/pdf/Hollow_M25_Vertical_Slice_Content_Lock.pdf`

Use the Unity menu item:

```bash
Hollow/Generation/Generate Milestone 25 Assets
```

Then run:

```bash
Hollow/Vertical Slice/Run M25 Lock Gate
Hollow/Validation/Run Milestone 25 Validation
```

Batchmode examples:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "/path/to/Hollow Soul - Unity" -quit -executeMethod Hollow.Editor.Generation.Milestone25AssetGenerator.Generate
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "/path/to/Hollow Soul - Unity" -quit -executeMethod Hollow.Editor.Build.VerticalSliceLockRunner.RunM25LockGate
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "/path/to/Hollow Soul - Unity" -quit -executeMethod Hollow.Editor.Validation.Milestone25Validator.Validate
```

## Lock Checks

The M25 gate validates:

- The lock asset pins `m20_branch_features_v1` and seed `15001`.
- The M13 macro fixtures are present and approved designer rooms, if any, import without duplicate IDs.
- The generated branch contains the required vertical-slice roles: origin, combat, reward, boss, and secret.
- The feature plan has a boss-key source room, visible secret room, boss room, and boss-key locked boss connection.
- Every branch connection uses explicit port-to-port traversal metadata.
- Hub state exposes exactly three shop offers and three next-branch portal choices.
- Required ArtPass prefab roles and cue definitions are present and non-fallback.
- Windows, Vision Pro bounded, and Vision Pro immersive have equal manual QA checklist coverage.

## Manual QA Checklist

- Windows: start New Run with the locked seed, clear combat rooms, collect rewards, unlock the boss door, defeat boss, enter hub, buy one shop card, and inspect all three next-branch portals.
- Windows: quit after a checkpoint and Continue to confirm room/reward/key/shop/hub state restores.
- Vision Pro bounded: repeat route smoke with tabletop scale `0.1`, HUD/minimap unscaled, readable door/shop/portal cards, and no ArtPass visual collider takeover.
- Vision Pro immersive: repeat route smoke at full world scale, verify comfort posture/readability, boss/projectile clarity, and next-branch portal placement.
- All platforms: confirm transient designer/sample sessions remain excluded from run saves and profile mutation.
