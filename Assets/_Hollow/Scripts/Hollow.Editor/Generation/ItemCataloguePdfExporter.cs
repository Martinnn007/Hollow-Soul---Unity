using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Data.Definitions;
using Hollow.Rewards;

namespace Hollow.Editor.Generation
{
    public static class ItemCataloguePdfExporter
    {
        private const int MaxLineLength = 96;
        private const int LinesPerPage = 47;

        public static void WritePdf(
            string path,
            RewardPoolDefinition standardPool,
            RewardPoolDefinition treasurePool,
            RewardPoolDefinition bossPool,
            RewardPoolDefinition weaponPool,
            WeaponCatalogDefinition weaponCatalog,
            ArmorCatalogDefinition armorCatalog,
            UsableItemCatalogDefinition usableCatalog)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "output/pdf");
            var lines = BuildLines(standardPool, treasurePool, bossPool, weaponPool, weaponCatalog, armorCatalog, usableCatalog).ToList();
            var pages = Paginate(lines).ToList();
            File.WriteAllBytes(path, BuildPdfBytes(pages));
        }

        private static IEnumerable<string> BuildLines(
            RewardPoolDefinition standardPool,
            RewardPoolDefinition treasurePool,
            RewardPoolDefinition bossPool,
            RewardPoolDefinition weaponPool,
            WeaponCatalogDefinition weaponCatalog,
            ArmorCatalogDefinition armorCatalog,
            UsableItemCatalogDefinition usableCatalog)
        {
            var rewardRecords = RewardRecords(standardPool, treasurePool, bossPool, weaponPool).ToList();
            var weaponRecords = weaponCatalog?.Weapons?.Where(weapon => weapon != null).ToArray() ?? Array.Empty<WeaponDefinition>();
            var armorRecords = armorCatalog?.Armors?.Where(armor => armor != null).ToArray() ?? Array.Empty<ArmorDefinition>();
            var usableRecords = usableCatalog?.Items?.Where(item => item != null).ToArray() ?? Array.Empty<UsableItemDefinition>();
            var total = rewardRecords.Count + weaponRecords.Length + armorRecords.Length + usableRecords.Length + 7;

            yield return "Hollow M54 Item Catalogue";
            yield return $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
            yield return $"Total visible pickups/catalog entries: {total}";
            yield return $"Rewards: {rewardRecords.Count}; Weapons: {weaponRecords.Length}; Armor: {armorRecords.Length}; Usables: {usableRecords.Length}; System pickups: 7";
            yield return string.Empty;
            yield return "Spawn / Eligibility Notes";
            yield return "- Standard combat/reward rooms: 2% Golden Chest, 12% Normal Chest, 38% loose coins, 24% HP refill, 24% no reward.";
            yield return "- M54 projectile passives appear only in treasure rooms, boss rewards, and hub shops.";
            yield return "- Pool odds are deterministic equal-pool rolls unless a row states an exact percentage.";
            yield return "- Golden chests keep the M52 rule: card/sustain/economy only, no M54 passive item drops.";
            yield return string.Empty;
            yield return "M54 Projectile Passive Items";
            foreach (var record in rewardRecords.Where(record => ProjectilePassiveResolver.IsM54ProjectilePassive(record.Reward.RewardId)).OrderBy(record => record.Reward.RewardId))
            {
                yield return Row(record.Reward.DisplayName, record.Reward.RewardKind.ToString(), record.Reward.Rarity.ToString(), record.Reward.MaxStacks, EffectText(record.Reward.Effects), record.Source);
            }

            yield return string.Empty;
            yield return "Reward Pools";
            foreach (var record in rewardRecords.OrderBy(record => record.Reward.RewardKind).ThenBy(record => record.Reward.DisplayName))
            {
                yield return Row(record.Reward.DisplayName, record.Reward.RewardKind.ToString(), record.Reward.Rarity.ToString(), record.Reward.MaxStacks, EffectText(record.Reward.Effects), record.Source);
            }

            yield return string.Empty;
            yield return "Weapons";
            foreach (var weapon in weaponRecords.OrderBy(weapon => weapon.Slot).ThenBy(weapon => weapon.DisplayName))
            {
                yield return Row(weapon.DisplayName, weapon.Slot.ToString(), weapon.Category.ToString(), 1, $"Light {weapon.LightAttack.Damage} dmg/{weapon.LightAttack.CooldownSeconds:0.##}s; Heavy {weapon.HeavyAttack.Damage} dmg/{weapon.HeavyAttack.CooldownSeconds:0.##}s", "Starter, weapon pool, boss/shop weapon rolls");
            }

            yield return string.Empty;
            yield return "Armor";
            foreach (var armor in armorRecords.OrderBy(armor => armor.DisplayName))
            {
                yield return Row(armor.DisplayName, "Armor", armor.Rarity.ToString(), 1, ArmorText(armor), "Treasure, boss, shop pools when reward is eligible");
            }

            yield return string.Empty;
            yield return "Active Items / Consumable Cards";
            foreach (var item in usableRecords.OrderBy(item => item.RewardKind).ThenBy(item => item.DisplayName))
            {
                yield return Row(item.DisplayName, item.RewardKind.ToString(), item.Rarity.ToString(), 1, EffectText(item.Effects), "Treasure, boss, shop, selected golden chest card outcomes");
            }

            yield return string.Empty;
            yield return "Chests, Coins, Heals, And Key-Like Pickups";
            yield return Row("Normal Chest", "Chest", "Common", 1, "Interact to open; 75% coins worth 8-14, 25% HP refill", "12% standard-room outcome");
            yield return Row("Golden Chest", "Chest", "Rare", 1, "Interact to open; better coins, HP+coins, or passive/consumable card+coins", "2% standard-room outcome");
            yield return Row("Copper Coin", "Coin", "Common", 12, "Worth 1 coin", "Loose coins, chests, barrels");
            yield return Row("Silver Coin", "Coin", "Rare", 12, "Worth 5 coins", "Loose coins, chests, barrels");
            yield return Row("Gold Coin", "Coin", "Very Rare", 12, "Worth 10 coins", "Loose coins, chests, barrels");
            yield return Row("HP Refill", "Heal", "Common", 1, "Restores up to max HP", "24% standard-room outcome, chest contents, shop heal");
            yield return Row("Boss Key", "Key-Like", "Boss", 1, "Unlocks the boss path in a branch", "Branch feature progression");
        }

        private static IEnumerable<(RewardDefinition Reward, string Source)> RewardRecords(params RewardPoolDefinition[] pools)
        {
            var records = new Dictionary<string, (RewardDefinition Reward, SortedSet<string> Sources)>();
            foreach (var pool in pools.Where(pool => pool != null))
            {
                foreach (var reward in pool.Rewards.Where(reward => reward != null))
                {
                    if (!records.TryGetValue(reward.RewardId, out var record))
                    {
                        record = (reward, new SortedSet<string>());
                        records[reward.RewardId] = record;
                    }

                    record.Sources.Add(SourceName(pool));
                }
            }

            return records.Values.Select(record => (record.Reward, string.Join(", ", record.Sources)));
        }

        private static string SourceName(RewardPoolDefinition pool)
        {
            if (pool == null)
            {
                return "Unknown";
            }

            return pool.PoolId.Contains("standard") ? "Standard rooms" :
                pool.PoolId.Contains("treasure") ? "Treasure rooms / shop item rolls" :
                pool.PoolId.Contains("boss") ? "Boss rewards" :
                pool.PoolId.Contains("weapon") ? "Weapon rolls / shops" :
                pool.PoolId;
        }

        private static string Row(string name, string category, string rarity, int maxStacks, string effects, string source)
        {
            return $"- {name} | {category} | {rarity} | stack {Math.Max(1, maxStacks)} | {effects} | {source}";
        }

        private static string EffectText(IEnumerable<RewardEffect> effects)
        {
            var lines = (effects ?? Array.Empty<RewardEffect>()).Where(effect => !effect.IsEmpty).Select(effect => effect.Kind switch
            {
                RewardEffectKind.MaxHealthBonus => $"+{effect.IntValue} max HP",
                RewardEffectKind.Heal => $"Heal {effect.IntValue}",
                RewardEffectKind.MoveSpeedBonus => $"+{effect.FloatValue:0.##} speed",
                RewardEffectKind.ProjectileDamageBonus => $"+{effect.IntValue} projectile damage",
                RewardEffectKind.MeleeDamageBonus => $"+{effect.IntValue} melee damage",
                RewardEffectKind.RangedDamageBonus => $"+{effect.IntValue} ranged damage",
                RewardEffectKind.AttackCooldownMultiplier => $"Cooldown x{effect.FloatValue:0.##}",
                RewardEffectKind.ProjectilePatternRank => effect.IntValue == 4 ? "Quad-shot pattern" : effect.IntValue == 3 ? "Triple-shot pattern" : "Double-barrel pattern",
                RewardEffectKind.RangedDamageMultiplier => $"Ranged projectile damage x{effect.FloatValue:0.##}",
                RewardEffectKind.RangedLightFireRateBonusPerSecond => $"+{effect.FloatValue:0.##}/s ranged light fire rate",
                RewardEffectKind.DefenseBonus => $"+{effect.IntValue} defense",
                RewardEffectKind.MaxStaminaBonus => $"+{effect.FloatValue:0} stamina",
                RewardEffectKind.StaminaRegenBonus => $"+{effect.FloatValue:0.#} stamina regen",
                RewardEffectKind.MeleeRangeBonusMeters => $"+{effect.FloatValue:0.##}m melee range",
                RewardEffectKind.RangedRangeBonusMeters => $"+{effect.FloatValue:0.##}m ranged range",
                _ => effect.Kind.ToString()
            }).ToArray();
            return lines.Length == 0 ? "See runtime/catalog behavior" : string.Join(", ", lines);
        }

        private static string ArmorText(ArmorDefinition armor)
        {
            var modifier = armor.StatModifier;
            var parts = new List<string>();
            if (modifier.Defense != 0) parts.Add($"+{modifier.Defense} defense");
            if (modifier.Speed != 0f) parts.Add($"{modifier.Speed:+0.##;-0.##} speed");
            if (modifier.MaxStamina != 0f) parts.Add($"+{modifier.MaxStamina:0} stamina");
            if (modifier.MeleeDamage != 0) parts.Add($"+{modifier.MeleeDamage} melee");
            if (modifier.RangedDamage != 0) parts.Add($"+{modifier.RangedDamage} ranged");
            return parts.Count == 0 ? "Armor stats" : string.Join(", ", parts);
        }

        private static IEnumerable<List<string>> Paginate(IEnumerable<string> lines)
        {
            var page = new List<string>();
            foreach (var line in lines.SelectMany(Wrap))
            {
                if (page.Count >= LinesPerPage)
                {
                    yield return page;
                    page = new List<string>();
                }

                page.Add(line);
            }

            if (page.Count > 0)
            {
                yield return page;
            }
        }

        private static IEnumerable<string> Wrap(string line)
        {
            line = Sanitize(line);
            if (line.Length <= MaxLineLength)
            {
                yield return line;
                yield break;
            }

            for (var index = 0; index < line.Length;)
            {
                var length = Math.Min(MaxLineLength, line.Length - index);
                if (index + length < line.Length)
                {
                    var breakAt = line.LastIndexOf(' ', index + length, length);
                    if (breakAt > index + 24)
                    {
                        length = breakAt - index;
                    }
                }

                yield return line.Substring(index, length).TrimEnd();
                index += length;
                while (index < line.Length && line[index] == ' ')
                {
                    index++;
                }
            }
        }

        private static byte[] BuildPdfBytes(IReadOnlyList<List<string>> pages)
        {
            var objects = new List<string> { "<< /Type /Catalog /Pages 2 0 R >>", string.Empty, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" };
            var pageObjectIds = new List<int>();
            foreach (var pageLines in pages)
            {
                var contentObjectId = objects.Count + 1;
                var pageObjectId = objects.Count + 2;
                pageObjectIds.Add(pageObjectId);
                var stream = BuildPageStream(pageLines);
                objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream");
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectId} 0 R >>");
            }

            objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"))}] /Count {pageObjectIds.Count} >>";
            var builder = new StringBuilder("%PDF-1.4\n");
            var offsets = new List<int> { 0 };
            for (var index = 0; index < objects.Count; index++)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
                builder.Append(index + 1).Append(" 0 obj\n").Append(objects[index]).Append("\nendobj\n");
            }

            var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
            builder.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n");
            for (var index = 1; index < offsets.Count; index++)
            {
                builder.Append(offsets[index].ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
            }

            builder.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\nstartxref\n").Append(xrefOffset).Append("\n%%EOF\n");
            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        private static string BuildPageStream(IReadOnlyList<string> lines)
        {
            var builder = new StringBuilder("BT\n/F1 10 Tf\n46 748 Td\n14 TL\n");
            foreach (var line in lines)
            {
                builder.Append('(').Append(EscapePdf(line)).Append(") Tj\nT*\n");
            }

            builder.Append("ET");
            return builder.ToString();
        }

        private static string Sanitize(string value)
        {
            return string.Concat((value ?? string.Empty).Select(character => character is >= ' ' and <= '~' ? character : '-'));
        }

        private static string EscapePdf(string value)
        {
            return Sanitize(value).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }
    }
}
