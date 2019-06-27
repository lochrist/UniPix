using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UniPix;
using UnityEditor;
using UnityEngine;

public class LayoutUtils
{
    public static void MainViewFullScreen()
    {

    }

    [MenuItem("Layout/Pix Browser No Docking")]
    public static void PixBrowserNoDocking()
    {
        ContainerWindow mainContainerWindow = null;
        var containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
        foreach (ContainerWindow window in containers)
        {
            if (window.showMode != ShowMode.MainWindow)
                continue;

            mainContainerWindow = window;
            break;
        }

        if (mainContainerWindow == null)
            return;

        try
        {
            ContainerWindow.SetFreezeDisplay(true);

            var width = mainContainerWindow.position.width;
            var height = mainContainerWindow.position.height;

            SplitView splitView = ScriptableObject.CreateInstance<SplitView>();
            splitView.vertical = false;

            {
                // Project Browser View
                var hostView = ScriptableObject.CreateInstance<SplitHostView>();
                hostView.SetActualViewInternal(ScriptableObject.CreateInstance<ProjectBrowser>(), true);
                splitView.AddChild(hostView);
            }

            // Pix
            {
                var hostView = ScriptableObject.CreateInstance<SplitHostView>();
                hostView.SetActualViewInternal(ScriptableObject.CreateInstance<PixEditor>(), true);
                splitView.AddChild(hostView);
            }

            splitView.children[0].position = new Rect(0, 0, width * 0.3f, height);
            splitView.children[1].position = new Rect(0, 0, width * 0.7f, height);

            var main = ScriptableObject.CreateInstance<PixMainView>();
            main.AddChild(splitView);

            splitView.Reflow();

            ScriptableObject.DestroyImmediate(mainContainerWindow.rootView, true);

            mainContainerWindow.rootView = main;
            mainContainerWindow.rootView.position = new Rect(0, 0, width, height);
            mainContainerWindow.DisplayAllViews();
        }
        finally
        {
            ContainerWindow.SetFreezeDisplay(false);
        }
    }

    [MenuItem("Layout/Pix Browser With Docking")]
    public static void PixBrowserWithDocking()
    {
        ContainerWindow mainContainerWindow = null;
        var containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
        foreach (ContainerWindow window in containers)
        {
            if (window.showMode != ShowMode.MainWindow)
                continue;

            mainContainerWindow = window;
            break;
        }

        if (mainContainerWindow == null)
            return;

        try
        {
            ContainerWindow.SetFreezeDisplay(true);

            var width = mainContainerWindow.position.width;
            var height = mainContainerWindow.position.height;

            SplitView splitView = ScriptableObject.CreateInstance<SplitView>();
            splitView.vertical = false;

            {
                // Project Browser View
                var da = ScriptableObject.CreateInstance<DockArea>();
                da.AddTab(ScriptableObject.CreateInstance<ProjectBrowser>());
                da.AddTab(ScriptableObject.CreateInstance<ConsoleWindow>());
                splitView.AddChild(da);
            }

            // Pix
            {
                var da = ScriptableObject.CreateInstance<DockArea>();
                da.AddTab(ScriptableObject.CreateInstance<PixEditor>());
                splitView.AddChild(da);
            }

            var main = ScriptableObject.CreateInstance<PixMainView>();
            main.AddChild(splitView);

            splitView.children[0].position = new Rect(0, 0, width * 0.4f, height);
            splitView.children[1].position = new Rect(0, 0, width * 0.6f, height);

            splitView.Reflow();

            ScriptableObject.DestroyImmediate(mainContainerWindow.rootView, true);

            mainContainerWindow.rootView = main;
            mainContainerWindow.rootView.position = new Rect(0, 0, width, height);
            mainContainerWindow.DisplayAllViews();
        }
        finally
        {
            ContainerWindow.SetFreezeDisplay(false);
        }
    }

    [MenuItem("Layout/Close Left")]
    public static void CloseLeft()
    {
        ContainerWindow mainContainerWindow = null;
        var containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
        foreach (ContainerWindow window in containers)
        {
            if (window.showMode != ShowMode.MainWindow)
                continue;

            mainContainerWindow = window;
            break;
        }

        if (mainContainerWindow == null)
            return;

        try
        {
            ContainerWindow.SetFreezeDisplay(true);
            var pixMainView = (PixMainView)mainContainerWindow.rootView;
            var pixWindow = ((HostView)pixMainView.children[0].children[1]).actualView;
            var view = ScriptableObject.CreateInstance<HostView>();
            view.SetActualViewInternal(pixWindow, true);

            var previousRootSize = mainContainerWindow.rootView.position;
            view.children[0].position = previousRootSize;
            view.position = previousRootSize;
            view.Reflow();

            ScriptableObject.DestroyImmediate(mainContainerWindow.rootView.children[0], true);
            ScriptableObject.DestroyImmediate(mainContainerWindow.rootView, true);

            mainContainerWindow.rootView = view;
            mainContainerWindow.rootView.position = previousRootSize;
            mainContainerWindow.DisplayAllViews();
        }
        finally
        {
            ContainerWindow.SetFreezeDisplay(false);
        }
    }

    [MenuItem("Layout/Pix No Docking")]
    public static void CreateSinglePix()
    {
        ContainerWindow mainContainerWindow = null;
        var containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
        foreach (ContainerWindow window in containers)
        {
            if (window.showMode != ShowMode.MainWindow)
                continue;

            mainContainerWindow = window;
            break;
        }

        if (mainContainerWindow == null)
            return;

        try
        {
            ContainerWindow.SetFreezeDisplay(true);
            var pixWindow = ScriptableObject.CreateInstance<PixEditor>();
            var view = ScriptableObject.CreateInstance<HostView>();
            view.SetActualViewInternal(pixWindow, true);

            ScriptableObject.DestroyImmediate(mainContainerWindow.rootView, true);

            mainContainerWindow.rootView = view;
            mainContainerWindow.DisplayAllViews();
        }
        finally
        {
            ContainerWindow.SetFreezeDisplay(false);
        }
    }

    [MenuItem("Layout/Project Bundle")]
    public static void CreateProject()
    {
        var projectBrowser = ScriptableObject.CreateInstance<ProjectBrowser>();
        var console = ScriptableObject.CreateInstance<ConsoleWindow>();
        var view = ScriptableObject.CreateInstance<SplitView>();
        var da = ScriptableObject.CreateInstance<DockArea>();
        da.AddTab(console);
        da.AddTab(projectBrowser);
        view.AddChild(da);

        const float kWidth = 500;
        const float kHeight = 400;

        view.children[0].position = new Rect(0, 0, kWidth, kHeight);

        var containerWindow = ScriptableObject.CreateInstance<ContainerWindow>();
        containerWindow.m_DontSaveToLayout = false;
        containerWindow.position = new Rect(100, 100, kWidth, kHeight);
        containerWindow.rootView = view;
        containerWindow.rootView.position = new Rect(0, 0, view.position.width, view.position.height);

        containerWindow.Show(ShowMode.NormalWindow, false, true, setFocus: true);
    }

    [UsedImplicitly, MenuItem("Layout/Save Pix Layout", false, 14005)]
    public static void SavePixLayout()
    {
        WindowLayout.SaveWindowLayout("Packages/com.unity.unipix/Editor/Layouts/Pix.wlt");
    }

    [UsedImplicitly, MenuItem("Layout/Save BrowsePix Layout", false, 14010)]
    public static void SaveBrowseLayout()
    {
        WindowLayout.SaveWindowLayout("Packages/com.unity.unipix/Editor/Layouts/Browse.wlt");
    }

    [UsedImplicitly, MenuItem("Layout/Save Debug Layout", false, 14015)]
    public static void SaveDebugLayout()
    {
        WindowLayout.SaveWindowLayout("Packages/com.unity.unipix/Editor/Layouts/Debug.wlt");
    }

    public static void ExampleDockingAndSplitView()
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

    static void ExampleCreatePix()
    {
        const float x = 300f;
        const float y = 90.0f;
        const float width = 800;
        const float height = 600;

        var pixEditor = ScriptableObject.CreateInstance<PixEditor>();
        var view = ScriptableObject.CreateInstance<HostView>();
        view.SetActualViewInternal(pixEditor, true);
        view.autoRepaintOnSceneChange = false;

        var cw = ScriptableObject.CreateInstance<ContainerWindow>();
        cw.m_DontSaveToLayout = true;
        cw.position = new Rect(x, y, width, height);
        cw.rootView = view;
        cw.rootView.position = new Rect(0, 0, cw.position.width, cw.position.height);
        cw.Show(ShowMode.MainWindow, true, false, true);
    }


}
