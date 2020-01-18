using MixItUp.Base.Model.Settings;
using MixItUp.Base.Util;
using Newtonsoft.Json.Linq;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public class SettingsV2Service
    {
        public const string SettingsFileExtension = ".miu";
        public const string DatabaseFileExtension = ".db";
        public const string LocalBackupFileExtension = ".backup";
        public const string PackagedBackupFileExtension = ".miubk";

        private const string SettingsDirectoryName = "Settings";
        private const string SettingsTemplateDatabaseFileName = "SettingsTemplateDatabase.db";

        private static SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public async Task<IEnumerable<SettingsV2ModelBase>> LoadSettings()
        {
            if (!Directory.Exists(SettingsDirectoryName))
            {
                Directory.CreateDirectory(SettingsDirectoryName);
            }

            List<SettingsV2ModelBase> settings = new List<SettingsV2ModelBase>();
            foreach (string filePath in Directory.GetFiles(SettingsDirectoryName))
            {
                if (filePath.EndsWith(SettingsFileExtension))
                {
                    SettingsV2ModelBase setting = await this.UpgradeSettings(filePath);
                    if (setting != null)
                    {
                        settings.Add(setting);
                    }
                    else
                    {
                        setting = await this.UpgradeSettings(filePath + SettingsV2Service.LocalBackupFileExtension);
                        if (setting != null)
                        {
                            settings.Add(setting);
                        }
                    }
                }
            }
            return settings;
        }

        public async Task<StreamerSettingsV2Model> CreateStreamerSettings()
        {
            StreamerSettingsV2Model settings = new StreamerSettingsV2Model();
            settings.Initialize();
            if (await this.Save(settings))
            {
                return settings;
            }
            return null;
        }

        public async Task<ModeratorSettingsV2Model> CreateModeratorSettings()
        {
            ModeratorSettingsV2Model settings = new ModeratorSettingsV2Model();
            settings.Initialize();
            if (await this.Save(settings))
            {
                return settings;
            }
            return null;
        }

        public async Task<bool> Save(SettingsV2ModelBase settings)
        {
            try
            {
                await semaphore.WaitAndRelease(async () =>
                {
                    await settings.CopyLatestValues();

                    await SerializerHelper.SerializeToFile(Path.Combine(SettingsDirectoryName, settings.FileName), settings);
                    await SerializerHelper.SerializeToFile(Path.Combine(SettingsDirectoryName, settings.BackupFileName), settings);
                });
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public async Task SavePackaged(SettingsV2ModelBase settings, string filePath)
        {
            await this.Save(settings);
            if (Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                using (ZipArchive zipFile = ZipFile.Open(filePath, ZipArchiveMode.Create))
                {
                    zipFile.CreateEntryFromFile(Path.Combine(SettingsDirectoryName, settings.FileName), settings.FileName);
                }
            }
        }

        public async Task PerformAutomaticBackupIfApplicable(SettingsV2ModelBase settings)
        {
            if (settings.SettingsBackupRate != SettingsBackupRateEnum.None && !string.IsNullOrEmpty(settings.SettingsBackupLocation))
            {
                DateTimeOffset newResetDate = settings.SettingsLastBackup;

                if (settings.SettingsBackupRate == SettingsBackupRateEnum.Daily) { newResetDate = newResetDate.AddDays(1); }
                else if (settings.SettingsBackupRate == SettingsBackupRateEnum.Weekly) { newResetDate = newResetDate.AddDays(7); }
                else if (settings.SettingsBackupRate == SettingsBackupRateEnum.Monthly) { newResetDate = newResetDate.AddMonths(1); }

                if (newResetDate < DateTimeOffset.Now)
                {
                    string filePath = Path.Combine(settings.SettingsBackupLocation, settings.ID + "-Backup-" + DateTimeOffset.Now.ToString("MM-dd-yyyy") + SettingsV2Service.PackagedBackupFileExtension);
                    await this.SavePackaged(settings, filePath);
                    settings.SettingsLastBackup = DateTimeOffset.Now;
                }
            }
        }

        private async Task<SettingsV2ModelBase> UpgradeSettings(string filePath)
        {
            try
            {
                int currentVersion = -1;
                string fileData = await ChannelSession.Services.FileService.ReadFile(filePath);
                if (!string.IsNullOrEmpty(fileData))
                {
                    JObject settingsJObj = JObject.Parse(fileData);
                    currentVersion = (int)settingsJObj["Version"];
                    if (currentVersion > 0)
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }
    }
}
