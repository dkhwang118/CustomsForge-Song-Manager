using CustomsForgeSongManager.DataObjects;
using CustomsForgeSongManager.LocalTools;
using DataGridViewTools;
using GenTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomsForgeSongManager.DataManager
{
    public static class SettingsManager
    {

        /// <summary>
        /// The list of application settings.
        /// </summary>
        private static Dictionary<string, ApplicationSetting> _appSettings = new Dictionary<string, ApplicationSetting>();

        /// <summary>
        /// The lock object to prevent cross-thread issues with access to the application settings.
        /// </summary>
        private static ReaderWriterLockSlim _appSettingsLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The application settings object that holds all the application settings.
        /// </summary>
        private static AppSettings _appSettingsInstance = null;

        private static RADataGridViewSettings _gridSettings = null;

        public static RADataGridViewSettings ManagerGridSettings
        {
            get { return _gridSettings; }
        }

        /// <summary>
        /// Set the application settings with the given AppSettings object.
        /// </summary>
        /// <param name="newSettings"></param>
        public static void SetApplicationSettings(AppSettings newSettings)
        {
            // Get the write lock
            _appSettingsLock.EnterWriteLock();

            // Set the application settings
            foreach (Tuple<string, object> setting in newSettings.GetSettings())
            {
                // Use the private method to do the setting
                setApplicationSetting(setting.Item1, setting.Item2);
            }

            // Set the application settings object
            _appSettingsInstance = newSettings;

            // Set the settings object as the singleton instance for the AppSettings class
            AppSettings.SetSingletonInstance(newSettings);

            // We need to specifically set the file path for the rocksmith install directory for now
            if (newSettings.RSInstalledDir != null)
            {
                // Set the RSInstalledDir property in the AppSettings class
                DirectoryManager.SetRocksmithInstallationDirectory(newSettings.RSInstalledDir);
            }

            // Release the write lock
            _appSettingsLock.ExitWriteLock();
        }

        /// <summary>
        /// Set the application settings with the given list of tuples, where each tuple contains a setting name and its value.
        /// </summary>
        /// <param name="appSettings"></param>
        public static void SetApplicationSettings(List<Tuple<string, object>> appSettings)
        {
            // Get the write lock
            _appSettingsLock.EnterWriteLock();

            // Set the application settings
            foreach (Tuple<string, object> setting in appSettings)
            {
                // Use the private method to do the setting
                setApplicationSetting(setting.Item1, setting.Item2);
            }

            // Release the write lock
            _appSettingsLock.ExitWriteLock();
        }

        /// <summary>
        /// Set the application setting with the given name, with the given value.
        /// </summary>
        /// <param name="settingName">The name of the Application Setting.</param>
        /// <param name="settingValue">The value of the Application Setting.</param>
        public static void SetApplicationSetting(string settingName, object settingValue)
        {
            // Get the write lock
            _appSettingsLock.EnterWriteLock();

            // Use the private method to do the setting
            setApplicationSetting(settingName, settingValue);

            // Release the write lock
            _appSettingsLock.ExitWriteLock();
        }

        /// <summary>
        /// Private method to set the application setting with the given name, with the given value.
        /// Created so the public methods can handle the read/write locking.
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="settingValue"></param>
        private static void setApplicationSetting(string settingName, object settingValue)
        {
            // If the setting is found in the dictionary
            if (_appSettings.TryGetValue(settingName, out ApplicationSetting setting))
            {
                // Update its value within the ApplicationSetting object
                setting.SetValue(settingValue);
            }
            else
            {
                // If the setting is not found, add it to the list
                _appSettings.Add(settingName, new ApplicationSetting(settingName, settingValue));
            }
        }

        /// <summary>
        /// Set the application setting with the given ApplicationSetting object.
        /// </summary>
        /// <param name="newSetting"></param>
        public static void SetApplicationSetting(ApplicationSetting newSetting)
        {
            // Get the write lock
            _appSettingsLock.EnterWriteLock();

            // Use the private method to do the setting
            setApplicationSetting(newSetting);

            // Release the write lock
            _appSettingsLock.ExitWriteLock();
        }

        /// <summary>
        /// Private method to set the application setting with the given name, with the given value.
        /// Created so the public methods can handle the read/write locking.
        /// </summary>
        /// <param name="newSetting"></param>
        private static void setApplicationSetting(ApplicationSetting newSetting)
        {
            // If we can get the setting from the dictionary
            if (_appSettings.TryGetValue(newSetting.Name, out ApplicationSetting setting))
            {
                // Update its value within the ApplicationSetting object
                setting.SetValue(newSetting.Value);
            }
            else
            {
                // If the setting is not found, add it to the dict
                _appSettings.Add(newSetting.Name, newSetting);
            }
        }

        /// <summary>
        /// Attempts to get the application setting with the given name's string value.
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="settingValue"></param>
        /// <returns></returns>
        public static bool TryGetApplicationSettingStringValue(string settingName, out string settingValue)
        {
            // Get the read lock
            _appSettingsLock.EnterReadLock();

            // If we can get the setting from the dictionary
            if (_appSettings.TryGetValue(settingName, out ApplicationSetting setting))
            {
                // Get the string value of the setting
                string settingVal = setting.StringValue;

                if (settingVal != null)
                {
                    settingValue = settingVal;
                    _appSettingsLock.ExitReadLock();
                    return true;
                }
            }

            // If we can't get the setting, or it's not a string, return false
            settingValue = null;
            _appSettingsLock.ExitReadLock();
            return false;
        }

        /// <summary>
        /// Loads the application settings from the settings file.
        /// </summary>
        /// <param name="verbose"></param>
        public static bool TryLoadApplicationSettingsFromFile(string settingsFilePath, bool verbose = false)
        {
            AppSettings settings = null;
            try
            {
                // Try to read the settings from the file 
                if (AppSettings.TryGetSettingsFromFile(settingsFilePath, out settings))
                {
                    // If the settings were successfully loaded, set the settings instance
                    SetApplicationSettings(settings);
                    return true;
                }
            }
            catch (Exception ex)
            {
                SMLog.Log(String.Format("<Error> LoadSettingsFromFile: {0}", ex.Message));
            }
            return false;
        }


        public static bool TryLoadDataGridViewSettingsFromFile(string settingsPath, out RADataGridViewSettings settings)
        {
            settings = null;

            // If a file exists at the given path
            if (File.Exists(settingsPath))
            {
                try
                {
                    // load the settings from the file
                    settings = SerialExtensions.LoadFromFile<RADataGridViewSettings>(settingsPath);

                    // Set it to the RAExtensions class
                    SetDataGridViewSettings(settings);

                    SMLog.Log("Loaded File: " + Path.GetFileName(settingsPath));
                    return true;
                }
                catch (Exception ex)
                {
                    SMLog.Log("<ERROR> GridSettings could not be loaded ...");
                    SMLog.Log("Windows 10 users must uninstall .Net 4.7 and manually install .Net 4.0 if this error persists ...");
                    SMLog.Log(ex.Message);
                    RAExtensions.ManagerGridSettings = null; // reset
                    _gridSettings = null; // reset
                }
            }

            return false;
        }

        public static void SetDataGridViewSettings(RADataGridViewSettings settings)
        {
            // Get the write lock
            _appSettingsLock.EnterWriteLock();
            // Set the settings
            RAExtensions.ManagerGridSettings = settings;
            _gridSettings = settings;
            // Release the write lock
            _appSettingsLock.ExitWriteLock();
        }
    }
}
