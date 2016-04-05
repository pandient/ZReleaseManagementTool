using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace RelSvr
{
    internal class CService
    {
        [DllImport("Ws2_32.dll")]
        private static extern uint ntohl(uint netlong);

        [DllImport("Ws2_32.dll")]
        private static extern ushort htonl(uint hostlong);

        private class TRequestHeader
        {
            public uint data_length;
            public uint request_id;
            public string user; //32 bytes long in length
            public byte[] reserved;

            public TRequestHeader()
            {
                reserved = new byte[24];
            }
        }

        //private class TResponseHeader
        //{
        //    public uint data_length;
        //    public uint request_id;
        //    public string user; //32 bytes long in length
        //    public uint status;
        //    public byte[] reserved;

        //    public TResponseHeader()
        //    {
        //        reserved = new byte[20];
        //    }
        //}

        private static int REQ_PRODUCT = 1;
        private static int REQ_VERSIONS = 2;
        private static int REQ_DOWNLOAD = 3;

        private Socket m_socket = null;
        private TRequestHeader m_request_hdr = new TRequestHeader();
        //private TResponseHeader m_response_hdr = new TResponseHeader();

        public CService(Socket sock)
        {
            m_socket = sock;
        }

        public void Start()
        {
            string data = null;
            byte[] bytes = new byte[1024];
            int size;

            while (true)
            {
                size = 0;
                while (size < 64)
                {
                    size += m_socket.Receive(bytes, size, 64 - size, SocketFlags.None);
                }

                if (size != 64)
                {
                    CLog.Log(string.Format("Wrong Header : {0}", size));
                    break;
                }

                m_request_hdr.data_length = ntohl(BitConverter.ToUInt32(bytes, 0));
                if (m_request_hdr.data_length < 0)
                {
                    CLog.Log(string.Format("Wrong Header : {0}", m_request_hdr.data_length));
                    break;
                }

                m_request_hdr.request_id = ntohl(BitConverter.ToUInt32(bytes, sizeof(uint)));

                m_request_hdr.user = Encoding.ASCII.GetString(bytes, 2 * sizeof(uint), 32);

                if (m_request_hdr.request_id == REQ_PRODUCT)
                {
                    SendProducts();
                    break;
                }
                else if (m_request_hdr.request_id == REQ_VERSIONS)
                {
                    break;
                }
                else if (m_request_hdr.request_id == REQ_DOWNLOAD)
                {
                    break;
                }
                else
                {
                    break;
                }
            }

            m_socket.Shutdown(SocketShutdown.Both);
            m_socket.Close();
        }

        private string UserID
        {
            get
            {
                string user = Environment.UserName; //Environment.UserDomainName + "\\" +

                if (user.Length > 32)
                {
                    user = user.Substring(0, 32);
                }
                else if (user.Length < 32)
                {
                    user = user.PadRight(32);
                }

                return user;
            }
        }

        private void SendResponseHeader(uint datasize, uint status)
        {
            byte[] buff;

            m_socket.Send(BitConverter.GetBytes(datasize));
            m_socket.Send(BitConverter.GetBytes(m_request_hdr.request_id));

            buff = Encoding.ASCII.GetBytes(UserID);
            m_socket.Send(buff);

            m_socket.Send(BitConverter.GetBytes(status));

            m_socket.Send(new byte[20]);
        }

        private void SendProducts()
        {
            string products;
            byte[] buff;

            products = CSettings.Products;
            products = products.Replace(',', '\a');

            SendResponseHeader((uint)products.Length, 0);
            buff = Encoding.ASCII.GetBytes(products);
            m_socket.Send(buff);
        }
    }

    internal class CUnicast
    {

        public CUnicast()
        {
            Start();
        }

        private static void Start()
        {
            int port = CSettings.TCPPort;
            if (port <= 0)
            {
                CLog.Log(string.Format("Invalid TCP port {0}", port));
                return;
            }

            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                while (true)
                {
                    CLog.Log("Waiting for a connection...");
                    Socket sock = listener.Accept();
                    CLog.Log("Accepted a connection");

                    CService service = new CService(sock);
                    Thread thd = new Thread(service.Start);
                    thd.Start();
                }

            }
            catch (Exception e)
            {
                CLog.Log(e);
            }
        }

    }
}
