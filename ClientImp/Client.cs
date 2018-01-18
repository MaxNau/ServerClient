using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClientImp
{
    public class Client
    {
        private Socket _client;

        public Client()
        {
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Conenct(string ip, int port)
        {
            _client.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
            var state = new State();
            _client.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, new AsyncCallback(OnReceive), state);
        }

        public void Send(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var lengthBytes = BitConverter.GetBytes(messageBytes.Length);
            var typeBytes = new byte[4];
            typeBytes[0] = 1;

            var buffer = new byte[messageBytes.Length + 8];
            typeBytes.CopyTo(buffer, 0);
            lengthBytes.CopyTo(buffer, typeBytes.Length);
            messageBytes.CopyTo(buffer, typeBytes.Length + lengthBytes.Length);

            _client.Send(buffer);
        }

        private void OnReceive(IAsyncResult ar)
        {
            Console.WriteLine(ar.CompletedSynchronously);
            var state = ar.AsyncState as State;
            var dataLength = _client.EndReceive(ar);

            if (state.IsHeaderPartial)
            {

            }

            if (state.Header == null)
            {
                state.Header.MessageType = BitConverter.ToInt32(state.Buffer, 0);
                state.Header.MessageLength = BitConverter.ToInt32(state.Buffer, 4);
            }

            if (state.IsMessageParial)
            {

            }
            else
            {
                state.Message.AppendFormat("{0}", 
                    Encoding.UTF8.GetString(state.Buffer, 0, state.Header.MessageLength));
                state.ResetState();
            }
        }
    }

    public class State
    {
        private int BUFFER_SIZE = 4096;
        public bool IsHeaderPartial { get; set; }
        public bool IsMessageParial { get; set; }
        public Header Header { get; set; }
        public byte[] Buffer { get; set; }
        public StringBuilder Message { get; set; }

        public State()
        {
            Buffer = new byte[BUFFER_SIZE];
            Message = new StringBuilder();
        }

        public void ResetState()
        {
            Buffer = null;
            Message = new StringBuilder();
            Buffer = new byte[BUFFER_SIZE];
            Header = null;
            IsHeaderPartial = false;
            IsMessageParial = false;
        }
    }

    // 8 bytes
    public class Header
    {
        public int MessageLength { get; set; } //4 bytes
        public int MessageType { get; set; } //4 bytes
    }
}
