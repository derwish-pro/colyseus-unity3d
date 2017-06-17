﻿using UnityEngine;
using System.Collections;
using System;
using Colyseus;

public class ColyseusClient : MonoBehaviour
{
    Client colyseus;
    Room<ChatRoom> chatRoom;
    public string serverName = "localhost";
    public string port = "3553";
    public string roomName = "chat";

    // Use this for initialization
    IEnumerator Start()
    {
        String uri = "ws://" + serverName + ":" + port;
        colyseus = new Client(uri);
        colyseus.OnOpen += OnOpenHandler;
        yield return StartCoroutine(colyseus.Connect());

        chatRoom = colyseus.Join<ChatRoom>(roomName);
        chatRoom.OnJoin += OnRoomJoined;
        chatRoom.OnUpdate += OnUpdateHandler;

        // chatRoom.state.Listen("players", "add", this.OnAddPlayer);
        // chatRoom.state.Listen("players/:id/:axis", "replace", this.OnPlayerMove);
        // chatRoom.state.Listen("players/:id", "remove", this.OnPlayerRemoved);
        // chatRoom.state.Listen(this.OnChangeFallback);

        int i = 0;

        while (true)
        {
            colyseus.Recv();

            // string reply = colyseus.RecvString();
            if (colyseus.error != null)
            {
                Debug.LogError("Error: " + colyseus.error);
                break;
            }

            i++;

            if (i % 50 == 0)
            {
                chatRoom.Send("some_command");
            }
            yield return 0;
        }

        OnApplicationQuit();
    }

    void OnOpenHandler(object sender, EventArgs e)
    {
        Debug.Log("Connected to server. Client id: " + colyseus.id);
    }

    void OnRoomJoined(object sender, EventArgs e)
    {
        Debug.Log("Joined room successfully.");
    }

    void OnAddPlayer(string[] path, object value)
    {
        Debug.Log("OnAddPlayer | " + PathToString(path) + " | " + ChatUtils.ValueToString(value));
    }

    void OnPlayerMove(string[] path, object value)
    {
        Debug.Log("OnPlayerMove | " + PathToString(path) + " | " + ChatUtils.ValueToString(value));
    }

    void OnPlayerRemoved(string[] path, object value)
    {
        Debug.Log("OnPlayerRemoved | " + PathToString(path) + " | " + ChatUtils.ValueToString(value));
    }

    void OnChangeFallback(string[] path, string operation, object value)
    {
        Debug.Log("OnChangeFallback | " + operation + " | " + PathToString(path) + " | " + ChatUtils.ValueToString(value));
    }

    private string PathToString(string[] path)
    {
        string fullPath = "";
        for (int i = 0; i < path.Length; i++)
        {
            fullPath += path[i];
            if (i != path.Length - 1)
                fullPath += ".";
        }
        return fullPath;
    }


    void OnUpdateHandler(object sender, RoomUpdateEventArgs<ChatRoom> e)
    {
        //Debug.Log(e.state);
    }

    void OnApplicationQuit()
    {
        // Ensure the connection with server is closed immediatelly
        colyseus.Close();
    }
}