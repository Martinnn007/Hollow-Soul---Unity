using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Platform Polish Profile", fileName = "PlatformPolishProfile")]
    public sealed class PlatformPolishProfileDefinition : HollowDefinition
    {
        [SerializeField] private PlatformPresentationMode mode = PlatformPresentationMode.WindowsStandard3D;
        [SerializeField] private float worldScale = 1f;
        [SerializeField] private Vector3 cameraLocalPosition = new(0f, 7f, -10f);
        [SerializeField] private Vector3 cameraLocalEulerAngles = new(35f, 0f, 0f);
        [SerializeField] private float cameraFieldOfView = 60f;
        [SerializeField] private float nearClipPlane = 0.03f;
        [SerializeField] private float farClipPlane = 80f;
        [SerializeField] private Color cameraBackgroundColor = new(0.02f, 0.025f, 0.035f, 1f);
        [SerializeField] private Color ambientLightColor = new(0.62f, 0.68f, 0.78f, 1f);
        [SerializeField] private int targetFrameRate = 90;
        [SerializeField] private int vSyncCount;
        [SerializeField] private float renderScale = 1f;
        [SerializeField] private bool useComfortVignette;
        [SerializeField] private float comfortVignetteRadius = 0.82f;
        [SerializeField] private float comfortVignetteOpacity = 0.18f;

        public PlatformPresentationMode Mode => mode;

        public float WorldScale => worldScale;

        public Vector3 CameraLocalPosition => cameraLocalPosition;

        public Vector3 CameraLocalEulerAngles => cameraLocalEulerAngles;

        public float CameraFieldOfView => cameraFieldOfView;

        public float NearClipPlane => nearClipPlane;

        public float FarClipPlane => farClipPlane;

        public Color CameraBackgroundColor => cameraBackgroundColor;

        public Color AmbientLightColor => ambientLightColor;

        public int TargetFrameRate => targetFrameRate;

        public int VSyncCount => vSyncCount;

        public float RenderScale => renderScale;

        public bool UseComfortVignette => useComfortVignette;

        public float ComfortVignetteRadius => comfortVignetteRadius;

        public float ComfortVignetteOpacity => comfortVignetteOpacity;

        public void Configure(
            PlatformPresentationMode nextMode,
            float nextWorldScale,
            Vector3 nextCameraLocalPosition,
            Vector3 nextCameraLocalEulerAngles,
            float nextCameraFieldOfView,
            float nextNearClipPlane,
            float nextFarClipPlane,
            Color nextCameraBackgroundColor,
            Color nextAmbientLightColor,
            int nextTargetFrameRate,
            int nextVSyncCount,
            float nextRenderScale,
            bool nextUseComfortVignette,
            float nextComfortVignetteRadius,
            float nextComfortVignetteOpacity)
        {
            mode = nextMode;
            worldScale = Mathf.Max(0.001f, nextWorldScale);
            cameraLocalPosition = nextCameraLocalPosition;
            cameraLocalEulerAngles = nextCameraLocalEulerAngles;
            cameraFieldOfView = Mathf.Clamp(nextCameraFieldOfView, 30f, 90f);
            nearClipPlane = Mathf.Max(0.001f, nextNearClipPlane);
            farClipPlane = Mathf.Max(nearClipPlane + 1f, nextFarClipPlane);
            cameraBackgroundColor = nextCameraBackgroundColor;
            ambientLightColor = nextAmbientLightColor;
            targetFrameRate = Mathf.Max(30, nextTargetFrameRate);
            vSyncCount = Mathf.Clamp(nextVSyncCount, 0, 4);
            renderScale = Mathf.Clamp(nextRenderScale, 0.5f, 1.5f);
            useComfortVignette = nextUseComfortVignette;
            comfortVignetteRadius = Mathf.Clamp01(nextComfortVignetteRadius);
            comfortVignetteOpacity = Mathf.Clamp01(nextComfortVignetteOpacity);
        }
    }
}
