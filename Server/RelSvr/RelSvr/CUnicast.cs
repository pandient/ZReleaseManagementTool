using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.IO;

namespace RelSvr
{
    internal class CRelease
    {

        private CRelease()
        {

        }

        public static string[] GetVersions(string product)
        {
            string file;
            int pos;
            List<string> versions = new List<string>();

            foreach (string f in Directory.GetDirectories(CSettings.ProductDirectory(product)))
            {
                pos = f.LastIndexOf('\\');
                if (pos >= 0)
                {
                    file = f.Substring(pos + 1);
                }
                else
                {
                    file = f;
                }

                if (file.ToArray()[0] >= '0' && file.ToArray()[0] <= '9')
                {
                    versions.Add(file);
                }
            }

            return versions.ToArray();
        }

        public static string[] GetFileList(string product, string version)
        {
            string dir = CSettings.ProductDirectory(product) + "\\" + version;
            string file;
            int pos;
            List<string> all = new List<string>();

            foreach (string f in Directory.GetFiles(dir))
            {
                pos = f.LastIndexOf('\\');
                if (pos >= 0)
                {
                    file = f.Substring(pos + 1);
                }
                else
                {
                    file = f;
                }

                if (file.ToArray()[0] != '.')
                {
                    all.Add(file);
                }
            }

            return all.ToArray();
        }

        public static void GetFileInfo(string product, string version, string file, out string fileName, out uint fileLen)
        {
            fileName = CSettings.ProductDirectory(product) + "\\" + version + "\\" + file;

            if (!File.Exists(fileName))
            {
                throw new Exception(fileName + " not found!");
            }

            fileLen = (uint)(new FileInfo(fileName).Length);
        }
    }

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
                request_id = 0;
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

        private static char SEP = '\r';

        private static int REQ_PRODUCT = 0x1;
        private static int REQ_VERSIONS = 0x2;
        private static int REQ_VERSION_FILE_LIST = 0x4;
        private static int REQ_DOWNLOAD_ALERT = 0x100;
        private static int REQ_DOWNLOAD_FILE = 0x200;

        private static uint STATUS_OK = 0;
        private static uint STATUS_ERR = 0x1;

        private static uint ERR_INVALID_REQ = 0x0100;
        private static uint ERR_INVALID_SIZE = 0x0200;
        private static uint ERR_INVALID_NAME = 0x0400;
        private static uint ERR_EMPTY = 0x0800;
        private static uint ERR_INVALID_COUNT = 0x1000;
        private static uint ERR_EXCEPTION = 0x2000;

        private Socket m_socket = null;
        private TRequestHeader m_request_hdr = new TRequestHeader();

        public CService(Socket sock)
        {
            m_socket = sock;

            m_socket.ReceiveTimeout = CSettings.TCPReadTimeout;
            m_socket.SendTimeout = CSettings.TCPWriteTimeout;
        }

        public static string Byte2Str(byte []buff, int startPos, int size)
        {
            return Encoding.ASCII.GetString(buff, startPos, size);
        }

        public void Start()
        {
            try
            {
                while (true)
                {
                    if(!ReadHeader())break;

                    if (m_request_hdr.request_id == REQ_PRODUCT)
                    {
                        SendProducts();
                    }
                    else if (m_request_hdr.request_id == REQ_VERSIONS)
                    {
                        SendVersions();
                    }
                    else if (m_request_hdr.request_id == REQ_VERSION_FILE_LIST)
                    {
                        SendFileList();
                    }
                    else if (m_request_hdr.request_id == REQ_DOWNLOAD_ALERT)
                    {
                        SendAlert();
                    }
                    else if (m_request_hdr.request_id == REQ_DOWNLOAD_FILE)
                    {
                        SendFile();
                    }
                    else
                    {
                        SendError();
                        break;
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
                    m_socket.Shutdown(SocketShutdown.Both);
                    m_socket.Close();
                }
                catch
                {
                }
            }
        }

        private void ReadData(byte []buff, int bytesToRead)
        {
            int size;

            size = 0;
            while (size < bytesToRead)
            {
                size += m_socket.Receive(buff, size, bytesToRead - size, SocketFlags.None);
            }
        }

        private bool ReadHeader()
        {
            byte[] buff = new byte[64];

            ReadData(buff, 64);

            m_request_hdr.data_length = ntohl(BitConverter.ToUInt32(buff, 0));
            if (m_request_hdr.data_length < 0)
            {
                CLog.Log(string.Format("Wrong Header : {0}", m_request_hdr.data_length));
                return false;
            }

            m_request_hdr.request_id = ntohl(BitConverter.ToUInt32(buff, sizeof(uint)));

            m_request_hdr.user = Encoding.ASCII.GetString(buff, 2 * sizeof(uint), 32);

            return true;

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

        private void SendHeader(uint datasize, uint status, string err)
        {
            bool iserr = ((status & STATUS_ERR) != 0 && !string.IsNullOrEmpty(err));

            if (iserr)
            {
                datasize = (uint)err.Length;
            }

            m_socket.Send(BitConverter.GetBytes(datasize));
            m_socket.Send(BitConverter.GetBytes(m_request_hdr.request_id));

            m_socket.Send(Encoding.ASCII.GetBytes(UserID));

            m_socket.Send(BitConverter.GetBytes(status));

            m_socket.Send(new byte[20]);

            if (iserr)
            {
                m_socket.Send(Encoding.ASCII.GetBytes(err));
            }
        }

        private void SendProducts()
        {
            string products;

            products = CSettings.Products;
            products = products.Replace(',', SEP);

            SendHeader((uint)products.Length, STATUS_OK, null);

            m_socket.Send(Encoding.ASCII.GetBytes(products));
        }

        private void SendVersions()
        {
            byte []buff = new byte[m_request_hdr.data_length];
            string product;
            string []versions;
            string all;

            ReadData(buff, (int)m_request_hdr.data_length);
            product = Byte2Str(buff, 0, (int)m_request_hdr.data_length);

            if (string.IsNullOrEmpty(product))
            {
                SendHeader(0, STATUS_ERR | ERR_INVALID_NAME, "product is empty");
                return;
            }

            try
            {
                versions = CRelease.GetVersions(product);
            }
            catch (Exception ex)
            {
                SendHeader(0, STATUS_ERR | ERR_EXCEPTION, ex.Message);
                return;
            }

            if (versions.Length == 0)
            {
                SendHeader(0, STATUS_ERR | ERR_EMPTY, product);
                return;
            }

            all = string.Join(SEP.ToString(), versions);

            SendHeader((uint)all.Length, STATUS_OK, null);
            m_socket.Send(Encoding.ASCII.GetBytes(all));
        }

        private void SendFileList()
        {
            byte[] buff = new byte[m_request_hdr.data_length];
            string tmp;
            string[] data;
            string product;
            string version;
            string all;

            ReadData(buff, (int)m_request_hdr.data_length);
            tmp = Byte2Str(buff, 0, (int)m_request_hdr.data_length);
            data = tmp.Split(SEP);
            if (data.Length != 2)
            {
                SendHeader(0, STATUS_ERR | ERR_INVALID_COUNT, tmp);
                return;
            }
            product = data[0];
            version = data[1];

            if (string.IsNullOrEmpty(product))
            {
                SendHeader(0, STATUS_ERR | ERR_INVALID_NAME, "product is empty");
                return;
            }
            if (string.IsNullOrEmpty(version))
            {
                SendHeader(0, STATUS_ERR | ERR_INVALID_NAME, "version is empty");
                return;
            }

            try
            {
                data = CRelease.GetFileList(product, version);
            }
            catch (Exception ex)
            {
                SendHeader(0, STATUS_ERR | ERR_EXCEPTION, ex.Message);
                return;
            }

            if (data.Length == 0)
            {
                SendHeader(0, STATUS_ERR | ERR_EMPTY, product + " | " + version);
                return;
            }

            all = string.Join(SEP.ToString(), data);

            SendHeader((uint)all.Length, STATUS_OK, null);
            m_socket.Send(Encoding.ASCII.GetBytes(all));
        }

        private void SendFile()
        {
            byte[] buff = new byte[m_request_hdr.data_length];
            string tmp;
            string[] data;
            string product;
            string version;
            string file;
            string filename;
            uint len;

            ReadData(buff, (int)m_request_hdr.data_length);
            tmp = Byte2Str(buff, 0, (int)m_request_hdr.data_length);
            data = tmp.Split(SEP);
            if (data.Length != 3)
            {
                SendHeader(0, STATUS_ERR | ERR_INVALID_COUNT, tmp);
                return;
            }
            product = data[0];
            version = data[1];
            file = data[2];

            if (string.IsNullOrEmpty(product))
            {
                SendHeader(0, STATUS_ERR | ERR_INVALID_NAME, "product is empty");
                return;
            }
            if (string.IsNullOrEmpty(version))
            {
                SendHeader(0, STATUS_ERR | ERR_INVALID_NAME, "version is empty");
                return;
            }
            if (string.IsNullOrEmpty(file))
            {
                SendHeader(0, STATUS_ERR | ERR_INVALID_NAME, "file is empty");
                return;
            }

            try
            {
                CRelease.GetFileInfo(product, version, file, out filename, out len);
            }
            catch (Exception ex)
            {
                SendHeader(0, STATUS_ERR | ERR_EXCEPTION, ex.Message);
                return;
            }

            SendHeader((uint)len, STATUS_OK, null);
            m_socket.SendFile(filename);
        }

        private void SendAlert()
        {
        }

        private void SendError()
        {
            SendHeader(0, STATUS_ERR | ERR_INVALID_REQ, null);
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

            IPHostEntry host = Dns.Resolve(Dns.GetHostName());
            IPAddress ip = host.AddressList[0];
            IPEndPoint point = new IPEndPoint(ip, port);

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(point);
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
