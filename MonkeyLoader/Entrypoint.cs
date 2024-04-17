﻿using MonkeyLoader;
using MonkeyLoader.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Doorstop
{
    internal static class Entrypoint
    {
        public static void Start()
        {
            var log = new FileLoggingHandler("MonkeyLoader/MonkeyLog.log");

            foreach (var file in Directory.EnumerateFiles("./"))
            {
                try
                {
                    if (Path.GetFileName(file).StartsWith("doorstop", StringComparison.OrdinalIgnoreCase)
                        && Path.GetExtension(file).Equals(".log", StringComparison.OrdinalIgnoreCase))
                        File.Delete(file);
                }
                catch
                {
                    log.Warn(() => $"Failed to delete doorstop logfile - probably the active one: {file}");
                }
            }

            try
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => log.Fatal(() => (e.ExceptionObject as Exception)?.Format("Unhandled Exception!") ?? "Unhandled Exception!");

                var loggingController = new LoggingController("MonkeyLoader")
                {
                    Level = LoggingLevel.Trace,
                    Handler = log
                };

                var loader = new MonkeyLoader.MonkeyLoader(loggingController: loggingController);

                AppDomain.CurrentDomain.ProcessExit += (_, _) => loader.Shutdown();

                var type = Type.GetType("Mono.Runtime");
                if (type != null)
                {
                    var displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                    if (displayName != null)
                        log.Info(() => $"Mono Runtime Version: {displayName.Invoke(null, null)}");
                }
                else
                {
                    log.Info(() => "Not running on Mono.");
                }

                log.Info(() => $".NET Runtime Version: {Environment.Version}");
                log.Info(() => $".NET Runtime: {RuntimeInformation.FrameworkDescription}");
                log.Info(() => $"Domain Target Framework: {AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName}");

                log.Info(() => $"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
                log.Info(() => $"Relative Search Directory: {AppDomain.CurrentDomain.RelativeSearchPath}");
                log.Info(() => $"Private Bin Path: {AppDomain.CurrentDomain.SetupInformation.PrivateBinPath}");
                log.Info(() => $"Entry Assembly: {Assembly.GetEntryAssembly()?.Location}");
                log.Info(() => "CMD Args: " + string.Join(" ", Environment.GetCommandLineArgs()));

                loader.FullLoad();

                //log.Log($"Loaded Assemblies:{Environment.NewLine}{string.Join(Environment.NewLine, AppDomain.CurrentDomain.GetAssemblies().Select(assembly => new MonkeyLoader.AssemblyName(assembly.GetName().Name)))}");
            }
            catch (Exception ex)
            {
                log.Fatal(ex.Format);
            }
        }
    }
}