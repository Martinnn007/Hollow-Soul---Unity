using UnityEngine;

namespace Hollow.Rewards
{
    public sealed class HubReturnPortal : MonoBehaviour
    {
        [SerializeField] private string label = "Return To Hub";

        public string Label => label;
    }
}
