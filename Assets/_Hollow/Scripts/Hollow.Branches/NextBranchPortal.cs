using UnityEngine;
using Hollow.Data.Definitions;
using Hollow.Presentation;

namespace Hollow.Branches
{
    public sealed class NextBranchPortal : MonoBehaviour
    {
        public NextBranchChoice Choice { get; private set; }

        public bool IsInteractable => Choice?.IsInteractable ?? false;

        public void Configure(NextBranchChoice choice)
        {
            Choice = choice;
            name = choice != null ? $"NextBranchPortal_{choice.Index}_{choice.Kind}_{choice.State}" : "NextBranchPortal";
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer == null || Choice == null)
            {
                return;
            }

            var role = Choice.Kind switch
            {
                HubPortalKind.NextWorld => MaterialRole.HubReturnPortal,
                HubPortalKind.FinalExtraction => MaterialRole.BossKeyPickup,
                _ => Choice.State == HubBranchPortalState.Defeated ? MaterialRole.DoorUnavailable : MaterialRole.NextBranchPortal
            };
            renderer.sharedMaterial = MaterialResolver.Resolve(role);
        }
    }
}
