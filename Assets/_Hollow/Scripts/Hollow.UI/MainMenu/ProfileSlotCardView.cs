using UnityEngine;

namespace Hollow.UI.MainMenu
{
    public sealed class ProfileSlotCardView : MonoBehaviour
    {
        [SerializeField] private int slotIndex;

        public int SlotIndex => slotIndex;
    }
}
