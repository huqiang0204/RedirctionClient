using huqiang;
using RedirctionClient.DataControll;
using System;
using System.Net;

namespace RedirctionClient
{
    class Program
    {
        static void Main(string[] args)
        {
            KcpDataControll.Instance.Connection("193.112.70.170", 6666);
            while(true)
            {
                var cmd = Console.ReadLine();
                if (cmd == "close" | cmd == "Close")
                    break;
            }
        }
    }
}
