using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RelSvr
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread svr = null;
            Thread brdsvr = null;

            if (CUnicast.IsRunable())
            {
                svr = new Thread(RunService);
                svr.Start();
            }

            if (CMulticast.IsRunable())
            {
                brdsvr = new Thread(RunBroadcastService);
                brdsvr.Start();
            }

            if(svr != null)svr.Join();
            if (brdsvr != null) brdsvr.Join();
        }

        private static void RunService()
        {
            new CUnicast();
        }

        private static void RunBroadcastService()
        {
            new CMulticast();
        }
    }
}
