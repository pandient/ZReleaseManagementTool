using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace RelSvr
{
    class CSettings
    {
        private CSettings()
        {
        }

        private static string Item(string key)
        {
            return ConfigurationManager.AppSettings[key] ?? string.Empty;
        }

        public static string Products
        {
            get
            {
                return Item("Products");
            }
        }

        public static string Adminstrators
        {
            get
            {
                return Item("Adminstrators");
            }
        }

        public static string ProductDirectory(string product)
        {
            return Item(product);
        }

        public static int TCPPort
        {
            get
            {
                int p;

                return int.TryParse(Item("TCPPort"), out p) ? p:-1;
            }
        }

        private static int GetTimeout(string key)
        {
            int p;

            return int.TryParse(Item(key), out p) ? 1000 * p : 0;
        }

        public static int TCPReadTimeout
        {
            get
            {
                return GetTimeout("TCPReadTimeout");
            }
        }

        public static int TCPWriteTimeout
        {
            get
            {
                return GetTimeout("TCPWriteTimeout");
            }
        }

        public static int UDPPort
        {
            get
            {
                int p;

                return int.TryParse(Item("UDPort"), out p) ? p : -1;
            }
        }

        public static string UDPAddress
        {
            get
            {
                return Item("UDPAddress");
            }
        }

        public static int UDPReadTimeout
        {
            get
            {
                return GetTimeout("UDPReadTimeout");
            }
        }

        public static int UDPWriteTimeout
        {
            get
            {
                return GetTimeout("UDPWriteTimeout");
            }
        }

    }
}
