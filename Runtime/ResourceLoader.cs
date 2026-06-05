using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Dreamy.Assets
{
    internal sealed class ResourceLoader : IAssetLoader
    {
        private int nextRequestId;

        public AssetRequest<TAsset> Load<TAsset>(string address) where TAsset : UnityEngine.Object
        {
            int requestId = nextRequestId++;
            AssetRequest<TAsset> request = new AssetRequest<TAsset>(requestId);
            IAssetRequest<TAsset> setter = request;
            TAsset result = Resources.Load<TAsset>(address);

            setter.SetTask(UniTask.FromResult(result));
            setter.SetProgressFunc(() => 1f);
            setter.SetResult(result);
            setter.SetStatus(result != null ? AssetRequestStatus.Succeeded : AssetRequestStatus.Failed);

            if (result == null)
            {
                setter.SetOperationException(new InvalidOperationException(
                    $"Requested resource asset was not found. Key: {address}."));
            }

            return request;
        }

        public AssetRequest<TAsset> LoadAsync<TAsset>(string address) where TAsset : UnityEngine.Object
        {
            int requestId = nextRequestId++;
            AssetRequest<TAsset> request = new AssetRequest<TAsset>(requestId);
            IAssetRequest<TAsset> setter = request;
            UniTaskCompletionSource<TAsset> completionSource = new UniTaskCompletionSource<TAsset>();

            ResourceRequest resourceRequest = Resources.LoadAsync<TAsset>(address);
            setter.SetTask(completionSource.Task);
            setter.SetProgressFunc(() => resourceRequest.progress);
            resourceRequest.completed += _ =>
            {
                TAsset result = resourceRequest.asset as TAsset;
                setter.SetResult(result);
                setter.SetStatus(result != null ? AssetRequestStatus.Succeeded : AssetRequestStatus.Failed);

                if (result == null)
                {
                    setter.SetOperationException(new InvalidOperationException(
                        $"Requested resource asset was not found. Key: {address}."));
                }

                completionSource.TrySetResult(result);
            };

            return request;
        }

        public void Release(AssetRequest request)
        {
        }

        public void ReleaseAll()
        {
        }
    }
}
