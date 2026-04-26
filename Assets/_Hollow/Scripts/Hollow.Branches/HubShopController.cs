using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class HubShopController : MonoBehaviour
    {
        [SerializeField] private string label = "Hub Shop";
        private readonly List<HubShopCard> cards = new();

        public string Label => label;

        public InterBranchHubState State { get; private set; } = InterBranchHubState.Inactive;

        public IReadOnlyList<HubShopCard> Cards => cards;

        public void Configure(InterBranchHubState state)
        {
            State = state ?? InterBranchHubState.Inactive;
        }

        public void BuildCards(int runSouls)
        {
            DestroyCards();
            for (var index = 0; index < State.ShopOffers.Count; index++)
            {
                var offer = State.ShopOffers[index];
                var cardObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cardObject.name = $"HubShopCard.{offer.OfferId}";
                cardObject.transform.SetParent(transform, false);
                cardObject.transform.localPosition = new Vector3(-1.1f + index * 1.1f, 0.48f, -0.72f);
                cardObject.transform.localScale = new Vector3(0.95f, 0.7f, 0.06f);
                MaterialResolver.ApplyTo(cardObject, MaterialRole.HubShop);

                var collider = cardObject.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }

                var card = cardObject.GetComponent<HubShopCard>() ?? cardObject.AddComponent<HubShopCard>();
                card.Configure(offer);
                card.Refresh(runSouls);
                cards.Add(card);
            }
        }

        public void RefreshCards(int runSouls)
        {
            foreach (var card in cards.Where(card => card != null))
            {
                card.Refresh(runSouls);
            }
        }

        public bool TryGetNearestCard(Vector3 parentLocalPlayerPosition, Transform sharedParent, float radius, out HubShopCard card)
        {
            card = cards
                .Where(candidate => candidate != null)
                .Select(candidate => new
                {
                    Card = candidate,
                    Distance = Vector3.Distance(
                        Flat(parentLocalPlayerPosition),
                        Flat(sharedParent != null ? sharedParent.InverseTransformPoint(candidate.transform.position) : candidate.transform.localPosition))
                })
                .Where(candidate => candidate.Distance <= radius)
                .OrderBy(candidate => candidate.Distance)
                .FirstOrDefault()?.Card;
            return card != null;
        }

        private void DestroyCards()
        {
            foreach (var card in cards)
            {
                if (card == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(card.gameObject);
                }
                else
                {
                    DestroyImmediate(card.gameObject);
                }
            }

            cards.Clear();
        }

        private static Vector3 Flat(Vector3 value)
        {
            return new Vector3(value.x, 0f, value.z);
        }
    }
}
