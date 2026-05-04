using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Combat
{
    [CreateAssetMenu(menuName = "Hollow/Combat/Enemy Authoring Protection Registry", fileName = "EnemyAuthoringProtectionRegistry")]
    public sealed class EnemyAuthoringProtectionRegistry : ScriptableObject
    {
        [SerializeField] private List<EnemyAuthoringProtectionEntry> protectedAssets = new();

        public IReadOnlyList<EnemyAuthoringProtectionEntry> ProtectedAssets => protectedAssets;

        public bool ContainsGuid(string guid)
        {
            return protectedAssets.Any(entry => string.Equals(entry.Guid, guid, StringComparison.Ordinal));
        }

        public void Record(
            string guid,
            string assetPath,
            string ownerId,
            string dataKind,
            string contentHash,
            string notes,
            long unixTimestamp)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return;
            }

            var existing = protectedAssets.FirstOrDefault(entry => string.Equals(entry.Guid, guid, StringComparison.Ordinal));
            if (existing == null)
            {
                existing = new EnemyAuthoringProtectionEntry();
                protectedAssets.Add(existing);
            }

            existing.Configure(guid, assetPath, ownerId, dataKind, contentHash, notes, unixTimestamp);
        }

        public void Remove(string guid)
        {
            protectedAssets.RemoveAll(entry => string.Equals(entry.Guid, guid, StringComparison.Ordinal));
        }
    }

    [Serializable]
    public sealed class EnemyAuthoringProtectionEntry
    {
        [SerializeField] private string guid = string.Empty;
        [SerializeField] private string assetPath = string.Empty;
        [SerializeField] private string ownerId = string.Empty;
        [SerializeField] private string dataKind = string.Empty;
        [SerializeField] private string contentHash = string.Empty;
        [SerializeField] private string notes = string.Empty;
        [SerializeField] private long unixTimestamp;

        public string Guid => guid ?? string.Empty;
        public string AssetPath => assetPath ?? string.Empty;
        public string OwnerId => ownerId ?? string.Empty;
        public string DataKind => dataKind ?? string.Empty;
        public string ContentHash => contentHash ?? string.Empty;
        public string Notes => notes ?? string.Empty;
        public long UnixTimestamp => unixTimestamp;

        public void Configure(
            string nextGuid,
            string nextAssetPath,
            string nextOwnerId,
            string nextDataKind,
            string nextContentHash,
            string nextNotes,
            long nextUnixTimestamp)
        {
            guid = nextGuid ?? string.Empty;
            assetPath = nextAssetPath ?? string.Empty;
            ownerId = nextOwnerId ?? string.Empty;
            dataKind = nextDataKind ?? string.Empty;
            contentHash = nextContentHash ?? string.Empty;
            notes = nextNotes ?? string.Empty;
            unixTimestamp = nextUnixTimestamp;
        }
    }
}
