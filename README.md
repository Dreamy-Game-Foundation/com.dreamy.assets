# Dreamy Assets

Package thuộc Dreamy Game Studio. Hướng dẫn dưới đây mô tả cấu trúc, cách cài vào project và tích hợp ở root/scene.

## Cài package

Dùng Unity 6000.0 trở lên. Sandbox đã tham chiếu package bằng `file:../LocalPackages/com.dreamy.assets`. Project khác dùng Package Manager > + > Install package from disk và chọn package.json, hoặc Git URL của repository nội bộ. Cài cả dependency Dreamy/Git vào manifest của game; version dependency không tự cấu hình registry riêng.

Dependency trực tiếp theo package.json:

- `com.unity.addressables` (2.7.2)
- `com.cysharp.unitask` (2.5.10)
- `com.dreamy.core` (1.1.2)

## Cấu trúc và asmdef

| Assembly | Reference | Phạm vi |
| --- | --- | --- |
| `Dreamy.Assets.Editor` |  | Chỉ Editor |
| `Dreamy.Assets.Runtime` | Dreamy.Core.Runtime, UniTask, Unity.Addressables, Unity.ResourceManager | Runtime |

Trong asmdef của game, thêm assembly chứa API trực tiếp sử dụng. Code bootstrap reference thêm Core/DataConfig/Datasave/Economy theo nhu cầu; code async reference UniTask. Code gọi type sample reference assembly sample. Giữ Editor reference trong asmdef Editor-only.

## Khởi tạo và tải asset

Runtime chứa AssetLoader và request/cache; Editor hỗ trợ thao tác trong Unity. AssetLoader dùng singleton, không có bước đăng ký installer riêng trong GameInstaller. Root quyết định thời điểm tải và giải phóng asset.

```csharp
using Dreamy.Assets;
using UnityEngine;

// Trong method async UniTask.
GameObject prefab = await AssetLoader.LoadAsync<GameObject>(
    PanelAddress.Home);
GameObject instance = Object.Instantiate(prefab, canvasTransform);
// Khi đã kết thúc mọi consumer:
Object.Destroy(instance);
AssetLoader.Unload<GameObject>(PanelAddress.Home);
```

canvasTransform là Transform Canvas của game. LoadAsync cache theo type/address và chia sẻ request đang chạy. Instance do Instantiate tạo phải Destroy riêng. Không unload prefab khi còn instance/consumer cần asset. Đổi scene không tự xóa cache; UnloadAll dành cho lúc kết thúc toàn bộ consumer.

RequestAsync<T> trả request có Progress, IsDone và Task để hiển thị tiến độ. LoadSprite(atlasAddress, spriteName) tải sprite từ atlas. LoadResource<T>(path) dùng key Resources không có phần mở rộng; đây là API riêng, không tự thay cho address sai.
## Sample

Manifest hiện không khai báo sample để import qua Package Manager.

## Addressables Group và class address

1. Lưu prefab/variant của game tại Assets/_Project/Prefabs/Panel/HomePanel.prefab. Với UIPanel, root phải có subclass tương ứng.
2. Mở Window > Asset Management > Addressables > Groups; tạo settings nếu chưa có.
3. Tạo group UI Panels và kéo prefab vào group.
4. Đặt cột Address thành Panel/HomePanel.prefab.
5. Tạo class dùng chung trong game:

```csharp
public static class PanelAddress
{
    public const string Home = "Panel/HomePanel.prefab";
    public const string Current = "Panel/HomePanel.prefab";
}
```

Đường dẫn asset trên disk và address là hai giá trị riêng. Address do bạn đặt, constant phải khớp chính xác cột Address. Tên group không phải key tải. HomePanel là ví dụ subclass do game tự tạo.

Dùng AssetLoader.LoadAsync<GameObject>(PanelAddress.Current), instantiate dưới Canvas rồi bind integration. Nếu dùng PanelManager, cài thêm Dreamy UI; Assets không tự quản lý presenter hay animation.

Build Addressables content cho target trước khi thử player. AssetLoader cache prefab; đóng panel không tự unload cache. Chỉ unload sau khi mọi instance/consumer đã kết thúc.
