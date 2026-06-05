using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Dreamy.Assets
{
    internal sealed class AddressableLoader : IAssetLoader
    {
        private int nextRequestId;

        private readonly Dictionary<int, AsyncOperationHandle> requests =
            new Dictionary<int, AsyncOperationHandle>();

        public AssetRequest<TAsset> Load<TAsset>(string address) where TAsset : Object
        {
            int requestId = nextRequestId++;
            AsyncOperationHandle<TAsset> operationHandle = Addressables.LoadAssetAsync<TAsset>(address);
            operationHandle.WaitForCompletion();
            requests.Add(requestId, operationHandle);

            AssetRequest<TAsset> request = new AssetRequest<TAsset>(requestId);
            ApplyCompletedOperation(request, operationHandle);
            request.SetTask(Task.FromResult(operationHandle.Result));
            request.SetProgressFunc(() => operationHandle.IsValid() ? operationHandle.PercentComplete : 1f);
            return request;
        }

        public AssetRequest<TAsset> LoadAsync<TAsset>(string address) where TAsset : Object
        {
            int requestId = nextRequestId++;
            AsyncOperationHandle<TAsset> operationHandle = Addressables.LoadAssetAsync<TAsset>(address);
            requests.Add(requestId, operationHandle);

            AssetRequest<TAsset> request = new AssetRequest<TAsset>(requestId);
            request.SetTask(CompleteAsync(request, operationHandle));
            request.SetProgressFunc(() => operationHandle.IsValid() ? operationHandle.PercentComplete : 0f);
            return request;
        }

        public void Release(AssetRequest request)
        {
            if (request == null || !requests.Remove(request.RequestId, out AsyncOperationHandle operationHandle))
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
            foreach (AsyncOperationHandle operationHandle in requests.Values)
            {
                if (operationHandle.IsValid())
                {
                    Addressables.Release(operationHandle);
                }
            }

            requests.Clear();
        }

        private static async Task<TAsset> CompleteAsync<TAsset>(
            AssetRequest<TAsset> request,
            AsyncOperationHandle<TAsset> operationHandle) where TAsset : Object
        {
            await operationHandle.Task;
            ApplyCompletedOperation(request, operationHandle);
            return operationHandle.Result;
        }

        private static void ApplyCompletedOperation<TAsset>(
            AssetRequest<TAsset> request,
            AsyncOperationHandle<TAsset> operationHandle) where TAsset : Object
        {
            request.SetResult(operationHandle.Result);
            request.SetStatus(operationHandle.Status == AsyncOperationStatus.Succeeded
                ? AssetRequestStatus.Succeeded
                : AssetRequestStatus.Failed);
            request.SetOperationException(operationHandle.OperationException);
        }
    }
}
