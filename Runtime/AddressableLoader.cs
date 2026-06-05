using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Dreamy.Assets
{
    internal sealed class AddressableLoader : IAssetLoader
    {
        private int nextRequestId;

        private readonly Dictionary<int, AsyncOperationHandle> requestDict =
            new Dictionary<int, AsyncOperationHandle>();

        public AssetRequest<TAsset> Load<TAsset>(string address) where TAsset : UnityEngine.Object
        {
            int requestId = nextRequestId++;
            AsyncOperationHandle<TAsset> operationHandle = Addressables.LoadAssetAsync<TAsset>(address);
            operationHandle.WaitForCompletion();
            requestDict.Add(requestId, operationHandle);

            AssetRequest<TAsset> request = new AssetRequest<TAsset>(requestId);
            IAssetRequest<TAsset> setter = request;
            setter.SetTask(UniTask.FromResult(operationHandle.Result));
            setter.SetProgressFunc(() => operationHandle.IsValid() ? operationHandle.PercentComplete : 1f);
            setter.SetResult(operationHandle.Result);
            setter.SetStatus(operationHandle.Status == AsyncOperationStatus.Succeeded
                ? AssetRequestStatus.Succeeded
                : AssetRequestStatus.Failed);
            setter.SetOperationException(operationHandle.OperationException);
            return request;
        }

        public AssetRequest<TAsset> LoadAsync<TAsset>(string address) where TAsset : UnityEngine.Object
        {
            int requestId = nextRequestId++;
            AsyncOperationHandle<TAsset> operationHandle = Addressables.LoadAssetAsync<TAsset>(address);
            requestDict.Add(requestId, operationHandle);

            AssetRequest<TAsset> request = new AssetRequest<TAsset>(requestId);
            IAssetRequest<TAsset> setter = request;
            UniTaskCompletionSource<TAsset> completionSource = new UniTaskCompletionSource<TAsset>();

            setter.SetTask(completionSource.Task);
            setter.SetProgressFunc(() => operationHandle.IsValid() ? operationHandle.PercentComplete : 0f);
            operationHandle.Completed += handle =>
            {
                setter.SetResult(handle.Result);
                setter.SetStatus(handle.Status == AsyncOperationStatus.Succeeded
                    ? AssetRequestStatus.Succeeded
                    : AssetRequestStatus.Failed);
                setter.SetOperationException(handle.OperationException);
                completionSource.TrySetResult(handle.Result);
            };

            return request;
        }

        public void Release(AssetRequest request)
        {
            if (request == null || !requestDict.Remove(request.RequestId, out AsyncOperationHandle operationHandle))
            {
                return;
            }

            if (operationHandle.IsValid())
            {
                Addressables.Release(operationHandle);
            }
        }

        public void ReleaseAll()
        {
            foreach (AsyncOperationHandle operationHandle in requestDict.Values)
            {
                if (operationHandle.IsValid())
                {
                    Addressables.Release(operationHandle);
                }
            }

            requestDict.Clear();
        }
    }
}
