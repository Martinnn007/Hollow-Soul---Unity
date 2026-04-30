using System.Collections.Generic;
using System.Linq;

namespace Hollow.Rewards
{
    public static class CoinDenominationResolver
    {
        public const int CopperValue = 1;
        public const int SilverValue = 5;
        public const int GoldValue = 10;
        public const int DefaultMaxPhysicalCoins = 12;

        public static int ValueFor(CoinDenomination denomination)
        {
            return denomination switch
            {
                CoinDenomination.Gold => GoldValue,
                CoinDenomination.Silver => SilverValue,
                _ => CopperValue
            };
        }

        public static string RewardIdFor(CoinDenomination denomination)
        {
            return denomination switch
            {
                CoinDenomination.Gold => "gold_coin",
                CoinDenomination.Silver => "silver_coin",
                _ => "copper_coin"
            };
        }

        public static string DisplayNameFor(CoinDenomination denomination)
        {
            return denomination switch
            {
                CoinDenomination.Gold => "Gold Coin",
                CoinDenomination.Silver => "Silver Coin",
                _ => "Copper Coin"
            };
        }

        public static IReadOnlyList<CoinDenomination> ResolveExactValue(int totalValue, int seed, int maxPhysicalCoins = DefaultMaxPhysicalCoins)
        {
            if (totalValue <= 0)
            {
                return System.Array.Empty<CoinDenomination>();
            }

            maxPhysicalCoins = System.Math.Max(1, maxPhysicalCoins);
            var result = new List<CoinDenomination>();
            var remaining = totalValue;
            var step = 0;
            while (remaining > 0)
            {
                var denomination = ChooseDenomination(remaining, seed, step, maxPhysicalCoins - result.Count);
                result.Add(denomination);
                remaining -= ValueFor(denomination);
                step++;
            }

            if (result.Count <= maxPhysicalCoins)
            {
                return result;
            }

            return CompactGreedy(totalValue).Take(maxPhysicalCoins).ToArray();
        }

        private static CoinDenomination ChooseDenomination(int remaining, int seed, int step, int remainingSlots)
        {
            if (remainingSlots <= 1)
            {
                return remaining >= GoldValue ? CoinDenomination.Gold : remaining >= SilverValue ? CoinDenomination.Silver : CoinDenomination.Copper;
            }

            var hash = StableHash($"{seed}|{remaining}|{step}");
            if (remaining >= GoldValue &&
                MinimumCoinCount(remaining - GoldValue) <= remainingSlots - 1 &&
                (hash % 100 < 48 || MinimumCoinCount(remaining) >= remainingSlots))
            {
                return CoinDenomination.Gold;
            }

            if (remaining >= SilverValue &&
                MinimumCoinCount(remaining - SilverValue) <= remainingSlots - 1 &&
                hash % 100 < 78)
            {
                return CoinDenomination.Silver;
            }

            return CoinDenomination.Copper;
        }

        private static IReadOnlyList<CoinDenomination> CompactGreedy(int totalValue)
        {
            var result = new List<CoinDenomination>();
            var remaining = totalValue;
            while (remaining >= GoldValue)
            {
                result.Add(CoinDenomination.Gold);
                remaining -= GoldValue;
            }

            while (remaining >= SilverValue)
            {
                result.Add(CoinDenomination.Silver);
                remaining -= SilverValue;
            }

            while (remaining > 0)
            {
                result.Add(CoinDenomination.Copper);
                remaining--;
            }

            return result;
        }

        private static int MinimumCoinCount(int value)
        {
            if (value <= 0)
            {
                return 0;
            }

            var gold = value / GoldValue;
            var remainder = value % GoldValue;
            return gold + remainder / SilverValue + remainder % SilverValue;
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
