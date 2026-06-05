using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Dreamy.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

namespace Dreamy.Assets
{
    public sealed class AssetLoader : LiveSingleton<AssetLoader>
    {
        private readonly Dictionary<Type, Dictionary<string, UnityEngine.Object>> assetCached =
            new Dictionary<Type, Dictionary<string, UnityEngine.Object>>();

        private readonly Dictionary<Type, Dictionary<string, AssetRequest>> requestCached =
            new Dictionary<Type, Dictionary<string, AssetRequest>>();

        private readonly IAssetLoader addressableLoader = new AddressableLoader();
        private readonly IAssetLoader resourceLoader = new ResourceLoader();

        protected override void Awake()
        {
            base.Awake();
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        protected override void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            base.OnDestroy();
        }

        public static async UniTask<TAsset> LoadAsync<TAsset>(string path) where TAsset : UnityEngine.Object
        {
            Type type = typeof(TAsset);
            Instance.EnsureTypeCache(type);

            Dictionary<string, UnityEngine.Object> assetsOfType = Instance.assetCached[type];
            Dictionary<string, AssetRequest> requestsOfType = Instance.requestCached[type];

            if (assetsOfType.TryGetValue(path, out UnityEngine.Object cachedAsset))
            {
                return (TAsset)cachedAsset;
            }

            if (requestsOfType.TryGetValue(path, out AssetRequest cachedRequest))
            {
                return await ((AssetRequest<TAsset>)cachedRequest).Task;
            }

            AssetRequest<TAsset> request = Instance.addressableLoader.LoadAsync<TAsset>(path);
            TAsset result = await request.Task;

            if (request.Status == AssetRequestStatus.Failed)
            {
                throw request.OperationException ?? new InvalidOperationException(
                    $"Failed to load addressable asset. Key: {path}.");
            }

            if (!assetsOfType.ContainsKey(path))
            {
                assetsOfType.Add(path, result);
                requestsOfType.Add(path, request);
            }

            return result;
        }

        public static AssetRequest<TAsset> RequestAsync<TAsset>(string path) where TAsset : UnityEngine.Object
        {
            Type type = typeof(TAsset);
            Instance.EnsureTypeCache(type);

            Dictionary<string, AssetRequest> requestsOfType = Instance.requestCached[type];
            if (requestsOfType.TryGetValue(path, out AssetRequest cachedRequest))
            {
                return (AssetRequest<TAsset>)cachedRequest;
            }

            AssetRequest<TAsset> request = Instance.addressableLoader.LoadAsync<TAsset>(path);
            requestsOfType[path] = request;
            Instance.CompleteAndCache(path, request);
            return request;
        }

        public static TAsset LoadResource<TAsset>(string path) where TAsset : UnityEngine.Object
        {
            Type type = typeof(TAsset);
            Instance.EnsureTypeCache(type);

            Dictionary<string, UnityEngine.Object> assetsOfType = Instance.assetCached[type];
            Dictionary<string, AssetRequest> requestsOfType = Instance.requestCached[type];

            if (assetsOfType.TryGetValue(path, out UnityEngine.Object cachedAsset))
            {
                return (TAsset)cachedAsset;
            }

            AssetRequest<TAsset> request = Instance.resourceLoader.Load<TAsset>(path);
            if (request.Status == AssetRequestStatus.Failed)
            {
                throw request.OperationException ?? new InvalidOperationException(
                    $"Failed to load resource asset. Key: {path}.");
            }

            if (!assetsOfType.ContainsKey(path))
            {
                assetsOfType.Add(path, request.Result);
                requestsOfType.Add(path, request);
            }

            return request.Result;
        }

        public static async UniTask<Sprite> LoadSprite(string atlasPath, string spriteName)
        {
            try
            {
                SpriteAtlas atlas = await LoadAsync<SpriteAtlas>(atlasPath);
                Sprite sprite = atlas.GetSprite(spriteName);

                if (sprite == null)
                {
                    Debug.LogError($"Not found sprite: {spriteName} in atlas: {atlasPath}");
                }

                return sprite;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Not found sprite: {spriteName} in atlas: {atlasPath}");
                return null;
            }
        }

        public static void Unload<TAsset>(string path) where TAsset : UnityEngine.Object
        {
            Type type = typeof(TAsset);

            if (!Instance.requestCached.TryGetValue(type, out Dictionary<string, AssetRequest> requestsOfType))
            {
                return;
            }

            if (!Instance.assetCached.TryGetValue(type, out Dictionary<string, UnityEngine.Object> assetsOfType))
            {
                return;
            }

            if (assetsOfType.ContainsKey(path) && requestsOfType.TryGetValue(path, out AssetRequest request))
            {
                Instance.addressableLoader.Release(request);
                assetsOfType.Remove(path);
                requestsOfType.Remove(path);
            }
        }

        public static void UnloadAll()
        {
            Instance.addressableLoader.ReleaseAll();
            Instance.assetCached.Clear();
            Instance.requestCached.Clear();
        }

        private void EnsureTypeCache(Type type)
        {
            if (assetCached.ContainsKey(type))
            {
                return;
            }

            assetCached.Add(type, new Dictionary<string, UnityEngine.Object>());
            requestCached.Add(type, new Dictionary<string, AssetRequest>());
        }

        private async void CompleteAndCache<TAsset>(string path, AssetRequest<TAsset> request)
            where TAsset : UnityEngine.Object
        {
            try
            {
                TAsset result = await request.Task;
                if (request.Status == AssetRequestStatus.Succeeded && result != null)
                {
                    assetCached[typeof(TAsset)][path] = result;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void OnActiveSceneChanged(Scene previousActiveScene, Scene newActiveScene)
        {
            UnloadAll();
        }
    }
}
