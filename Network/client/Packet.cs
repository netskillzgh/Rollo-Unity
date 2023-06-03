
using System;
using UnityEngine;

namespace Rollo.Client
{

    public class Packet
    {
        public readonly byte[] Payload;
        public readonly int Cmd;
        public readonly OpCodeList OpCodeList;
        public long time;
        public object o;

        public Packet(byte[] payload, int cmd)
        {
            if (payload == null)
            {
                payload = new byte[0];
            }

            Cmd = cmd;
            Payload = payload;
            OpCodeList = OpCode.GetOpCodeEnum(cmd);
        }

        public T GetObject<T>()
        {
            if (o != null)
            {
                try
                {
                    return (T)o;
                }
                catch (InvalidCastException ue)
                {
                    Debug.Log(ue.ToString());
                }
            }

            return default(T);
        }
    }
}