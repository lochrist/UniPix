using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace UniPix
{

    public static class PixRoleProvider
    {
        const string k_RoleName = "unipix";
        const string k_Mode = "unipix";
        const string k_PixPath = "pix-path";

        #region Slave

        [UsedImplicitly, UnityEditor.MPE.RoleProvider(k_RoleName, UnityEditor.MPE.ProcessEvent.Create)]
        private static void CreatePixProcess()
        {
            Debug.Log("Uni Pix created");
        }

        [UsedImplicitly, UnityEditor.MPE.RoleProvider(k_RoleName, UnityEditor.MPE.ProcessEvent.Initialize)]
        private static void InitializePixProcess()
        {
            Debug.Log("Uni Pix Initialize");

            if (Application.HasARGV(k_PixPath))
            {
                var prefabPath = Application.GetValueForARGV(k_PixPath).Replace("\\", "/");
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(prefabPath);
                if (PixUtils.IsValidPixSource(obj))
                {
                    PixCommands.EditInPix(new [] {obj});
                }
            }
        }

        [UsedImplicitly, UnityEditor.MPE.RoleProvider(k_RoleName, UnityEditor.MPE.ProcessEvent.AfterDomainReload)]
        private static void AfterDomainReloadPix()
        {
            Debug.Log("Uni Pix Domain Reload");
            EditorApplication.updateMainWindowTitle += titleDescriptor =>
            {
                titleDescriptor.title = "UniPix 0.42";
            };
        }

        #endregion

        public static void StartUniPixIsolated(string path = null)
        {
            var args = new List<string>();
            args.Add("ump-window-title");
            args.Add("UniPix");
            args.Add("ump-cap");
            args.Add("main_window");
            args.Add("ump-cap");
            args.Add("menu_bar");
            args.Add("editor-mode");
            args.Add(k_Mode);

            if (path != null)
            {
                args.Add(k_PixPath);
                args.Add(path);
            }

            UnityEditor.MPE.ProcessService.Launch(k_RoleName, args.ToArray());
        }

        [MenuItem("Window/UniPix (isolated)", false, 1105)]
        static void StartIsolated()
        {
            StartUniPixIsolated();
        }

        [UsedImplicitly, MenuItem("Assets/Open in UniPix (isolated)", false, 180005)]
        private static void OpenInPix()
        {
            if (!PixUtils.IsValidPixSource(Selection.activeObject))
                return;

            var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(assetPath))
                return;

            StartUniPixIsolated(assetPath);
        }

        [UsedImplicitly, MenuItem("Assets/Open in UniPix (isolated)", true, 180005)]
        private static bool OpenInPixValidate()
        {
            return PixUtils.IsValidPixSource(Selection.activeObject);
        }
    }

}