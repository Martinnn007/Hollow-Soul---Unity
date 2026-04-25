using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Hollow.Persistence
{
    public sealed class JsonProfileStore : IProfileStore
    {
        private const string FileName = "hollow_profiles.json";
        private readonly string savePath;

        public JsonProfileStore()
            : this(Application.persistentDataPath)
        {
        }

        public JsonProfileStore(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("Profile root directory cannot be empty.", nameof(rootDirectory));
            }

            savePath = Path.Combine(rootDirectory, FileName);
        }

        public IReadOnlyList<ProfileSlotSummary> LoadSlotSummaries()
        {
            return LoadData().slots
                .OrderBy(slot => slot.slotIndex)
                .Select(slot => slot.ToSummary())
                .ToArray();
        }

        public ProfileSlotSummary CreateOrLoadProfile(ProfileSlotId slotId, string displayName)
        {
            var data = LoadData();
            var slot = data.slots[slotId.Value];
            if (!string.IsNullOrWhiteSpace(slot.profileId))
            {
                return slot.ToSummary();
            }

            var now = DateTime.UtcNow.Ticks;
            slot.profileId = Guid.NewGuid().ToString("N");
            slot.displayName = string.IsNullOrWhiteSpace(displayName) ? $"Profile {slotId.Value + 1}" : displayName.Trim();
            slot.createdAtUtcTicks = now;
            slot.lastPlayedUtcTicks = now;
            slot.totalRuns = 0;
            slot.hasActiveRun = false;
            SaveData(data);
            return slot.ToSummary();
        }

        public ProfileSlotSummary MarkLastPlayed(ProfileSlotId slotId)
        {
            var data = LoadData();
            var slot = data.slots[slotId.Value];
            if (string.IsNullOrWhiteSpace(slot.profileId))
            {
                throw new InvalidOperationException($"Cannot mark empty profile slot {slotId.Value} as played.");
            }

            slot.lastPlayedUtcTicks = DateTime.UtcNow.Ticks;
            SaveData(data);
            return slot.ToSummary();
        }

        public void DeleteProfile(ProfileSlotId slotId)
        {
            var data = LoadData();
            data.slots[slotId.Value] = CreateEmptySlot(slotId.Value);
            SaveData(data);
        }

        private ProfileStoreSaveData LoadData()
        {
            if (!File.Exists(savePath))
            {
                var empty = CreateEmptyData();
                SaveData(empty);
                return empty;
            }

            var json = File.ReadAllText(savePath);
            var data = JsonUtility.FromJson<ProfileStoreSaveData>(json) ?? CreateEmptyData();
            Normalize(data);
            return data;
        }

        private void SaveData(ProfileStoreSaveData data)
        {
            Normalize(data);
            Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            File.WriteAllText(savePath, JsonUtility.ToJson(data, prettyPrint: true));
        }

        private static ProfileStoreSaveData CreateEmptyData()
        {
            var data = new ProfileStoreSaveData();
            for (var index = 0; index < ProfileSlotConstants.MaxSlots; index++)
            {
                data.slots.Add(CreateEmptySlot(index));
            }

            return data;
        }

        private static ProfileSlotSaveData CreateEmptySlot(int index)
        {
            return new ProfileSlotSaveData
            {
                slotIndex = index,
                profileId = string.Empty,
                displayName = string.Empty,
                createdAtUtcTicks = 0,
                lastPlayedUtcTicks = 0,
                totalRuns = 0,
                hasActiveRun = false
            };
        }

        private static void Normalize(ProfileStoreSaveData data)
        {
            data.schemaVersion = 1;
            data.slots ??= new List<ProfileSlotSaveData>();
            while (data.slots.Count < ProfileSlotConstants.MaxSlots)
            {
                data.slots.Add(CreateEmptySlot(data.slots.Count));
            }

            if (data.slots.Count > ProfileSlotConstants.MaxSlots)
            {
                data.slots.RemoveRange(ProfileSlotConstants.MaxSlots, data.slots.Count - ProfileSlotConstants.MaxSlots);
            }

            for (var index = 0; index < data.slots.Count; index++)
            {
                data.slots[index] ??= CreateEmptySlot(index);
                data.slots[index].slotIndex = index;
            }
        }
    }
}
