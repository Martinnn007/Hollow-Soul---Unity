using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Rewards;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Branches
{
    public static class BranchGenerator
    {
        public const string LegacyFiveRoomBranchId = "m7_five_room_cross";
        public const string MacroFixtureBranchId = "m14_macro_fixture_branch_v1";
        public const string SeededMacroBranchId = "m15_seeded_macro_branch_v1";
        public const string FeatureBranchId = "m17_feature_branch_v1";
        public const string EnemyEncounterBranchId = "m19_enemy_encounter_content_v1";
        public const string BranchFeaturesId = "m20_branch_features_v1";
        public const string DirectedEncounterBranchId = "m46_encounter_director_curve_v1";
        public const string CorruptedChestRoomAssetId = "corrupted_chest_single_1x1";
        public const string WaveRoomAssetId = "wave_room_single_1x1";
        public const string SpecialSoulEaterRoomAssetId = SpecialEncounterResolver.SoulEaterRoomAssetId;
        public const string SpecialEscapistRoomAssetId = SpecialEncounterResolver.EscapistRoomAssetId;
        public const int DefaultMacroFixtureSeed = 14001;
        public const int DefaultSeededMacroSeed = 15001;

        public static bool ShouldRollCorruptedChestLeaf(string branchId, int seed)
        {
            return StableBranchHash($"{branchId}|{seed}|m130_corrupted_chest") % 100 < ChestRewardResolver.CorruptedChestRollPercent;
        }

        public static bool ShouldRollSpecialEncounterLeaf(string branchId, int seed)
        {
            return SpecialEncounterResolver.ShouldRollSpecialEncounterLeaf(branchId, seed);
        }

        public static BranchFloorGraph CreateFiveRoomCross(ImportedRoomRuntimeAsset roomAsset)
        {
            var graph = new BranchFloorGraph(LegacyFiveRoomBranchId, 0);
            var roomAssetId = roomAsset?.Id ?? string.Empty;
            graph.AddRoom(new BranchRoomState(BranchRoomId.Origin, Vector2Int.zero, new BranchRoomInstanceId("origin"), roomAssetId, null, BranchRoomRole.Origin));
            graph.AddRoom(new BranchRoomState(BranchRoomId.North, new Vector2Int(0, -1), new BranchRoomInstanceId("north"), roomAssetId, null, BranchRoomRole.Reward));
            graph.AddRoom(new BranchRoomState(BranchRoomId.South, new Vector2Int(0, 1), new BranchRoomInstanceId("south"), roomAssetId, null, BranchRoomRole.Reward));
            graph.AddRoom(new BranchRoomState(BranchRoomId.East, new Vector2Int(1, 0), new BranchRoomInstanceId("east"), roomAssetId, null, BranchRoomRole.Reward));
            graph.AddRoom(new BranchRoomState(BranchRoomId.West, new Vector2Int(-1, 0), new BranchRoomInstanceId("west"), roomAssetId, null, BranchRoomRole.Reward));

            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.North, "north", "south");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.South, "south", "north");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.East, "east", "west");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.West, "west", "east");
            return graph;
        }

        public static BranchFloorGraph CreateMacroFixtureBranch(IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> roomPool, int seed)
        {
            var graph = new BranchFloorGraph(MacroFixtureBranchId, seed == 0 ? DefaultMacroFixtureSeed : seed);
            var origin = RequireRoomByIdOrShape(roomPool, "combat_macro_single_1x1", RoomFootprintShape.Single1x1);
            var north = RequireRoomByIdOrShape(roomPool, "combat_macro_tall_1x2", RoomFootprintShape.Tall1x2);
            var south = RequireRoomByIdOrShape(roomPool, "combat_macro_l_3cell", RoomFootprintShape.L3Cell);
            var east = RequireRoomByIdOrShape(roomPool, "combat_macro_wide_2x1", RoomFootprintShape.Wide2x1);
            var west = RequireRoomByIdOrShape(roomPool, "combat_macro_block_2x2", RoomFootprintShape.Block2x2);

            graph.AddRoom(CreateRoom(BranchRoomId.Origin, Vector2Int.zero, origin, BranchRoomRole.Origin));
            graph.AddRoom(CreateRoom(BranchRoomId.North, new Vector2Int(0, -2), north, BranchRoomRole.Reward));
            graph.AddRoom(CreateRoom(BranchRoomId.South, new Vector2Int(0, 1), south, BranchRoomRole.Reward));
            graph.AddRoom(CreateRoom(BranchRoomId.East, new Vector2Int(1, 0), east, BranchRoomRole.Reward));
            graph.AddRoom(CreateRoom(BranchRoomId.West, new Vector2Int(-2, -1), west, BranchRoomRole.Reward));

            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.North, "north", "south", "north_0", "south_0");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.South, "south", "north", "south_0", "north_0");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.East, "east", "west", "east_0", "west_0");
            graph.AddBidirectionalConnection(BranchRoomId.Origin, BranchRoomId.West, "west", "east", "west_0", "east_1");
            ConnectAdjacentCompatiblePorts(graph, roomPool);
            return graph;
        }

        public static BranchFloorGraph CreateSeededMacroBranch(BranchSessionContent content, BranchGenerationSettingsDefinition settings, int seed)
        {
            return CreateSeededMacroBranch(content, settings, seed, RoomBiomeIds.HollowThreshold);
        }

        public static BranchFloorGraph CreateSeededMacroBranch(BranchSessionContent content, BranchGenerationSettingsDefinition settings, int seed, string biomeId)
        {
            if (content == null || !content.HasMacroFixturePool)
            {
                throw new InvalidOperationException("Seeded macro branch generation requires a complete macro room pool.");
            }

            settings = settings != null ? settings : BranchGenerationSettingsDefinition.CreateRuntimeDefault();
            if (settings.AllowLoops)
            {
                throw new InvalidOperationException("M15 seeded macro branch generation does not support loops.");
            }

            return CreateSeededBranch(content, settings, seed, SeededMacroBranchId, enableTreasureLeaf: false, milestoneLabel: "M15", activeRoomPool: ActivePoolFor(content, biomeId));
        }

        public static BranchFloorGraph CreateSeededFeatureBranch(BranchSessionContent content, BranchGenerationSettingsDefinition settings, int seed)
        {
            return CreateSeededFeatureBranch(content, settings, seed, RoomBiomeIds.HollowThreshold);
        }

        public static BranchFloorGraph CreateSeededFeatureBranch(BranchSessionContent content, BranchGenerationSettingsDefinition settings, int seed, string biomeId)
        {
            if (content == null || !content.HasMacroFixturePool)
            {
                throw new InvalidOperationException("Seeded feature branch generation requires a complete macro room pool.");
            }

            settings = settings != null ? settings : BranchGenerationSettingsDefinition.CreateRuntimeDefault();
            if (settings.AllowLoops)
            {
                throw new InvalidOperationException("M17 seeded feature branch generation does not support loops.");
            }

            return CreateSeededBranch(content, settings, seed, FeatureBranchId, enableTreasureLeaf: true, milestoneLabel: "M17", activeRoomPool: ActivePoolFor(content, biomeId));
        }

        public static BranchFloorGraph CreateSeededEncounterBranch(BranchSessionContent content, BranchGenerationSettingsDefinition settings, int seed)
        {
            return CreateSeededEncounterBranch(content, settings, seed, RoomBiomeIds.HollowThreshold);
        }

        public static BranchFloorGraph CreateSeededEncounterBranch(BranchSessionContent content, BranchGenerationSettingsDefinition settings, int seed, string biomeId)
        {
            if (content == null || !content.HasMacroFixturePool)
            {
                throw new InvalidOperationException("Seeded encounter branch generation requires a complete macro room pool.");
            }

            settings = settings != null ? settings : BranchGenerationSettingsDefinition.CreateRuntimeDefault();
            if (settings.AllowLoops)
            {
                throw new InvalidOperationException("M19 seeded encounter branch generation does not support loops.");
            }

            return CreateSeededBranch(content, settings, seed, EnemyEncounterBranchId, enableTreasureLeaf: true, milestoneLabel: "M19", activeRoomPool: ActivePoolFor(content, biomeId));
        }

        public static BranchFloorGraph CreateSeededBranchFeatures(BranchSessionContent content, BranchGenerationSettingsDefinition settings, int seed)
        {
            return CreateSeededBranchFeatures(content, settings, seed, RoomBiomeIds.HollowThreshold);
        }

        public static BranchFloorGraph CreateSeededBranchFeatures(BranchSessionContent content, BranchGenerationSettingsDefinition settings, int seed, string biomeId)
        {
            return CreateSeededBranchFeatures(content, settings, seed, biomeId, enableCorruptedChestLeaf: false);
        }

        public static BranchFloorGraph CreateSeededBranchFeatures(BranchSessionContent content, BranchGenerationSettingsDefinition settings, int seed, string biomeId, bool enableCorruptedChestLeaf)
        {
            return CreateSeededBranchFeatures(content, settings, seed, biomeId, enableCorruptedChestLeaf, enableWaveRoomLeaf: false);
        }

        public static BranchFloorGraph CreateSeededBranchFeatures(BranchSessionContent content, BranchGenerationSettingsDefinition settings, int seed, string biomeId, bool enableCorruptedChestLeaf, bool enableWaveRoomLeaf)
        {
            return CreateSeededBranchFeatures(content, settings, seed, biomeId, enableCorruptedChestLeaf, enableWaveRoomLeaf, enableSpecialEncounterLeaf: false);
        }

        public static BranchFloorGraph CreateSeededBranchFeatures(BranchSessionContent content, BranchGenerationSettingsDefinition settings, int seed, string biomeId, bool enableCorruptedChestLeaf, bool enableWaveRoomLeaf, bool enableSpecialEncounterLeaf)
        {
            if (content == null || !content.HasMacroFixturePool)
            {
                throw new InvalidOperationException("M20 branch feature generation requires a complete macro room pool.");
            }

            settings = settings != null ? settings : BranchGenerationSettingsDefinition.CreateRuntimeDefault();
            if (settings.AllowLoops)
            {
                throw new InvalidOperationException("M20 branch feature generation does not support loops.");
            }

            var graph = CreateSeededBranch(
                content,
                settings,
                seed,
                BranchFeaturesId,
                enableTreasureLeaf: true,
                milestoneLabel: "M20",
                activeRoomPool: ActivePoolFor(content, biomeId),
                enableCorruptedChestLeaf: enableCorruptedChestLeaf,
                corruptedChestRoomAsset: content.CorruptedChestRoomAsset,
                enableWaveRoomLeaf: enableWaveRoomLeaf,
                waveRoomAsset: content.WaveRoomAsset,
                enableSpecialEncounterLeaf: enableSpecialEncounterLeaf,
                specialSoulEaterRoomAsset: content.SpecialSoulEaterRoomAsset,
                specialEscapistRoomAsset: content.SpecialEscapistRoomAsset);
            ApplyBossKeyLock(graph);
            return graph;
        }

        public static BranchFloorGraph CreateDirectedEncounterBranch(
            BranchSessionContent content,
            BranchGenerationSettingsDefinition settings,
            EncounterDirectorProfileDefinition directorProfile,
            int worldIndex,
            int seed)
        {
            return CreateDirectedEncounterBranch(content, settings, directorProfile, worldIndex, seed, string.Empty);
        }

        public static BranchFloorGraph CreateDirectedEncounterBranch(
            BranchSessionContent content,
            BranchGenerationSettingsDefinition settings,
            EncounterDirectorProfileDefinition directorProfile,
            int worldIndex,
            int seed,
            string bossRoomAssetId)
        {
            return CreateDirectedEncounterBranch(content, settings, directorProfile, worldIndex, seed, bossRoomAssetId, RoomBiomeIds.HollowThreshold);
        }

        public static BranchFloorGraph CreateDirectedEncounterBranch(
            BranchSessionContent content,
            BranchGenerationSettingsDefinition settings,
            EncounterDirectorProfileDefinition directorProfile,
            int worldIndex,
            int seed,
            string bossRoomAssetId,
            string biomeId)
        {
            return CreateDirectedEncounterBranch(content, settings, directorProfile, worldIndex, seed, bossRoomAssetId, biomeId, enableCorruptedChestLeaf: false);
        }

        public static BranchFloorGraph CreateDirectedEncounterBranch(
            BranchSessionContent content,
            BranchGenerationSettingsDefinition settings,
            EncounterDirectorProfileDefinition directorProfile,
            int worldIndex,
            int seed,
            string bossRoomAssetId,
            string biomeId,
            bool enableCorruptedChestLeaf)
        {
            return CreateDirectedEncounterBranch(content, settings, directorProfile, worldIndex, seed, bossRoomAssetId, biomeId, enableCorruptedChestLeaf, enableWaveRoomLeaf: false);
        }

        public static BranchFloorGraph CreateDirectedEncounterBranch(
            BranchSessionContent content,
            BranchGenerationSettingsDefinition settings,
            EncounterDirectorProfileDefinition directorProfile,
            int worldIndex,
            int seed,
            string bossRoomAssetId,
            string biomeId,
            bool enableCorruptedChestLeaf,
            bool enableWaveRoomLeaf)
        {
            return CreateDirectedEncounterBranch(content, settings, directorProfile, worldIndex, seed, bossRoomAssetId, biomeId, enableCorruptedChestLeaf, enableWaveRoomLeaf, enableSpecialEncounterLeaf: false);
        }

        public static BranchFloorGraph CreateDirectedEncounterBranch(
            BranchSessionContent content,
            BranchGenerationSettingsDefinition settings,
            EncounterDirectorProfileDefinition directorProfile,
            int worldIndex,
            int seed,
            string bossRoomAssetId,
            string biomeId,
            bool enableCorruptedChestLeaf,
            bool enableWaveRoomLeaf,
            bool enableSpecialEncounterLeaf)
        {
            if (content == null || !content.HasMacroFixturePool)
            {
                throw new InvalidOperationException("M46 directed encounter branch generation requires a complete macro room pool.");
            }

            settings = settings != null ? settings : BranchGenerationSettingsDefinition.CreateRuntimeDefault();
            if (settings.AllowLoops)
            {
                throw new InvalidOperationException("M46 directed encounter branch generation does not support loops.");
            }

            var profile = EncounterDirectorProfileDefinition.Resolve(directorProfile);
            var targetRooms = profile.WorldConfigFor(worldIndex).TargetRoomCount;
            var graph = CreateSeededBranch(
                content,
                settings,
                seed,
                DirectedEncounterBranchId,
                enableTreasureLeaf: true,
                milestoneLabel: "M46",
                targetRoomCountOverride: targetRooms,
                bossRoomAssetId: bossRoomAssetId,
                activeRoomPool: ActivePoolFor(content, biomeId),
                enableCorruptedChestLeaf: enableCorruptedChestLeaf,
                corruptedChestRoomAsset: content.CorruptedChestRoomAsset,
                enableWaveRoomLeaf: enableWaveRoomLeaf,
                waveRoomAsset: content.WaveRoomAsset,
                enableSpecialEncounterLeaf: enableSpecialEncounterLeaf,
                specialSoulEaterRoomAsset: content.SpecialSoulEaterRoomAsset,
                specialEscapistRoomAsset: content.SpecialEscapistRoomAsset);
            ApplyBossKeyLock(graph);
            return graph;
        }

        private static BranchFloorGraph CreateSeededBranch(
            BranchSessionContent content,
            BranchGenerationSettingsDefinition settings,
            int seed,
            string branchId,
            bool enableTreasureLeaf,
            string milestoneLabel,
            int targetRoomCountOverride = 0,
            string bossRoomAssetId = "",
            IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> activeRoomPool = null,
            bool enableCorruptedChestLeaf = false,
            ImportedRoomRuntimeAsset corruptedChestRoomAsset = null,
            bool enableWaveRoomLeaf = false,
            ImportedRoomRuntimeAsset waveRoomAsset = null,
            bool enableSpecialEncounterLeaf = false,
            ImportedRoomRuntimeAsset specialSoulEaterRoomAsset = null,
            ImportedRoomRuntimeAsset specialEscapistRoomAsset = null)
        {
            var resolvedSeed = seed == 0 ? settings.DefaultSeed : seed;
            var random = new System.Random(resolvedSeed);
            var roomPool = activeRoomPool != null && activeRoomPool.Count > 0 ? activeRoomPool : content.MacroRoomPool;
            var fixturePool = content.FixtureRoomPool;
            var candidatesByShape = BuildCandidatesByShape(roomPool.Values);
            var fixtureIds = settings.AllowedFixtureIds
                .Where(id => fixturePool.ContainsKey(id))
                .Distinct()
                .OrderBy(id => id)
                .ToList();
            if (fixtureIds.Count == 0)
            {
                fixtureIds = fixturePool.Keys.OrderBy(id => id).ToList();
            }

            var targetRoomCount = Mathf.Max(2, targetRoomCountOverride > 0 ? targetRoomCountOverride : settings.TargetRoomCount);
            var records = new List<PlacementRecord>();
            var usedPortsByTempIndex = new Dictionary<int, HashSet<string>>();
            var occupiedCells = new HashSet<Vector2Int>();

            var originAsset = ChooseCandidateForShape(
                candidatesByShape,
                RequireRoom(fixturePool, "combat_macro_single_1x1"),
                random);
            var origin = new PlacementRecord(0, originAsset, Vector2Int.zero, PlaceFootprint(originAsset.Footprint, Vector2Int.zero));
            records.Add(origin);
            usedPortsByTempIndex[0] = new HashSet<string>();
            RegisterCells(occupiedCells, origin.Footprint);

            var requiresBossRoom = settings.EnableBossLeaf;
            var requiresSecretRoom = branchId == BranchFeaturesId || branchId == DirectedEncounterBranchId;
            var reservedRoomCount = (requiresBossRoom ? 1 : 0) +
                                    (requiresSecretRoom ? 1 : 0) +
                                    (enableWaveRoomLeaf ? 1 : 0);
            var normalPlacementTarget = Mathf.Max(0, targetRoomCount - 1 - reservedRoomCount);

            for (var tempIndex = 1; tempIndex <= normalPlacementTarget; tempIndex++)
            {
                if (!TryPlaceNextRecord(records, usedPortsByTempIndex, occupiedCells, fixturePool, candidatesByShape, fixtureIds, random, settings.MaxPlacementAttempts, tempIndex, out var record))
                {
                    throw new InvalidOperationException($"{milestoneLabel} seeded branch generation failed to place room {tempIndex} after {settings.MaxPlacementAttempts} attempts.");
                }

                records.Add(record);
                usedPortsByTempIndex[record.TempIndex] = new HashSet<string> { record.ToPortId };
                usedPortsByTempIndex[record.ParentTempIndex].Add(record.FromPortId);
                RegisterCells(occupiedCells, record.Footprint);
            }

            var secretTempIndex = -1;
            if (requiresSecretRoom)
            {
                var secretTempCandidate = records.Count;
                var secretFixtureIds = new[] { "combat_macro_single_1x1" };
                if (!TryPlaceEndpointRecord(records, usedPortsByTempIndex, occupiedCells, fixturePool, candidatesByShape, secretFixtureIds, random, settings.MaxPlacementAttempts, secretTempCandidate, requireSingleRoom: true, excludedParentTempIndices: null, out var secretRecord))
                {
                    throw new InvalidOperationException($"{milestoneLabel} seeded branch generation failed to place a terminal single-room secret after {settings.MaxPlacementAttempts} attempts.");
                }

                records.Add(secretRecord);
                usedPortsByTempIndex[secretRecord.TempIndex] = new HashSet<string> { secretRecord.ToPortId };
                usedPortsByTempIndex[secretRecord.ParentTempIndex].Add(secretRecord.FromPortId);
                RegisterCells(occupiedCells, secretRecord.Footprint);
                secretTempIndex = secretRecord.TempIndex;
            }

            var waveTempIndex = -1;
            if (enableWaveRoomLeaf)
            {
                var waveTempCandidate = records.Count;
                var excludedWaveParents = new HashSet<int>();
                if (secretTempIndex >= 0)
                {
                    excludedWaveParents.Add(secretTempIndex);
                }

                var waveFixturePool = roomPool;
                var waveCandidatesByShape = candidatesByShape;
                var waveFixtureIds = new[] { "combat_macro_single_1x1" };
                var exactWaveFixtureId = false;
                if (waveRoomAsset != null && !string.IsNullOrWhiteSpace(waveRoomAsset.Id))
                {
                    var poolWithWaveRoom = new Dictionary<string, ImportedRoomRuntimeAsset>(roomPool)
                    {
                        [waveRoomAsset.Id] = waveRoomAsset
                    };
                    waveFixturePool = poolWithWaveRoom;
                    waveCandidatesByShape = BuildCandidatesByShape(poolWithWaveRoom.Values);
                    waveFixtureIds = new[] { waveRoomAsset.Id };
                    exactWaveFixtureId = true;
                }

                if (!TryPlaceEndpointRecord(
                        records,
                        usedPortsByTempIndex,
                        occupiedCells,
                        waveFixturePool,
                        waveCandidatesByShape,
                        waveFixtureIds,
                        random,
                        settings.MaxPlacementAttempts,
                        waveTempCandidate,
                        requireSingleRoom: true,
                        excludedParentTempIndices: excludedWaveParents,
                        out var waveRecord,
                        exactFixtureIds: exactWaveFixtureId))
                {
                    throw new InvalidOperationException($"{milestoneLabel} seeded branch generation failed to place a terminal wave room after {settings.MaxPlacementAttempts} attempts.");
                }

                records.Add(waveRecord);
                usedPortsByTempIndex[waveRecord.TempIndex] = new HashSet<string> { waveRecord.ToPortId };
                usedPortsByTempIndex[waveRecord.ParentTempIndex].Add(waveRecord.FromPortId);
                RegisterCells(occupiedCells, waveRecord.Footprint);
                waveTempIndex = waveRecord.TempIndex;
            }

            var bossTempIndex = -1;
            if (requiresBossRoom)
            {
                var bossTempCandidate = records.Count;
                var excludedBossParents = new HashSet<int>();
                if (secretTempIndex >= 0)
                {
                    excludedBossParents.Add(secretTempIndex);
                }

                if (waveTempIndex >= 0)
                {
                    excludedBossParents.Add(waveTempIndex);
                }

                var hasExactBossRoomAsset = !string.IsNullOrWhiteSpace(bossRoomAssetId) &&
                                            roomPool.ContainsKey(bossRoomAssetId);
                IReadOnlyList<string> bossFixtureIds = hasExactBossRoomAsset
                    ? new[] { bossRoomAssetId }
                    : fixtureIds;
                var exactBossRoom = hasExactBossRoomAsset;
                var bossFixturePool = exactBossRoom ? roomPool : fixturePool;
                if (!TryPlaceEndpointRecord(records, usedPortsByTempIndex, occupiedCells, bossFixturePool, candidatesByShape, bossFixtureIds, random, settings.MaxPlacementAttempts, bossTempCandidate, requireSingleRoom: false, excludedParentTempIndices: excludedBossParents, out var bossRecord, exactFixtureIds: exactBossRoom))
                {
                    throw new InvalidOperationException($"{milestoneLabel} seeded branch generation failed to place a terminal boss room after {settings.MaxPlacementAttempts} attempts.");
                }

                records.Add(bossRecord);
                usedPortsByTempIndex[bossRecord.TempIndex] = new HashSet<string> { bossRecord.ToPortId };
                usedPortsByTempIndex[bossRecord.ParentTempIndex].Add(bossRecord.FromPortId);
                RegisterCells(occupiedCells, bossRecord.Footprint);
                bossTempIndex = bossRecord.TempIndex;
            }

            var treasureTempIndex = enableTreasureLeaf && !requiresSecretRoom ? SelectTreasureLeaf(records, bossTempIndex) : -1;
            var corruptedTempIndex = -1;
            if (enableCorruptedChestLeaf &&
                ShouldRollCorruptedChestLeaf(branchId, resolvedSeed))
            {
                var corruptedTempCandidate = records.Count;
                var excludedCorruptedParents = new HashSet<int>();
                if (secretTempIndex >= 0)
                {
                    excludedCorruptedParents.Add(secretTempIndex);
                }

                if (bossTempIndex >= 0)
                {
                    excludedCorruptedParents.Add(bossTempIndex);
                }

                if (treasureTempIndex >= 0)
                {
                    excludedCorruptedParents.Add(treasureTempIndex);
                }

                if (waveTempIndex >= 0)
                {
                    excludedCorruptedParents.Add(waveTempIndex);
                }

                var corruptedFixturePool = roomPool;
                var corruptedCandidatesByShape = candidatesByShape;
                var corruptedFixtureIds = new[] { "combat_macro_single_1x1" };
                var exactCorruptedFixtureId = false;
                if (corruptedChestRoomAsset != null && !string.IsNullOrWhiteSpace(corruptedChestRoomAsset.Id))
                {
                    var poolWithCorruptedRoom = new Dictionary<string, ImportedRoomRuntimeAsset>(roomPool)
                    {
                        [corruptedChestRoomAsset.Id] = corruptedChestRoomAsset
                    };
                    corruptedFixturePool = poolWithCorruptedRoom;
                    corruptedCandidatesByShape = BuildCandidatesByShape(poolWithCorruptedRoom.Values);
                    corruptedFixtureIds = new[] { corruptedChestRoomAsset.Id };
                    exactCorruptedFixtureId = true;
                }

                if (TryPlaceEndpointRecord(
                        records,
                        usedPortsByTempIndex,
                        occupiedCells,
                        corruptedFixturePool,
                        corruptedCandidatesByShape,
                        corruptedFixtureIds,
                        random,
                        settings.MaxPlacementAttempts,
                        corruptedTempCandidate,
                        requireSingleRoom: true,
                        excludedParentTempIndices: excludedCorruptedParents,
                        out var corruptedRecord,
                        exactFixtureIds: exactCorruptedFixtureId))
                {
                    records.Add(corruptedRecord);
                    usedPortsByTempIndex[corruptedRecord.TempIndex] = new HashSet<string> { corruptedRecord.ToPortId };
                    usedPortsByTempIndex[corruptedRecord.ParentTempIndex].Add(corruptedRecord.FromPortId);
                    RegisterCells(occupiedCells, corruptedRecord.Footprint);
                    corruptedTempIndex = corruptedRecord.TempIndex;
                }
            }

            var specialTempIndex = -1;
            if (enableSpecialEncounterLeaf &&
                ShouldRollSpecialEncounterLeaf(branchId, resolvedSeed))
            {
                var specialTempCandidate = records.Count;
                var excludedSpecialParents = new HashSet<int>();
                if (secretTempIndex >= 0)
                {
                    excludedSpecialParents.Add(secretTempIndex);
                }

                if (bossTempIndex >= 0)
                {
                    excludedSpecialParents.Add(bossTempIndex);
                }

                if (treasureTempIndex >= 0)
                {
                    excludedSpecialParents.Add(treasureTempIndex);
                }

                if (corruptedTempIndex >= 0)
                {
                    excludedSpecialParents.Add(corruptedTempIndex);
                }

                if (waveTempIndex >= 0)
                {
                    excludedSpecialParents.Add(waveTempIndex);
                }

                var specialKind = SpecialEncounterResolver.ResolveKind(branchId, resolvedSeed);
                var specialRoomAsset = specialKind == SpecialEncounterKind.SoulEater
                    ? specialSoulEaterRoomAsset
                    : specialEscapistRoomAsset;
                if (specialRoomAsset != null && !string.IsNullOrWhiteSpace(specialRoomAsset.Id))
                {
                    var poolWithSpecialRoom = new Dictionary<string, ImportedRoomRuntimeAsset>(roomPool)
                    {
                        [specialRoomAsset.Id] = specialRoomAsset
                    };
                    if (TryPlaceEndpointRecord(
                            records,
                            usedPortsByTempIndex,
                            occupiedCells,
                            poolWithSpecialRoom,
                            BuildCandidatesByShape(poolWithSpecialRoom.Values),
                            new[] { specialRoomAsset.Id },
                            random,
                            settings.MaxPlacementAttempts,
                            specialTempCandidate,
                            requireSingleRoom: true,
                            excludedParentTempIndices: excludedSpecialParents,
                            out var specialRecord,
                            exactFixtureIds: true))
                    {
                        records.Add(specialRecord);
                        usedPortsByTempIndex[specialRecord.TempIndex] = new HashSet<string> { specialRecord.ToPortId };
                        usedPortsByTempIndex[specialRecord.ParentTempIndex].Add(specialRecord.FromPortId);
                        RegisterCells(occupiedCells, specialRecord.Footprint);
                        specialTempIndex = specialRecord.TempIndex;
                    }
                }
            }

            var rewardTempIndices = SelectRewardRoomTempIndices(
                records,
                bossTempIndex,
                new[] { secretTempIndex, treasureTempIndex, corruptedTempIndex, waveTempIndex, specialTempIndex },
                branchId,
                resolvedSeed);
            var idByTempIndex = AssignRoomIds(records, bossTempIndex);
            var graph = new BranchFloorGraph(branchId, resolvedSeed);

            foreach (var record in records)
            {
                var roomId = idByTempIndex[record.TempIndex];
                var role = record.TempIndex == 0
                    ? BranchRoomRole.Origin
                    : record.TempIndex == bossTempIndex
                        ? BranchRoomRole.Boss
                        : record.TempIndex == secretTempIndex
                            ? BranchRoomRole.Secret
                            : record.TempIndex == corruptedTempIndex
                            ? BranchRoomRole.CorruptedChest
                            : record.TempIndex == waveTempIndex
                            ? BranchRoomRole.Wave
                            : record.TempIndex == specialTempIndex
                            ? BranchRoomRole.SpecialEncounter
                            : record.TempIndex == treasureTempIndex
                                ? BranchRoomRole.Treasure
                                : rewardTempIndices.Contains(record.TempIndex) ? BranchRoomRole.Reward : BranchRoomRole.Combat;
                graph.AddRoom(new BranchRoomState(
                    roomId,
                    record.PrimaryCell,
                    new BranchRoomInstanceId(roomId.Value),
                    record.Asset.Id,
                    record.Footprint,
                    role));
            }

            foreach (var record in records.Where(record => record.TempIndex != 0))
            {
                var parentId = idByTempIndex[record.ParentTempIndex];
                var childId = idByTempIndex[record.TempIndex];
                graph.AddBidirectionalConnection(
                    parentId,
                    childId,
                    record.FromDirection,
                    record.ToDirection,
                    record.FromPortId,
                    record.ToPortId);
            }

            ConnectAdjacentCompatiblePorts(graph, roomPool);
            if (!ValidateSpecialRoomTopology(graph, out var topologyError))
            {
                throw new InvalidOperationException($"{milestoneLabel} seeded branch generation produced invalid special-room topology: {topologyError}");
            }

            return graph;
        }

        private static bool TryPlaceNextRecord(
            IReadOnlyList<PlacementRecord> records,
            IReadOnlyDictionary<int, HashSet<string>> usedPortsByTempIndex,
            HashSet<Vector2Int> occupiedCells,
            IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> fixturePool,
            IReadOnlyDictionary<RoomFootprintShape, IReadOnlyList<ImportedRoomRuntimeAsset>> candidatesByShape,
            IReadOnlyList<string> fixtureIds,
            System.Random random,
            int maxAttempts,
            int tempIndex,
            out PlacementRecord record)
        {
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var parent = records[random.Next(records.Count)];
                var parentPorts = parent.Asset.DoorPorts
                    .Where(port => IsNormalConnectablePort(port) && !usedPortsByTempIndex[parent.TempIndex].Contains(port.Id))
                    .OrderBy(_ => random.Next())
                    .ToList();
                if (parentPorts.Count == 0)
                {
                    continue;
                }

                var parentPort = parentPorts[random.Next(parentPorts.Count)];
                var childFallback = fixturePool[ChooseBodyFixtureId(fixturePool, fixtureIds, random)];
                var childAsset = ChooseCandidateForShape(candidatesByShape, childFallback, random);
                var childPorts = childAsset.DoorPorts
                    .Where(port => IsNormalConnectablePort(port) && port.Direction == Opposite(parentPort.Direction))
                    .OrderBy(_ => random.Next())
                    .ToList();
                if (childPorts.Count == 0)
                {
                    continue;
                }

                var childPort = childPorts[random.Next(childPorts.Count)];
                var parentWorldHost = WorldHostCell(parent, parentPort);
                var childWorldHost = parentWorldHost + DirectionOffset(parentPort.Direction);
                var childPrimary = childWorldHost - childPort.HostCell + childAsset.Footprint.PrimaryCell;
                var childFootprint = PlaceFootprint(childAsset.Footprint, childPrimary);
                if (Overlaps(occupiedCells, childFootprint))
                {
                    continue;
                }

                record = new PlacementRecord(tempIndex, childAsset, childPrimary, childFootprint)
                {
                    ParentTempIndex = parent.TempIndex,
                    FromDirection = parentPort.Direction,
                    ToDirection = childPort.Direction,
                    FromPortId = parentPort.Id,
                    ToPortId = childPort.Id,
                    Depth = parent.Depth + 1
                };
                return true;
            }

            record = null;
            return false;
        }

        private static string ChooseBodyFixtureId(
            IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> fixturePool,
            IReadOnlyList<string> fixtureIds,
            System.Random random)
        {
            if (fixturePool == null || fixtureIds == null || fixtureIds.Count == 0)
            {
                return string.Empty;
            }

            var weighted = fixtureIds
                .Where(id => !string.IsNullOrWhiteSpace(id) && fixturePool.ContainsKey(id))
                .Select(id => new
                {
                    Id = id,
                    Weight = BranchPacingPolicy.WeightForShape(RoomFootprintShapeUtility.Classify(fixturePool[id].Footprint))
                })
                .Where(candidate => candidate.Weight > 0)
                .OrderBy(candidate => candidate.Id)
                .ToArray();
            if (weighted.Length == 0)
            {
                return fixtureIds[random.Next(fixtureIds.Count)];
            }

            var roll = random.Next(weighted.Sum(candidate => candidate.Weight));
            foreach (var candidate in weighted)
            {
                roll -= candidate.Weight;
                if (roll < 0)
                {
                    return candidate.Id;
                }
            }

            return weighted[weighted.Length - 1].Id;
        }

        private static bool TryPlaceEndpointRecord(
            IReadOnlyList<PlacementRecord> records,
            IReadOnlyDictionary<int, HashSet<string>> usedPortsByTempIndex,
            HashSet<Vector2Int> occupiedCells,
            IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> fixturePool,
            IReadOnlyDictionary<RoomFootprintShape, IReadOnlyList<ImportedRoomRuntimeAsset>> candidatesByShape,
            IReadOnlyList<string> fixtureIds,
            System.Random random,
            int maxAttempts,
            int tempIndex,
            bool requireSingleRoom,
            ISet<int> excludedParentTempIndices,
            out PlacementRecord record,
            bool exactFixtureIds = false)
        {
            var candidateAssets = (exactFixtureIds
                    ? (fixtureIds ?? Array.Empty<string>())
                        .Select(id => fixturePool != null && fixturePool.TryGetValue(id, out var asset) ? asset : null)
                        .Where(asset => asset != null)
                        .OrderBy(_ => random.Next())
                        .ToList()
                    : CandidateAssetsForFixtureIds(fixturePool, candidatesByShape, fixtureIds, random))
                .Where(asset => !requireSingleRoom || RoomFootprintShapeUtility.Classify(asset.Footprint) == RoomFootprintShape.Single1x1)
                .ToList();
            if (candidateAssets.Count == 0)
            {
                record = null;
                return false;
            }

            var checkedPlacements = 0;
            var parents = records
                .Where(parent => excludedParentTempIndices == null || !excludedParentTempIndices.Contains(parent.TempIndex))
                .OrderByDescending(parent => parent.Depth)
                .ThenBy(_ => random.Next())
                .ToList();

            foreach (var parent in parents)
            {
                if (!usedPortsByTempIndex.TryGetValue(parent.TempIndex, out var usedParentPorts))
                {
                    continue;
                }

                var parentPorts = parent.Asset.DoorPorts
                    .Where(port => IsNormalConnectablePort(port) && !usedParentPorts.Contains(port.Id))
                    .OrderBy(_ => random.Next())
                    .ToList();
                foreach (var parentPort in parentPorts)
                {
                    foreach (var childAsset in candidateAssets.OrderBy(_ => random.Next()))
                    {
                        var childPorts = childAsset.DoorPorts
                            .Where(port => IsNormalConnectablePort(port) && port.Direction == Opposite(parentPort.Direction))
                            .OrderBy(_ => random.Next())
                            .ToList();
                        foreach (var childPort in childPorts)
                        {
                            checkedPlacements++;
                            if (checkedPlacements > Mathf.Max(1, maxAttempts) * 16)
                            {
                                record = null;
                                return false;
                            }

                            var parentWorldHost = WorldHostCell(parent, parentPort);
                            var childWorldHost = parentWorldHost + DirectionOffset(parentPort.Direction);
                            var childPrimary = childWorldHost - childPort.HostCell + childAsset.Footprint.PrimaryCell;
                            var childFootprint = PlaceFootprint(childAsset.Footprint, childPrimary);
                            if (Overlaps(occupiedCells, childFootprint) ||
                                !IsEndpointCandidate(childAsset, childPrimary, childFootprint, records))
                            {
                                continue;
                            }

                            record = new PlacementRecord(tempIndex, childAsset, childPrimary, childFootprint)
                            {
                                ParentTempIndex = parent.TempIndex,
                                FromDirection = parentPort.Direction,
                                ToDirection = childPort.Direction,
                                FromPortId = parentPort.Id,
                                ToPortId = childPort.Id,
                                Depth = parent.Depth + 1
                            };
                            return true;
                        }
                    }
                }
            }

            record = null;
            return false;
        }

        private static List<ImportedRoomRuntimeAsset> CandidateAssetsForFixtureIds(
            IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> fixturePool,
            IReadOnlyDictionary<RoomFootprintShape, IReadOnlyList<ImportedRoomRuntimeAsset>> candidatesByShape,
            IReadOnlyList<string> fixtureIds,
            System.Random random)
        {
            var assetsById = new Dictionary<string, ImportedRoomRuntimeAsset>();
            foreach (var fixtureId in fixtureIds ?? Array.Empty<string>())
            {
                if (fixturePool == null || !fixturePool.TryGetValue(fixtureId, out var fallback) || fallback == null)
                {
                    continue;
                }

                var shape = RoomFootprintShapeUtility.Classify(fallback.Footprint);
                if (shape != RoomFootprintShape.Unsupported &&
                    candidatesByShape != null &&
                    candidatesByShape.TryGetValue(shape, out var candidates) &&
                    candidates.Count > 0)
                {
                    foreach (var candidate in candidates.Where(candidate => candidate != null))
                    {
                        assetsById[candidate.Id] = candidate;
                    }
                }
                else
                {
                    assetsById[fallback.Id] = fallback;
                }
            }

            return assetsById.Values.OrderBy(_ => random.Next()).ToList();
        }

        private static IReadOnlyDictionary<RoomFootprintShape, IReadOnlyList<ImportedRoomRuntimeAsset>> BuildCandidatesByShape(IEnumerable<ImportedRoomRuntimeAsset> assets)
        {
            return (assets ?? Enumerable.Empty<ImportedRoomRuntimeAsset>())
                .Where(asset => asset != null && RoomFootprintShapeUtility.IsSupported(asset.Footprint))
                .GroupBy(asset => RoomFootprintShapeUtility.Classify(asset.Footprint))
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<ImportedRoomRuntimeAsset>)group.OrderBy(asset => asset.Id).ToArray());
        }

        public static void ConnectAdjacentCompatiblePorts(BranchFloorGraph graph, IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> roomPool)
        {
            if (graph == null || roomPool == null)
            {
                return;
            }

            var roomRecords = graph.Rooms
                .Where(room => room?.Footprint != null &&
                               !string.IsNullOrWhiteSpace(room.RuntimeRoomAssetId) &&
                               roomPool.ContainsKey(room.RuntimeRoomAssetId))
                .Select(room => new AutoConnectRoomRecord(room, roomPool[room.RuntimeRoomAssetId]))
                .ToArray();

            foreach (var fromRoom in roomRecords)
            {
                foreach (var fromPort in fromRoom.Asset.DoorPorts.Where(IsNormalConnectablePort))
                {
                    var requiredToDirection = Opposite(fromPort.Direction);
                    if (string.IsNullOrWhiteSpace(requiredToDirection))
                    {
                        continue;
                    }

                    var requiredToHostCell = WorldHostCell(fromRoom, fromPort) + DirectionOffset(fromPort.Direction);
                    foreach (var toRoom in roomRecords.Where(room => room.Room.Id != fromRoom.Room.Id))
                    {
                        foreach (var toPort in toRoom.Asset.DoorPorts.Where(port =>
                                     IsNormalConnectablePort(port) &&
                                     port.Direction == requiredToDirection &&
                                     WorldHostCell(toRoom, port) == requiredToHostCell))
                        {
                            if (graph.HasConnectionByPortPair(fromRoom.Room.Id, fromPort.Id, toRoom.Room.Id, toPort.Id))
                            {
                                continue;
                            }

                            graph.AddBidirectionalConnection(
                                fromRoom.Room.Id,
                                toRoom.Room.Id,
                                fromPort.Direction,
                                toPort.Direction,
                                fromPort.Id,
                                toPort.Id);
                        }
                    }
                }
            }
        }

        public static bool IsSingleRoomFootprint(BranchRoomState room)
        {
            return room != null && RoomFootprintShapeUtility.Classify(room.Footprint) == RoomFootprintShape.Single1x1;
        }

        public static bool ValidateSpecialRoomTopology(BranchFloorGraph graph, out string error)
        {
            if (graph == null)
            {
                error = "Graph is null.";
                return false;
            }

            var bossRooms = graph.Rooms.Where(room => room.Role == BranchRoomRole.Boss).ToArray();
            if (bossRooms.Length > 1)
            {
                error = "More than one boss room exists.";
                return false;
            }

            if (bossRooms.Length == 1)
            {
                var boss = bossRooms[0];
                var bossConnections = graph.ConnectionsFrom(boss.Id);
                var uniqueNeighbors = bossConnections.Select(connection => connection.ToRoomId).Distinct().ToArray();
                if (uniqueNeighbors.Length != 1 || bossConnections.Count != 1)
                {
                    error = $"Boss room '{boss.Id}' must have exactly one entrance, but has {uniqueNeighbors.Length} neighbor(s) and {bossConnections.Count} outgoing connection(s).";
                    return false;
                }
            }

            var nonSingleSecret = graph.Rooms.FirstOrDefault(room => room.Role == BranchRoomRole.Secret && !IsSingleRoomFootprint(room));
            if (nonSingleSecret != null)
            {
                error = $"Secret room '{nonSingleSecret.Id}' must use a 1x1 footprint.";
                return false;
            }

            var corruptedRooms = graph.Rooms.Where(room => room.Role == BranchRoomRole.CorruptedChest).ToArray();
            if (corruptedRooms.Length > 1)
            {
                error = "More than one corrupted chest room exists.";
                return false;
            }

            if (corruptedRooms.Length == 1)
            {
                var corrupted = corruptedRooms[0];
                var corruptedNeighbors = graph.ConnectionsFrom(corrupted.Id)
                    .Select(connection => connection.ToRoomId)
                    .Distinct()
                    .ToArray();
                if (corruptedNeighbors.Length != 1)
                {
                    error = $"Corrupted chest room '{corrupted.Id}' must be a terminal leaf.";
                    return false;
                }

                if (!graph.TryGetRoom(corruptedNeighbors[0], out var parent) ||
                    parent.Role is BranchRoomRole.Boss or BranchRoomRole.Secret or BranchRoomRole.Treasure)
                {
                    error = $"Corrupted chest room '{corrupted.Id}' must not attach to boss, secret, or treasure endpoints.";
                    return false;
                }
            }

            var waveRooms = graph.Rooms.Where(room => room.Role == BranchRoomRole.Wave).ToArray();
            if (waveRooms.Length > 1)
            {
                error = "More than one wave room exists.";
                return false;
            }

            if (waveRooms.Length == 1)
            {
                var wave = waveRooms[0];
                var waveNeighbors = graph.ConnectionsFrom(wave.Id)
                    .Select(connection => connection.ToRoomId)
                    .Distinct()
                    .ToArray();
                if (waveNeighbors.Length != 1)
                {
                    error = $"Wave room '{wave.Id}' must be a terminal leaf.";
                    return false;
                }

                if (!graph.TryGetRoom(waveNeighbors[0], out var parent) ||
                    parent.Role is BranchRoomRole.Boss or BranchRoomRole.Secret or BranchRoomRole.Treasure or BranchRoomRole.CorruptedChest)
                {
                    error = $"Wave room '{wave.Id}' must not attach to boss, secret, treasure, or corrupted endpoints.";
                    return false;
                }
            }

            var specialRooms = graph.Rooms.Where(room => room.Role == BranchRoomRole.SpecialEncounter).ToArray();
            if (specialRooms.Length > 1)
            {
                error = "More than one special encounter room exists.";
                return false;
            }

            if (specialRooms.Length == 1)
            {
                var special = specialRooms[0];
                var specialNeighbors = graph.ConnectionsFrom(special.Id)
                    .Select(connection => connection.ToRoomId)
                    .Distinct()
                    .ToArray();
                if (specialNeighbors.Length != 1)
                {
                    error = $"Special encounter room '{special.Id}' must be a terminal leaf.";
                    return false;
                }

                if (!graph.TryGetRoom(specialNeighbors[0], out var parent) ||
                    parent.Role is BranchRoomRole.Boss or BranchRoomRole.Secret or BranchRoomRole.Treasure or BranchRoomRole.CorruptedChest or BranchRoomRole.Wave)
                {
                    error = $"Special encounter room '{special.Id}' must not attach to boss, secret, treasure, corrupted, or wave endpoints.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool IsEndpointCandidate(
            ImportedRoomRuntimeAsset asset,
            Vector2Int primaryCell,
            RoomInstanceFootprint footprint,
            IReadOnlyList<PlacementRecord> placedRecords)
        {
            if (asset == null || footprint == null || placedRecords == null)
            {
                return false;
            }

            var adjacentRooms = new HashSet<int>();
            var adjacentFaceCount = 0;
            foreach (var placed in placedRecords)
            {
                foreach (var candidateCell in footprint.OccupiedCells)
                {
                    foreach (var placedCell in placed.Footprint.OccupiedCells)
                    {
                        if (Mathf.Abs(candidateCell.x - placedCell.x) + Mathf.Abs(candidateCell.y - placedCell.y) != 1)
                        {
                            continue;
                        }

                        adjacentRooms.Add(placed.TempIndex);
                        adjacentFaceCount++;
                    }
                }
            }

            return adjacentRooms.Count >= 1 &&
                   adjacentFaceCount >= 1 &&
                   CountCompatibleAdjacentPortPairs(asset, primaryCell, placedRecords) == 1;
        }

        private static int CountCompatibleAdjacentPortPairs(
            ImportedRoomRuntimeAsset asset,
            Vector2Int primaryCell,
            IReadOnlyList<PlacementRecord> placedRecords)
        {
            if (asset == null || placedRecords == null)
            {
                return 0;
            }

            var count = 0;
            var offset = primaryCell - asset.Footprint.PrimaryCell;
            foreach (var fromPort in asset.DoorPorts.Where(IsNormalConnectablePort))
            {
                var requiredDirection = Opposite(fromPort.Direction);
                var requiredHostCell = fromPort.HostCell + offset + DirectionOffset(fromPort.Direction);
                foreach (var placed in placedRecords)
                {
                    count += placed.Asset.DoorPorts.Count(toPort =>
                        IsNormalConnectablePort(toPort) &&
                        toPort.Direction == requiredDirection &&
                        WorldHostCell(placed, toPort) == requiredHostCell);
                }
            }

            return count;
        }

        private static ImportedRoomRuntimeAsset ChooseCandidateForShape(
            IReadOnlyDictionary<RoomFootprintShape, IReadOnlyList<ImportedRoomRuntimeAsset>> candidatesByShape,
            ImportedRoomRuntimeAsset fallback,
            System.Random random)
        {
            var shape = RoomFootprintShapeUtility.Classify(fallback?.Footprint);
            if (shape == RoomFootprintShape.Unsupported ||
                candidatesByShape == null ||
                !candidatesByShape.TryGetValue(shape, out var candidates) ||
                candidates.Count == 0)
            {
                return fallback;
            }

            return candidates.Count == 1 ? candidates[0] : candidates[random.Next(candidates.Count)];
        }

        private static HashSet<int> SelectRewardRoomTempIndices(
            IReadOnlyList<PlacementRecord> records,
            int bossTempIndex,
            IEnumerable<int> excludedTempIndices,
            string branchId,
            int seed)
        {
            var selected = new HashSet<int>();
            if (records == null || records.Count == 0 || bossTempIndex < 0)
            {
                return selected;
            }

            var excluded = new HashSet<int>((excludedTempIndices ?? Array.Empty<int>()).Where(index => index >= 0))
            {
                0,
                bossTempIndex
            };
            var recordsByTempIndex = records.ToDictionary(record => record.TempIndex);
            var path = new List<PlacementRecord>();
            var cursor = bossTempIndex;
            while (recordsByTempIndex.TryGetValue(cursor, out var record))
            {
                cursor = record.ParentTempIndex;
                if (cursor <= 0)
                {
                    break;
                }

                if (recordsByTempIndex.TryGetValue(cursor, out var pathRecord) &&
                    !excluded.Contains(pathRecord.TempIndex))
                {
                    path.Add(pathRecord);
                }
            }

            if (path.Count == 0)
            {
                path = records
                    .Where(record => !excluded.Contains(record.TempIndex))
                    .ToList();
            }

            var desiredCount = BranchPacingPolicy.RewardRoomCount(branchId, seed);
            foreach (var candidate in path
                         .OrderBy(record => BranchPacingPolicy.RewardRoomShapePreference(RoomFootprintShapeUtility.Classify(record.Footprint)))
                         .ThenBy(record => StableBranchHash($"{branchId}|{seed}|m134_reward_room|{record.TempIndex}"))
                         .ThenByDescending(record => record.Depth)
                         .Take(desiredCount))
            {
                selected.Add(candidate.TempIndex);
            }

            return selected;
        }

        private static Dictionary<int, BranchRoomId> AssignRoomIds(IReadOnlyList<PlacementRecord> records, int bossTempIndex)
        {
            var idByTempIndex = new Dictionary<int, BranchRoomId> { [0] = BranchRoomId.Origin };
            var roomNumber = 1;
            foreach (var record in records.Where(record => record.TempIndex != 0).OrderBy(record => record.TempIndex))
            {
                if (record.TempIndex == bossTempIndex)
                {
                    idByTempIndex[record.TempIndex] = new BranchRoomId("boss_01");
                    continue;
                }

                idByTempIndex[record.TempIndex] = new BranchRoomId($"room_{roomNumber:00}");
                roomNumber++;
            }

            return idByTempIndex;
        }

        private static int SelectBossLeaf(IReadOnlyList<PlacementRecord> records)
        {
            var parentIds = records.Where(record => record.TempIndex != 0)
                .Select(record => record.ParentTempIndex)
                .ToHashSet();
            return records
                .Where(record => record.TempIndex != 0 && !parentIds.Contains(record.TempIndex))
                .OrderByDescending(record => record.Depth)
                .ThenBy(record => $"room_{record.TempIndex:00}")
                .First()
                .TempIndex;
        }

        private static int SelectTreasureLeaf(IReadOnlyList<PlacementRecord> records, int bossTempIndex)
        {
            var parentIds = records.Where(record => record.TempIndex != 0)
                .Select(record => record.ParentTempIndex)
                .ToHashSet();
            var leaves = records
                .Where(record => record.TempIndex != 0 && record.TempIndex != bossTempIndex && !parentIds.Contains(record.TempIndex))
                .OrderByDescending(record => record.Depth)
                .ThenBy(record => $"room_{record.TempIndex:00}")
                .ToList();
            if (leaves.Count > 0)
            {
                return leaves[0].TempIndex;
            }

            var fallback = records
                .Where(record => record.TempIndex != 0 && record.TempIndex != bossTempIndex)
                .OrderByDescending(record => record.Depth)
                .ThenBy(record => $"room_{record.TempIndex:00}")
                .ToList();
            return fallback.Count > 0 ? fallback[0].TempIndex : -1;
        }

        private static void ApplyBossKeyLock(BranchFloorGraph graph)
        {
            var bossRoom = graph.Rooms.FirstOrDefault(room => room.Role == BranchRoomRole.Boss);
            if (bossRoom == null)
            {
                return;
            }

            foreach (var connection in graph.Connections.Where(connection =>
                         connection.FromRoomId == bossRoom.Id || connection.ToRoomId == bossRoom.Id))
            {
                connection.SetLockKind(BranchConnectionLockKind.BossKey);
            }
        }

        private static int DistanceFromOrigin(BranchFloorGraph graph, BranchRoomId target)
        {
            var distances = new Dictionary<BranchRoomId, int>();
            var queue = new Queue<BranchRoomId>();
            distances[BranchRoomId.Origin] = 0;
            queue.Enqueue(BranchRoomId.Origin);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target)
                {
                    return distances[current];
                }

                foreach (var connection in graph.ConnectionsFrom(current))
                {
                    if (distances.ContainsKey(connection.ToRoomId))
                    {
                        continue;
                    }

                    distances[connection.ToRoomId] = distances[current] + 1;
                    queue.Enqueue(connection.ToRoomId);
                }
            }

            return 0;
        }

        private static BranchRoomState CreateRoom(BranchRoomId id, Vector2Int primaryCell, ImportedRoomRuntimeAsset asset, BranchRoomRole role)
        {
            return new BranchRoomState(
                id,
                primaryCell,
                new BranchRoomInstanceId(id.Value),
                asset.Id,
                PlaceFootprint(asset.Footprint, primaryCell),
                role);
        }

        private static RoomInstanceFootprint PlaceFootprint(RoomInstanceFootprint source, Vector2Int primaryCell)
        {
            if (source == null)
            {
                return null;
            }

            var offset = primaryCell - source.PrimaryCell;
            var placedCells = source.OccupiedCells.Select(cell => cell + offset).ToArray();
            return new RoomInstanceFootprint(primaryCell, placedCells, source.ChunkBasisTiles);
        }

        private static ImportedRoomRuntimeAsset RequireRoom(IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> roomPool, string id)
        {
            if (roomPool != null && roomPool.TryGetValue(id, out var asset) && asset != null)
            {
                return asset;
            }

            throw new KeyNotFoundException($"Macro fixture branch requires room asset '{id}'.");
        }

        private static ImportedRoomRuntimeAsset RequireRoomByIdOrShape(
            IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> roomPool,
            string id,
            RoomFootprintShape fallbackShape)
        {
            if (roomPool != null && roomPool.TryGetValue(id, out var asset) && asset != null)
            {
                return asset;
            }

            asset = roomPool?.Values
                .Where(candidate => candidate != null && RoomFootprintShapeUtility.Classify(candidate.Footprint) == fallbackShape)
                .OrderBy(candidate => candidate.Id)
                .FirstOrDefault();
            if (asset != null)
            {
                return asset;
            }

            throw new KeyNotFoundException($"Macro fixture branch requires room asset '{id}' or a {fallbackShape} room.");
        }

        private static IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> ActivePoolFor(BranchSessionContent content, string biomeId)
        {
            if (content == null)
            {
                return null;
            }

            var pool = content.ResolveRoomPoolForBiome(biomeId, out var usedFallback);
            if (usedFallback)
            {
                Debug.LogWarning($"Biome room pool '{RoomBiomeIds.Normalize(biomeId)}' is incomplete; falling back to the global macro room pool.");
            }

            return pool;
        }

        private static Vector2Int WorldHostCell(PlacementRecord record, RoomDoorPort port)
        {
            var offset = record.PrimaryCell - record.Asset.Footprint.PrimaryCell;
            return port.HostCell + offset;
        }

        private static Vector2Int WorldHostCell(AutoConnectRoomRecord record, RoomDoorPort port)
        {
            var offset = record.Room.Footprint.PrimaryCell - record.Asset.Footprint.PrimaryCell;
            return port.HostCell + offset;
        }

        private static void RegisterCells(HashSet<Vector2Int> occupiedCells, RoomInstanceFootprint footprint)
        {
            foreach (var cell in footprint.OccupiedCells)
            {
                occupiedCells.Add(cell);
            }
        }

        private static bool Overlaps(HashSet<Vector2Int> occupiedCells, RoomInstanceFootprint footprint)
        {
            return footprint.OccupiedCells.Any(occupiedCells.Contains);
        }

        private static Vector2Int DirectionOffset(string direction)
        {
            return direction switch
            {
                "north" => new Vector2Int(0, -1),
                "south" => new Vector2Int(0, 1),
                "east" => new Vector2Int(1, 0),
                "west" => new Vector2Int(-1, 0),
                _ => Vector2Int.zero
            };
        }

        private static string Opposite(string direction)
        {
            return direction switch
            {
                "north" => "south",
                "south" => "north",
                "east" => "west",
                "west" => "east",
                _ => string.Empty
            };
        }

        private static bool IsNormalConnectablePort(RoomDoorPort port)
        {
            return string.Equals(port?.Kind, "available", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(port?.Kind, "door", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(port?.Kind, "normal", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class AutoConnectRoomRecord
        {
            public AutoConnectRoomRecord(BranchRoomState room, ImportedRoomRuntimeAsset asset)
            {
                Room = room;
                Asset = asset;
            }

            public BranchRoomState Room { get; }

            public ImportedRoomRuntimeAsset Asset { get; }
        }

        private sealed class PlacementRecord
        {
            public PlacementRecord(int tempIndex, ImportedRoomRuntimeAsset asset, Vector2Int primaryCell, RoomInstanceFootprint footprint)
            {
                TempIndex = tempIndex;
                Asset = asset;
                PrimaryCell = primaryCell;
                Footprint = footprint;
            }

            public int TempIndex { get; }

            public ImportedRoomRuntimeAsset Asset { get; }

            public Vector2Int PrimaryCell { get; }

            public RoomInstanceFootprint Footprint { get; }

            public int ParentTempIndex { get; set; } = -1;

            public int Depth { get; set; }

            public string FromDirection { get; set; } = string.Empty;

            public string ToDirection { get; set; } = string.Empty;

            public string FromPortId { get; set; } = string.Empty;

            public string ToPortId { get; set; } = string.Empty;
        }

        private static int StableBranchHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7fffffff);
            }
        }
    }

    public static class BranchPacingPolicy
    {
        public const int RewardRoomGuaranteedCount = 1;
        public const int RewardRoomSecondRollPercent = 50;
        public const int Single1x1Weight = 30;
        public const int Wide2x1Weight = 25;
        public const int Tall1x2Weight = 20;
        public const int Block2x2Weight = 15;
        public const int L3CellWeight = 10;

        public static int RewardRoomCount(string branchId, int seed)
        {
            return RewardRoomGuaranteedCount +
                   (StableBranchHash($"{branchId}|{seed}|m134_reward_room_second") % 100 < RewardRoomSecondRollPercent ? 1 : 0);
        }

        public static int WeightForShape(RoomFootprintShape shape)
        {
            return shape switch
            {
                RoomFootprintShape.Single1x1 => Single1x1Weight,
                RoomFootprintShape.Wide2x1 => Wide2x1Weight,
                RoomFootprintShape.Tall1x2 => Tall1x2Weight,
                RoomFootprintShape.Block2x2 => Block2x2Weight,
                RoomFootprintShape.L3Cell => L3CellWeight,
                _ => 1
            };
        }

        public static int RewardRoomShapePreference(RoomFootprintShape shape)
        {
            return shape switch
            {
                RoomFootprintShape.Single1x1 => 0,
                RoomFootprintShape.Wide2x1 => 1,
                RoomFootprintShape.Tall1x2 => 2,
                RoomFootprintShape.Block2x2 => 3,
                RoomFootprintShape.L3Cell => 4,
                _ => 5
            };
        }

        private static int StableBranchHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
