using Hollow.Platform;
using UnityEngine;

namespace Hollow.UI.MainMenu
{
    [RequireComponent(typeof(Canvas))]
    public sealed class MainMenuPlatformPresenter : MonoBehaviour
    {
        [SerializeField] private HollowPlatformKind platformKind = HollowPlatformKind.WindowsStandard3D;

        public HollowPlatformKind PlatformKind => platformKind;

        private void Awake()
        {
            Apply(platformKind);
        }

        public void Apply(HollowPlatformKind nextPlatformKind)
        {
            platformKind = nextPlatformKind;
            var canvas = GetComponent<Canvas>();
            if (platformKind == HollowPlatformKind.WindowsStandard3D)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                transform.localPosition = Vector3.zero;
                transform.localScale = Vector3.one;
                return;
            }

            canvas.renderMode = RenderMode.WorldSpace;
            transform.localPosition = new Vector3(0f, 1.45f, 2.2f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * 0.0018f;
        }
    }
}
