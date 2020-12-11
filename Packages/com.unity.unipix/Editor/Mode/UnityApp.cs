#define UNITY_APP
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

#if UNITY_APP
[InitializeOnLoad]
public static class UnityApp
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
    const string lastBuildAppFolderKey = "last_build_app_folder";

    static bool workspaceRegistered = false;
    static List<WorkspaceInfo> workspaces = new List<WorkspaceInfo>();

    static UnityApp()
    {
        LoadWorkspaces();

        WindowLayout.onLayoutLoaded += SetupLayout;
        AssetDatabase.beforeRefresh += RegisterWorkspaces;
        EditorApplication.updateMainWindowTitle += UpdateUnityAppTitle;
    }

    public static string[] GetAllRoots()
    {
        return AssetDatabase.GetRoots();
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
            UpdateWorkspaces();
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
        EditorUtility.RebuildAllMenus();
    }

    [CommandHandler(nameof(BuildApp), CommandHint.Menu)]
    internal static void BuildApp(CommandExecuteContext c)
    {
        var excludes = new string[0];
        if (ModeService.GetModeDataSection("build_exclude_patterns") is IList<object> l)
            excludes = l.Cast<string>().ToArray();

        var outputDir = Path.Combine(Path.GetDirectoryName(EditorApplication.applicationPath), "..");
        outputDir = EditorPrefs.GetString(lastBuildAppFolderKey, outputDir);
        outputDir = EditorUtility.SaveFolderPanel("Select build output folder...", outputDir, string.Empty);
        if (string.IsNullOrEmpty(outputDir))
            return;
        EditorPrefs.SetString(lastBuildAppFolderKey, outputDir);

        outputDir = outputDir.Replace("\\", "/").Trim('/');
        var unityDir = Path.GetDirectoryName(EditorApplication.applicationPath).Replace("\\", "/").Trim('/');
        var appDir = Application.dataPath.Replace("/Assets", "").Replace("\\", "/").Trim('/');
        System.Threading.Tasks.Task.Run(() => BuildThread(unityDir, appDir, outputDir, excludes));
    }

    private static void BuildThread(string unityDir, string appDir, string buildDir, string[] excludes)
    {
        var progressId = Progress.Start("Unity App");
        Progress.SetPriority(progressId, Progress.Priority.Low);       

        try
        {
            var fileCount = 0;
            var totalBytes = 0L;
            var rgx = excludes.Select(e => new Regex(e, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled)).ToArray();

            CopyFiles(progressId, unityDir, buildDir, rgx, ref fileCount, ref totalBytes);
            CopyFiles(progressId, appDir, $"{buildDir}/UnityApp", rgx, ref fileCount, ref totalBytes);

            Debug.Log($"Build App Report: Updated {fileCount} files and copied {totalBytes / (1024L * 1024L)} mb");
            Progress.Finish(progressId);
        }
        catch (Exception ex)
        {
            Progress.SetDescription(progressId, ex.Message);
            Progress.Finish(progressId, Progress.Status.Failed);
            Debug.LogException(ex);
        }
    }

    private static void CopyFiles(int progressId, string sourceDir, string outputDir, Regex[] rgx, ref int fileCount, ref long totalBytes)
    {
        //Debug.Log($"Copying files {sourceDir} to {outputDir}");
        Progress.Report(progressId, -1f, "Gathering files...");
        var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);

        Progress.Report(progressId, 0f, "Copying files...");
        for (int i = 0; i < files.Length; ++i)
        {
            Progress.Report(progressId, (i+1) / (float)files.Length, files[i]);
            if (!CopyFile(sourceDir, files[i], outputDir, rgx, out var fileSize))
                continue;
            fileCount++;
            totalBytes += fileSize;
        }
    }

    private static bool CopyFile(string sourceDir, string source, string outputDir, Regex[] rgx, out long fileSize)
    {
        var absPath = source.Replace("\\", "/").Trim('/');
        var relPath = absPath.Replace(sourceDir, "").Replace("\\", "/").Trim('/');
        var destPath = $"{outputDir}/{relPath}";

        var sfi = new FileInfo(absPath);
        var dfi = new FileInfo(destPath);

        fileSize = sfi.Length;

        if (dfi.Exists && sfi.LastWriteTime <= dfi.LastWriteTime)
            return false;

        foreach (var r in rgx)
        {
            if (r.IsMatch(relPath))
            {
                //Debug.Log($"Exclude[{r}] {relPath}");
                return false;
            }
        }

        if (!dfi.Directory.Exists)
            dfi.Directory.Create();
        sfi.CopyTo(dfi.FullName, true);
        dfi.LastWriteTime = sfi.LastWriteTime;
        return true;
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
#endif