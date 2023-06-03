using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Rollo.Client
{
    public abstract class StreamLoops
    {
        static readonly AsyncLocal<byte[]> Header = new AsyncLocal<byte[]>();
        static readonly AsyncLocal<byte[]> CmdHeader = new AsyncLocal<byte[]>();
        static readonly AsyncLocal<byte[]> Payload = new AsyncLocal<byte[]>();

        public Queue<MessageStream> ReceiveQueue = new Queue<MessageStream>();

        protected bool GetNextMessage(out MessageStream message)
        {
            return ReceiveQueue.TryDequeue(out message);
        }

        [ItemCanBeNull]
        protected static async Task<Packet> ReadMessage(Stream stream)
        {
            Debug.Assert(stream != null);
            Packet message;

            if (Header.Value == null)
                Header.Value = new byte[4];


            if (!await stream.ReadExactlyAsync(Header.Value, 4))
            {
                Debug.LogError("stop");
                return null;
            }

            var size = Utils.BytesToIntBigEndian(Header.Value);

            if (CmdHeader.Value == null)
                CmdHeader.Value = new byte[2];

            if (!await stream.ReadExactlyAsync(CmdHeader.Value, 2))
            {
                Debug.LogError("stop");
                return null;
            }

            var cmd = Utils.BytesToIntBigEndianCommand(CmdHeader.Value);

            message = new Packet(new byte[size], cmd);

            if (!await stream.ReadExactlyAsync(message.Payload, size))
            {
                Debug.LogError("stop");
                return null;
            }

            return message;
        }

        protected bool SendMessages(Stream stream, Packet[] messages)
        {
            Debug.Assert(stream.CanWrite);
            Debug.Assert(stream != null);
            int packetSize = 0;

            for (int i = 0; i < messages.Length; ++i)
                packetSize += sizeof(uint) + sizeof(ushort) + messages[i].Payload.Length;

            if (packetSize <= 0) return true;

            if (Payload.Value == null || Payload.Value.Length < packetSize)
                Payload.Value = new byte[packetSize];

            Debug.Assert(Payload.Value != null && Payload.Value.Length >= packetSize);

            int position = 0;
            for (int i = 0; i < messages.Length; ++i)
            {
                if (Header.Value == null)
                    Header.Value = new byte[4];

                var sourceArray = messages[i].Payload;
                if (sourceArray != null)
                {
                    Utils.IntToBytesBigEndianNonAlloc(sourceArray.Length, Header.Value);

                    if (CmdHeader.Value == null)
                        CmdHeader.Value = new byte[2];

                    Utils.CommandToBytesBigEndianNonAlloc(messages[i].Cmd, CmdHeader.Value);

                    Array.Copy(Header.Value, 0, Payload.Value, position, Header.Value.Length);
                    Array.Copy(CmdHeader.Value, 0, Payload.Value, position + Header.Value.Length, CmdHeader.Value.Length);

                    if (sourceArray.Length > 0)
                    {
                        Array.Copy(sourceArray, 0, Payload.Value, position + Header.Value.Length + CmdHeader.Value.Length,
                        sourceArray.Length);
                    }

                    position += Header.Value.Length + CmdHeader.Value.Length + sourceArray.Length;
                }
            }

            Debug.Assert(Payload.Value.Length >= packetSize && packetSize > 0);
            Debug.Assert(packetSize > 0);
            stream.Write(Payload.Value, 0, packetSize);

            return true;
        }
    }
}