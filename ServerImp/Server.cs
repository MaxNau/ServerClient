using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerImp
{
    public class Server
    {
        private Socket _server;

        public Server()
        {
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Start()
        {
            _server.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000));
            _server.Listen(100);
            _server.BeginAccept(new AsyncCallback(OnConnect), null);
        }

        private void OnConnect(IAsyncResult ar)
        {
            var _client = _server.EndAccept(ar);
            _client.ReceiveBufferSize = 1;
            var state = new State();
            state.Client = _client;
            state.Client.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, new AsyncCallback(OnReceive), state);
        }

        private void OnReceive(IAsyncResult ar)
        {
            var streamPosition = 0;

            Console.WriteLine(ar.CompletedSynchronously);
            var state = ar.AsyncState as State;
            var dataLength = state.Client.EndReceive(ar);
            state.StreamBytesToReceive = dataLength;

            if (state.IsHeaderPartial)
            {

            }

            while (streamPosition < dataLength)
            {
                if (state.Header == null)
                {
                    if (dataLength - streamPosition < 8 && state.Header == null)
                    {
                        state.IsHeaderPartial = true;
                        state.PartialHeaderBytes = state.Buffer.Skip(streamPosition).ToArray();
                        streamPosition = state.PartialHeaderBytes.Length;
                        break;
                    }

                    if (!state.IsHeaderPartial)
                    {
                        state.Header = new Header();
                        state.Header.MessageType = BitConverter.ToInt32(state.Buffer, streamPosition);
                        state.Header.MessageLength = BitConverter.ToInt32(state.Buffer, streamPosition + 4);
                        streamPosition += 8;
                        state.StreamBytesToReceive -= 8;
                    }
                    else
                    {
                        var headerBuffer = new byte[8];
                        state.PartialHeaderBytes.CopyTo(headerBuffer, 0);
                        state.Buffer.Take(8-state.PartialHeaderBytes.Length).ToArray().CopyTo(headerBuffer, state.PartialHeaderBytes.Length);
                        state.Header = new Header();
                        state.Header.MessageType = BitConverter.ToInt32(headerBuffer, streamPosition);
                        state.Header.MessageLength = BitConverter.ToInt32(headerBuffer, streamPosition + 4);
                        streamPosition += 8 - state.PartialHeaderBytes.Length;
                        state.PartialHeaderBytes = null;
                        state.IsHeaderPartial = false;
                    }

                }

                if (streamPosition >= state.Header.MessageLength && state.StreamBytesToReceive >= state.Header.MessageLength || state.IsMessageParial == true)
                {
                    if (state.Message.Length == 0)
                    {
                        state.Message.AppendFormat("{0}",
                                  Encoding.UTF8.GetString(state.Buffer, streamPosition, state.Header.MessageLength));
                        Console.WriteLine(state.Message.ToString());
                        streamPosition += state.Header.MessageLength;
                        state.StreamBytesToReceive -= state.Header.MessageLength;
                        state.Header = null;
                        state.Message = new StringBuilder();
                    }
                    else
                    {
                        var s = state.Message.Length;
                        state.Message.AppendFormat("{0}",
                                 Encoding.UTF8.GetString(state.Buffer, streamPosition, state.Header.MessageLength - s));
                        Console.WriteLine(state.Message.ToString());
                        streamPosition += state.Header.MessageLength - s;
                        state.StreamBytesToReceive -= state.Header.MessageLength - s;
                        state.Header = null;
                        state.Message = new StringBuilder();
                    }
                }
                else
                {
                    state.Message.AppendFormat("{0}",
                              Encoding.UTF8.GetString(state.Buffer, streamPosition, state.StreamBytesToReceive));
                    streamPosition += streamPosition - state.Header.MessageLength;
                    state.IsMessageParial = true;
                }

                //if (state.IsMessageParial)
                //{
                //    if (state.IsFirstPart)
                //    {
                //        state.Message.AppendFormat("{0}",
                //           Encoding.UTF8.GetString(state.Buffer, 8, dataLength - 8));
                //        state.IsFirstPart = false;
                //        state.MessageBytesReceived += dataLength - 8;
                //    }
                //    else
                //    {
                //        state.Message.AppendFormat("{0}",
                //           Encoding.UTF8.GetString(state.Buffer, 0, state.Header.MessageLength - state.MessageBytesReceived));

                //        streamPosition = state.Header.MessageLength - state.MessageBytesReceived;
                //        state.MessageBytesReceived += state.Header.MessageLength - state.MessageBytesReceived;
                //    }

                //    Console.WriteLine(state.Message.ToString());

                //    if (state.MessageBytesReceived == state.Header.MessageLength)
                //    {

                //    }


            }

            state.Client.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, new AsyncCallback(OnReceive), state);
            //else
            //{
            //    state.Message.AppendFormat("{0}",
            //        Encoding.UTF8.GetString(state.Buffer, 8, state.Header.MessageLength));

            //    Console.WriteLine(state.Message.ToString());

            //    state.ResetState();


            //}
        }
    }

    public class State
    {
        private int BUFFER_SIZE = 33;
        public bool IsHeaderPartial { get; set; }
        public bool IsMessageParial { get; set; }
        public Header Header { get; set; }
        public byte[] Buffer { get; set; }
        public byte[] PartialHeaderBytes { get; set; }
        public StringBuilder Message { get; set; }
        public Socket Client { get; set; }
        public bool IsFirstPart { get; set; }
        public int MessageBytesReceived { get; set; }
        public int StreamBytesToReceive { get; set; }

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
            MessageBytesReceived = 0;
        }
    }

    // 8 bytes
    public class Header
    {
        public int MessageLength { get; set; } //4 bytes
        public int MessageType { get; set; } //4 bytes
    }
}
