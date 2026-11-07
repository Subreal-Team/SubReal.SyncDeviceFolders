using SubRealTeam.ConsoleUtility.Common.Extensions;
using SubRealTeam.ConsoleUtility.Common.Logging;
using SyncDeviceFolders;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

Logger.AddInstance(new ConsoleLogger());
Directory.CreateDirectory("logs");
Logger.AddInstance(new FileLogger(null, "logs"));

if (!File.Exists("config.json"))
{
    Logger.Error("No config found. Please create 'config.json' file.");
    var configSample = JsonSerializer.Serialize(new SyncFolderSettings
    {
        FolderSettings = new Dictionary<string, Dictionary<string, SyncFolderSetting>>
        {
            {
                "Local folder name to sync", new Dictionary<string, SyncFolderSetting>
                {
                    {
                        "device-name", new SyncFolderSetting
                        {
                            Path = "Path to sync folder on device",
                            Process = "Process name: if need monitor running process",
                            Filter = "Filter files: *.* by default"
                        }
                    }
                }
            }
        }
    }, AppJsonContext.Default.SyncFolderSettings);
    Console.WriteLine("Example config:");
    Console.WriteLine(configSample);
    return;
}

var configJson = File.ReadAllText("config.json");
SyncFolderSettings? config;

try
{
    config = JsonSerializer.Deserialize(configJson, AppJsonContext.Default.SyncFolderSettings);
}
catch (Exception ex)
{
    Logger.Error(ex, $"Failed to load config: ");
    throw;
}


var machineName = Environment.MachineName;
Logger.Info($"Machine name is: {machineName}");

if (config == null)
{
    Logger.Error("No config found");
    return;
}

if (config.FolderSettings == null || config.FolderSettings.Count == 0)
{
    Logger.Warn("Config is empty");
    return;
}

var watchers = new List<SyncFileWatcher>();

foreach (var folderSetting in config.FolderSettings)
{
    Logger.Info($"Config found: {folderSetting.Key}");
    var machineSettings = folderSetting.Value.Where(setting => setting.Key == machineName);

    foreach (var setting in machineSettings)
    {
        if (setting.Value.Disabled)
        {
            Logger.Info($"Setting '{setting.Key}' is disabled. Skip ...");
            continue;
        }

        setting.Value.Path = setting.Value.Path.AddTrailingSlash();
        Logger.Info($"Machine sync path is: {machineName}:{setting.Value.Path}");

        var syncFolder = Path.Combine(folderSetting.Key, "sync");
        var backupFolder = Path.Combine(folderSetting.Key, machineName, "backup");

        Sync(syncFolder, backupFolder, setting.Value);

        if (!string.IsNullOrEmpty(setting.Value.Process))
            _ = WaitForProcess(setting.Value.Process, () => Sync(syncFolder, backupFolder, setting.Value));

        if (setting.Value.Watch)
        {
            var watcher = new SyncFileWatcher(setting.Value, syncFolder);

            watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;

            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            watchers.Add(watcher);
            Logger.Info($"Watching changes for path: {setting.Value.Path}");
        }
    }
}

Console.WriteLine("Press <ENTER> to exit ...");
Console.ReadLine();

return;

async Task WaitForProcess(string processName, Action callback)
{
    Logger.Info($"Waiting for Process is starting: '{processName}' ...");

    while (true)
    {
        var process = Process.GetProcessesByName(processName).FirstOrDefault();

        if (process == null)
            await Task.Delay(TimeSpan.FromMinutes(1));
        else
        {
            Logger.Info($"Process found: '{processName}' (ID: {process.Id})");
            Logger.Info($"Waiting for closing process '{processName}' ...");
            await process.WaitForExitAsync();
            Logger.Info($"Process '{process.ProcessName}' closed! SYNC Saves ...");

            callback();
        }
    }
    // ReSharper disable once FunctionNeverReturns
}

void OnChanged(object sender, FileSystemEventArgs e)
{
    if (e.ChangeType != WatcherChangeTypes.Changed)
        return;
    if (sender is not SyncFileWatcher watcher)
        return;

    var settings = watcher.Setting;
    SyncFile(e.FullPath, watcher.SyncFolder, settings);

}
void OnCreated(object sender, FileSystemEventArgs e)
{
    if (e.ChangeType != WatcherChangeTypes.Created)
        return;
    if (sender is not SyncFileWatcher watcher)
        return;

    var settings = watcher.Setting;
    SyncFile(e.FullPath, watcher.SyncFolder, settings);
}
void OnDeleted(object sender, FileSystemEventArgs e)
{
    Logger.Debug($"File or directory deleted: {e.FullPath}");
}
void OnRenamed(object sender, RenamedEventArgs e)
{
    Logger.Debug($"File or directory renamed: {e.FullPath} from '{e.OldName}' to '{e.Name}'");
}
void OnError(object sender, ErrorEventArgs e)
{
    Logger.Error(e.GetException());
}

void SyncFile(string filePath, string syncFolder, SyncFolderSetting setting)
{
    if (!filePath.IsFileAllowed(setting))
        return;

    Thread.Sleep(500);

    var attr = File.GetAttributes(filePath);
    if (attr.HasFlag(FileAttributes.Directory))
        return;

    var destFile = Path.Combine(syncFolder, filePath[setting.Path.Length..]);
    if (File.Exists(destFile))
    {
        var fileInfo = new FileInfo(filePath);
        var destFileInfo = new FileInfo(destFile);

        if (fileInfo.LastWriteTimeUtc > destFileInfo.LastWriteTimeUtc)
        {
            File.Copy(filePath, destFile, true);
            Logger.Info($"File synced: {filePath}");
        }
        else if (fileInfo.LastWriteTimeUtc < destFileInfo.LastWriteTimeUtc)
        {
            File.Copy(destFile, filePath, true);
            Logger.Info($"File synced: {filePath}");
        }

        return;
    }

    var subDir = Path.GetDirectoryName(destFile);
    if (subDir != null && subDir != syncFolder && !Directory.Exists(subDir))
        Directory.CreateDirectory(subDir);

    File.Copy(filePath, destFile);
    Logger.Info($"File synced: {filePath}");
    return;
}

void Sync(string syncFolder, string backupFolder, SyncFolderSetting setting)
{
    Logger.Warn($"STARTING SYNC FOR {syncFolder}");

    if (!Directory.Exists(setting.Path))
    {
        Logger.Error($"Device folder does not exist: {setting.Path}");
        return;
    }

    var mainFilter = setting.Filter.Contains(',') 
        ? SyncFolderSetting.DefaultFilter 
        : setting.Filter;

    var files = Directory.GetFiles(setting.Path, mainFilter, SearchOption.AllDirectories)
        .FilterByExt(setting.Extensions)
        .ExcludeByDirs(setting.Excludes);

    Logger.Info($"Found {files.Length} files");

    if (!Directory.Exists(syncFolder))
        Directory.CreateDirectory(syncFolder);

    var backupZipFile = Path.Combine(backupFolder, $"{DateTime.UtcNow:yyyy.MM.dd.HH.mm.ss}.zip");

    Logger.Info($"Backup local files to: {backupFolder} ... ");
    if (!Directory.Exists(backupFolder))
        Directory.CreateDirectory(backupFolder);

    using ZipArchive zipFile = ZipFile.Open(backupZipFile, ZipArchiveMode.Create);
    foreach (string file in files) 
    {
        var entryName = Path.GetRelativePath(setting.Path, file);
        zipFile.CreateEntryFromFile(file, entryName);
    }

    zipFile.Dispose();

    Logger.Info("finished.");

    Logger.Info($"Compare and sync files to local: .{Path.DirectorySeparatorChar}{syncFolder} ... ");

    var syncFiles = Directory
        .GetFiles(syncFolder, setting.Filter, SearchOption.AllDirectories)
        .ToList();

    var filesSynced = 0;
    foreach (var file in files)
    {
        var destFile = Path.Combine(syncFolder, file[setting.Path.Length..]);
        if (File.Exists(destFile))
        {
            var fileInfo = new FileInfo(file);
            var destFileInfo = new FileInfo(destFile);

            if (fileInfo.LastWriteTimeUtc > destFileInfo.LastWriteTimeUtc)
            {
                File.Copy(file, destFile, true);
                filesSynced++;
            }
            else if (fileInfo.LastWriteTimeUtc < destFileInfo.LastWriteTimeUtc)
            {
                File.Copy(destFile, file, true);
                filesSynced++;
            }

            syncFiles.Remove(destFile);

            continue;
        }

        var subDir = Path.GetDirectoryName(destFile);
        if (subDir != null && subDir != syncFolder && !Directory.Exists(subDir))
            Directory.CreateDirectory(subDir);

        File.Copy(file, destFile);
    }

    foreach (var syncFile in syncFiles)
    {
        var destFile = Path.Combine(setting.Path, syncFile[(syncFolder.Length + 1)..]);

        var subDir = Path.GetDirectoryName(destFile);
        if (subDir != null && subDir != setting.Path && !Directory.Exists(subDir))
            Directory.CreateDirectory(subDir);

        File.Copy(syncFile, destFile);
        filesSynced++;
    }

    Logger.Info($"Files synced: {filesSynced}");
}