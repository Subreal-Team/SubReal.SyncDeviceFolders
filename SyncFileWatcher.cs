namespace SyncDeviceFolders
{
    internal class SyncFileWatcher : FileSystemWatcher
    {
        public SyncFolderSetting Setting { get; set; }
        public string SyncFolder { get; }

        public SyncFileWatcher(SyncFolderSetting setting, string syncFolder)
        {
            Setting = setting;
            Path = setting.Path;
            Filter = setting.Extensions.Length > 0 ? SyncFolderSetting.DefaultFilter : setting.Filter;
            NotifyFilter =
                NotifyFilters.Attributes | NotifyFilters.CreationTime |
                NotifyFilters.DirectoryName | NotifyFilters.FileName |
                NotifyFilters.LastWrite | NotifyFilters.Security;

            SyncFolder = syncFolder;
        }
    }
}
