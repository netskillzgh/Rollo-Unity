using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rollo.Client
{
    class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance;
        [SerializeField] public string masterHost = "127.0.0.1";
        [SerializeField] private int masterPort = 6666;
        [SerializeField] private int heartbeatInterval = 6;
        [SerializeField] private bool withTls = false;
        public NetworkClient Client;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Application.runInBackground = true;
                Client = new NetworkClient(withTls);
            }

            if (Instance != this)
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartCoroutine(HeartBeat());
        }

        private void OnDestroy()
        {
            CloseConnection();
        }

        private void OnApplicationQuit()
        {
            CloseConnection();
        }

        private void CloseConnection()
        {
            Client?.client?.Disconnect();
            Debug.Assert(!Client?.client?.Connected ?? false);
        }

        public async Task<bool> ReconnectServer()
        {
            return await ConnectToServer(null);
        }

        public async Task<bool> ConnectToServer(ServerListener authServerListener)
        {
            return await Client.ConnectToServer(masterHost, masterPort);
        }

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }


        private IEnumerator HeartBeat()
        {
            while (true)
            {
                yield return new WaitForSeconds(heartbeatInterval);
                Client?.HearthBeat(heartbeatInterval);
            }
        }
    }
}