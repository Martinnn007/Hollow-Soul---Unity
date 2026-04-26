using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Hollow.Persistence
{
    public sealed class JsonProfileStore : IProfileStore, IRunSaveStore
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
            slot.bankedSouls = 0;
            slot.completedRuns = 0;
            slot.activeRun = null;
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

        public ProfileSlotSummary MarkRunStarted(ProfileSlotId slotId)
        {
            var data = LoadData();
            var slot = data.slots[slotId.Value];
            EnsureExistingProfile(slot, slotId);

            slot.totalRuns++;
            slot.activeRun = null;
            slot.hasActiveRun = false;
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

        public bool TryLoadActiveRun(ProfileSlotId slotId, out RunSaveSnapshot snapshot)
        {
            var data = LoadData();
            var slot = data.slots[slotId.Value];
            snapshot = slot.activeRun;
            if (IsMeaningfulActiveRun(snapshot))
            {
                return true;
            }

            snapshot = null;
            return false;
        }

        public void SaveActiveRun(ProfileSlotId slotId, RunSaveSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var data = LoadData();
            var slot = data.slots[slotId.Value];
            EnsureExistingProfile(slot, slotId);

            snapshot.savedAtUtcTicks = DateTime.UtcNow.Ticks;
            if (string.IsNullOrWhiteSpace(snapshot.runId))
            {
                snapshot.runId = Guid.NewGuid().ToString("N");
            }

            slot.activeRun = snapshot;
            slot.hasActiveRun = true;
            slot.lastPlayedUtcTicks = snapshot.savedAtUtcTicks;
            SaveData(data);
        }

        public void ClearActiveRun(ProfileSlotId slotId)
        {
            var data = LoadData();
            var slot = data.slots[slotId.Value];
            EnsureExistingProfile(slot, slotId);

            slot.activeRun = null;
            slot.hasActiveRun = false;
            slot.lastPlayedUtcTicks = DateTime.UtcNow.Ticks;
            SaveData(data);
        }

        public void CompleteActiveRun(ProfileSlotId slotId, RunCompletionSummary summary)
        {
            var data = LoadData();
            var slot = data.slots[slotId.Value];
            EnsureExistingProfile(slot, slotId);

            slot.bankedSouls += Math.Max(0, summary?.soulsToBank ?? 0);
            slot.completedRuns++;
            slot.activeRun = null;
            slot.hasActiveRun = false;
            slot.lastPlayedUtcTicks = DateTime.UtcNow.Ticks;
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
                hasActiveRun = false,
                bankedSouls = 0,
                completedRuns = 0,
                activeRun = null
            };
        }

        private static void Normalize(ProfileStoreSaveData data)
        {
            var previousSchemaVersion = data.schemaVersion;
            data.schemaVersion = 2;
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
                data.slots[index].profileId ??= string.Empty;
                data.slots[index].displayName ??= string.Empty;
                if (previousSchemaVersion < 2)
                {
                    data.slots[index].bankedSouls = 0;
                    data.slots[index].completedRuns = 0;
                    data.slots[index].activeRun = null;
                }

                data.slots[index].hasActiveRun = IsMeaningfulActiveRun(data.slots[index].activeRun);
                if (!data.slots[index].hasActiveRun)
                {
                    data.slots[index].activeRun = null;
                }
            }
        }

        private static void EnsureExistingProfile(ProfileSlotSaveData slot, ProfileSlotId slotId)
        {
            if (string.IsNullOrWhiteSpace(slot.profileId))
            {
                throw new InvalidOperationException($"Cannot mutate empty profile slot {slotId.Value}.");
            }
        }

        private static bool IsMeaningfulActiveRun(RunSaveSnapshot snapshot)
        {
            return snapshot != null && !string.IsNullOrWhiteSpace(snapshot.runId);
        }
    }
}
