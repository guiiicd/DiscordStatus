using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace DiscordStatus
{
    internal static class ConfigManager
    {
        internal static string? FileDir;
        internal static string? FilePath;

        internal static void GetPath(string ModuleDirectory, string ModuleName)
        {
            // Garante que o diretório pai existe antes de tentar usá-lo.
            var parentDir = Directory.GetParent(ModuleDirectory)?.FullName;
            if (parentDir == null)
            {
                DSLog.Log(2, "Could not find parent directory for configs.");
                return;
            }
            var parentOfParentDir = Directory.GetParent(parentDir)?.FullName;
            if (parentOfParentDir == null)
            {
                 DSLog.Log(2, "Could not find parent of parent directory for configs.");
                 return;
            }

            FileDir = Path.Combine(parentOfParentDir, @$"configs/plugins/{ModuleName}");
            FilePath = Path.Combine(FileDir, $"{ModuleName}.json");
        }

        internal static async Task UpdateAsync(Globals globals)
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                DSLog.Log(2, "Config file path is not set. Cannot update settings.");
                return;
            }
            try
            {
                var json = await File.ReadAllTextAsync(FilePath);
                var configData = JsonConvert.DeserializeObject<DSconfig>(json);

                if (configData == null)
                {
                    DSLog.Log(2, "Failed to deserialize config JSON.");
                    return;
                }

                // Atualiza as propriedades na instância Globals.
                globals.Config = configData;
                globals.GConfig = configData.GeneralConfig;
                globals.WConfig = configData.WebhookConfig;
                globals.EConfig = configData.EmbedConfig;
                globals.ServerIP = configData.GeneralConfig.ServerIP;
                globals.MessageID = configData.WebhookConfig.StatusMessageID;
                globals.NameFormat = configData.EmbedConfig.NameFormat;
                globals.ConnectURL = string.Concat(configData.GeneralConfig.PHPURL, $"?ip={globals.ServerIP}");
                globals.HasCC = configData.EmbedConfig.NameFormat.Contains("{CC}") || configData.EmbedConfig.NameFormat.Contains("{FLAG}");
                globals.HasRC = configData.EmbedConfig.NameFormat.Contains("{RC}");
                DSLog.Log(1, "Read configuration data successfully.");
            }
            catch (JsonException ex)
            {
                DSLog.Log(2, $"Failed deserializing json: {ex.Message}");
            }
            catch (Exception ex)
            {
                DSLog.Log(2, $"Failed to read configuration data: {ex.Message}");
                throw;
            }
        }

        internal static async Task SaveAsync(string className, string propertyName, object propertyValue)
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                DSLog.Log(2, "Config file path is not set. Cannot save settings.");
                return;
            }

            var json = File.ReadAllText(FilePath);
            var configData = JsonConvert.DeserializeObject<DSconfig>(json);

            if (configData == null)
            {
                DSLog.Log(2, "Failed to deserialize config JSON for saving.");
                return;
            }

            var classProperty = typeof(DSconfig).GetProperty(className);
            if (classProperty == null)
            {
                DSLog.Log(2, $"Class '{className}' not found in DSconfig.");
                return;
            }

            var classInstance = classProperty.GetValue(configData);
            if (classInstance == null)
            {
                 DSLog.Log(2, $"Class instance '{className}' is null in DSconfig.");
                 return;
            }

            var propertyInfo = classInstance.GetType().GetProperty(propertyName);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(classInstance, propertyValue);
            }
            else
            {
                DSLog.Log(2, $"'{propertyName}' is not found in the '{className}' class.");
                return;
            }

            var updatedJson = JsonConvert.SerializeObject(configData, Formatting.Indented);
            await File.WriteAllTextAsync(FilePath, updatedJson);
            DSLog.Log(1, $"Saved {propertyName} to {className} in DSconfig.");
        }

        internal static async Task RenameAsync(DSconfig Config)
        {
            if (string.IsNullOrEmpty(FileDir) || string.IsNullOrEmpty(FilePath))
            {
                DSLog.Log(2, "Config file path is not set. Cannot rename old config.");
                return;
            }
            var oldConfigName = Path.GetFileNameWithoutExtension(FilePath) + "(old).json";
            var oldConfigPath = Path.Combine(FileDir, oldConfigName);
            File.Move(FilePath, oldConfigPath);
            string? json = JsonConvert.SerializeObject(Config, Formatting.Indented);
            await File.WriteAllTextAsync(FilePath, json);
        }
    }
}
