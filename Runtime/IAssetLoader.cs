namespace Dreamy.Assets
{
    internal interface IAssetLoader
    {
        AssetRequest<TAsset> Load<TAsset>(string address) where TAsset : UnityEngine.Object;

        AssetRequest<TAsset> LoadAsync<TAsset>(string address) where TAsset : UnityEngine.Object;

        void Release(AssetRequest request);

        void ReleaseAll();
    }
}
