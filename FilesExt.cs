namespace SyncDeviceFolders;

public static class FilesExt
{
    extension(string[] files)
    {
        public string[] FilterByExt(string[] extensions)
        {
            if (extensions.Length > 0)
            {
                var extSet = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
                var filtered = new List<string>(files.Length);

                foreach (var file in files)
                {
                    var ext = Path.GetExtension(file);
                    if (extSet.Contains(ext))
                        filtered.Add(file);
                }

                return [.. filtered];
            }

            return files;
        }

        public string[] ExcludeByDirs(string[] excludes)
        {
            if (excludes.Length > 0)
            {
                var excludeSet = new HashSet<string>(excludes, StringComparer.OrdinalIgnoreCase);
                var filtered = new List<string>(files.Length);

                foreach (var file in files)
                {
                    var dir = Path.GetDirectoryName(file);
                    if (dir is null)
                    {
                        filtered.Add(file);
                        continue;
                    }

                    var segments = dir.Split(
                        [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                        StringSplitOptions.RemoveEmptyEntries);

                    var excluded = false;
                    foreach (var segment in segments)
                    {
                        if (excludeSet.Contains(segment))
                        {
                            excluded = true;
                            break;
                        }
                    }

                    if (!excluded)
                        filtered.Add(file);
                }

                return [.. filtered];
            }

            return files;
        }
    }

    extension(string file)
    {
        public bool IsFileAllowed(SyncFolderSetting setting)
        {
            string[] files = [file];
            return files
                .FilterByExt(setting.Extensions)
                .ExcludeByDirs(setting.Excludes)
                .Length == 1;
        }
    }
}