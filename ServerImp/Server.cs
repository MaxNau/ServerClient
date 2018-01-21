using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerImp
{
    public class DataReceivedeEventArgs : EventArgs
    {
        public DataReceivedeEventArgs(string json)
        {
            Json = json;
        }

        public string Json { get; set; }
    }

    public class Server
    {
        private Socket _server;

        public event EventHandler<DataReceivedeEventArgs> DataReceived;

        public Server()
        {
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void OnDataReceived(string json)
        {
            DataReceived?.Invoke(this, new DataReceivedeEventArgs(json));
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
            state.Client.ReceiveBufferSize = 2000000;
            state.Client.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, new AsyncCallback(OnReceive), state);
        }

        private void OnReceive(IAsyncResult ar)
        {
            var streamPosition = 0;

            //Console.WriteLine(ar.CompletedSynchronously);
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
                        streamPosition += state.PartialHeaderBytes.Length;
                        state.StreamBytesToReceive -= state.PartialHeaderBytes.Length;
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
                        state.StreamBytesToReceive -= streamPosition;
                        state.PartialHeaderBytes = null;
                        state.IsHeaderPartial = false;
                    }

                }

                if (state.IsMessageParial)
                {
                    if (state.MessageBytesToReceive > state.StreamBytesToReceive)
                    {
                        state.Message.AppendFormat("{0}",
                                            Encoding.UTF8.GetString(state.Buffer, streamPosition,state.StreamBytesToReceive));
                        streamPosition += state.StreamBytesToReceive;
                        state.MessageBytesReceived += state.StreamBytesToReceive;
                        state.MessageBytesToReceive -= state.StreamBytesToReceive;
                        state.StreamBytesToReceive -= state.StreamBytesToReceive;
                        break;
                    }
                    else
                    {
                        var messageBytesLeft = state.Header.MessageLength - state.Message.Length;
                        state.Message.AppendFormat("{0}",
                                             Encoding.UTF8.GetString(state.Buffer, streamPosition, messageBytesLeft));
                        streamPosition += messageBytesLeft;

                        //Console.WriteLine(state.Message.ToString());
                        OnDataReceived(state.Message.ToString());

                        state.StreamBytesToReceive -= messageBytesLeft;
                        state.MessageBytesReceived += messageBytesLeft;
                        state.MessageBytesToReceive -= messageBytesLeft;
                        state.MessageBytesReceived = 0;
                        state.Header = null;
                        state.Message = new StringBuilder();
                        state.IsMessageParial = false;
                    }
                }
                else
                {
                    if (streamPosition >= state.Header.MessageLength && state.StreamBytesToReceive >= state.Header.MessageLength)
                    {
                        if (streamPosition != state.Buffer.Length)
                        {
                            if (state.Message.Length == 0 && state.StreamBytesToReceive >= state.Header.MessageLength)
                            {
                                state.Message.AppendFormat("{0}",
                                          Encoding.UTF8.GetString(state.Buffer, streamPosition, state.Header.MessageLength));

                                //Console.WriteLine(state.Message.ToString());
                                OnDataReceived(state.Message.ToString());

                                streamPosition += state.Header.MessageLength;
                                state.StreamBytesToReceive -= state.Header.MessageLength;
                                state.Header = null;
                                state.Message = new StringBuilder();
                            }
                            else
                            {
                                var s = state.Message.Length;
                                state.Message.AppendFormat("{0}",
                                         Encoding.UTF8.GetString(state.Buffer, streamPosition, state.Header.MessageLength));

                                //Console.WriteLine(state.Message.ToString());
                                OnDataReceived(state.Message.ToString());

                                streamPosition += state.Header.MessageLength - s;
                                state.StreamBytesToReceive -= state.Header.MessageLength - s;
                                state.Header = null;
                                state.Message = new StringBuilder();
                            }
                        }
                    }
                    else
                    {
                        if (state.StreamBytesToReceive == state.Header.MessageLength)
                        {
                            state.Message.AppendFormat("{0}",
                                      Encoding.UTF8.GetString(state.Buffer, streamPosition, state.Header.MessageLength));

                            //Console.WriteLine(state.Message.ToString());
                            OnDataReceived(state.Message.ToString());

                            streamPosition += state.Header.MessageLength;
                            state.StreamBytesToReceive -= state.Header.MessageLength;
                            state.Header = null;
                            state.Message = new StringBuilder();
                        }
                        else if (state.StreamBytesToReceive < state.Header.MessageLength)
                        {
                            state.Message.AppendFormat("{0}",
                                      Encoding.UTF8.GetString(state.Buffer, streamPosition, state.StreamBytesToReceive));
                            //Console.WriteLine(state.Message.ToString());
                            streamPosition += state.StreamBytesToReceive;
                            state.MessageBytesReceived += state.StreamBytesToReceive;
                            state.MessageBytesToReceive = state.Header.MessageLength - state.StreamBytesToReceive;
                            state.StreamBytesToReceive -= state.StreamBytesToReceive;
                            state.IsMessageParial = true;
                        }
                        else
                        {
                            state.Message.AppendFormat("{0}",
                                     Encoding.UTF8.GetString(state.Buffer, streamPosition, state.Header.MessageLength));

                            //Console.WriteLine(state.Message.ToString());
                            OnDataReceived(state.Message.ToString());

                            streamPosition += state.Header.MessageLength;
                            state.StreamBytesToReceive -= state.Header.MessageLength;
                            state.Header = null;
                            state.Message = new StringBuilder();
                        }
                    }
                }

            }

            state.Client.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, new AsyncCallback(OnReceive), state);
        }
    }

    public class State
    {
        private int BUFFER_SIZE = 4096;
        public bool IsHeaderPartial { get; set; }
        public bool IsMessageParial { get; set; }
        public Header Header { get; set; }
        public byte[] Buffer { get; set; }
        public byte[] PartialHeaderBytes { get; set; }
        public StringBuilder Message { get; set; }
        public Socket Client { get; set; }
        public bool IsFirstPart { get; set; }
        public int MessageBytesReceived { get; set; }
        public int MessageBytesToReceive { get; set; }
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
