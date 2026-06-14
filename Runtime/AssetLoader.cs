using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Dreamy.Core;
using UnityEngine;
using UnityEngine.U2D;

namespace Dreamy.Assets
{
    public sealed class AssetLoader : LiveSingleton<AssetLoader>
    {
        private readonly Dictionary<Type, Dictionary<string, UnityEngine.Object>> assetCached =
            new Dictionary<Type, Dictionary<string, UnityEngine.Object>>();

        private readonly Dictionary<Type, Dictionary<string, AssetRequest>> requestCached =
            new Dictionary<Type, Dictionary<string, AssetRequest>>();

        private readonly Dictionary<Type, Dictionary<string, IAssetLoader>> loaderCached =
            new Dictionary<Type, Dictionary<string, IAssetLoader>>();

        private readonly IAssetLoader addressableLoader = new AddressableLoader();
        private readonly IAssetLoader resourceLoader = new ResourceLoader();

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
                AssetRequest<TAsset> typedRequest = (AssetRequest<TAsset>)cachedRequest;
                TAsset cachedResult = await typedRequest.Task;
                if (typedRequest.Status == AssetRequestStatus.Failed || cachedResult == null)
                {
                    Instance.RemoveRequest(type, path, typedRequest, true);
                    throw typedRequest.OperationException ?? new InvalidOperationException(
                        $"Failed to load addressable asset. Key: {path}.");
                }

                if (!Instance.IsActiveRequest(type, path, typedRequest, requestsOfType))
                {
                    throw new OperationCanceledException(
                        $"Asset request was unloaded before completion. Key: {path}.");
                }

                return cachedResult;
            }

            AssetRequest<TAsset> request = Instance.addressableLoader.LoadAsync<TAsset>(path);
            requestsOfType[path] = request;
            Instance.loaderCached[type][path] = Instance.addressableLoader;

            try
            {
                TAsset result = await request.Task;
                if (request.Status == AssetRequestStatus.Failed || result == null)
                {
                    throw request.OperationException ?? new InvalidOperationException(
                        $"Failed to load addressable asset. Key: {path}.");
                }

                if (!Instance.IsActiveRequest(type, path, request, requestsOfType))
                {
                    throw new OperationCanceledException(
                        $"Asset request was unloaded before completion. Key: {path}.");
                }

                if (requestsOfType.TryGetValue(path, out AssetRequest activeRequest) &&
                    ReferenceEquals(activeRequest, request))
                {
                    assetsOfType[path] = result;
                }

                return result;
            }
            catch
            {
                Instance.RemoveRequest(type, path, request, true);
                throw;
            }
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
            Instance.loaderCached[type][path] = Instance.addressableLoader;
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

            if (requestsOfType.ContainsKey(path))
            {
                throw new InvalidOperationException(
                    $"Asset key '{path}' is already being loaded asynchronously as {type.Name}.");
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
                Instance.loaderCached[type].Add(path, Instance.resourceLoader);
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

            if (requestsOfType.TryGetValue(path, out AssetRequest request))
            {
                Instance.RemoveRequest(type, path, request, true);
            }
        }

        public static void UnloadAll()
        {
            Instance.addressableLoader.ReleaseAll();
            Instance.resourceLoader.ReleaseAll();
            Instance.assetCached.Clear();
            Instance.requestCached.Clear();
            Instance.loaderCached.Clear();
        }

        private void EnsureTypeCache(Type type)
        {
            if (assetCached.ContainsKey(type))
            {
                return;
            }

            assetCached.Add(type, new Dictionary<string, UnityEngine.Object>());
            requestCached.Add(type, new Dictionary<string, AssetRequest>());
            loaderCached.Add(type, new Dictionary<string, IAssetLoader>());
        }

        private async void CompleteAndCache<TAsset>(string path, AssetRequest<TAsset> request)
            where TAsset : UnityEngine.Object
        {
            try
            {
                TAsset result = await request.Task;
                if (request.Status == AssetRequestStatus.Succeeded && result != null)
                {
                    Type type = typeof(TAsset);
                    if (requestCached.TryGetValue(
                            type,
                            out Dictionary<string, AssetRequest> requestsOfType) &&
                        IsActiveRequest(type, path, request, requestsOfType))
                    {
                        assetCached[type][path] = result;
                    }

                    return;
                }

                RemoveRequest(typeof(TAsset), path, request, true);
            }
            catch (Exception ex)
            {
                RemoveRequest(typeof(TAsset), path, request, true);
                Debug.LogException(ex);
            }
        }

        private void RemoveRequest(
            Type type,
            string path,
            AssetRequest expectedRequest,
            bool release)
        {
            if (!requestCached.TryGetValue(type, out Dictionary<string, AssetRequest> requestsOfType) ||
                !requestsOfType.TryGetValue(path, out AssetRequest activeRequest) ||
                !ReferenceEquals(activeRequest, expectedRequest))
            {
                return;
            }

            if (release &&
                loaderCached.TryGetValue(type, out Dictionary<string, IAssetLoader> loadersOfType) &&
                loadersOfType.TryGetValue(path, out IAssetLoader loader))
            {
                loader.Release(activeRequest);
            }

            requestsOfType.Remove(path);
            assetCached[type].Remove(path);
            loaderCached[type].Remove(path);
        }

        private bool IsActiveRequest(
            Type type,
            string path,
            AssetRequest request,
            Dictionary<string, AssetRequest> expectedTypeCache)
        {
            return requestCached.TryGetValue(
                       type,
                       out Dictionary<string, AssetRequest> currentTypeCache) &&
                   ReferenceEquals(currentTypeCache, expectedTypeCache) &&
                   currentTypeCache.TryGetValue(path, out AssetRequest activeRequest) &&
                   ReferenceEquals(activeRequest, request);
        }
    }
}
