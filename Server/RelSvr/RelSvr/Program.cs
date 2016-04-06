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
            Thread unicast = null;
            Thread multicast = null;

            if (CUnicast.IsRunable())
            {
                unicast = new Thread(UnicastListening);
                unicast.Start();
            }

            if (CMulticast.IsRunable())
            {
                multicast = new Thread(MulticastListening);
                multicast.Start();
            }

            if(unicast != null)unicast.Join();
            if (multicast != null) multicast.Join();
        }

        private static void UnicastListening()
        {
            new CUnicast();
        }

        private static void MulticastListening()
        {
            new CMulticast();
        }
    }
}
