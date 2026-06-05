using System;
using Cysharp.Threading.Tasks;

namespace Dreamy.Assets
{
    internal interface IAssetRequest<TAsset> where TAsset : UnityEngine.Object
    {
        void SetTask(UniTask<TAsset> task);

        void SetProgressFunc(Func<float> progress);

        void SetResult(TAsset result);

        void SetStatus(AssetRequestStatus status);

        void SetOperationException(Exception ex);
    }
}
