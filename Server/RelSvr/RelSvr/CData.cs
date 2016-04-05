using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace RelSvr
{
    internal abstract class CSerialize
    {
        public string Serialize()
        {
            XmlSerializer x = new XmlSerializer(this.GetType());
            MemoryStream ms = new MemoryStream();

            x.Serialize(ms, this);
            ms.Position = 0;
            return ms.ToString();
        }

        public static object Deserialize(Type t, string xml)
        {
            XmlSerializer serializer = new XmlSerializer(t);
            MemoryStream ms = new MemoryStream();
            byte[] ba = Encoding.ASCII.GetBytes(xml);

            ms.Write(ba, 0, ba.Length);
            return serializer.Deserialize(ms);
        }
    }

    [Serializable]
    internal abstract class CHeader : CSerialize
    {
        public int PayloadSize;
    }

    [Serializable]
    internal class CRequest : CHeader
    {
        public const int REQUEST_NONE = 0x0;
        public const int REQUEST_PRODUCTS = 0x1;
        public const int REQUEST_RELEASES = 0x2;

        public int Request;
    }

    [Serializable]
    internal class CProducts : CRequest
    {
        public List<string> Product;  // contains zema, ddx, ...
    }


    [Serializable]
    internal class CVersionPathInfo : CRequest
    {
        public string Version;
        public string Path;
        public string Notes;
    }

    [Serializable]
    internal class CReleases : CRequest
    {
        public string Product;
        public List<CVersionPathInfo> Release;
    }

    [Serializable]
    internal class CDirectory : CRequest
    {
        public const int ACTION_NONE = 0x0;
        public const int ACTION_DIRECTORY_CREATE = 0x1;
        public const int ACTION_DIRECTORY_DELETE_ = 0x02;

        public int Action;
        public string Product;
        public string Directory;
        public string Release;
    }


}
