using System.Linq;
using UnityEngine;
using Hollow.Data.Definitions;
using Hollow.Presentation;

namespace Hollow.Branches
{
    public sealed class NextBranchPortal : MonoBehaviour
    {
        private const string LabelName = "NextBranchPortal_Label";

        private string displayLabel;
        private TextMesh labelMesh;

        public NextBranchChoice Choice { get; private set; }

        public bool IsInteractable => Choice?.IsInteractable ?? false;

        public string DisplayLabel => string.IsNullOrWhiteSpace(displayLabel) ? Choice?.DisplayName ?? string.Empty : displayLabel;

        public void Configure(NextBranchChoice choice)
        {
            Configure(choice, null);
        }

        public void Configure(NextBranchChoice choice, string displayNameOverride)
        {
            Choice = choice;
            displayLabel = string.IsNullOrWhiteSpace(displayNameOverride) ? choice?.DisplayName ?? string.Empty : displayNameOverride;
            name = choice != null ? $"NextBranchPortal_{choice.Index}_{choice.Kind}_{choice.State}" : "NextBranchPortal";
            ApplyVisualState();
            ApplyLabel();
        }

        private void ApplyVisualState()
        {
            var renderer = GetComponentsInChildren<Renderer>(includeInactive: true)
                .FirstOrDefault(candidate => candidate.GetComponent<TextMesh>() == null);
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

        private void ApplyLabel()
        {
            if (Choice == null)
            {
                return;
            }

            labelMesh ??= transform.Find(LabelName)?.GetComponent<TextMesh>();
            if (labelMesh == null)
            {
                var labelObject = new GameObject(LabelName, typeof(TextMesh));
                labelObject.transform.SetParent(transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 0.42f, 0f);
                labelObject.transform.localRotation = Quaternion.Euler(64f, 0f, 0f);
                labelObject.transform.localScale = Vector3.one * 0.085f;
                labelMesh = labelObject.GetComponent<TextMesh>();
                labelMesh.anchor = TextAnchor.MiddleCenter;
                labelMesh.alignment = TextAlignment.Center;
                labelMesh.characterSize = 1f;
                labelMesh.fontSize = 42;
            }

            var suffix = Choice.Kind == HubPortalKind.Branch && Choice.State == HubBranchPortalState.Defeated ? "\nDefeated" : string.Empty;
            labelMesh.text = $"{DisplayLabel}{suffix}";
            labelMesh.color = Choice.Kind == HubPortalKind.Branch && Choice.State == HubBranchPortalState.Defeated
                ? new Color(0.58f, 0.56f, 0.52f, 0.92f)
                : Choice.Kind == HubPortalKind.NextWorld
                    ? new Color(0.68f, 0.95f, 1f, 0.96f)
                    : new Color(1f, 0.82f, 0.38f, 0.96f);
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
