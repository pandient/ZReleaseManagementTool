using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RelSvr
{
    internal class CEvent
    {
        public static object m_lock = new object();
        public static string m_message = string.Empty;

        private static ManualResetEvent m_receiveDone = new ManualResetEvent(false);

        private CEvent()
        {
        }

        public static void Wait()
        {
            m_receiveDone.WaitOne();
        }

        public static void Set()
        {
            m_receiveDone.Set();
        }

        public static void Reset()
        {
            m_receiveDone.Reset();
        }
    }
}
