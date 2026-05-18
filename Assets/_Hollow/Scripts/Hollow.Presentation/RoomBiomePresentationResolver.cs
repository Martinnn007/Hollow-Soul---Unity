using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Presentation
{
    public static class RoomBiomePresentationResolver
    {
        private static readonly System.Collections.Generic.Dictionary<MaterialRole, Material> VerdantRuntimeMaterials = new();

        public static Material ResolveMaterial(string biomeId, MaterialRole role)
        {
            var catalog = RoomBiomeCatalogDefinition.LoadDefault();
            if (catalog != null &&
                catalog.TryGetBiome(biomeId, out var definition) &&
                definition != null &&
                definition.TryResolve(role, out var material) &&
                material != null)
            {
                return material;
            }

            if (RoomBiomeIds.Matches(biomeId, RoomBiomeIds.VerdantRuins) && TryCreateVerdantRuntimeMaterial(role, out var verdant))
            {
                return verdant;
            }

            return MaterialResolver.Resolve(role);
        }

        public static void ApplyTo(string biomeId, GameObject target, MaterialRole role)
        {
            if (target == null)
            {
                return;
            }

            ApplyTo(biomeId, target.GetComponentInChildren<Renderer>(), role);
        }

        public static void ApplyTo(string biomeId, Renderer renderer, MaterialRole role)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.sharedMaterial = ResolveMaterial(biomeId, role);
        }

        public static GameObject ResolvePrefab(string biomeId, PresentationPrefabRole role)
        {
            var catalog = RoomBiomeCatalogDefinition.LoadDefault();
            if (catalog != null &&
                catalog.TryGetBiome(biomeId, out var definition) &&
                definition != null &&
                definition.TryResolve(role, out var prefab) &&
                prefab != null)
            {
                return prefab;
            }

            return PresentationPrefabResolver.Resolve(role);
        }

        public static GameObject InstantiateVisual(
            string biomeId,
            PresentationPrefabRole role,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale)
        {
            var catalog = RoomBiomeCatalogDefinition.LoadDefault();
            GameObject overridePrefab = null;
            if (catalog != null &&
                catalog.TryGetBiome(biomeId, out var definition) &&
                definition != null)
            {
                definition.TryResolve(role, out overridePrefab);
            }

            var visual = PresentationPrefabResolver.InstantiateVisual(role, overridePrefab, parent, localPosition, localScale);
            if (visual != null && TryMaterialRoleFor(role, out var materialRole))
            {
                var material = ResolveMaterial(biomeId, materialRole);
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>(includeInactive: true))
                {
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = material;
                    }
                }
            }

            return visual;
        }

        public static bool TryResolveDecorPrefabRole(string biomeId, string decorKind, out PresentationPrefabRole role)
        {
            var catalog = RoomBiomeCatalogDefinition.LoadDefault();
            if (catalog != null &&
                catalog.TryGetBiome(biomeId, out var definition) &&
                definition != null &&
                definition.TryResolveDecorRole(decorKind, out role))
            {
                return true;
            }

            return RoomBiomeDecorKinds.TryResolveDefaultPrefabRole(decorKind, out role);
        }

        private static bool TryCreateVerdantRuntimeMaterial(MaterialRole role, out Material material)
        {
            var supported = role is MaterialRole.RoomFloor or
                MaterialRole.RoomWall or
                MaterialRole.RoomWallTransparent or
                MaterialRole.RoomObstacleRock or
                MaterialRole.DoorActive or
                MaterialRole.DoorCleared or
                MaterialRole.DoorLocked or
                MaterialRole.DoorUnavailable or
                MaterialRole.DecorGrassTuft or
                MaterialRole.DecorCrystalCluster or
                MaterialRole.DecorSmallTree or
                MaterialRole.DecorStoneRuin;
            if (!supported)
            {
                material = null;
                return false;
            }

            if (!VerdantRuntimeMaterials.TryGetValue(role, out material) || material == null)
            {
                material = MaterialResolver.CreateRuntimeMaterial(VerdantColorFor(role));
                material.name = $"RuntimeVerdant_{role}";
                VerdantRuntimeMaterials[role] = material;
            }

            return true;
        }

        private static Color VerdantColorFor(MaterialRole role)
        {
            return role switch
            {
                MaterialRole.RoomFloor => new Color(0.28f, 0.38f, 0.3f, 1f),
                MaterialRole.RoomWall => new Color(0.24f, 0.34f, 0.3f, 1f),
                MaterialRole.RoomWallTransparent => new Color(0.24f, 0.34f, 0.3f, 0.32f),
                MaterialRole.RoomObstacleRock => new Color(0.32f, 0.36f, 0.28f, 1f),
                MaterialRole.DoorActive => new Color(0.28f, 0.56f, 0.38f, 1f),
                MaterialRole.DoorCleared => new Color(0.48f, 0.9f, 0.52f, 1f),
                MaterialRole.DoorLocked => new Color(0.55f, 0.34f, 0.24f, 1f),
                MaterialRole.DoorUnavailable => new Color(0.26f, 0.32f, 0.28f, 0.9f),
                MaterialRole.DecorCrystalCluster => new Color(0.42f, 0.95f, 0.8f, 1f),
                MaterialRole.DecorSmallTree => new Color(0.14f, 0.35f, 0.18f, 1f),
                MaterialRole.DecorStoneRuin => new Color(0.45f, 0.48f, 0.39f, 1f),
                _ => new Color(0.24f, 0.55f, 0.24f, 1f)
            };
        }

        private static bool TryMaterialRoleFor(PresentationPrefabRole role, out MaterialRole materialRole)
        {
            switch (role)
            {
                case PresentationPrefabRole.RoomFloor:
                    materialRole = MaterialRole.RoomFloor;
                    return true;
                case PresentationPrefabRole.RoomObstacleRock:
                    materialRole = MaterialRole.RoomObstacleRock;
                    return true;
                case PresentationPrefabRole.DoorLocked:
                    materialRole = MaterialRole.DoorLocked;
                    return true;
                case PresentationPrefabRole.DoorActive:
                    materialRole = MaterialRole.DoorActive;
                    return true;
                case PresentationPrefabRole.DoorCleared:
                    materialRole = MaterialRole.DoorCleared;
                    return true;
                case PresentationPrefabRole.DoorUnavailable:
                    materialRole = MaterialRole.DoorUnavailable;
                    return true;
                case PresentationPrefabRole.DecorGrassTuft:
                    materialRole = MaterialRole.DecorGrassTuft;
                    return true;
                case PresentationPrefabRole.DecorCrystalCluster:
                    materialRole = MaterialRole.DecorCrystalCluster;
                    return true;
                case PresentationPrefabRole.DecorSmallTree:
                    materialRole = MaterialRole.DecorSmallTree;
                    return true;
                case PresentationPrefabRole.DecorStoneRuin:
                    materialRole = MaterialRole.DecorStoneRuin;
                    return true;
                default:
                    materialRole = default;
                    return false;
            }
        }
    }
}
