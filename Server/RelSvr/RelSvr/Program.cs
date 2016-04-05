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
            var unicast = new Thread(UnicastListening);
            var multicast = new Thread(MulticastListening);

            unicast.Start();
            unicast.Join();

            //multicast.Start();
            //multicast.Join();
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
