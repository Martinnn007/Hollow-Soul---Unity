using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Hollow.Presentation
{
    public static class AddressableAssetLoader
    {
        public static AsyncOperationHandle<T> LoadAssetAsync<T>(string key) where T : Object
        {
            return Addressables.LoadAssetAsync<T>(key);
        }
    }
}
