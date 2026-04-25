using UnityEngine;

namespace Hollow.Input
{
    public enum HollowInputContext
    {
        Menu = 0,
        Gameplay = 1,
        RoomDesigner = 2,
        Debug = 3
    }

    public sealed class InputRouter : MonoBehaviour
    {
        [SerializeField] private HollowInputContext activeContext = HollowInputContext.Menu;

        public HollowInputContext ActiveContext => activeContext;

        public void SetContext(HollowInputContext nextContext)
        {
            activeContext = nextContext;
        }
    }
}
