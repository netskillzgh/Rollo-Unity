using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Rollo.Client
{
    public class ServerListener : MonoBehaviour
    {
        [SerializeField] public bool reactOnDisconnectFromServer = true;
        [SerializeField] private bool disconnectOnDestroy = false;
        [SerializeField] private bool reconnectOnStart = false;
        [SerializeField] public UnityEvent onDisconnectFromServer;
        [SerializeField] public UnityEvent onReconnectedToTheServer;
        [SerializeField] public UnityEvent onTryingToReconnectToTheServer;
        public bool listen = true;

        public NetworkClient client => NetworkManager.Instance.Client;

        private void Start()
        {
            StartCoroutine(Listener());
        }

        private IEnumerator Listener()
        {
            while (true)
            {

                if (reactOnDisconnectFromServer && !client.client.Connected && !client.client.Connecting)
                {
                    setReactionOnDisconnect(false);
                    onDisconnectFromServer?.Invoke();
                    yield return new WaitForSeconds(0.05f);
                }

                if (listen)
                {
                    client.ListenMessages();
                }

                yield return new WaitForSeconds(0.001f);
            }
        }

        private void OnDestroy()
        {
            if (disconnectOnDestroy)
            {
                client.Disconnect();
            }
        }

        private void Awake()
        {
            OnAwakeAsync();
        }

        private async void OnAwakeAsync()
        {
            if (reconnectOnStart && !client.client.Connected)
            {
                onTryingToReconnectToTheServer?.Invoke();
                var result = await NetworkManager.Instance.ReconnectServer();

                if (result)
                {
                    setReactionOnDisconnect(true);
                    onReconnectedToTheServer?.Invoke();
                }
                else
                {
                    onDisconnectFromServer?.Invoke();
                }
            }
        }

        public void setReactionOnDisconnect(bool state)
        {
            reactOnDisconnectFromServer = state;
        }
    }
}
