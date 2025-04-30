using CustomsForgeSongManager.DataObjects;
using CustomsForgeSongManager.LocalTools;
using CustomsForgeSongManager.UControls;
using CustomsForgeSongManager.UITheme;
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
using static ICSharpCode.SharpZipLib.Zip.FastZip;

namespace CustomsForgeSongManager.DataManager
{
    public static class SettingsManager
    {
        #region Fields

        /// <summary>
        /// The list of application settings.
        /// </summary>
        private static Dictionary<string, ApplicationSetting> _appSettings = new Dictionary<string, ApplicationSetting>();

        /// <summary>
        /// The list of DataGridView settings, where the key is the name of the DataGridView.
        /// </summary>
        private static Dictionary<string, RADataGridViewSettings> _dgvSettingsByDgvName = new Dictionary<string, RADataGridViewSettings>();

        /// <summary>
        /// The lock object to prevent cross-thread issues with access to the application settings.
        /// </summary>
        private static ReaderWriterLockSlim _appSettingsLock = new ReaderWriterLockSlim();

        private static object _dgvSettingsLock = new object();

        /// <summary>
        /// The application settings object that holds all the application settings.
        /// </summary>
        private static AppSettings _appSettingsInstance = null;
        /// <summary>
        /// The instance of the application settings object that holds all the application settings.
        /// </summary>
        public static AppSettings Settings
        {
            get { return _appSettingsInstance; }
        }

        /// <summary>
        /// The current DataGridView object that is being used.
        /// </summary>
        private static RADataGridView _currentDgv = null;

        /// <summary>
        /// Instance of the current repair options.
        /// </summary>
        private static RepairOptions _repairOptions = null;

        /// <summary>
        /// Instance of the current audio options.
        /// </summary>
        private static AudioOptions _audioOptions = null;

        /// <summary>
        /// The number of CPU cores for the system.
        /// </summary>
        private static int _coreCount = 1;

        /// <summary>
        /// The number of CPU cores for the system.
        /// </summary>
        public static int CoreCount { get { return _coreCount; } }

        #endregion Fields

        /// <summary>
        /// Method to initialize the SettingsManager.
        /// Attempts to load the application settings from file,
        /// but if it can't it creates a new AppSettings instance.
        /// </summary>
        public static void Initialize()
        {
            // If we can't load the app settings from the file
            if (!TryLoadApplicationSettingsFromFile())
            {
                // TODO: Ask the user to select its location or prompt to create a new one
                // For now, we will create a new one with default values
                SetApplicationSettings(AppSettings.StartSingletonInstance());
            }

            // Get the system's core count
            int coreCount = SysExtensions.GetCoreCount();
            if (coreCount < 1)
            {
                _coreCount = 1;
            }
            else
            {
                _coreCount = coreCount;
            }
        }



        #region Application Settings
        /*
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
        */

        /// <summary>
        /// Set the application settings with the given AppSettings object.
        /// </summary>
        /// <param name="newSettings"></param>
        public static void SetApplicationSettings(AppSettings newSettings)
        {
            // Get the write lock
            _appSettingsLock.EnterWriteLock();

            // Set the application settings
            /*
            foreach (Tuple<string, object> setting in newSettings.GetSettings())
            {
                // Use the private method to do the setting
                setApplicationSetting(setting.Item1, setting.Item2);
            }
            */

            // Set the application settings object
            _appSettingsInstance = newSettings;

            // If we do not have audio options in the new settings
            // BUT we have audio options in the current settings
            if (_appSettingsInstance.AudioOptions != null)
            {
                // Set the audio options object
                _audioOptions = _appSettingsInstance.AudioOptions;
            }
            // If we do not have new audio options but we have old ones
            else if (_audioOptions != null)
            {
                // Set the old audio options in the new settings
                _appSettingsInstance.AudioOptions = _audioOptions;
            }

            // If we have repair options in the new settings
            if (_appSettingsInstance.RepairOptions != null)
            {
                // Set the audio options object
                _repairOptions = _appSettingsInstance.RepairOptions;
            }
            // If we do not have new audio options but we have old ones
            else if (_repairOptions != null)
            {
                // Set the old audio options in the new settings
                _appSettingsInstance.RepairOptions = _repairOptions;
            }

            // We need to specifically set the file path for the rocksmith install directory for now
            if (_appSettingsInstance.RSInstalledDir != null)
            {
                // Set the RSInstalledDir property in the AppSettings class
                DirectoryManager.SetRocksmithInstallationDirectory(newSettings.RSInstalledDir);
            }

            // Set the settings object as the singleton instance for the AppSettings class
            AppSettings.SetSingletonInstance(_appSettingsInstance);

            // Save the settings to file
            SaveApplicationSettingsToFile();

            // Release the write lock
            _appSettingsLock.ExitWriteLock();
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
        /// Loads the application settings from the settings file.
        /// </summary>
        /// <param name="verbose"></param>
        public static bool TryLoadApplicationSettingsFromFile(string settingsFilePath = null, bool verbose = false)
        {
            AppSettings settings = null;
            try
            {
                // Try to read the settings from the file 
                if (settingsFilePath == null)
                {
                    settingsFilePath = DirectoryManager.AppSettingsPath;
                }           

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

        /// <summary>
        /// Gets the current application settings.
        /// </summary>
        /// <returns></returns>
        public static AppSettings GetApplicationSettings()
        {
            return _appSettingsInstance;
        }

        /// <summary>
        /// Sets the given repair options as the current application-wide repair options.
        /// </summary>
        /// <param name="options"></param>
        public static void SetRepairOptions(RepairOptions options)
        {
            // Get the write lock
            _appSettingsLock.EnterWriteLock();

            // Set the repair options object
            _repairOptions = options;

            // If the application settings object has been instantiated
            if (_appSettingsInstance != null)
            {
                // Set the repair options in the application settings object
                _appSettingsInstance.RepairOptions = options;
            }

            // Release the write lock
            _appSettingsLock.ExitWriteLock();
        }

        /// <summary>
        /// Sets the given audio options as the current application-wide repair options.
        /// </summary>
        /// <param name="options"></param>
        public static void SetAudioOptions(AudioOptions options)
        {
            // Get the write lock
            _appSettingsLock.EnterWriteLock();

            // Set the repair options object
            _audioOptions = options;

            // If the application settings object has been instantiated
            if (_appSettingsInstance != null)
            {
                // Set the repair options in the application settings object
                _appSettingsInstance.AudioOptions = options;
            }

            // Release the write lock
            _appSettingsLock.ExitWriteLock();
        }

        /// <summary>
        /// Saves the current application settings to the settings file.
        /// </summary>
        /// <param name="verbose"></param>
        public static void SaveApplicationSettingsToFile()
        {
            try
            {
                using (var fs = new FileStream(DirectoryManager.AppSettingsPath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    _appSettingsInstance.SerializeXml(fs);
                    SMLog.Log("Saved CFSM Settings File: " + Path.GetFileName(DirectoryManager.AppSettingsPath));
                }
            }
            catch (Exception ex)
            {
                SMLog.Log(String.Format("<Error> SaveSettingsToFile: {0}", ex.Message));
            }
        }

       #endregion Application Settings

        // Data Grid View Stuff
        #region DataGridView Settings

        /// <summary>
        /// Save the given DGV settings for the given DGV object to a file.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="dgvToSave"></param>
        public static void SaveDataGridViewSettings(DataGridView dgvToSave)
        {
            // Get the write lock
            _appSettingsLock.EnterWriteLock();

            // Get the settings off of the dgv given
            RADataGridViewSettings settings = RAExtensions.SaveColumnOrder(dgvToSave);

            // Save the settings as the current settings for the given data grid view
            _dgvSettingsByDgvName[dgvToSave.Name] = settings;

            // Save the settings to file
            FileTools.SaveDataGridViewSettingsToFile(settings, dgvToSave);

            // Release the write lock
            _appSettingsLock.ExitWriteLock();
        }


        /// <summary>
        /// Loads or initializes the DataGridView settings for the given DataGridView.
        /// </summary>
        /// <param name="dgv"></param>
        public static RADataGridViewSettings LoadOrInitializeDataGridViewSettings(ref RADataGridView dgv)
        {
            RADataGridViewSettings settings = null;

            // => Load the settings from file
            // respect processing order => ?????
            DgvExtensions.DoubleBuffered(dgv);
            CFSMTheme.InitializeDgvAppearance(dgv);


            // Get the settings for the given DGV
            settings = GetSettingsForDataGridView(dgv);

            // Apply them to the ref DGV
            dgv.ReLoadColumnOrder(settings.ColumnOrder);

            // Set the given dgv as the current one
            SetCurrentDataGridView(dgv, settings);

            return settings;
        }


        public static void SetCurrentDataGridView(RADataGridView dgv, RADataGridViewSettings settings = null)
        {
            // Get the write lock
            _appSettingsLock.EnterWriteLock();

            // Save the current DGV
            _currentDgv = dgv;

            // Get the settings
            if (settings == null)
            {
                settings = RAExtensions.SaveColumnOrder(dgv);
            }

            // Set the settings
            _dgvSettingsByDgvName[dgv.Name] = settings;

            // Release the write lock
            _appSettingsLock.ExitWriteLock();
        }


        public static RADataGridView GetCurrentDataGridView()
        {
            try
            {
                // Get the read lock
                _appSettingsLock.EnterReadLock();
                return _currentDgv;
            }
            finally
            {
                // Release the read lock
                _appSettingsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns the local settings for the given DataGridView.
        /// </summary>
        /// <param name="dgv"></param>
        /// <returns></returns>
        public static RADataGridViewSettings GetSettingsForDataGridView(RADataGridView dgv)
        {
            RADataGridViewSettings settings = null;
            lock (_dgvSettingsLock)
            {
                // If we can't get the settings from our local dictionary
                if (!_dgvSettingsByDgvName.TryGetValue(dgv.Name, out settings))
                {
                    // If we can load the settings from our file
                    if (FileTools.TryLoadDataGridViewSettingsFromFile(dgv, out settings))
                    {
                        // Set the settings in our local dictionary
                        _dgvSettingsByDgvName[dgv.Name] = settings;
                    }
                    else
                    {
                        // Create the settings
                        settings = RAExtensions.SaveColumnOrder(dgv);
                        _dgvSettingsByDgvName[dgv.Name] = settings;
                        FileTools.SaveDataGridViewSettingsToFile(settings, dgv);
                    }
                }
            }


            return settings;
        }

        #endregion DataGridView Settings


        #region Shutdown Call

        /// <summary>
        /// Shuts down the SettingsManager by saving all current settings to file.
        /// </summary>
        public static void ShutDown()
        {
            // Save the current application settings
            SaveApplicationSettingsToFile();

            // Save all of the current DGV settings that we have to file
            foreach (KeyValuePair<string,RADataGridViewSettings> kvp  in _dgvSettingsByDgvName)
            {
                FileTools.SaveDataGridViewSettingsToFile(kvp.Value, kvp.Key);
            }
        }

        #endregion
    }
}
