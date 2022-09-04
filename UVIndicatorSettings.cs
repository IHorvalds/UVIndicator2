using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace UVIndicator2
{
    public class UVIndicatorSettings : ApplicationSettingsBase
    {
        [UserScopedSetting()]
        [DefaultSettingValue("Bucharest,RO")]
        public string SavedLocation
        {
            get
            {
                return ((string)this["SavedLocation"]);
            }
            set
            {
                this["SavedLocation"] = (string)value;
            }
        }

        [UserScopedSetting()]
        [DefaultSettingValue("one_hour")]
        public string RefreshInterval
        {
            get
            {
                return ((string)this["RefreshInterval"]);
            }
            set
            {
                this["RefreshInterval"] = (string)value;
            }
        }
    }
}
