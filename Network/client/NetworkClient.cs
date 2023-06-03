using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using UnityEngine;

namespace Rollo.Client
{
    public class NetworkClient : StreamLoops
    {
        const int Timeout = 30000;
        public bool Connecting => _connecting;
        private volatile bool _connecting;
        public readonly object receiveQueueLock = new object();
        private DateTime _lastMessageSent;
        private TcpClient _client;
        private Thread _receiveThread;
        private Thread _sendThread;
        private Thread _writerThreadBuffer;
        private Stream _stream;
        private readonly bool _withTls;
        private string _currentServer;
        private readonly ManualResetEvent _receivePending = new ManualResetEvent(false);
        public readonly ManualResetEvent SendPending = new ManualResetEvent(false);
        private readonly SegQueue<Packet> _sendQueue = new SegQueue<Packet>();
        private DateTime _lastMessageReceived;
        /// <summary>
        /// Create the packet in another thread to avoid blocking the main thread.
        /// </summary>
        private static System.Threading.Channels.Channel<Action> fastWriter = Channel.CreateUnbounded<Action>();
        private List<long> latencies = new List<long>();

        public bool Connected => _client != null &&
                                 _client.Client != null &&
                                 _client.Client.Connected;

        public NetworkClient(bool withTls)
        {
            _withTls = withTls;
        }

        public NetworkClient client => this;

        private async void WriterThread()
        {
            fastWriter = Channel.CreateUnbounded<Action>();

            while (Connected)
            {
                var action = await fastWriter.Reader.ReadAsync();
                action();
            }
        }

        private async void ReceiveThread()
        {
            try
            {
                while (Connected && !Connecting)
                {
                    var task = ReadMessage(_stream);
                    if (await Task.WhenAny(task, Task.Delay(Timeout)) != task)
                    {
                        break;
                    }

                    var message = task.Result;

                    if (message != null)
                    {
                        _lastMessageReceived = DateTime.Now;
                        if (message.OpCodeList != OpCodeList.HeartBeat)
                        {
                            DeserializeFunction fn = null;

                            RolloNotificationCenter.Instance.GetDesialisationFunction(message.OpCodeList, out fn);

                            if (fn != null)
                            {
                                message.o = fn(message.Payload);
                            }


                            lock (receiveQueueLock)
                            {
                                ReceiveQueue.Enqueue(new MessageStream(EventType.Data, message));
                            }
                        }
                        else
                        {
                            HandleHeartbeat(message);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (SocketException)
            {
                lock (receiveQueueLock)
                {
                    ReceiveQueue.Enqueue(new MessageStream(EventType.Disconnected, null));
                }
            }
            catch (ThreadInterruptedException)
            {
            }
            catch (ThreadAbortException)
            {
            }
            catch
            {
                Debug.Log("Closing Receiving thread.");
            }

            Disconnect();
        }

        public void ListenMessages()
        {
            lock (receiveQueueLock)
            {
                while (Connected && _client.Connected && GetNextMessage(out var msg))
                {
                    switch (msg.EventType)
                    {
                        case EventType.Connected:
                            break;
                        case EventType.Data:
                            if (msg.Data.Cmd != 0)
                            {
                                RolloNotificationCenter.Instance.PushEvent(msg.Data.OpCodeList, msg.Data);
                            }
                            else
                                HandleHeartbeat(msg.Data);
                            break;
                        case EventType.Disconnected:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        /// <summary>
        /// Send a message to the server. If the client is not connected, the message will be queued and sent when the client connects.
        /// </summary>
        public bool Send(byte[] content, OpCodeList opCodeList, bool sendNow = true)
        {
            if (Connected && !Connecting)
            {
                try
                {
                    if (content == null)
                    {
                        content = new byte[] { };
                    }

                    return fastWriter.Writer.TryWrite(() =>
                      {
                          var message = new Packet(content, OpCode.GetOpCodeNumber(opCodeList));

                          Send(message, sendNow);
                          _lastMessageSent = DateTime.Now;
                      });
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Use this function if you want to do something complex/heavy in another thread.
        /// Perfect to create a packet in another thread to avoid blocking the main thread and send it when it's ready.
        /// it's share the same channel as the standard send function. -> send 1 -> send 2 -> send 3 -> send 4 -> send 5
        /// </summary>
        public bool SendWithThread(Action action)
        {
            if (Connected && !Connecting)
            {
                try
                {
                    return fastWriter.Writer.TryWrite(action);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public bool Send(OpCodeList opCodeList)
        {
            return Send(new byte[] { }, opCodeList);
        }

        private bool SendNow()
        {
            if (_sendQueue.Count <= 0)
            {
                return false;
            }

            SendPending.Set();
            return true;
        }

        private bool Send(Packet data, bool sendNow)
        {
            if (Connected)
            {

                _sendQueue.Enqueue(data);

                if (sendNow)
                {
                    SendPending.Set();
                }

                return true;

            }

            return false;

        }

        private void SendThread()
        {
            try
            {
                while (Connected)
                {
                    SendPending.Reset();
                    if (Connected && _sendQueue.TryDequeueAll(out var messages))
                    {
                        if (!SendMessages(_stream, messages))
                        {
                            break;
                        }
                    }

                    SendPending.WaitOne();
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (ThreadInterruptedException)
            {
            }
            catch
            {
                Debug.Log("Closing Sending thread.");
            }

            Disconnect();
        }

        public async Task<bool> ConnectToServer(string ip, int port)
        {
            if ((Connected || Connecting) &&
                (_currentServer != null && ip + port == _currentServer))
            {
                return Connected;
            }

            _connecting = true;

            try
            {
                _currentServer = ip + port;
                _client = new TcpClient();
                _client.NoDelay = true;

                int timeout = 7500;
                var task = _client.ConnectAsync(ip, port);
                _client.ReceiveBufferSize = 16000;
                if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
                {
                    throw new SystemException("Error waiting too long.");
                }

                _stream = _client.GetStream();
                Debug.Assert(_stream != null);

                if (_withTls)
                {
                    var sslStream = new SslStream(
                        _stream,
                        true,
                        ValidateServerCertificate,
                        null
                    );

                    var taskSSL = sslStream.AuthenticateAsClientAsync(
                        "exemple.com",
                        null,
                        SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls,
                        false);
                    if (await Task.WhenAny(taskSSL, Task.Delay(timeout)) != taskSSL)
                    {
                        throw new SystemException("Error waiting too long.");
                    }

                    _stream = sslStream;
                }

                lock (receiveQueueLock)
                {
                    ReceiveQueue = new Queue<MessageStream>();
                }

                _sendQueue.Clear();
                _connecting = false;

                _receiveThread = new Thread(ReceiveThread);
                _receiveThread.IsBackground = true;
                _receiveThread.Start();

                _sendThread = new Thread(SendThread);
                _sendThread.IsBackground = true;
                _sendThread.Start();


                _writerThreadBuffer = new Thread(WriterThread);
                _writerThreadBuffer.IsBackground = true;
                _writerThreadBuffer.Start();

                // sleep for 1.5 seconds to let the connection establish.
                await Task.Delay(1500);
            }
            catch
            {
                _connecting = false;
                Disconnect();
                return false;
            }

            return true;
        }

        private static Int64 GetTimestamp(DateTime value)
        {
            return Int64.Parse(value.ToString("yyyyMMddHHmmssfff"));
        }

        private void CalculLatency(long latency)
        {
            latencies.Add(latency);

            if (latencies.Count > 10)
            {
                latencies.RemoveAt(0);
            }

            Latency = (long)latencies.Average();
        }

        private void HandleHeartbeat(Packet message)
        {
            var newDate = DateTime.Now;
            if (message.Payload.Length != 16) return;
            var s = BitConverter.ToInt64(message.Payload, 0);

            var date = DateTime.ParseExact(s.ToString(), "yyyyMMddHHmmssfff", null);

            var Latency = (long)(newDate - date).TotalMilliseconds;

            CalculLatency(Latency);
        }

        private DateTime _lastDateTime;
        public long Latency = 50;

        public void HearthBeat(int period)
        {
            if (Connected && !Connecting)
            {
                _lastDateTime = DateTime.Now;
                var currentTime = GetTimestamp(_lastDateTime);
                var time = BitConverter.GetBytes(currentTime);
                var ltc = Utils.UintToBigEndianX(Latency);
                var content = time.Concat(ltc).ToArray();
                Send(content, OpCodeList.HeartBeat);
            }
        }

        public void Disconnect()
        {
            lock (receiveQueueLock)
            {
                ReceiveQueue.Clear();
            }

            _stream?.Close();
            _stream?.Dispose();
            _client?.Close();
            _client?.Dispose();

            _receiveThread?.Interrupt();
            _sendThread?.Interrupt();
            _writerThreadBuffer?.Interrupt();

            _receiveThread = null;
            _sendThread = null;
            _writerThreadBuffer = null;

            _stream = null;
            _client = null;

            _connecting = false;
        }

        private static bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}