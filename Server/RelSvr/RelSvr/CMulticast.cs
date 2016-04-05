using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace RelSvr
{
    class CMulticast
    {
        private void StartListening()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            IPAddress ip = IPAddress.Parse("224.5.6.7");
            s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip));
            s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);
            IPEndPoint ipep = new IPEndPoint(ip, 4567); 
            s.Connect(ipep);

            byte[] b = new byte[10];
            for (int x = 0; x < b.Length; x++) b[x] = (byte)(x + 65);

            s.Send(b, b.Length, SocketFlags.None);

            s.Close();
        }

        private void Client()
        {
            Socket s=new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            IPEndPoint ipep=new IPEndPoint(IPAddress.Any, 4567);
            s.Bind(ipep);
 
            IPAddress ip=IPAddress.Parse("224.5.6.7");

            s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip,IPAddress.Any));

            byte[] b=new byte[1024];
            s.Receive(b);
            string str = System.Text.Encoding.ASCII.GetString(b,0,b.Length);
            Console.WriteLine(str.Trim());
        }
    }
}
