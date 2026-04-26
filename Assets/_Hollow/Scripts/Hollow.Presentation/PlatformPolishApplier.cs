using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Presentation
{
    public sealed class PlatformPolishApplier : MonoBehaviour
    {
        [SerializeField] private PlatformPolishProfileDefinition profile;

        public PlatformPolishProfileDefinition Profile => profile;

        public void Configure(PlatformPolishProfileDefinition nextProfile)
        {
            profile = nextProfile;
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
                presentationRoot.Configure(profile.Mode.ToPlatformKind(), profile.WorldScale);
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
