using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dreamy.Assets
{
    internal sealed class ResourceLoader : IAssetLoader
    {
        private int nextRequestId;

        public AssetRequest<TAsset> Load<TAsset>(string address) where TAsset : Object
        {
            int requestId = nextRequestId++;
            TAsset result = Resources.Load<TAsset>(address);

            AssetRequest<TAsset> request = new AssetRequest<TAsset>(requestId);
            request.SetTask(Task.FromResult(result));
            request.SetProgressFunc(() => 1f);
            request.SetResult(result);
            request.SetStatus(result != null ? AssetRequestStatus.Succeeded : AssetRequestStatus.Failed);

            if (result == null)
            {
                request.SetOperationException(new InvalidOperationException(
                    $"Requested resource asset was not found. Key: {address}."));
            }

            return request;
        }

        public AssetRequest<TAsset> LoadAsync<TAsset>(string address) where TAsset : Object
        {
            int requestId = nextRequestId++;
            ResourceRequest resourceRequest = Resources.LoadAsync<TAsset>(address);

            AssetRequest<TAsset> request = new AssetRequest<TAsset>(requestId);
            request.SetTask(CompleteAsync(request, resourceRequest, address));
            request.SetProgressFunc(() => resourceRequest.progress);
            return request;
        }

        public void Release(AssetRequest request)
        {
        }

        public void ReleaseAll()
        {
        }

        private static async Task<TAsset> CompleteAsync<TAsset>(
            AssetRequest<TAsset> request,
            ResourceRequest resourceRequest,
            string address) where TAsset : Object
        {
            while (!resourceRequest.isDone)
            {
                await Task.Yield();
            }

            TAsset result = resourceRequest.asset as TAsset;
            request.SetResult(result);
            request.SetStatus(result != null ? AssetRequestStatus.Succeeded : AssetRequestStatus.Failed);

            if (result == null)
            {
                request.SetOperationException(new InvalidOperationException(
                    $"Requested resource asset was not found. Key: {address}."));
            }

            return result;
        }
    }
}
