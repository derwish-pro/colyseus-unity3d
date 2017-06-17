using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using UnityEngine;


public class WebSocket
{
    private Uri mUrl;

    public WebSocket(Uri url)
    {
        mUrl = url;

        string protocol = mUrl.Scheme;
        if (!protocol.Equals("ws") && !protocol.Equals("wss"))
            throw new ArgumentException("Unsupported protocol: " + protocol);
    }

    public void SendString(string str)
    {
        Send(Encoding.UTF8.GetBytes(str));
    }

    public string RecvString()
    {
        byte[] retval = Recv();
        if (retval == null)
            return null;
        return Encoding.UTF8.GetString(retval);
    }


    BestHTTP.WebSocket.WebSocket m_Socket;
    Queue<byte[]> m_Messages = new Queue<byte[]>();
    Queue<string> string_Messages = new Queue<string>();
    bool m_IsConnected = false;
    string m_Error = null;

    public IEnumerator Connect()
    {
        m_Socket = new BestHTTP.WebSocket.WebSocket(mUrl);
        m_Socket.OnMessage += (webSocket, message) => string_Messages.Enqueue(message);
        m_Socket.OnBinary += (webSocket, message) => m_Messages.Enqueue(message);
        m_Socket.OnOpen += (webSocket) => m_IsConnected = true;
        m_Socket.OnError += (webSocket, e) => m_Error = e.Message;
        m_Socket.OnClosed += (webSocket, code, message) => Debug.Log("Socket closed");

        m_Socket.Open();

        while (!m_IsConnected && m_Error == null)
            yield return 0;
    }

    public void Send(byte[] buffer)
    {
        m_Socket.Send(buffer);
    }


    public byte[] Recv()
    {
        if (m_Messages.Count != 0)
            return m_Messages.Dequeue();

        if (string_Messages.Count != 0)
            return Encoding.UTF8.GetBytes(string_Messages.Dequeue());

        return null;
    }

    public void Close()
    {
        m_Socket.Close();
    }

    public string error
    {
        get
        {
            return m_Error;
        }
    }
}
