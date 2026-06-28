using System.Diagnostics;
using System.Text.Json;
using System.IO.Compression;
using SubRealTeam.ConsoleUtility.Common.Logging;
using SyncDeviceFolders;

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
    }, new JsonSerializerOptions { WriteIndented = true, TypeInfoResolver = new AppJsonContext() });
    Console.WriteLine("Example config:");
    Console.WriteLine(configSample);
    return;
}
    
var configJson = File.ReadAllText("config.json");
var config = JsonSerializer.Deserialize<SyncFolderSettings>(configJson,
    new JsonSerializerOptions { TypeInfoResolver = new AppJsonContext() });

var machineName = Environment.MachineName;
Logger.Info($"Machine name is: {machineName}");

if (config == null)
{
    Logger.Error("No config found");
    return;
}

foreach (var folderSetting in config.FolderSettings)
{
    Logger.Info($"Config found: {folderSetting.Key}");
    var machineSettings = folderSetting.Value.Where(setting => setting.Key == machineName);

    foreach (var setting in machineSettings)
    {
        Logger.Info($"Machine sync path is: {machineName}:{setting.Value.Path}");

        var syncFolder = Path.Combine(folderSetting.Key, "sync");
        var backupFolder = Path.Combine(folderSetting.Key, machineName, "backup");
        
        Sync(syncFolder, backupFolder, setting.Value);

        if (!string.IsNullOrEmpty(setting.Value.Process))
            _ = WaitForProcess(setting.Value.Process, () => Sync(syncFolder, backupFolder, setting.Value));
    }

    Console.WriteLine("Press <ENTER> to exit ...");
    Console.ReadLine();
}

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

void Sync(string syncFolder, string backupFolder, SyncFolderSetting setting)
{
    Logger.Warn($"STARTING SYNC FOR {syncFolder}");

    if (!Directory.Exists(setting.Path))
    {
        Logger.Error($"Device folder does not exist: {setting.Path}");
        return;
    }
    var files = Directory.GetFiles(setting.Path, setting.Filter, SearchOption.AllDirectories);
    Logger.Info($"Found {files.Length} files");
    
    if (!Directory.Exists(syncFolder))
        Directory.CreateDirectory(syncFolder);
    
    var backupZipFile = Path.Combine(backupFolder, $"{DateTime.UtcNow:yyyy.MM.dd.HH.mm.ss}.zip");
            
    Logger.Info($"Backup local files to: {backupFolder} ... ");
    if (!Directory.Exists(backupFolder))
        Directory.CreateDirectory(backupFolder);
    
    ZipFile.CreateFromDirectory(setting.Path, backupZipFile);
    Logger.Info("finished.");
            
    Logger.Info($"Compare and sync files to local: .{Path.DirectorySeparatorChar}{syncFolder} ... ");
            
    var syncFiles = Directory
        .GetFiles(syncFolder, setting.Filter, SearchOption.AllDirectories)
        .ToList();
                
    foreach (var file in files)
    {
        var destFile = Path.Combine(syncFolder, file.Remove(0, setting.Path.Length + 1));
        if (File.Exists(destFile))
        {
            var fileInfo = new FileInfo(file);
            var destFileInfo = new FileInfo(destFile);

            if (fileInfo.LastWriteTimeUtc > destFileInfo.LastWriteTimeUtc)
                File.Copy(file, destFile, true);
            else if (fileInfo.LastWriteTimeUtc < destFileInfo.LastWriteTimeUtc)
                File.Copy(destFile, file, true);
                    
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
        var destFile = Path.Combine(setting.Path, syncFile.Remove(0, syncFolder.Length + 1));
        File.Copy(syncFile, destFile);
    }
            
    Logger.Info("finished.");
}