using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace DreamMachine.UnityBridge
{
    /// <summary>
    /// Optional viewer. The headless sim (`--serve` on port 4077) keeps the
    /// full tick loop. This only applies the latest line of state.
    /// </summary>
    public sealed class RemoteView : MonoBehaviour
    {
        public bool Active;
        public string Latest = "";
        public string Status = "local sim";
        TcpClient _client;
        Thread _thread;

        public void Connect(string host, int port)
        {
            Active = true;
            Status = "connecting " + host + ":" + port;
            _thread = new Thread(delegate ()
            {
                try
                {
                    _client = new TcpClient(host, port);
                    var reader = new StreamReader(_client.GetStream());
                    Status = "viewing " + host;
                    while (Active)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;
                        Latest = line;
                    }
                }
                catch (Exception ex)
                {
                    Status = ex.Message;
                    Active = false;
                }
            });
            _thread.IsBackground = true;
            _thread.Start();
        }

        void OnDestroy()
        {
            Active = false;
            if (_client != null) _client.Close();
        }
    }
}
