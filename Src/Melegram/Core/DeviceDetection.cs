using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Melegram.Enums;

namespace Melegram.Core
{
    public static class DeviceDetection
    {
        public static DeviceType DetectDeviceType()
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                return DeviceType.Phone;
            }

            else
            {
                return DeviceType.Desktop;
            }
        }
    }
}
