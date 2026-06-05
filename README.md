# com.dreamy.assets

Addressables-based asset loading package for Dreamy internal Unity projects.

The v0.1 API intentionally follows the current project `Assets/_BaseSource/Base.LoadAsset` style: `LiveSingleton`-backed `AssetLoader`, typed cache, UniTask request progress, sprite atlas helper, `Resources` fallback, and scene-change cache cleanup. It removes Odin and keeps Addressables release safer than the old base implementation.

## Requirements

- Unity 6000.0+
- `com.dreamy.core`
- UniTask
- `com.unity.addressables`

When using private Git URL packages, keep Core, UniTask, and Addressables in the game template manifest so every project resolves the same version.

## Install

```json
{
  "dependencies": {
    "com.unity.addressables": "2.7.2",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.dreamy.core": "https://github.com/Dreamy-Game-Foundation/com.dreamy.core.git#v2.0.0",
    "com.dreamy.assets": "https://github.com/Dreamy-Game-Foundation/com.dreamy.assets.git#v0.1.0"
  }
}
```

## Usage

```csharp
AudioClip clip = await AssetLoader.LoadAsync<AudioClip>("sfx_click");
audioSource.PlayOneShot(clip);

GameObject prefab = await AssetLoader.LoadAsync<GameObject>("enemy_prefab");
GameObject instance = Object.Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
```

Use the request form when a loading UI needs progress:

```csharp
AssetRequest<GameObject> request = AssetLoader.RequestAsync<GameObject>("enemy_prefab");
while (!request.IsDone)
{
    loadingBar.value = request.Progress;
    await UniTask.Yield();
}

GameObject prefab = await request.Task;
```

Load sprites from an addressable atlas:

```csharp
Sprite icon = await AssetLoader.LoadSprite("resource_atlas", "Gold");
```

Load old `Resources` assets when migrating gradually:

```csharp
TweenSettings settings = AssetLoader.LoadResource<TweenSettings>("TweenBaseSettings");
```

## Ownership Rules

- `LoadAsync<T>` caches Addressables assets by type and address.
- `LoadResource<T>` caches `Resources` assets by type and address.
- `Unload<T>(address)` releases one Addressables asset.
- `UnloadAll()` releases all Addressables assets and clears cache.
- Cache is automatically cleared when active scene changes.
- Instantiated prefabs are normal Unity instances; destroy them with `Destroy(instance)`.
- Do not call `Resources.UnloadUnusedAssets()` from gameplay hot paths.

## Scope

This package owns runtime asset loading. Project templates own dependency version wiring, Addressables group setup, labels, profiles, and remote content policy.
