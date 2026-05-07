# M99: AI Playability Stabilization Lock

## Goal
Make the current game reliably playable before expanding enemy AI again.

## Stabilized Surfaces
- Known current enemy spawn kinds resolve through the runtime catalog even when serialized enemy assets are incomplete.
- Unknown spawn kinds still fall back explicitly to Normal Chaser and report fallback.
- Macro rooms without prebaked Unity NavMesh data can build runtime NavMesh data and begin combat instead of blocking play.
- Grounded enemy navigation fails soft to local steering if a NavMesh agent cannot attach, warp, or resolve a path, preventing frozen chasers.
- Room builds keep doors and interactive objects alive even if optional dynamic carving setup is unavailable.
- Unavailable doors use a brighter opaque fallback material so inactive exits remain readable.

## Runtime Rules
- Unity NavMesh remains the preferred grounded backend.
- Local steering is now a playability recovery path only when Unity NavMesh cannot produce movement.
- Dynamic `NavMeshObstacle` carving is best-effort in M99 because static interactive blockers are already baked into room NavMesh geometry.
- Active attacks, boss behavior, harmless ordinary contact, and existing combat rules are unchanged.

## Smoke Tests
- `Milestone99AiPlayabilityStabilizationTests`
  - partial serialized catalog resolves all known runtime spawn kinds without generic fallback
  - unknown spawn kind falls back to Normal Chaser
  - macro rooms build runtime NavMesh and can begin combat
  - doors remain built and blocking interactives remain present
  - NavMesh failure falls back to local steering instead of freezing

## QA Status
Unity batchmode could not run while the project was already open in the editor. Re-run M99 EditMode smoke tests after closing the editor or from the Unity Test Runner.
