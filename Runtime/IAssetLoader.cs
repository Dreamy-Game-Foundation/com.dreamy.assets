using UnityEngine;

namespace Dreamy.Assets
{
    internal interface IAssetLoader
    {
        AssetRequest<TAsset> Load<TAsset>(string address) where TAsset : Object;

        AssetRequest<TAsset> LoadAsync<TAsset>(string address) where TAsset : Object;

        void Release(AssetRequest request);

        void ReleaseAll();
    }
}
