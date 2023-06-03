using System.Collections.Generic;
using UnityEngine;

namespace Rollo.Client
{
    public class RolloNotificationCenter : MonoBehaviour
    {
        public static RolloNotificationCenter Instance = null;
        private Dictionary<OpCodeList, (NotificationHandler, DeserializeFunction)> handlers = new Dictionary<OpCodeList, (NotificationHandler, DeserializeFunction)>();
        private object thisLock = new object();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
        }

        public void AddEventListener(OpCodeList opCode, NotificationHandler listener, DeserializeFunction deserializeFunction = null)
        {
            lock (thisLock)
            {
                if (handlers.ContainsKey(opCode))
                {
                    handlers[opCode] = (listener, deserializeFunction);
                }
                else
                {
                    handlers.Add(opCode, (listener, deserializeFunction));
                }
            }
        }

        public void GetDesialisationFunction(OpCodeList opCode, out DeserializeFunction desialisationFunction)
        {
            lock (thisLock)
            {
                if (handlers.ContainsKey(opCode))
                {
                    desialisationFunction = handlers[opCode].Item2;
                }
                else
                {
                    desialisationFunction = null;
                }
            }
        }

        public void Clear()
        {
            lock (thisLock)
            {
                handlers.Clear();
            }
        }

        public void Remove(OpCodeList key)
        {
            lock (thisLock)
            {
                handlers.Remove(key);
            }
        }

        public void Dispose(Object obj)
        {
            lock (thisLock)
            {
                List<KeyValuePair<OpCodeList, (NotificationHandler, DeserializeFunction)>> list = new List<KeyValuePair<OpCodeList, (NotificationHandler, DeserializeFunction)>>();
                foreach (var handler in handlers)
                {
                    if (handler.Value.Item1.Target == null || ReferenceEquals(handler.Value.Item1.Target, obj))
                    {
                        list.Add(handler);
                    }
                }

                foreach (var handler in list)
                {
                    handlers.Remove(handler.Key);
                }
            }
        }

        private void Clean()
        {
            lock (thisLock)
            {
                List<KeyValuePair<OpCodeList, (NotificationHandler, DeserializeFunction)>> list = new List<KeyValuePair<OpCodeList, (NotificationHandler, DeserializeFunction)>>();
                foreach (var handler in handlers)
                {
                    if (handler.Value.Item1.Target == null)
                    {
                        list.Add(handler);
                    }
                }

                foreach (var handler in list)
                {
                    handlers.Remove(handler.Key);
                }
            }
        }

        public void PushEvent(OpCodeList opCode, Packet arg)
        {
            lock (thisLock)
            {
                if (!handlers.ContainsKey(opCode))
                {
                    return;
                }

                if (handlers[opCode].Item1 != null)
                {
                    if (handlers[opCode].Item1.Target == null)
                    {
                        handlers.Remove(opCode);
                        return;
                    }
                    handlers[opCode].Item1?.Invoke(arg);
                }
            }
        }
    }
}