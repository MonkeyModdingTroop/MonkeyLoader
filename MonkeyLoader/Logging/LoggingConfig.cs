using MonkeyLoader.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MonkeyLoader.Logging
{
    public sealed class LoggingConfig : ConfigSection
    {
        public const string FileExtension = ".log";
        public const string FileNamePrefix = $"MonkeyLog_";
        public const string FileSearchPattern = "*" + FileExtension;
        public const string TimestampFormat = "yyyy-MM-ddTHH-mm-ss";

        public readonly DefiningConfigKey<string?> DirectoryPathKey = new("DirectoryPath", "The directory to write log files to.\nChanges will only take effect on restart.", () => "./MonkeyLoader/Logs");
        public readonly DefiningConfigKey<int> FilesToPreserveKey = new("FilesToPreserve", "The number of recent log files to keep around. Set <1 to disable.\nChanges take effect on restart.", () => 16);
        public readonly DefiningConfigKey<LoggingLevel> LevelKey = new("Level", "The logging level used to filter logging requests. May be ignored in the initial startup phase.\nChanges take effect immediately.", () => LoggingLevel.Info);
        public readonly DefiningConfigKey<bool> ShouldLogToConsoleKey = new("ShouldLogToConsole", "Whether to spawn a console window for logging.\nIf one isn't already present, it may be spawned.\nChanges take effect immediately.", () => false);
        public readonly DefiningConfigKey<ConsoleWindowStyle> ConsoleWindowStartUpStyleKey = new("ConsoleWindowStartUpStyle", "How should the window be displayed to the user at start-up.", () => ConsoleWindowStyle.Normal);

        private readonly Lazy<string?> _currentLogFilePath;
        private LoggingController _loggingController;

        /// <summary>
        /// Gets the <see cref="LoggingController"/> used by the
        /// <see cref="MonkeyLoader">loader</see> that owns this config and everything loaded by it.
        /// </summary>
        public LoggingController Controller
        {
            get => _loggingController;
            internal set
            {
                if (_loggingController is not null)
                    throw new InvalidOperationException("The logging controller must only be set once!");

                _loggingController = value;
                _loggingController.Level = LevelKey;

                EnsureDirectory();
                CleanLogDirectory();
                SetupLoggers();
            }
        }

        public string? CurrentLogFilePath => _currentLogFilePath.Value;

        /// <inheritdoc/>
        public override string Description => "Contains the options for where and what to log.";

        public string? DirectoryPath => DirectoryPathKey;
        public int FilesToPreserve => FilesToPreserveKey;

        /// <inheritdoc/>
        public override string Id => "Logging";

        /// <inheritdoc/>
        public override int Priority => 30;

        public bool ShouldCleanLogDirectory => ShouldWriteLogFile && FilesToPreserve > 0;

        public bool ShouldLogToConsole => ShouldLogToConsoleKey;

        public ConsoleWindowStyle ConsoleWindowStartUpStyle => ConsoleWindowStartUpStyleKey;

        [MemberNotNullWhen(true, nameof(DirectoryPath), nameof(CurrentLogFilePath))]
        public bool ShouldWriteLogFile => !string.IsNullOrWhiteSpace(DirectoryPathKey.GetValue());

        /// <inheritdoc/>
        public override Version Version { get; } = new Version(1, 0, 0);

        public LoggingConfig()
        {
            _currentLogFilePath = new(() => ShouldWriteLogFile ? Path.Combine(DirectoryPath, $"{FileNamePrefix}{DateTime.UtcNow.ToString(TimestampFormat, CultureInfo.InvariantCulture)}{FileExtension}") : null);

            LevelKey.Changed += LevelChanged;
            ShouldLogToConsoleKey.Changed += ShouldLogToConsoleChanged;
        }

        public static bool TryGetTimestamp(string logFile, [NotNullWhen(true)] out DateTime? timestamp)
        {
            timestamp = default;
            var fileName = Path.GetFileNameWithoutExtension(logFile);

            if (!fileName.StartsWith(FileNamePrefix))
                return false;

            var timestampStr = fileName[FileNamePrefix.Length..];

            if (DateTime.TryParseExact(timestampStr, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
            {
                timestamp = result;
                return true;
            }

            return false;
        }

        private void CleanLogDirectory()
        {
            if (!ShouldCleanLogDirectory)
                return;

            Config.Logger.Info(() => $"Cleaning old log files to keep only the latest {FilesToPreserve}");

            try
            {
                var logFilesToDelete = Directory.EnumerateFiles(DirectoryPath, FileSearchPattern)
                    .Select(logFile => (File: logFile, Success: TryGetTimestamp(logFile, out var timestamp), Created: timestamp))
                    .Where(logFile => logFile.Success)
                    .OrderByDescending(logFile => logFile.Created)
                    .Skip(FilesToPreserve)
                    .ToArray();

                foreach (var logFile in logFilesToDelete)
                {
                    try
                    {
                        File.Delete(logFile.File);
                    }
                    catch (Exception ex)
                    {
                        Config.Logger.Warn(() => ex.Format($"Failed to delete old log file: {logFile.File}"));
                    }
                }
            }
            catch (Exception ex)
            {
                Config.Logger.Error(() => ex.Format($"Failed to access and clean log files in directory: {CurrentLogFilePath}"));
            }
        }

        private void EnsureDirectory()
        {
            if (!ShouldWriteLogFile)
                return;

            try
            {
                Directory.CreateDirectory(DirectoryPath);
            }
            catch (Exception ex)
            {
                Config.Logger.Error(ex.LogFormat($"Exception while trying to create logging directory: {DirectoryPath}"));
            }
        }

        private void LevelChanged(object sender, ConfigKeyChangedEventArgs<LoggingLevel> configKeyChangedEventArgs)
        {
            if (Controller is not null)
                Controller.Level = configKeyChangedEventArgs.NewValue;
        }

        private void SetupLoggers()
        {
            LoggingHandler loggingHandlers = MissingLoggingHandler.Instance;

            if (ShouldWriteLogFile)
                loggingHandlers += new FileLoggingHandler(CurrentLogFilePath);

            if (ShouldLogToConsole)
            {
                ConsoleLoggingHandler.StartUpWindowStyle = ConsoleWindowStartUpStyle;
                loggingHandlers += ConsoleLoggingHandler.Instance;

                using var cts = new CancellationTokenSource();
                cts.CancelAfter(10000);

                ConsoleLoggingHandler.ConnectAsync(cts.Token).Wait();
            }

            Controller.Handler += loggingHandlers;
        }

        private void ShouldLogToConsoleChanged(object sender, ConfigKeyChangedEventArgs<bool> configKeyChangedEventArgs)
        {
            if (_loggingController is null)
                return;

            if (configKeyChangedEventArgs.NewValue)
            {
                ConsoleLoggingHandler.TryConnect();
                Controller.Handler += ConsoleLoggingHandler.Instance;
            }
            else
            {
                ConsoleLoggingHandler.Disconnect();
                Controller.Handler -= ConsoleLoggingHandler.Instance;
            }
        }
    }
}