using System;
using System.Threading.Tasks;
using UnityEngine;

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

    public sealed class AssetRequest<TAsset> : AssetRequest where TAsset : Object
    {
        internal AssetRequest(int requestId) : base(requestId)
        {
        }

        public TAsset Result { get; private set; }

        public Task<TAsset> Task { get; private set; }

        internal void SetTask(Task<TAsset> task)
        {
            Task = task;
        }

        internal void SetProgressFunc(Func<float> progressFunc)
        {
            ProgressFunc = progressFunc;
        }

        internal void SetResult(TAsset result)
        {
            Result = result;
        }

        internal void SetStatus(AssetRequestStatus status)
        {
            Status = status;
        }

        internal void SetOperationException(Exception ex)
        {
            OperationException = ex;
        }
    }
}
