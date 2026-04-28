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

            ClearArtPassVisualChildren();
            var prefabRole = Choice.Kind switch
            {
                HubPortalKind.NextWorld => PresentationPrefabRole.HubReturnPortal,
                HubPortalKind.FinalExtraction => PresentationPrefabRole.BossKeyPickup,
                _ => PresentationPrefabRole.NextBranchPortal
            };
            PresentationPrefabResolver.InstantiateVisual(prefabRole, transform, Vector3.zero, Vector3.one);
        }

        private void ClearArtPassVisualChildren()
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                var child = transform.GetChild(index);
                if (!child.TryGetComponent<PresentationVisualMarker>(out _))
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
