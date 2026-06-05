using System.Threading.Tasks;
using UnityEngine;

namespace Dreamy.Assets.Samples
{
    public sealed class AddressableLoadExample : MonoBehaviour
    {
        [SerializeField] private string prefabAddress;

        private GameObject spawnedInstance;

        private async void Start()
        {
            await SpawnAsync();
        }

        private void OnDestroy()
        {
            if (spawnedInstance != null)
            {
                Destroy(spawnedInstance);
            }
        }

        private async Task SpawnAsync()
        {
            if (string.IsNullOrWhiteSpace(prefabAddress))
            {
                return;
            }

            GameObject prefab = await AssetLoader.LoadAsync<GameObject>(prefabAddress);
            spawnedInstance = Instantiate(prefab, transform.position, transform.rotation);
        }
    }
}
