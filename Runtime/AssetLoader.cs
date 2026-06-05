using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace Dreamy.Assets
{
    public static class AssetLoader
    {
        private static readonly Dictionary<Type, Dictionary<string, Object>> AssetCached =
            new Dictionary<Type, Dictionary<string, Object>>();

        private static readonly Dictionary<Type, Dictionary<string, AssetRequest>> RequestCached =
            new Dictionary<Type, Dictionary<string, AssetRequest>>();

        private static readonly IAssetLoader AddressableLoader = new AddressableLoader();
        private static readonly IAssetLoader ResourceLoader = new ResourceLoader();

        static AssetLoader()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        public static async Task<TAsset> LoadAsync<TAsset>(string address) where TAsset : Object
        {
            EnsureTypeCache<TAsset>();

            Dictionary<string, Object> assetsOfType = AssetCached[typeof(TAsset)];
            Dictionary<string, AssetRequest> requestsOfType = RequestCached[typeof(TAsset)];

            if (assetsOfType.TryGetValue(address, out Object cachedAsset))
            {
                return (TAsset)cachedAsset;
            }

            if (requestsOfType.TryGetValue(address, out AssetRequest cachedRequest))
            {
                return await ((AssetRequest<TAsset>)cachedRequest).Task;
            }

            AssetRequest<TAsset> request = AddressableLoader.LoadAsync<TAsset>(address);
            TAsset result = await request.Task;

            if (request.Status == AssetRequestStatus.Failed)
            {
                throw request.OperationException ?? new InvalidOperationException(
                    $"Failed to load addressable asset. Key: {address}.");
            }

            assetsOfType[address] = result;
            requestsOfType[address] = request;
            return result;
        }

        public static AssetRequest<TAsset> RequestAsync<TAsset>(string address) where TAsset : Object
        {
            EnsureTypeCache<TAsset>();

            Dictionary<string, AssetRequest> requestsOfType = RequestCached[typeof(TAsset)];
            if (requestsOfType.TryGetValue(address, out AssetRequest cachedRequest))
            {
                return (AssetRequest<TAsset>)cachedRequest;
            }

            AssetRequest<TAsset> request = AddressableLoader.LoadAsync<TAsset>(address);
            requestsOfType[address] = request;
            CompleteAndCache(address, request);
            return request;
        }

        public static TAsset LoadResource<TAsset>(string address) where TAsset : Object
        {
            EnsureTypeCache<TAsset>();

            Dictionary<string, Object> assetsOfType = AssetCached[typeof(TAsset)];
            Dictionary<string, AssetRequest> requestsOfType = RequestCached[typeof(TAsset)];

            if (assetsOfType.TryGetValue(address, out Object cachedAsset))
            {
                return (TAsset)cachedAsset;
            }

            AssetRequest<TAsset> request = ResourceLoader.Load<TAsset>(address);
            if (request.Status == AssetRequestStatus.Failed)
            {
                throw request.OperationException ?? new InvalidOperationException(
                    $"Failed to load resource asset. Key: {address}.");
            }

            assetsOfType[address] = request.Result;
            requestsOfType[address] = request;
            return request.Result;
        }

        public static async Task<Sprite> LoadSprite(string atlasAddress, string spriteName)
        {
            SpriteAtlas atlas = await LoadAsync<SpriteAtlas>(atlasAddress);
            Sprite sprite = atlas.GetSprite(spriteName);

            if (sprite == null)
            {
                Debug.LogError($"Not found sprite: {spriteName} in atlas: {atlasAddress}");
            }

            return sprite;
        }

        public static void Unload<TAsset>(string address) where TAsset : Object
        {
            Type type = typeof(TAsset);
            if (!RequestCached.TryGetValue(type, out Dictionary<string, AssetRequest> requestsOfType))
            {
                return;
            }

            if (!requestsOfType.Remove(address, out AssetRequest request))
            {
                return;
            }

            AddressableLoader.Release(request);

            if (AssetCached.TryGetValue(type, out Dictionary<string, Object> assetsOfType))
            {
                assetsOfType.Remove(address);
            }
        }

        public static void UnloadAll()
        {
            AddressableLoader.ReleaseAll();
            AssetCached.Clear();
            RequestCached.Clear();
        }

        private static void EnsureTypeCache<TAsset>() where TAsset : Object
        {
            Type type = typeof(TAsset);
            if (AssetCached.ContainsKey(type))
            {
                return;
            }

            AssetCached.Add(type, new Dictionary<string, Object>());
            RequestCached.Add(type, new Dictionary<string, AssetRequest>());
        }

        private static async void CompleteAndCache<TAsset>(
            string address,
            AssetRequest<TAsset> request) where TAsset : Object
        {
            try
            {
                TAsset result = await request.Task;
                if (request.Status == AssetRequestStatus.Succeeded && result != null)
                {
                    AssetCached[typeof(TAsset)][address] = result;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void OnActiveSceneChanged(Scene previousActiveScene, Scene newActiveScene)
        {
            UnloadAll();
        }
    }
}
