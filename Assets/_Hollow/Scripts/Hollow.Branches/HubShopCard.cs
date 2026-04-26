using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class HubShopCard : MonoBehaviour
    {
        [SerializeField] private string offerId = string.Empty;
        [SerializeField] private TextMesh label;
        [SerializeField] private Renderer cardRenderer;
        private HubShopOffer offer;
        private HubShopCardViewModel viewModel;
        private MaterialPropertyBlock materialPropertyBlock;

        public string OfferId => offerId;

        public bool IsInteractable => viewModel.IsInteractable;

        public HubShopCardViewModel ViewModel => viewModel;

        public void Configure(HubShopOffer nextOffer)
        {
            offer = nextOffer;
            offerId = nextOffer?.OfferId ?? string.Empty;
            BuildIfNeeded();
        }

        public void Refresh(int runSouls)
        {
            if (offer == null)
            {
                return;
            }

            BuildIfNeeded();
            viewModel = HubShopCardViewModel.FromOffer(offer, runSouls);
            if (label != null)
            {
                label.text = viewModel.BodyText;
                label.color = viewModel.IsSold
                    ? new Color(0.55f, 0.55f, 0.55f, 1f)
                    : viewModel.IsAffordable ? Color.white : new Color(1f, 0.34f, 0.28f, 1f);
            }

            if (cardRenderer != null)
            {
                materialPropertyBlock ??= new MaterialPropertyBlock();
                var color = viewModel.IsSold
                    ? new Color(0.24f, 0.24f, 0.28f, 0.85f)
                    : viewModel.IsAffordable ? new Color(0.22f, 0.45f, 0.7f, 1f) : new Color(0.42f, 0.18f, 0.16f, 1f);
                cardRenderer.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetColor("_BaseColor", color);
                materialPropertyBlock.SetColor("_Color", color);
                cardRenderer.SetPropertyBlock(materialPropertyBlock);
            }
        }

        private void BuildIfNeeded()
        {
            cardRenderer = cardRenderer != null ? cardRenderer : GetComponentInChildren<Renderer>();
            if (cardRenderer != null)
            {
                MaterialResolver.ApplyTo(cardRenderer, MaterialRole.HubShop);
            }

            if (label != null)
            {
                return;
            }

            var labelObject = new GameObject("HubShopCard.Label", typeof(TextMesh));
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(-0.43f, 0.31f, -0.055f);
            labelObject.transform.localRotation = Quaternion.identity;
            labelObject.transform.localScale = Vector3.one * 0.075f;
            label = labelObject.GetComponent<TextMesh>();
            label.anchor = TextAnchor.UpperLeft;
            label.alignment = TextAlignment.Left;
            label.fontSize = 48;
            label.characterSize = 0.2f;
            label.color = Color.white;
        }
    }
}
