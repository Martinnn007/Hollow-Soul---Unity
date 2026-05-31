using System;
using System.Collections.Generic;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Presentation
{
    public static class RoomBiomePresentationResolver
    {
        private static readonly Dictionary<MaterialRole, Material> VerdantRuntimeMaterials = new();
        private static readonly Dictionary<string, BiomeCacheEntry> BiomeEntries = new(System.StringComparer.Ordinal);
        private static readonly MaterialRole[] PrewarmMaterialRoles =
        {
            MaterialRole.RoomFloor,
            MaterialRole.RoomWall,
            MaterialRole.RoomWallTransparent,
            MaterialRole.RoomObstacleRock,
            MaterialRole.DoorActive,
            MaterialRole.DoorCleared,
            MaterialRole.DoorLocked,
            MaterialRole.DoorUnavailable,
            MaterialRole.DecorGrassTuft,
            MaterialRole.DecorCrystalCluster,
            MaterialRole.DecorSmallTree,
            MaterialRole.DecorStoneRuin
        };
        private static readonly PresentationPrefabRole[] PrewarmPrefabRoles =
        {
            PresentationPrefabRole.RoomFloor,
            PresentationPrefabRole.RoomObstacleRock,
            PresentationPrefabRole.DoorActive,
            PresentationPrefabRole.DoorCleared,
            PresentationPrefabRole.DoorLocked,
            PresentationPrefabRole.DoorUnavailable,
            PresentationPrefabRole.DecorGrassTuft,
            PresentationPrefabRole.DecorCrystalCluster,
            PresentationPrefabRole.DecorSmallTree,
            PresentationPrefabRole.DecorStoneRuin,
            PresentationPrefabRole.RoomHazardSpike,
            PresentationPrefabRole.StandardBarrel,
            PresentationPrefabRole.ExplosiveBarrel
        };
        private static RoomBiomeCatalogDefinition cachedCatalog;

        public static Material ResolveMaterial(string biomeId, MaterialRole role)
        {
            var entry = EntryFor(biomeId);
            if (entry.Materials.TryGetValue(role, out var cached) && cached != null)
            {
                return cached;
            }

            if (entry.Definition != null &&
                entry.Definition.TryResolve(role, out var material) &&
                material != null)
            {
                entry.Materials[role] = material;
                return material;
            }

            if (RoomBiomeIds.Matches(entry.BiomeId, RoomBiomeIds.VerdantRuins) && TryCreateVerdantRuntimeMaterial(role, out var verdant))
            {
                entry.Materials[role] = verdant;
                return verdant;
            }

            var fallback = MaterialResolver.Resolve(role);
            entry.Materials[role] = fallback;
            return fallback;
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
            var entry = EntryFor(biomeId);
            if (entry.Prefabs.TryGetValue(role, out var cached) && cached != null)
            {
                return cached;
            }

            if (entry.Definition != null &&
                entry.Definition.TryResolve(role, out var prefab) &&
                prefab != null)
            {
                PresentationPrefabResolver.PrewarmPrefab(prefab);
                entry.Prefabs[role] = prefab;
                return prefab;
            }

            var fallback = PresentationPrefabResolver.Resolve(role);
            entry.Prefabs[role] = fallback;
            return fallback;
        }

        public static GameObject InstantiateVisual(
            string biomeId,
            PresentationPrefabRole role,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale)
        {
            var fallbackPrefab = PresentationPrefabResolver.Resolve(role);
            var resolvedPrefab = ResolvePrefab(biomeId, role);
            var overridePrefab = resolvedPrefab != fallbackPrefab ? resolvedPrefab : null;
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
            var entry = EntryFor(biomeId);
            var normalizedKind = RoomBiomeDecorKinds.Normalize(decorKind);
            if (entry.DecorRoles.TryGetValue(normalizedKind, out role))
            {
                return true;
            }

            if (entry.Definition != null &&
                entry.Definition.TryResolveDecorRole(normalizedKind, out role))
            {
                entry.DecorRoles[normalizedKind] = role;
                return true;
            }

            if (RoomBiomeDecorKinds.TryResolveDefaultPrefabRole(normalizedKind, out role))
            {
                entry.DecorRoles[normalizedKind] = role;
                return true;
            }

            return false;
        }

        public static void Prewarm(string biomeId)
        {
            foreach (var materialRole in PrewarmMaterialRoles)
            {
                ResolveMaterial(biomeId, materialRole);
            }

            foreach (var prefabRole in PrewarmPrefabRoles)
            {
                ResolvePrefab(biomeId, prefabRole);
            }

            TryResolveDecorPrefabRole(biomeId, RoomBiomeDecorKinds.GrassTuft, out _);
            TryResolveDecorPrefabRole(biomeId, RoomBiomeDecorKinds.CrystalCluster, out _);
            TryResolveDecorPrefabRole(biomeId, RoomBiomeDecorKinds.SmallTree, out _);
            TryResolveDecorPrefabRole(biomeId, RoomBiomeDecorKinds.StoneRuin, out _);
        }

        internal static void ClearCache()
        {
            BiomeEntries.Clear();
            cachedCatalog = null;
            VerdantRuntimeMaterials.Clear();
        }

        private static BiomeCacheEntry EntryFor(string biomeId)
        {
            var catalog = RoomBiomeCatalogDefinition.LoadDefault();
            if (cachedCatalog != catalog)
            {
                BiomeEntries.Clear();
                cachedCatalog = catalog;
            }

            var normalized = RoomBiomeIds.Normalize(biomeId);
            if (BiomeEntries.TryGetValue(normalized, out var cached))
            {
                M136PerformanceOperationCounters.ReportPresentationBiomeCacheHit();
                return cached;
            }

            M136PerformanceOperationCounters.ReportPresentationCacheMiss("biome", normalized, catalog != null ? catalog.name : "no-biome-catalog");
            RoomBiomeDefinition definition = null;
            if (catalog != null)
            {
                catalog.TryGetBiome(normalized, out definition);
            }

            var entry = new BiomeCacheEntry(normalized, definition);
            BiomeEntries[normalized] = entry;
            return entry;
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
                case PresentationPrefabRole.NextBranchPortal:
                    materialRole = MaterialRole.NextBranchPortal;
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

        private sealed class BiomeCacheEntry
        {
            public BiomeCacheEntry(string biomeId, RoomBiomeDefinition definition)
            {
                BiomeId = RoomBiomeIds.Normalize(biomeId);
                Definition = definition;
                PrimeDefinitionMappings();
            }

            public string BiomeId { get; }

            public RoomBiomeDefinition Definition { get; }

            public Dictionary<MaterialRole, Material> Materials { get; } = new();

            public Dictionary<PresentationPrefabRole, GameObject> Prefabs { get; } = new();

            public Dictionary<string, PresentationPrefabRole> DecorRoles { get; } = new(StringComparer.Ordinal);

            private void PrimeDefinitionMappings()
            {
                foreach (var binding in RoomBiomeCatalogDefinition.DefaultDecorBindings())
                {
                    DecorRoles[RoomBiomeDecorKinds.Normalize(binding.DecorKind)] = binding.PrefabRole;
                }

                if (Definition == null)
                {
                    return;
                }

                foreach (var binding in Definition.MaterialOverrides)
                {
                    if (binding.Material != null && !Materials.ContainsKey(binding.Role))
                    {
                        Materials.Add(binding.Role, binding.Material);
                    }
                }

                foreach (var binding in Definition.PrefabOverrides)
                {
                    if (binding.Prefab == null || Prefabs.ContainsKey(binding.Role))
                    {
                        continue;
                    }

                    PresentationPrefabResolver.PrewarmPrefab(binding.Prefab);
                    Prefabs.Add(binding.Role, binding.Prefab);
                }

                foreach (var binding in Definition.DecorPrefabBindings)
                {
                    DecorRoles[RoomBiomeDecorKinds.Normalize(binding.DecorKind)] = binding.PrefabRole;
                }
            }
        }
    }
}
