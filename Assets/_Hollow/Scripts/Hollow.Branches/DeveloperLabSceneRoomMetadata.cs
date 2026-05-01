using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Branches
{
    [DisallowMultipleComponent]
    public sealed class DeveloperLabSceneRoomMetadata : MonoBehaviour
    {
        [SerializeField] private string roomId = string.Empty;
        [SerializeField] private string title = string.Empty;
        [SerializeField] private int roomIndex = 1;
        [SerializeField] private string footprintPreset = "Wide2x1";
        [SerializeField] private string outputJsonPath = string.Empty;
        [SerializeField] private string contentAssetPath = string.Empty;

        public string RoomId => roomId;
        public string Title => title;
        public int RoomIndex => roomIndex;
        public string FootprintPreset => footprintPreset;
        public string OutputJsonPath => outputJsonPath;
        public string ContentAssetPath => contentAssetPath;

        public void Configure(
            string nextRoomId,
            string nextTitle,
            int nextRoomIndex,
            string nextFootprintPreset,
            string nextOutputJsonPath,
            string nextContentAssetPath)
        {
            roomId = nextRoomId ?? string.Empty;
            title = nextTitle ?? string.Empty;
            roomIndex = Mathf.Max(1, nextRoomIndex);
            footprintPreset = string.IsNullOrWhiteSpace(nextFootprintPreset) ? "Wide2x1" : nextFootprintPreset;
            outputJsonPath = nextOutputJsonPath ?? string.Empty;
            contentAssetPath = nextContentAssetPath ?? string.Empty;
        }
    }
}
