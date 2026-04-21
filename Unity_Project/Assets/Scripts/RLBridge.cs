using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

public class RLServerBridge : MonoBehaviour
{
    public static RLServerBridge Instance;

    [Header("Server")]
    public int port = 5555;
    public bool autoStart = true;

    private TcpListener listener;
    private TcpClient client;
    private StreamReader reader;
    private StreamWriter writer;

    public bool Connected => client != null && client.Connected;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (autoStart)
            StartServer();
    }

    public void StartServer()
    {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        Debug.Log($"RL Server started on port {port}");
        listener.BeginAcceptTcpClient(OnClientConnected, null);
    }

    private void OnClientConnected(IAsyncResult ar)
    {
        client = listener.EndAcceptTcpClient(ar);

        NetworkStream stream = client.GetStream();
        reader = new StreamReader(stream);
        writer = new StreamWriter(stream);
        writer.AutoFlush = true;

        Debug.Log("Python trainer connected.");
    }

    public void SendState(string json)
    {
        if (!Connected) return;
        writer.WriteLine(json);
    }

    public string ReceiveAction()
    {
        if (!Connected) return null;
        return reader.ReadLine();
    }

    private void OnApplicationQuit()
    {
        reader?.Close();
        writer?.Close();
        client?.Close();
        listener?.Stop();
    }
}