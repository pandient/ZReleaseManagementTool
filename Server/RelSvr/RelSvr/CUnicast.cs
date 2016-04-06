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

        private static void CheckProduct(string product)
        {
            string []p = CSettings.Products.Split(',');

            if (Array.FindIndex(CSettings.Products.Split(','), (w) => { return string.Compare(w, product, true) == 0; }) < 0)
            {
                throw new Exception("Invalid " + product);
            }
        }

        public static string[] GetVersions(string product)
        {
            CheckProduct(product);

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
            CheckProduct(product);

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
            CheckProduct(product);

            fileName = CSettings.ProductDirectory(product) + "\\" + version + "\\" + file;

            if (!File.Exists(fileName))
            {
                throw new Exception(fileName + " not found!");
            }

            fileLen = (uint)(new FileInfo(fileName).Length);
        }

        public static void GetAlert(string product, string version, out string alert)
        {
            CheckProduct(product);

            string fileName = CSettings.ProductDirectory(product) + "\\" + version + "\\$alert";

            alert = string.Empty;
            if (!File.Exists(fileName)) return;

            alert = File.ReadAllText(fileName);
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

        private static int REQ_PRODUCT = 1;
        private static int REQ_VERSIONS = 2;
        private static int REQ_ADMIN = 3;
        private static int REQ_VERSION_FILE_LIST = 4;
        private static int REQ_DOWNLOAD_FILE = 5;
        private static int REQ_DOWNLOAD_ALERT = 6;
        private static int REQ_BROADCAST = 7;

        private static uint STATUS_OK = 0;
        private static uint STATUS_ERR = 0x1;

        private static uint ERR_INVALID_REQ = 0x0100;
        private static uint ERR_INVALID_SIZE = 0x0200;
        private static uint ERR_INVALID_NAME = 0x0400;
        private static uint ERR_EMPTY = 0x0800;
        private static uint ERR_INVALID_COUNT = 0x1000;
        private static uint ERR_EXCEPTION = 0x2000;
        private static uint ERR_ALERT = 0x4000;
        private static uint ERR_NOT_ADMIN = 0x8000;

        private Socket m_sock = null;
        private TRequestHeader m_request_hdr = new TRequestHeader();

        public CService(Socket sock)
        {
            m_sock = sock;

            m_sock.ReceiveTimeout = CSettings.TCPReadTimeout;
            m_sock.SendTimeout = CSettings.TCPWriteTimeout;
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
                    else if (m_request_hdr.request_id == REQ_ADMIN)
                    {
                        SendAdmin();
                    }
                    else if (m_request_hdr.request_id == REQ_BROADCAST)
                    {
                        SendBroadcast();
                    }
                    else
                    {
                        SendError();
                        break;
                    }

                    if (!Peek()) break;
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
                    m_sock.Shutdown(SocketShutdown.Both);
                    m_sock.Close();
                }
                catch
                {
                }
            }
        }

        private bool Peek()
        {
            try
            {
                return (m_sock.Poll(CSettings.TCPReadTimeout, SelectMode.SelectRead));
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        private void ReadData(byte []buff, int bytesToRead)
        {
            int size;

            size = 0;
            while (size < bytesToRead)
            {
                size += m_sock.Receive(buff, size, bytesToRead - size, SocketFlags.None);
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

            m_sock.Send(BitConverter.GetBytes(datasize));
            m_sock.Send(BitConverter.GetBytes(m_request_hdr.request_id));

            m_sock.Send(Encoding.ASCII.GetBytes(UserID));

            m_sock.Send(BitConverter.GetBytes(status));

            m_sock.Send(new byte[20]);

            if (iserr)
            {
                m_sock.Send(Encoding.ASCII.GetBytes(err));
            }
        }

        private void SendProducts()
        {
            string products;

            products = CSettings.Products;
            products = products.Replace(',', SEP);

            SendHeader((uint)products.Length, STATUS_OK, null);

            m_sock.Send(Encoding.ASCII.GetBytes(products));

            Broadcast(m_request_hdr.user + " asks for products");
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
            m_sock.Send(Encoding.ASCII.GetBytes(all));

            Broadcast(m_request_hdr.user + " asks for versions");
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
            m_sock.Send(Encoding.ASCII.GetBytes(all));

            Broadcast(m_request_hdr.user + " asks for file list");
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
            m_sock.SendFile(filename);

            Broadcast(m_request_hdr.user + " downloads file " + file);
        }

        private void SendAlert()
        {
            byte[] buff = new byte[m_request_hdr.data_length];
            string tmp;
            string[] data;
            string product;
            string version;
            string alert;

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
                CRelease.GetAlert(product, version, out alert);
            }
            catch (Exception ex)
            {
                SendHeader(0, STATUS_ERR | ERR_EXCEPTION, ex.Message);
                return;
            }

            if (string.IsNullOrEmpty(alert))
            {
                SendHeader(0, STATUS_OK, null);
            }
            else
            {
                SendHeader(0, STATUS_ERR|ERR_ALERT, alert);
            }

            Broadcast(m_request_hdr.user + " asks for Alert");
        }

        private void SendAdmin()
        {
            bool b;
            string[] admins;

            admins = CSettings.Adminstrators.Split(',');
            b = Array.FindIndex(admins, (w) => { return string.Compare(w, m_request_hdr.user, true) == 0; })>=0;

            SendHeader(0, (b? STATUS_OK:STATUS_ERR | ERR_NOT_ADMIN), null);

            Broadcast(m_request_hdr.user + " says :  Am I an admin ?");
        }

        private void SendBroadcast()
        {
            uint len = m_request_hdr.data_length;
            if (len <= 0)
            {
                SendHeader(0, STATUS_ERR|ERR_EMPTY, null);
                return;
            }

            byte[] buff = new byte[1024];
            string msg = string.Empty;
            int ret;

            while (true)
            {
                if (len == 0) break;

                ret = m_sock.Receive(buff, 0, (buff.Length>len ? (int)len:buff.Length), SocketFlags.None);
                if (ret > 0)
                {
                    len -= (uint)ret;
                    msg += Byte2Str(buff, 0, ret);
                }
            }

            SendHeader(0, STATUS_OK, null);

            Broadcast(m_request_hdr.user + " says :  " + msg);
        }

        private void SendError()
        {
            SendHeader(0, STATUS_ERR | ERR_INVALID_REQ, null);
        }

        private void Broadcast(string msg)
        {
            lock (CEvent.m_lock)
            {
                CEvent.m_message = msg;
            }
            CEvent.Set();
        }
    }

    internal class CUnicast
    {

        public CUnicast()
        {
            Start();
        }

        public static bool IsRunable()
        {
            int port = CSettings.TCPPort;

            if (port <= 0)
            {
                CLog.Log(string.Format("Invalid TCP port {0}", port));
                return false;
            }

            return true;
        }

        private void Start()
        {
            IPHostEntry host = Dns.Resolve(Dns.GetHostName());
            IPAddress ip = host.AddressList[0];
            IPEndPoint point = new IPEndPoint(ip, CSettings.TCPPort);

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
