using UnityEngine;

namespace Hollow.RoomDesigner
{
    public sealed class RoomDesignerLightingController
    {
        private Transform lightingRoot;

        public void Ensure(Transform owner)
        {
            if (owner == null || lightingRoot != null)
            {
                return;
            }

            var existing = owner.Find("RoomDesignerSceneLightingRig");
            if (existing != null)
            {
                lightingRoot = existing;
                return;
            }

            var root = new GameObject("RoomDesignerSceneLightingRig");
            root.transform.SetParent(owner, false);
            lightingRoot = root.transform;

            AddLight("KeyLight", LightType.Directional, new Vector3(42f, -38f, 0f), Color.white, 1.05f);
            AddLight("FillLight", LightType.Point, new Vector3(-4.5f, 5.5f, -3.5f), new Color(0.62f, 0.72f, 1f), 1.25f, 18f);
            AddLight("WarmRimLight", LightType.Point, new Vector3(5f, 3.5f, 4.5f), new Color(1f, 0.72f, 0.48f), 0.8f, 14f);
            root.SetActive(false);
        }

        public void Apply(RoomDesignerPreviewMode mode)
        {
            if (lightingRoot != null)
            {
                lightingRoot.gameObject.SetActive(mode == RoomDesignerPreviewMode.Scene);
            }
        }

        private void AddLight(string name, LightType type, Vector3 localPositionOrEuler, Color color, float intensity, float range = 10f)
        {
            var lightObject = new GameObject(name, typeof(Light));
            lightObject.transform.SetParent(lightingRoot, false);
            var light = lightObject.GetComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            if (type == LightType.Directional)
            {
                lightObject.transform.localRotation = Quaternion.Euler(localPositionOrEuler);
            }
            else
            {
                lightObject.transform.localPosition = localPositionOrEuler;
            }
        }
    }
}
