using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace RelSvr
{
    internal class CMulticast
    {
        private Socket m_sock = null;

        public CMulticast()
        {
            Start();
        }

        public static bool IsRunable()
        {
            int port = CSettings.UDPPort;

            if (port <= 0)
            {
                CLog.Log(string.Format("Invalid UDP Port {0}", port));
                return false;
            }

            if (string.IsNullOrEmpty(CSettings.UDPAddress))
            {
                CLog.Log("Invalid UDP Address");
                return false;
            }

            return true;
        }

        private void Start()
        {
            m_sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            IPAddress ip = IPAddress.Parse(CSettings.UDPAddress);
            m_sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip));
            m_sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);
            IPEndPoint ipep = new IPEndPoint(ip, CSettings.UDPPort);

            try
            {
                m_sock.Connect(ipep);

                byte[] buff;

                while (true)
                {
                    CEvent.Wait();

                    lock (CEvent.m_lock)
                    {
                        buff = Encoding.ASCII.GetBytes(CEvent.m_message);
                        m_sock.Send(buff, buff.Length, SocketFlags.None);
                    }

                    CEvent.Reset();
                }
            }
            catch (Exception ex)
            {
                CLog.Log(ex);
            }
            finally
            {
                try
                {
                    m_sock.Close();
                }
                catch
                {
                }
            }
        }

    }
}
