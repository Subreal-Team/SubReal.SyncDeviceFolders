using System.Diagnostics;
using System.Text.Json;
using System.IO.Compression;
using SyncDeviceFolders;

var configJson = File.ReadAllText("config.json");
var config = JsonSerializer.Deserialize<SyncFolderSettings>(configJson,
    new JsonSerializerOptions { TypeInfoResolver = new AppJsonContext() });

var machineName = Environment.MachineName;
Console.WriteLine($"Machine name is: {machineName}");

if (config == null)
{
    Console.WriteLine("No config found");
    return;
}

foreach (var folderSetting in config.FolderSettings)
{
    Console.WriteLine($"Config found: {folderSetting.Key}");
    var machineSettings = folderSetting.Value.Where(setting => setting.Key == machineName);

    foreach (var setting in machineSettings)
    {
        Console.WriteLine($"Machine sync path is: {machineName}:{setting.Value.Path}");

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
    Console.WriteLine($"Waiting for Process is starting: '{processName}' ...");
    
    while (true)
    {
        var process = Process.GetProcessesByName(processName).FirstOrDefault();
        
        if (process == null)
            await Task.Delay(TimeSpan.FromMinutes(1));
        else
        {
            Console.WriteLine($"Process found: '{processName}' (ID: {process.Id})");
            Console.WriteLine($"Waiting for closing process '{processName}' ...");
            await process.WaitForExitAsync();
            Console.WriteLine($"Process '{process.ProcessName}' closed! SYNC Saves ...");
    
            callback();        
        }
    }
    // ReSharper disable once FunctionNeverReturns
}

void Sync(string syncFolder, string backupFolder, SyncFolderSetting setting)
{
    Console.WriteLine($"STARTING SYNC FOR {syncFolder}");
    
    var files = Directory.GetFiles(setting.Path, setting.Filter, SearchOption.AllDirectories);
    Console.WriteLine($"Found {files.Length} files");
    
    if (!Directory.Exists(syncFolder))
        Directory.CreateDirectory(syncFolder);
    
    var backupZipFile = Path.Combine(backupFolder, $"{DateTime.UtcNow:yyyy.MM.dd.HH.mm.ss}.zip");
            
    Console.Write($"Backup local files to: {backupFolder} ... ");
    if (!Directory.Exists(backupFolder))
        Directory.CreateDirectory(backupFolder);
    
    ZipFile.CreateFromDirectory(setting.Path, backupZipFile);
    Console.WriteLine($"finished.");
            
    Console.Write($"Compare and sync files to local: .{Path.DirectorySeparatorChar}{syncFolder} ... ");
            
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
        var destFile = Path.Combine(setting.Path, Path.GetFileName(syncFile));
        File.Copy(syncFile, destFile);
    }
            
    Console.WriteLine($"finished.");
}