using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomsForgeSongManager.DataObjects
{
    public abstract class AbstractApplicationSetting<T> where T : class
    {

        private string _settingName = null;

        private T _settingValue = null;

        protected AbstractApplicationSetting() { }

        protected AbstractApplicationSetting(string settingName, T settingValue)
        {
            _settingName = settingName;
            _settingValue = settingValue;
        }

        public string Name
        {
            get { return _settingName; }
            set { _settingName = value; }
        }

        public T Value
        {
            get { return _settingValue; }
            set { _settingValue = value; }
        }
    }
}
