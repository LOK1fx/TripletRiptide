﻿using RiptideNetworking;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum ServerToClientId : ushort
{
    spawnPlayer = 1,
    playerMovement,
}
public enum ClientToServerId : ushort
{
    playerName = 1,
    playerInput,
}

public class NetworkManager : MonoBehaviour
{
    private static NetworkManager _singleton;
    public static NetworkManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying object!");
                Destroy(value);
            }
        }
    }

    [SerializeField] private ushort port;
    [SerializeField] private ushort maxClientCount;
    [SerializeField] private GameObject playerPrefab;

    public GameObject PlayerPrefab { get => playerPrefab; }

    public Server Server { get; private set; }
    private ActionQueue actionQueue;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

#if UNITY_EDITOR
        RiptideLogger.Initialize(Debug.Log, false);
#else
        Console.Title = "Pirate Game Server";
        Console.Clear();
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        RiptideLogger.Initialize(Debug.Log, true);
#endif

        Dictionary<ushort, Server.MessageHandler> messageHandlers = new Dictionary<ushort, Server.MessageHandler>()
        {
            { (ushort)ClientToServerId.playerName, ServerHandle.PlayerName },
            { (ushort)ClientToServerId.playerInput, ServerHandle.PlayerInput },
        };

        actionQueue = new ActionQueue();

        Server = new Server();
        Server.ClientConnected += NewPlayerConnected;
        Server.ClientDisconnected += PlayerLeft;

        Server.Start(port, maxClientCount, messageHandlers, actionQueue);
    }

    private void FixedUpdate()
    {
        actionQueue.ExecuteAll();
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    private void NewPlayerConnected(object sender, ServerClientConnectedEventArgs e)
    {
        foreach (Player player in Player.List.Values)
        {
            if (player.Id != e.Client.Id)
            {
                player.SendSpawn(e.Client);
            }
        }
    }

    private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        Destroy(Player.List[e.Id].gameObject);
    }
}