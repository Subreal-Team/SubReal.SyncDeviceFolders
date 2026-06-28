namespace SyncDeviceFolders;

public class SyncFolderSettings
{
    public Dictionary<string, Dictionary<string, SyncFolderSetting>> FolderSettings { get; set; } = new();
}

public class SyncFolderSetting
{
    public string Path { get; set; } = string.Empty;
    public string Process { get; set; } = string.Empty;
    public byte Backups { get; set; } = 3;
    public string Filter { get; set; } = "*.*";
}