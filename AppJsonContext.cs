using System.Text.Json.Serialization;

namespace SyncDeviceFolders;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SyncFolderSettings))]
internal partial class AppJsonContext : JsonSerializerContext;