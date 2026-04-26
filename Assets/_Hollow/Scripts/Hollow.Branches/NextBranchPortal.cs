using UnityEngine;

namespace Hollow.Branches
{
    public sealed class NextBranchPortal : MonoBehaviour
    {
        public NextBranchChoice Choice { get; private set; }

        public void Configure(NextBranchChoice choice)
        {
            Choice = choice;
            name = choice != null ? $"NextBranchPortal_{choice.Index}" : "NextBranchPortal";
        }
    }
}
