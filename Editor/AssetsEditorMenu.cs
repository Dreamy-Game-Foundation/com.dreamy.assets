using UnityEditor;

namespace Dreamy.Assets.Editor
{
    public static class AssetsEditorMenu
    {
        private const string MenuRoot = "Tools/Dreamy/Assets/";

        [MenuItem(MenuRoot + "Open Addressables Groups")]
        public static void OpenAddressablesGroups()
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
        }

        [MenuItem(MenuRoot + "Open Addressables Profiles")]
        public static void OpenAddressablesProfiles()
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Profiles");
        }
    }
}
