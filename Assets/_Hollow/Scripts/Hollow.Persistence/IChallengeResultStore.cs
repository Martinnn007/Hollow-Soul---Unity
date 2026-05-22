using System.Collections.Generic;

namespace Hollow.Persistence
{
    public interface IChallengeResultStore
    {
        IReadOnlyList<ChallengeResultRecord> LoadChallengeRecords(ProfileSlotId slotId);

        ChallengeResultRecord GetChallengeRecord(ProfileSlotId slotId, string challengeId);

        ChallengeResultRecord MarkChallengeAttemptStarted(ProfileSlotId slotId, string challengeId, int seed);

        ChallengeResultRecord CompleteChallengeAttempt(ProfileSlotId slotId, string challengeId, int seed, float clearTimeSeconds);

        ChallengeResultRecord FailChallengeAttempt(ProfileSlotId slotId, string challengeId, int seed);
    }
}
