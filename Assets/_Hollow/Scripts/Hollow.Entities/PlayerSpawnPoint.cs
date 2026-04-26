using UnityEngine;

namespace Hollow.Entities
{
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        public Vector3 WorldPosition => transform.position;
    }
}
