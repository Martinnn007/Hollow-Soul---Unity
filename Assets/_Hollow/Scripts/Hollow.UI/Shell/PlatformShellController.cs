using Hollow.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.UI.Shell
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public sealed class PlatformShellController : MonoBehaviour
    {
        [SerializeField] private HollowPlatformKind platformKind = HollowPlatformKind.WindowsStandard3D;

        public void Configure(HollowPlatformKind nextPlatformKind)
        {
            platformKind = nextPlatformKind;
        }

        public void ApplyConfiguration()
        {
            Build();
        }

        private void Start()
        {
            ApplyConfiguration();
        }

        private void Build()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = platformKind == HollowPlatformKind.WindowsStandard3D ? RenderMode.ScreenSpaceOverlay : RenderMode.WorldSpace;
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                transform.localPosition = new Vector3(0f, 1.3f, 2.4f);
                transform.localScale = Vector3.one * 0.002f;
            }

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            EnsureRuntimeHudControllers();
        }

        private void EnsureRuntimeHudControllers()
        {
            if (GetComponent<PlayerBuildHudController>() == null)
            {
                gameObject.AddComponent<PlayerBuildHudController>();
            }

            if (GetComponent<PickupRevealController>() == null)
            {
                gameObject.AddComponent<PickupRevealController>();
            }

            if (GetComponent<PauseMenuController>() == null)
            {
                gameObject.AddComponent<PauseMenuController>();
            }
        }
    }
}
