using CustomsForgeSongManager.LocalTools;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomsForgeSongManager.DataObjects
{
    /// <summary>
    /// Class to hold all the data for the application.
    /// There really isn't too much.
    /// </summary>
    public class Model
    {

        private static Model _instance;

        private static readonly object _lock = new object();

        public static Model Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new Model();
                    }
                    return _instance;
                }
            }
        }

        private Model()
        {
            
        }

        // This region contains the methods, fields, properties, etc. related to the repair options.
        #region Repair Options 

        #region Repair Options Properties

        private bool _skipRemasteredFiles = false;

        private bool _addDynamicDifficulty = false;

        // copied below from RepairTools.cs


        private string _cfgPath;
        private int _phraseLen = 8;
        private string _rampUpPath;
        private decimal _scrollSpeed = 1.3m;
        private bool _fixAppId = true; // default repair startup value

        public bool SkipRemastered { get; set; }
        public bool UsingOrgFiles { get; set; }
        public bool AddDD { get; set; }

        public int PhraseLength
        {
            get { return _phraseLen < 8 ? (_phraseLen = 8) : _phraseLen; }
            set { _phraseLen = value; }
        }

        public bool RemoveSustain { get; set; }

        public string CfgPath
        {
            get { return _cfgPath ?? (_cfgPath = String.Empty); }
            set { _cfgPath = value; }
        }

        public string RampUpPath
        {
            get { return _rampUpPath ?? (_rampUpPath = String.Empty); }
            set { _rampUpPath = value; }
        }

        public bool OverwriteDD { get; set; }
        //
        public bool RepairMastery { get; set; }
        public bool PreserveStats { get; set; }
        public bool IgnoreMultitone { get; set; }
        //
        public bool RepairMaxFive { get; set; }
        public bool RemoveNDD { get; set; }
        public bool RemoveBass { get; set; }
        public bool RemoveGuitar { get; set; }
        public bool RemoveBonus { get; set; }
        public bool RemoveMetronome { get; set; }
        public bool IgnoreStopLimit { get; set; }
        //
        public bool AdjustScrollSpeed { get; set; }
        public decimal ScrollSpeed
        {
            get { return _scrollSpeed < 0.5m || _scrollSpeed > 4.5m ? (_scrollSpeed = 1.3m) : _scrollSpeed; }
            set { _scrollSpeed = value; }
        }
        //
        public bool RemoveSections { get; set; }
        public bool FixLowBass { get; set; }
        public bool FixAppId // { get; set; }
        {
            get { return _fixAppId; }
            set { _fixAppId = value; }
        }
        //
        public bool DLFolderProcess { get; set; }
        public bool DLFolderMonitor { get; set; }
        public bool SkipDupes { get; set; }
        #endregion Repair Options Properties

        /// <summary>
        /// The lock that will prevent cross-thread issues with access to the repair options.
        /// </summary>
        ReaderWriterLockSlim _repairOptionsLock = new ReaderWriterLockSlim();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public void SetRepairOptions(RepairOptions options)
        {
            // Get the write lock
            _repairOptionsLock.EnterWriteLock();

            // Set the repair options
            SkipRemastered = options.SkipRemastered;
            AddDD = options.AddDD;
            PhraseLength = (int)options.PhraseLength;
            RemoveSustain = options.RemoveSustain;
            CfgPath = String.IsNullOrEmpty(options.CfgPath.ToString()) ? "" : options.CfgPath.ToString();
            RampUpPath = String.IsNullOrEmpty(options.RampUpPath.ToString()) ? "" : options.RampUpPath.ToString();
            OverwriteDD = options.OverwriteDD;
            RepairMastery = options.RepairMastery;
            PreserveStats = options.PreserveStats;
            UsingOrgFiles = options.UsingOrgFiles;
            IgnoreMultitone = options.IgnoreMultitone;
            RepairMaxFive = options.RepairMaxFive;
            RemoveNDD = options.RemoveNDD;
            RemoveBass = options.RemoveBass;
            RemoveGuitar = options.RemoveGuitar;
            RemoveBonus = options.RemoveBonus;
            RemoveMetronome = options.RemoveMetronome;
            IgnoreStopLimit = options.IgnoreStopLimit;
            RemoveSections = options.RemoveSections;
            AdjustScrollSpeed = options.AdjustScrollSpeed;
            ScrollSpeed = options.ScrollSpeed;
            FixLowBass = options.FixLowBass;
            FixAppId = options.FixAppId;
            DLFolderProcess = options.DLFolderProcess;
            DLFolderMonitor = options.DLFolderMonitor;
            SkipDupes = options.SkipDupes;

            // Release the write lock
            _repairOptionsLock.ExitWriteLock();

            return;
        }

        /// <summary>
        /// Gets the repair options.
        /// </summary>
        /// <returns>Returns the current Repair Options as an object.</returns>
        public RepairOptions GetRepairOptions()
        {
            // Get the read lock
            _repairOptionsLock.EnterReadLock();

            // Create a new RepairOptions object and set its properties
            RepairOptions options = new RepairOptions
            {
                SkipRemastered = SkipRemastered,
                AddDD = AddDD,
                PhraseLength = PhraseLength,
                RemoveSustain = RemoveSustain,
                CfgPath = CfgPath,
                RampUpPath = RampUpPath,
                OverwriteDD = OverwriteDD,
                RepairMastery = RepairMastery,
                PreserveStats = PreserveStats,
                UsingOrgFiles = UsingOrgFiles,
                IgnoreMultitone = IgnoreMultitone,
                RepairMaxFive = RepairMaxFive,
                RemoveNDD = RemoveNDD,
                RemoveBass = RemoveBass,
                RemoveGuitar = RemoveGuitar,
                RemoveBonus = RemoveBonus,
                RemoveMetronome = RemoveMetronome,
                IgnoreStopLimit = IgnoreStopLimit,
                RemoveSections = RemoveSections,
                AdjustScrollSpeed = AdjustScrollSpeed,
                ScrollSpeed = ScrollSpeed,
                FixLowBass = FixLowBass,
                FixAppId = FixAppId,
                DLFolderProcess = DLFolderProcess,
                DLFolderMonitor = DLFolderMonitor,
                SkipDupes = SkipDupes
            };

            // Release the read lock
            _repairOptionsLock.ExitReadLock();

            return options;
        }

        #endregion Repair Options


        // This region contains the methods, fields, properties, etc. related to the application settings.
        #region Application Settings

        /// <summary>
        /// The list of application settings.
        /// </summary>
        private Dictionary<string, ApplicationSetting> _appSettings = new Dictionary<string, ApplicationSetting>();

        /// <summary>
        /// The lock object to prevent cross-thread issues with access to the application settings.
        /// </summary>
        private ReaderWriterLockSlim _appSettingsLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The application settings object that holds all the application settings.
        /// </summary>
        private AppSettings _appSettingsInstance = null;

        /// <summary>
        /// Set the application settings with the given AppSettings object.
        /// </summary>
        /// <param name="newSettings"></param>
        public void SetApplicationSettings(AppSettings newSettings)
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

            // Release the write lock
            _appSettingsLock.ExitWriteLock();
        }

        /// <summary>
        /// Set the application settings with the given list of tuples, where each tuple contains a setting name and its value.
        /// </summary>
        /// <param name="appSettings"></param>
        public void SetApplicationSettings(List<Tuple<string,object>> appSettings)
        {
            // Get the write lock
            _appSettingsLock.EnterWriteLock();

            // Set the application settings
            foreach (Tuple<string,object> setting in appSettings)
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
        public void SetApplicationSetting(string settingName, object settingValue)
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
        private void setApplicationSetting(string settingName, object settingValue)
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
        public void SetApplicationSetting(ApplicationSetting newSetting)
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
        private void setApplicationSetting(ApplicationSetting newSetting)
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
        public bool TryGetApplicationSettingStringValue(string settingName, out string settingValue)
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


        #endregion Application Settings
    }
}
