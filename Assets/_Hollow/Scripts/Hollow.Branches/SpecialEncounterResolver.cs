using System;
using Hollow.Rewards;

namespace Hollow.Branches
{
    public static class SpecialEncounterResolver
    {
        public const int SpecialEncounterRollPercent = 15;
        public const int SoulEaterSoulPrice = 10;
        public const float EscapistTimerSeconds = 20f;
        public const string SoulEaterRoomAssetId = "special_soul_eater_single_1x1";
        public const string EscapistRoomAssetId = "special_escapist_single_1x1";
        public const string EscapistSpawnKind = "spawnEnemyEscapist";

        public static bool ShouldRollSpecialEncounterLeaf(string branchId, int seed)
        {
            return StableHash($"{branchId}|{seed}|m133_special_encounter") % 100 < SpecialEncounterRollPercent;
        }

        public static SpecialEncounterKind ResolveKind(string branchId, int seed)
        {
            return StableHash($"{branchId}|{seed}|m133_special_encounter_kind") % 2 == 0
                ? SpecialEncounterKind.SoulEater
                : SpecialEncounterKind.Escapist;
        }

        public static SpecialEncounterKind KindForRoomAssetId(string roomAssetId)
        {
            if (string.Equals(roomAssetId, SoulEaterRoomAssetId, StringComparison.Ordinal))
            {
                return SpecialEncounterKind.SoulEater;
            }

            return string.Equals(roomAssetId, EscapistRoomAssetId, StringComparison.Ordinal)
                ? SpecialEncounterKind.Escapist
                : SpecialEncounterKind.None;
        }

        public static string DisplayNameFor(SpecialEncounterKind kind)
        {
            return kind switch
            {
                SpecialEncounterKind.SoulEater => "Soul Eater",
                SpecialEncounterKind.Escapist => "Escapist",
                _ => "Special Encounter"
            };
        }

        public static string DisplayNameForAssetId(string roomAssetId)
        {
            return DisplayNameFor(KindForRoomAssetId(roomAssetId));
        }

        public static bool IsEscapistSpawnKind(string spawnKind)
        {
            return string.Equals(spawnKind, EscapistSpawnKind, StringComparison.Ordinal);
        }

        public static RewardGrant ResolveSoulEaterOffer(string branchId, int seed, string roomId)
        {
            var grant = ChestRewardResolver.ResolveCuratedRareReward(
                branchId,
                seed,
                roomId,
                "m133_soul_eater_offer");
            return new RewardGrant(
                SoulEaterRewardContextId(roomId),
                grant.RewardId,
                grant.DisplayName,
                grant.RewardKind,
                grant.Souls,
                grant.Coins,
                grant.Effects,
                grant.MaxStacks);
        }

        public static string SoulEaterRewardContextId(string roomId)
        {
            return $"{roomId}:soul_eater_offer";
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= (uint)character;
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
