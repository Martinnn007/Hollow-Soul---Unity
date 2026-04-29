using System;

namespace Hollow.Persistence
{
    public sealed class ChallengeResultRecord
    {
        public ChallengeResultRecord(string challengeId, int attempts, int completions, float bestClearTimeSeconds, string lastResult, int lastPlayedSeed)
        {
            ChallengeId = challengeId ?? string.Empty;
            Attempts = Math.Max(0, attempts);
            Completions = Math.Max(0, completions);
            BestClearTimeSeconds = Math.Max(0f, bestClearTimeSeconds);
            LastResult = lastResult ?? string.Empty;
            LastPlayedSeed = Math.Max(0, lastPlayedSeed);
        }

        public string ChallengeId { get; }

        public int Attempts { get; }

        public int Completions { get; }

        public float BestClearTimeSeconds { get; }

        public string LastResult { get; }

        public int LastPlayedSeed { get; }

        public bool HasBestClearTime => BestClearTimeSeconds > 0f;
    }

    [Serializable]
    public sealed class ChallengeRecordSaveState
    {
        public string challengeId;
        public int attempts;
        public int completions;
        public float bestClearTimeSeconds;
        public string lastResult;
        public int lastPlayedSeed;

        public ChallengeResultRecord ToRecord()
        {
            return new ChallengeResultRecord(challengeId, attempts, completions, bestClearTimeSeconds, lastResult, lastPlayedSeed);
        }

        public static ChallengeRecordSaveState FromRecord(ChallengeResultRecord record)
        {
            return new ChallengeRecordSaveState
            {
                challengeId = record?.ChallengeId ?? string.Empty,
                attempts = record?.Attempts ?? 0,
                completions = record?.Completions ?? 0,
                bestClearTimeSeconds = record?.BestClearTimeSeconds ?? 0f,
                lastResult = record?.LastResult ?? string.Empty,
                lastPlayedSeed = record?.LastPlayedSeed ?? 0
            };
        }
    }
}
