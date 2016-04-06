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

                byte[] b;

                while (true)
                {
                    if (m_sock.Poll(CSettings.UDPReadTimeout, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1024];

                        m_sock.Receive(buff);
                        string str = System.Text.Encoding.ASCII.GetString(buff, 0, buff.Length);
                        CLog.Log(str.Trim());
                    }

                    if (m_sock.Poll(CSettings.UDPWriteTimeout, SelectMode.SelectWrite))
                    {
                        b = Encoding.ASCII.GetBytes(DateTime.Now.ToString());
                        m_sock.Send(b, b.Length, SocketFlags.None);
                        System.Threading.Thread.Sleep(CSettings.UDPReadTimeout);
                    }
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

        //private void Client()
        //{
        //    Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        //    IPEndPoint ipep = new IPEndPoint(IPAddress.Any, CSettings.UDPPort);
        //    sock.Bind(ipep);

        //    IPAddress ip = IPAddress.Parse(CSettings.UDPAddress);

        //    sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip,IPAddress.Any));

        //    byte[] b=new byte[1024];
        //    sock.Receive(b);
        //    string str = System.Text.Encoding.ASCII.GetString(b, 0, b.Length);
        //    Console.WriteLine(str.Trim());
        //}
    }
}
