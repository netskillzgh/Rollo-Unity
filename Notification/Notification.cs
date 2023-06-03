using System;
using UnityEngine;

namespace Rollo.Client
{
    public delegate object DeserializeFunction(byte[] data);
    public delegate void NotificationHandler(Packet agr);

    public class Notification
    {
        private string _type;
        private Packet _arg;

        public Notification(string type, Packet arg)
        {
            _type = type;
            _arg = arg;
        }
    }

    public class NotificationArg
    {
        private object value;

        public NotificationArg(object v)
        {
            value = v;
        }

        public T GetValue<T>()
        {
            try
            {
                return (T)value;

            }
            catch (InvalidCastException ue)
            {
                Debug.Log(ue.ToString());
            }

            return default(T);
        }
    }
}