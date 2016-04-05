using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace RelSvr
{
    class CLog
    {
        [DllImport("kernel32.dll")]
        static extern void OutputDebugString(string lpOutputString);

        private CLog()
        {
        }

        public static void Log(Exception ex)
        {
            Log(ex.Message);
            Log(ex.StackTrace);
        }

        public static void Log(string s)
        {
            OutputDebugString(s);
        }
    }
}
