using ClientImp;
using Newtonsoft.Json;
using ServerImp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerClient
{
    class Program
    {
        const string AllowedChars =
"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        public static Socket sock;
        static void Main(string[] args)
        {
           // TcpClient tcp = new TcpClient(ips.AddressList[0])
            var ips = Dns.GetHostEntry("stackoverflow.com");
             sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            sock.Connect(ips.AddressList[0], 443);

            ClientImp.State st = new ClientImp.State();
            st.Buffer = new byte[4096];
            
            //sock.BeginReceive(st.Buffer, 0, 4096, SocketFlags.None, new AsyncCallback(Rec), st);

           string h = "GET /tags.html HTTP/1.1\r\n" + "Host: stackoverflow.com\r\n" + 
        "\r\n";
            var buffer = Encoding.UTF8.GetBytes(h);
            sock.Send(buffer);

            using (var networkStream = new NetworkStream(sock, true)) {
                using (var sslStream = new SslStream(networkStream, true))
                {
                    sslStream.AuthenticateAsClient("stackoverflow.com", null, System.Security.Authentication.SslProtocols.Tls12, false);
                    var r = sslStream.Read(st.Buffer, 0, 4096);
                    string response = Encoding.ASCII.GetString(st.Buffer, 0, r);
                }
            }

            var ix = sock.Receive(st.Buffer);

            string responsePart = Encoding.ASCII.GetString(st.Buffer, 0, ix);

            var j = GenerateJson(1, 1);
            var test = JsonConvert.DeserializeObject<JsonRepresentation>(j);


            Server server = new Server();
            server.DataReceived += Server_DataReceived; 
            server.Start();

            Client client = new Client();
            client.Conenct("127.0.0.1", 2000);
            client.Send("");
            for (int i = 0; i < 2000; i++)
            {
                client.Send(GenerateJson(i, 1));
            }

            Console.ReadKey();
        }

        private static void Rec(IAsyncResult ar)
        {
            var st = ar.AsyncState as ClientImp.State;
            var r = sock.EndReceive(ar);
            var resp = Encoding.UTF8.GetString(st.Buffer);
            var buf = new byte[4096];
            sock.BeginReceive(st.Buffer, 0, 4096, SocketFlags.None, new AsyncCallback(Rec), st);
        }

        private static void Server_DataReceived(object sender, DataReceivedeEventArgs e)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<JsonRepresentation>(e.Json);

                Console.WriteLine(string.Format("[id: {0}] [length: {1}]", obj.Id, obj.Image.Length));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static IEnumerable<string> RandomStrings(
    string allowedChars,
    int minLength,
    int maxLength,
    int count,
    Random rng)
        {
            char[] chars = new char[maxLength];
            int setLength = allowedChars.Length;

            while (count-- > 0)
            {
                int length = rng.Next(minLength, maxLength + 1);

                for (int i = 0; i < length; ++i)
                {
                    chars[i] = allowedChars[rng.Next(setLength)];
                }

                yield return new string(chars, 0, length);
            }
        }

        public static string GenerateJson(int id, int evType)
        {
            Random rng = new Random();
            var json = "{'Id':" + id + ",'EventType':" + evType + ",'Image':" + "'" + string.Concat(RandomStrings(AllowedChars, 0, 25000000, 1, rng)) + "'" + ",'Desc':" + "'" + string.Concat(RandomStrings(AllowedChars, 0, 3, 1, rng)) + "'" + ",'Whatever':" + "'" + string.Concat(RandomStrings(AllowedChars, 1, 25, 1, rng)) + "'" + "}";
            return json;
        }
    }


    public class JsonRepresentation
    {
        public int Id { get; set; }
        public int EventType { get; set; }
        public string Image { get; set; }
        public string Desc { get; set; }
        public string Whatever { get; set; }
    }
}
