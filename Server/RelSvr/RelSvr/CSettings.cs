﻿using System;
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

    }
}