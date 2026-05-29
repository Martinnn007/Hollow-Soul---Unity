using Hollow.Data.Definitions;
using Hollow.Core;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class MeleeSwipePresenter : MonoBehaviour, IPooledRuntimeObject
    {
        private const float LightDurationSeconds = 0.12f;
        private const float HeavyDurationSeconds = 0.18f;
        private const float LightWidthMeters = 0.24f;
        private const float HeavyWidthMeters = 0.38f;

        private Renderer swipeRenderer;
        private Mesh swipeMesh;
        private Color startColor;
        private float durationSeconds;
        private float ageSeconds;

        public static GameObject Spawn(
            Transform parent,
            Vector3 localOrigin,
            Vector3 direction,
            float rangeMeters,
            AttackKind attackKind)
        {
            var safeRange = Mathf.Max(0.1f, rangeMeters);
            var safeDirection = direction.sqrMagnitude < 0.001f ? Vector3.forward : direction.normalized;
            var swipe = HollowRuntimePool.RentGenerated("MeleeSwipe", parent, () => new GameObject("MeleeSwipe", typeof(MeshFilter), typeof(MeshRenderer)));
            swipe.name = attackKind == AttackKind.Heavy ? "MeleeSwipe.Heavy" : "MeleeSwipe.Light";
            swipe.transform.SetParent(parent, false);
            swipe.transform.localPosition = localOrigin +
                                            safeDirection * (safeRange * 0.5f) +
                                            new Vector3(0f, CombatFeelTuning.MeleeHitHeightMeters, 0f);
            swipe.transform.localRotation = Quaternion.LookRotation(safeDirection, Vector3.up);
            var width = attackKind == AttackKind.Heavy ? HeavyWidthMeters : LightWidthMeters;
            swipe.transform.localScale = new Vector3(width, 1f, safeRange);
            var presenter = swipe.GetComponent<MeleeSwipePresenter>() ?? swipe.AddComponent<MeleeSwipePresenter>();
            presenter.Configure(attackKind);
            return swipe;
        }

        private static Mesh CreateSwipeMesh()
        {
            var mesh = new Mesh
            {
                name = "MeleeSwipeQuad"
            };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f)
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void Configure(AttackKind attackKind)
        {
            durationSeconds = attackKind == AttackKind.Heavy ? HeavyDurationSeconds : LightDurationSeconds;
            ageSeconds = 0f;
            startColor = attackKind == AttackKind.Heavy
                ? new Color(1f, 0.78f, 0.25f, 0.62f)
                : new Color(0.6f, 0.95f, 1f, 0.48f);
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh == null)
            {
                meshFilter.sharedMesh = CreateSwipeMesh();
            }

            swipeMesh = meshFilter != null ? meshFilter.sharedMesh : null;
            swipeRenderer = GetComponent<Renderer>();
            if (swipeRenderer == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Standard");
            if (shader == null)
            {
                return;
            }

            if (swipeRenderer.sharedMaterial == null)
            {
                swipeRenderer.sharedMaterial = new Material(shader);
            }

            swipeRenderer.sharedMaterial.color = startColor;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public bool Tick(float deltaTime)
        {
            ageSeconds += Mathf.Max(0f, deltaTime);
            if (swipeRenderer != null && swipeRenderer.sharedMaterial != null && durationSeconds > 0f)
            {
                var t = Mathf.Clamp01(ageSeconds / durationSeconds);
                var color = startColor;
                color.a *= 1f - t;
                swipeRenderer.sharedMaterial.color = color;
            }

            if (ageSeconds < durationSeconds)
            {
                return true;
            }

            if (Application.isPlaying)
            {
                HollowRuntimePool.Return(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }

            return false;
        }

        private void OnDestroy()
        {
            var material = swipeRenderer != null ? swipeRenderer.sharedMaterial : null;
            if (Application.isPlaying)
            {
                if (material != null)
                {
                    Destroy(material);
                }

                if (swipeMesh != null)
                {
                    Destroy(swipeMesh);
                }
            }
            else
            {
                if (material != null)
                {
                    DestroyImmediate(material);
                }

                if (swipeMesh != null)
                {
                    DestroyImmediate(swipeMesh);
                }
            }
        }

        public void OnRentFromPool()
        {
            ageSeconds = 0f;
            gameObject.SetActive(true);
        }

        public void OnReturnToPool()
        {
            ageSeconds = 0f;
        }
    }
}
