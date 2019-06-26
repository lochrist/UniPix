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
        ExampleDockingAndSplitView();
    }

    [UsedImplicitly, RoleProvider(k_RoleName, ProcessEvent.UMP_EVENT_AFTER_DOMAIN_RELOAD)]
    private static void AfterDomainReloadPix()
    {
        Debug.Log("Uni Pix Domain Reload");
    }
    #endregion

    private static void MainViewFullScreen()
    {
        
    }

    [MenuItem("Layout/Project Browser With Docking")]
    private static void ProjectBrowserWithDocking()
    {

    }

    private static void ExampleDockingAndSplitView()
    {
        float width = 1200;
        float height = 800;

        var mainSplitView = ScriptableObject.CreateInstance<SplitView>();

        var hierarchy = ScriptableObject.CreateInstance<SceneHierarchyWindow>();
        var da = ScriptableObject.CreateInstance<DockArea>();
        da.AddTab(hierarchy);
        mainSplitView.AddChild(da);

        var middleSplitView = ScriptableObject.CreateInstance<SplitView>();
        middleSplitView.vertical = true;

        var sceneView = ScriptableObject.CreateInstance<SceneView>();
        da = ScriptableObject.CreateInstance<DockArea>();
        da.AddTab(sceneView);
        middleSplitView.AddChild(da);

        var project = ScriptableObject.CreateInstance<ProjectBrowser>();
        var console = ScriptableObject.CreateInstance<ConsoleWindow>();
        da = ScriptableObject.CreateInstance<DockArea>();
        da.AddTab(console);
        da.AddTab(project);
        middleSplitView.AddChild(da);
        mainSplitView.AddChild(middleSplitView);

        middleSplitView.children[0].position = new Rect(0, 0, 0.6f * width, 0.6f * height);
        middleSplitView.children[1].position = new Rect(0, 0, 0.6f * width, 0.4f * height);

        var inspector = ScriptableObject.CreateInstance<InspectorWindow>();
        da = ScriptableObject.CreateInstance<DockArea>();
        da.AddTab(inspector);
        mainSplitView.AddChild(da);

        mainSplitView.children[0].position = new Rect(0, 0, 0.2f * width, height);
        mainSplitView.children[1].position = new Rect(0, 0, 0.6f * width, height);
        mainSplitView.children[2].position = new Rect(0, 0, 0.2f * width, height);

        var containerWindow = ScriptableObject.CreateInstance<ContainerWindow>();
        containerWindow.m_DontSaveToLayout = false;
        containerWindow.position = new Rect(100, 100, width, height);
        containerWindow.rootView = mainSplitView;
        containerWindow.rootView.position = new Rect(0, 0, mainSplitView.position.width, mainSplitView.position.height);

        containerWindow.Show(ShowMode.MainWindow, false, true, setFocus: true);
    }


    [MenuItem("Window/UniPix Isolated")]
    static void StartUniPixIsolated()
    {
        var args = new List<string>();
        args.Add("ump-window-title");
        args.Add("UniPix");
        // args.Add("ump-cap");
        // args.Add("main_window");
        args.Add("ump-cap");
        args.Add("menu_bar");
        // args.Add("editor-mode");
        // args.Add(k_Mode);

        ProcessService.LaunchSlave(k_RoleName, args.ToArray());
    }
}
