using UnityEngine;
using UnityEngine.Events;

namespace Hollow.UI.MainMenu
{
    public sealed class MenuActionButton : MonoBehaviour
    {
        [SerializeField] private UnityEvent clicked;

        public void Invoke()
        {
            clicked?.Invoke();
        }
    }
}
