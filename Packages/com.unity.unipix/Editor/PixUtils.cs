using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using UnityEditor.Graphs;
using UnityEngine.Analytics;

namespace UniPix
{
    internal class DebugLogTimer : IDisposable
    {
        private System.Diagnostics.Stopwatch m_Timer;
        public string msg { get; set; }

        public DebugLogTimer(string m)
        {
            msg = m;
            m_Timer = System.Diagnostics.Stopwatch.StartNew();
        }

        public static DebugLogTimer Start(string m)
        {
            return new DebugLogTimer(m);
        }

        public void Dispose()
        {
            m_Timer.Stop();
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, msg + " - " + m_Timer.ElapsedMilliseconds + "ms");
        }
    }

    internal static class Underscore
    {
        internal class TimeoutData
        {
            public Action callback;
            public System.Diagnostics.Stopwatch timer;
            public long timeout;
        };

        internal class DebounceData
        {
            public Action callback;
            public System.Diagnostics.Stopwatch timer;
            public long timeout;
            public Action debounced;
        };
        static List<DebounceData> s_Debouncees = new List<DebounceData>();
        static List<TimeoutData> s_TimeoutCallbacks = new List<TimeoutData>();
        static bool s_IsUpdating;

        public static Action SetTimeout(Action callback, int timeout)
        {
            if (s_TimeoutCallbacks.FindIndex(td => td.callback == callback) != -1)
            {
                throw new Exception("Already schedule for timeout");
            }
            var data = new TimeoutData()
            {
                callback = callback,
                timer = new System.Diagnostics.Stopwatch(),
                timeout = timeout
            };
            data.timer.Start();
            s_TimeoutCallbacks.Add(data);
            CheckUdpate();
            return () => ClearTimeout(callback);
        }

        public static Action Debounce(Action callback, int timeout)
        {
            if (s_Debouncees.FindIndex(td => td.callback == callback) != -1)
            {
                throw new Exception("Already debounced");
            }
            var debounceData = new DebounceData()
            {
                callback = callback,
                timer = new System.Diagnostics.Stopwatch(),
                timeout = timeout
            };
            debounceData.debounced = () =>
            {
                var index = s_Debouncees.FindIndex(td => td.callback == callback);
                if (index != -1)
                {
                    var now = System.Diagnostics.Stopwatch.GetTimestamp();

                    if (s_Debouncees[index].timer.IsRunning)
                    {
                        s_Debouncees[index].timer.Restart();
                    }
                    else
                    {
                        s_Debouncees[index].timer.Reset();
                        s_Debouncees[index].timer.Start();
                    }
                    CheckUdpate();
                }
            };

            s_Debouncees.Add(debounceData);
            CheckUdpate();
            return debounceData.debounced;
        }

        public static void ClearDebounce(Action debounced)
        {
            s_Debouncees.RemoveAll(d => d.debounced == debounced);
            CheckUdpate();
        }

        public static void ClearTimeout(Action callback)
        {
            s_TimeoutCallbacks.RemoveAll(td => td.callback == callback);
            CheckUdpate();
        }

        private static void CheckUdpate()
        {
            if (s_IsUpdating)
            {
                if (s_Debouncees.FindIndex(d => d.timer.IsRunning) == -1 && s_TimeoutCallbacks.Count == 0)
                {
                    EditorApplication.update -= OnUpdate;
                    s_IsUpdating = false;
                }
            }
            else
            {
                if (s_Debouncees.FindIndex(d => d.timer.IsRunning) != -1 || s_TimeoutCallbacks.Count > 0)
                {
                    EditorApplication.update += OnUpdate;
                    s_IsUpdating = true;
                }
            }
        }

        private static void OnUpdate()
        {
            var checkUpdate = false;
            var now = System.Diagnostics.Stopwatch.GetTimestamp();
            for(var i = s_TimeoutCallbacks.Count - 1; i >= 0; i--)
            {
                if (s_TimeoutCallbacks[i].timer.ElapsedMilliseconds >= s_TimeoutCallbacks[i].timeout)
                {
                    var callback = s_TimeoutCallbacks[i].callback;
                    s_TimeoutCallbacks.RemoveAt(i);
                    checkUpdate = true;
                    try
                    {
                        callback();
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }

            for (var i = s_Debouncees.Count - 1; i >= 0; i--)
            {
                if (s_Debouncees[i].timer.IsRunning && s_Debouncees[i].timer.ElapsedMilliseconds >= s_Debouncees[i].timeout)
                {
                    s_Debouncees[i].timer.Stop();
                    var callback = s_Debouncees[i].callback;
                    checkUpdate = true;
                    try
                    {
                        callback();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }

            if (checkUpdate)
            {
                CheckUdpate();
            }
        }
    }

    public static class PixUtils
    {
        public static void ExtractPaletteFrom(Frame frame, List<Color> colors)
        {
            foreach (var layer in frame.Layers)
            {
                ExtractPaletteFrom(layer, colors);
            }
        }

        public static void ExtractPaletteFrom(Layer layer, List<Color> colors)
        {
            foreach (var pixel in layer.Pixels)
            {
                if (pixel.a == 1f && !colors.Contains(pixel))
                {
                    colors.Add(pixel);
                }
            }
        }

        public static string GetBasePath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                path = FileUtil.GetProjectRelativePath(path);
            }

            var directoryName = Path.GetDirectoryName(path).Replace("\\", "/");
            return $"{Path.GetDirectoryName(path)}/{GetBaseName(path)}";
        }

        public static string GetBaseName(string path)
        {
            var name = Path.GetFileName(path);
            var firstDot = name.IndexOf(".");
            var baseName = name.Substring(0, firstDot);
            return baseName;
        }

        public static string GetUniquePath(string basePath, string extension, int index = 1)
        {
            var path = $"{basePath}_{index}{extension}";
            while (File.Exists(path))
            {
                ++index;
                path = $"{basePath}_{index}{extension}";
            }
            return path;
        }

        public static bool IsValidPixSource(UnityEngine.Object obj)
        {
            return obj is PixImage || obj is Sprite || obj is Texture2D;
        }

        private static Func<string, bool> s_HasARGV;
        public static bool HasARGV(string name)
        {
            if (s_HasARGV == null)
            {
                var assembly = typeof(Application).Assembly;
                var managerType = assembly.GetTypes().First(t => t.Name == "Application");
                var methodInfo = managerType.GetMethod("HasARGV", BindingFlags.Static | BindingFlags.NonPublic);
                s_HasARGV = argName => (bool)methodInfo.Invoke(null, new[] { argName });
            }

            return s_HasARGV(name);
        }

        private static Func<string, string> s_GetARGV;
        public static string GetValueForARGV(string name)
        {
            if (s_GetARGV == null)
            {
                var assembly = typeof(Application).Assembly;
                var managerType = assembly.GetTypes().First(t => t.Name == "Application");
                var methodInfo = managerType.GetMethod("GetValueForARGV", BindingFlags.Static | BindingFlags.NonPublic);
                s_GetARGV = argName => methodInfo.Invoke(null, new[] { argName }) as string;
            }

            return s_GetARGV(name);
        }

        private static Action<string, bool> s_LoadWindowLayout;
        public static void LoadWindowLayout(string path)
        {
            if (s_LoadWindowLayout == null)
            {
                var assembly = typeof(EditorStyles).Assembly;
                var managerType = assembly.GetTypes().First(t => t.Name == "WindowLayout");
                var methodInfo = managerType.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(mi => mi.Name == "LoadWindowLayout" && mi.GetParameters().Length == 2);
                s_LoadWindowLayout = (_path, newProjectLayoutWasCreated) => methodInfo.Invoke(null, new[] { _path, (object)newProjectLayoutWasCreated });
            }

            s_LoadWindowLayout(path, false);
        }

        private static Action<bool, string> s_LoadDynamicLayout;
        public static void LoadDynamicLayout(bool keepMainWindow, string layoutSpecification)
        {
            if (s_LoadDynamicLayout == null)
            {
                var assembly = typeof(EditorStyles).Assembly;
                var managerType = assembly.GetTypes().First(t => t.Name == "WindowLayout");
                var methodInfo = managerType.GetMethod("LoadDynamicLayout", BindingFlags.Static | BindingFlags.Public);
                s_LoadDynamicLayout = (_keepMainWindow, _layoutSpec) => methodInfo.Invoke(null, new[] { (object)_keepMainWindow, _layoutSpec });
            }

            s_LoadDynamicLayout(keepMainWindow, layoutSpecification);
        }
    }
}