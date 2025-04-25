using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomsForgeSongManager.DataObjects
{
    /// <summary>
    /// Class to hold a single application setting.
    /// </summary>
    public class ApplicationSetting
    {
        /// <summary>
        /// The name of the setting.
        /// </summary>
        private string _settingName = null;

        /// <summary>
        /// The object representing the setting value.
        /// </summary>
        private object _settingValue = null;

        /// <summary>
        /// The initial type of the setting value. 
        /// </summary>
        private Type _settingValueType = null;

        public ApplicationSetting(string settingName, object settingValue)
        {
            _settingName = settingName;
            _settingValue = settingValue;

            if (settingValue != null)
            {
                _settingValueType = settingValue.GetType();
            }
            else
            {
                // Default to an empty string if the value is null.
                _settingValue = string.Empty;
                _settingValueType = typeof(string);
            }
        }

        public string Name
        {
            get { return _settingName; }
        }

        public object Value
        {
            get { return _settingValue; }
        }

        public void SetValue(object settingValue)
        {
            _settingValue = settingValue;
            _settingValueType = settingValue.GetType();
        }

        public Type ValueType
        {
            get { return _settingValueType; }
        }

        public bool IsStringValue()
        {
            if (_settingValue == null)
                return false;
            else
                return _settingValue is string;
        }

        public bool IsIntValue()
        {
            if (_settingValue == null)
                return false;
            else
                return _settingValue is int;
        }

        public bool IsBoolValue()
        {
            if (_settingValue == null)
                return false;
            else
                return _settingValue is bool;
        }

        public string StringValue
        {
            get
            {
                if (_settingValue == null)
                    return null;
                else if (_settingValue is string)
                    return (string)_settingValue;
                else if (_settingValue is int)
                    return ((int)_settingValue).ToString();
                else if (_settingValue is bool)
                    return ((bool)_settingValue).ToString();
                else
                    return _settingValue.ToString();
            }
        }

        public int IntValue
        {
            get
            {
                if (_settingValue == null)
                    return -1;
                else if (_settingValue is int)
                    return (int)_settingValue;
                else if (_settingValue is string)
                {
                    int result;
                    if (int.TryParse((string)_settingValue, out result))
                        return result;
                    else
                        return -1;
                }
                else if (_settingValue is bool)
                {
                    if ((bool)_settingValue)
                        return 1;
                    else
                        return 0;
                }
                else
                    return -1;
            }
        }

        public bool BoolValue
        {
            get
            {
                if (_settingValue == null)
                    return false;
                else if (_settingValue is bool)
                {
                    return (bool)_settingValue;
                }
                else if (_settingValue is int)
                {
                    if ((int)_settingValue == 1)
                        return true;
                    else
                        return false;
                }
                else if (_settingValue is string)
                {
                    if (string.Compare((string)_settingValue, "true", true) == 0)
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
        }

        public override string ToString()
        {
            return String.Format("{0} = {1}", _settingName, _settingValue.ToString());
        }
    }
}
