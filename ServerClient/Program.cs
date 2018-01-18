using ClientImp;
using ServerImp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server();
            server.Start();

            Client client = new Client();
            client.Conenct("127.0.0.1", 2000);

            client.Send("Ssssssss");
            client.Send("xxxxxx");
            client.Send("xxxxxx");
            client.Send("xxxxxx");
            client.Send("xxxxxx");
            client.Send("xxxxxx");

            Console.ReadKey();
        }
    }
}
