using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Presentation
{
    public sealed class PlatformPolishApplier : MonoBehaviour
    {
        [SerializeField] private PlatformPolishProfileDefinition profile;
        [SerializeField] private HollowRenderProfileDefinition renderProfileOverride;

        public PlatformPolishProfileDefinition Profile => profile;

        public HollowRenderProfileDefinition RenderProfileOverride => renderProfileOverride;

        public void Configure(PlatformPolishProfileDefinition nextProfile)
        {
            profile = nextProfile;
        }

        public void ConfigureRenderProfileOverride(HollowRenderProfileDefinition nextRenderProfileOverride)
        {
            renderProfileOverride = nextRenderProfileOverride;
        }

        private void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            Apply(FindCamera(), FindAnyObjectByType<PlatformPresentationRoot>());
        }

        public void Apply(Camera targetCamera, PlatformPresentationRoot presentationRoot)
        {
            if (profile == null)
            {
                return;
            }

            ApplyPerformance();
            if (presentationRoot != null)
            {
                presentationRoot.Configure(profile.Mode.ToPlatformKind(), profile.WorldScale, profile.WorldYawDegrees);
            }

            if (targetCamera != null)
            {
                ApplyCamera(targetCamera);
            }
        }

        private void ApplyCamera(Camera targetCamera)
        {
            targetCamera.transform.localPosition = profile.CameraLocalPosition;
            targetCamera.transform.localRotation = Quaternion.Euler(profile.CameraLocalEulerAngles);
            targetCamera.fieldOfView = profile.CameraFieldOfView;
            targetCamera.nearClipPlane = profile.NearClipPlane;
            targetCamera.farClipPlane = profile.FarClipPlane;
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = profile.CameraBackgroundColor;
            RenderSettings.ambientLight = profile.AmbientLightColor;

            var vignette = targetCamera.GetComponent<ComfortVignettePresenter>() ?? targetCamera.gameObject.AddComponent<ComfortVignettePresenter>();
            vignette.Configure(profile.UseComfortVignette, profile.ComfortVignetteRadius, profile.ComfortVignetteOpacity);
        }

        private void ApplyPerformance()
        {
            var renderProfile = renderProfileOverride != null ? renderProfileOverride : profile.RenderProfile;
            if (renderProfile != null)
            {
                RenderProfileApplier.Apply(renderProfile);
                return;
            }

            Application.targetFrameRate = profile.TargetFrameRate;
            QualitySettings.vSyncCount = profile.VSyncCount;
        }

        private Camera FindCamera()
        {
            var childCamera = GetComponentInChildren<Camera>(includeInactive: true);
            if (childCamera != null)
            {
                return childCamera;
            }

            return Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        }
    }
}
