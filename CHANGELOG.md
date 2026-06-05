# Changelog

## [0.1.0] - 2026-06-06

- Added Addressables asset loader package using the local `Base.LoadAsset` style.
- Added static `AssetLoader.LoadAsync<T>`, `RequestAsync<T>`, `LoadResource<T>`, `LoadSprite`, `Unload<T>`, and `UnloadAll`.
- Added request progress/status model inspired by local `Base.LoadAsset`.
- Added typed asset cache and scene-change cleanup.
- Added `LiveSingleton<AssetLoader>` and UniTask request flow to match local base usage.
- Fixed old base release behavior by using `Addressables.Release` for loaded asset handles.
- Added editor menu shortcuts for Addressables windows.
