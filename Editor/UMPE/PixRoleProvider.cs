using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.MPE;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PixRoleProvider
{
    const string k_RoleName = "unipix";
    const string k_Mode = "unipix";
    #region Slave
    [UsedImplicitly, RoleProvider(k_RoleName, ProcessEvent.UMP_EVENT_CREATE)]
    private static void CreatePixProcess()
    {
        Debug.Log("Uni Pix created");
    }

    [UsedImplicitly, RoleProvider(k_RoleName, ProcessEvent.UMP_EVENT_INITIALIZE)]
    private static void InitializePixProcess()
    {
        Debug.Log("Uni Pix Initialize");
    }

    [UsedImplicitly, RoleProvider(k_RoleName, ProcessEvent.UMP_EVENT_AFTER_DOMAIN_RELOAD)]
    private static void AfterDomainReloadPix()
    {
        Debug.Log("Uni Pix Domain Reload");
    }
    #endregion

    [MenuItem("Window/UniPix Isolated")]
    static void StartUniPixIsolated()
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

        ProcessService.LaunchSlave(k_RoleName, args.ToArray());
    }
}
