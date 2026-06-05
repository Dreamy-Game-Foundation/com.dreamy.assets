using System;
using Cysharp.Threading.Tasks;

namespace Dreamy.Assets
{
    public abstract class AssetRequest
    {
        protected AssetRequest(int requestId)
        {
            RequestId = requestId;
        }

        public int RequestId { get; }

        public AssetRequestStatus Status { get; protected set; }

        public bool IsDone => Status != AssetRequestStatus.None;

        public Exception OperationException { get; protected set; }

        protected Func<float> ProgressFunc { get; set; }

        public float Progress => ProgressFunc != null ? ProgressFunc() : 0f;
    }

    public sealed class AssetRequest<TAsset> : AssetRequest, IAssetRequest<TAsset> where TAsset : UnityEngine.Object
    {
        public AssetRequest(int requestId) : base(requestId)
        {
        }

        public TAsset Result { get; private set; }

        public UniTask<TAsset> Task { get; private set; }

        void IAssetRequest<TAsset>.SetTask(UniTask<TAsset> task)
        {
            Task = task;
        }

        void IAssetRequest<TAsset>.SetProgressFunc(Func<float> progressFunc)
        {
            ProgressFunc = progressFunc;
        }

        void IAssetRequest<TAsset>.SetResult(TAsset result)
        {
            Result = result;
        }

        void IAssetRequest<TAsset>.SetStatus(AssetRequestStatus status)
        {
            Status = status;
        }

        void IAssetRequest<TAsset>.SetOperationException(Exception ex)
        {
            OperationException = ex;
        }
    }
}
