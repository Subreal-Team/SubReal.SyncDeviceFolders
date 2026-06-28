using System.Text.Json.Serialization;

namespace SyncDeviceFolders;

[JsonSerializable(typeof(SyncFolderSettings))]
public partial class AppJsonContext : JsonSerializerContext;