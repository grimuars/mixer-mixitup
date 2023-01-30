﻿using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MixItUp.Base.Util
{
    public static class ProcessHelper
    {
        public static void LaunchLink(string url)
        {
            try
            {
                if (!string.IsNullOrEmpty(url))
                {
                    ProcessStartInfo processInfo = new ProcessStartInfo(url)
                    {
                        UseShellExecute = true
                    };
                    Process.Start(processInfo);
                }
            }
            catch (Exception ex) { Logger.Log(ex); }
        }

        public static void LaunchFolder(string folderPath)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(ProcessHelper.GetRootedPath(folderPath))
                {
                    UseShellExecute = true
                };
                Process.Start(processInfo);
            }
            catch (Exception ex) { Logger.Log(ex); }
        }

        public static void LaunchProgram(string filePath, string arguments = "")
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(ProcessHelper.GetRootedPath(filePath), arguments)
                {
                    UseShellExecute = true,
                };
                Process.Start(processInfo);
            }
            catch (Exception ex) { Logger.Log(ex); }
        }

        public static IEnumerable<Process> GetProcessesByName(string name)
        {
            try
            {
                return Process.GetProcessesByName(name);
            }
            catch (Exception ex) { Logger.Log(ex); }
            return new List<Process>();
        }

        private static string GetRootedPath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), path);
            }
            return path;
        }
    }
}
