# M66: Developer Lab Unity Scene Authoring Pipeline

M66 turns the Developer Lab into a Unity scene-authored inspection pipeline.

## Workflow
- Open scenes under `Assets/_Hollow/Scenes/DeveloperLab/`.
- If the folder is missing, Unity will auto-create the 10 authoring scenes once after scripts reload; you can also run `Hollow/Developer Lab/Generate Developer Lab Scenes` manually.
- Move `AuthoringMarkers/*` objects to change exported runtime positions.
- Keep child meshes and ArtPass previews visual-only; moving child visuals does not affect exported data.
- Use `Hollow/Developer Lab/Export Selected Developer Lab Scene` to export the current scene.
- Use `Hollow/Developer Lab/Export All Developer Lab Scenes` to refresh all room JSON plus the content definition.
- Use `Hollow/Developer Lab/Validate Developer Lab Scenes` before relying on the lab for QA.

## Outputs
- Room shell JSON: `Assets/_Hollow/Data/Rooms/DeveloperLab/{roomId}.hollowruntime.json`
- Gallery layout: `Assets/_Hollow/Data/DeveloperLab/DeveloperLabContentDefinition.asset`
- Runtime scenes read the content definition through `BranchSessionController`.

## Rules
- Developer Lab remains non-authoritative for saves, rewards, challenge records, and normal branch generation.
- Scene markers are source-of-truth for Developer Lab only.
- Room Designer and normal runtime rooms are unchanged by moving lab markers until the export tools are run.
