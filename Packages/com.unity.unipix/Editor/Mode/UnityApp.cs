using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

[InitializeOnLoad]
static class UnityApp
{
    struct WorkspaceInfo
    {
        public string name;
        public string path;

        public bool valid => !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(path);
        public string guid => Hash128.Compute(name + path).ToString();
    }

    const string workspaceConfigPaths = "UserSettings/workspaces.paths";
    const string lastWorkspaceFolderKey = "last_workspace_folder";

    static bool workspaceRegistered = false;
    static List<WorkspaceInfo> workspaces = new List<WorkspaceInfo>();

    static UnityApp()
    {
        LoadWorkspaces();

        WindowLayout.onLayoutLoaded += SetupLayout;
        AssetDatabase.beforeRefresh += RegisterWorkspaces;
        EditorApplication.updateMainWindowTitle += UpdateUnityAppTitle;
    }

    private static void LoadWorkspaces()
    {
        var workspacesRoots = AssetDatabase.GetRoots().Where(r => r.StartsWith("Workspaces"));
        foreach (var r in workspacesRoots)
            AddWorkspace(r);

        if (File.Exists(workspaceConfigPaths))
        {
            var paths = File.ReadAllLines(workspaceConfigPaths);
            foreach (var p in paths)
            {
                var parts = p.Split(';');
                if (parts.Length != 2)
                    continue;
                AddWorkspace(parts[0], parts[1]);
            }
        }
    }

    private static string GetUniqueName(string name, int digit = 0)
    {
        var dn = name;
        if (digit > 0)
            dn = $"{name}{digit}";
        var w = workspaces.FirstOrDefault(w => string.Equals(w.name, dn, StringComparison.OrdinalIgnoreCase));
        if (!w.valid)
            return name;

        return GetUniqueName(name, ++digit);
    }

    private static void RegisterWorkspaces()
    {
        if (workspaceRegistered)
            return;

        foreach (var w in workspaces)
            RegisterWorkspace(w);

        workspaceRegistered = true;
    }

    private static bool RegisterWorkspace(WorkspaceInfo w)
    {
        var wp = $"Workspaces/{w.name}";
        if (Directory.Exists(wp) || !Directory.Exists(w.path))
            return false;

        AssetDatabase.RegisterRedirectedAssetFolder("Workspaces", w.name, w.path, false, w.guid);
        return true;
    }

    private static void SetupLayout()
    {
        UpdateWorkspaces();
    }

    private static void UpdateUnityAppTitle(ApplicationTitleDescriptor desc)
    {
        desc.title = $"{(string)ModeService.GetModeDataSection("label")} {(string)ModeService.GetModeDataSection("version")}";
    }

    private static WorkspaceInfo AddWorkspace(string workspacePath)
    {
        return AddWorkspace(Path.GetFileName(workspacePath), workspacePath);
    }

    private static WorkspaceInfo AddWorkspace(string name, string workspacePath)
    {
        var ws = workspaces.FirstOrDefault(w => w.path == workspacePath);
        if (!ws.valid)
        {
            ws = new WorkspaceInfo { name = GetUniqueName(name), path = workspacePath };
            workspaces.Add(ws);
        }
        return ws;
    }

    [CommandHandler(nameof(PrintRoots), CommandHint.Menu)]
    internal static void PrintRoots(CommandExecuteContext c)
    {
        foreach (var r in AssetDatabase.GetRoots().OrderBy(r => r))
            Debug.Log($"{r} -> {new DirectoryInfo(r).FullName}");
    }

    [CommandHandler(nameof(ReloadMode), CommandHint.Menu)]
    internal static void ReloadMode(CommandExecuteContext c)
    {
        ModeService.Refresh(c);
        AssetDatabase.Refresh();
        EditorUtility.RequestScriptReload();
        EditorUtility.RebuildAllMenus();
    }

    [CommandHandler(nameof(BuildApp), CommandHint.Menu)]
    internal static void BuildApp(CommandExecuteContext c)
    {
        Debug.Log("BuildApp");
    }

    [MenuItem("Workspaces/Add...")]
    internal static void AddWorkspace()
    {
        var workspaceFolder = EditorPrefs.GetString(lastWorkspaceFolderKey, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        workspaceFolder = EditorUtility.OpenFolderPanel("Select workspace...", workspaceFolder, string.Empty);
        if (string.IsNullOrEmpty(workspaceFolder))
            return;
        EditorPrefs.SetString(lastWorkspaceFolderKey, workspaceFolder);
        var ws = AddWorkspace(workspaceFolder);
        if (RegisterWorkspace(ws))
            UpdateWorkspaces();
    }

    private static void UpdateWorkspaces()
    {
        AssetDatabase.Refresh();
        Menu.RemoveMenuItem("Workspaces");

        var wpaths = new StringBuilder();
        Menu.AddMenuItem($"Workspaces/Add...", string.Empty, false, 0, AddWorkspace, null);
        foreach (var w in workspaces)
        {
            var workspaceName = w.name;
            Menu.AddMenuItem($"Workspaces/{workspaceName}/Select", string.Empty, false, 100, () => SelectWorkspace(w.path), null);
            Menu.AddMenuItem($"Workspaces/{workspaceName}/Remove", string.Empty, false, 101, () => RemoveWorkspace(workspaceName), null);

            wpaths.AppendLine($"{w.name};{w.path}");
        }
        File.WriteAllText(workspaceConfigPaths, wpaths.ToString());
        EditorUtility.Internal_UpdateAllMenus();
        SearchService.RefreshWindows();
    }

    private static void SelectWorkspace(string path)
    {
        EditorGUIUtility.PingObject(AssetDatabase.GetMainAssetInstanceID(path));
    }

    private static void RemoveWorkspace(string workspaceName)
    {
        var w = workspaces.FirstOrDefault(w => w.name == workspaceName);
        if (!w.valid)
            return;
        if (!workspaces.Remove(w))
            return;
        AssetDatabase.UnregisterRedirectedAssetFolder("Workspaces", w.name);
        UpdateWorkspaces();
    }
}
