using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [Serializable]
    public struct PresentationPrefabBinding
    {
        [SerializeField] private PresentationPrefabRole role;
        [SerializeField] private GameObject prefab;

        public PresentationPrefabRole Role => role;

        public GameObject Prefab => prefab;

        public PresentationPrefabBinding(PresentationPrefabRole role, GameObject prefab)
        {
            this.role = role;
            this.prefab = prefab;
        }
    }
}
