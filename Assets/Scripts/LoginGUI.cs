using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Colyseus;
using Marvin.JsonPatch;
using Newtonsoft.Json;
using Marvin.JsonPatch.Operations;

public class LoginGUI : MonoBehaviour
{
    public static string hostName = "localhost";
    public static int port = 2657;
    public static string userName = "User";
    public static string roomName = "chat";

    public GameObject loginPanel;
    public InputField serverInput;
    public InputField userInput;
    public InputField roomInput;

    public Text log;

    public static List<string> users = null;

    public static Client colyseus;
    public static Room<ChatRoom> chatRoom;


    public static string logText;

    public static LoginGUI inst;

    public LoginGUI()
    {
        inst = this;
    }

    void Awake()
    {
        //         var document = "{ firstName: \"Albert\", contactDetails: { phoneNumbers:[]} }";

        //         var patch = @"[ 
        //   { op: ""replace"", path: ""/firstName"", value: ""Joachim""}, 
        //   { op: ""add"", path: ""/lastName"", value: ""Wester"" }, 
        //   { op: ""add"", path: ""/contactDetails/phoneNumbers/0"", value: { number: ""555-123"" }  }
        // ]";



        // var doc = new SimpleDTO()
        // {
        //     StringProperty = "A",
        //     List = new List<DemoModel>()
        // };
        // doc.List.Add(new DemoModel { a = "A", b = 13 });


        // Log(doc.List[0].a);

        // // create patch

        // var patchDoc = new JsonPatchDocument<SimpleDTO>();
        // patchDoc.Add<string>(o => o.List[0].a, "B");

        // var serialized = JsonConvert.SerializeObject(patchDoc);
        // var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

        // deserialized.ApplyTo(doc);

        // Log(doc.List[0].a);
    }

    void Start()
    {
        Application.runInBackground = true;

        userName = "User" + Random.Range(1, 1000000);

        serverInput.text = hostName;
        userInput.text = userName;
        roomInput.text = roomName;

        loginPanel.SetActive(true);
    }

    void Update()
    {
        log.text = logText;
    }

    public static void Log(string message)
    {
        logText += message + "\n";
        Debug.Log(message);
    }

    public void OnLoginClick()
    {
        hostName = serverInput.text;
        userName = userInput.text;
        roomName = roomInput.text;
        StartCoroutine(Login());
    }

    IEnumerator Login()
    {
        String uri = "ws://" + hostName + ":" + port;

        Log("Connecting to " + hostName + ":" + port + " ...");

        colyseus = new Client(uri);
        colyseus.OnOpen += OnOpenHandler;

        yield return StartCoroutine(colyseus.Connect());

        chatRoom = colyseus.Join<ChatRoom>(roomName);
        chatRoom.OnJoin += OnRoomJoined;
        // chatRoom.OnUpdate += OnUpdateHandler;
        chatRoom.OnLeave += OnRoomLeave;

        // chatRoom.Listen("messsages", "add", this.OnAddMessages);
        // chatRoom.Listen("messsages", "replace", this.OnAddMessages);

        // chatRoom.state.Listen(this.OnChangeFallback);

        while (true)
        {
            colyseus.Recv();

            // string reply = colyseus.RecvString();
            if (colyseus.error != null)
            {
                Log("Error: " + colyseus.error);
                break;
            }
            yield return 0;
        }

        OnApplicationQuit();
    }

    private void OnAddMessages(string[] path, object value)
    {
        Debug.Log("OnAddMessages | " + ChatUtils.PathToString(path) + " | " + ChatUtils.ValueToString(value));
        List<object> messages = (List<object>)value;
        foreach (string m in messages)
        {
            Log(m);
        }
    }


    void OnOpenHandler(object sender, EventArgs e)
    {
        Log("Connected to server. Client id: " + colyseus.id);
    }

    void OnRoomJoined(object sender, EventArgs e)
    {
        Log("Joined room successfully.");
        loginPanel.SetActive(false);

        // chatRoom.state.RemoveAllListeners();
        GetComponent<ChatGUI>().StartChat(chatRoom);
        // chatRoom.state.Listen(this.OnChangeFallback);
    }

    private void OnRoomLeave(object sender, EventArgs e)
    {
        Log("Leave room.");
        loginPanel.SetActive(true);
    }

    void OnChangeFallback(string[] path, string operation, object value)
    {
        Log("OnChangeFallback | " + operation + " | " + ChatUtils.PathToString(path) + " | " + ChatUtils.ValueToString(value));
    }

    void OnUpdateHandler(object sender, RoomUpdateEventArgs<ChatRoom> e)
    {
        //Log(e.state);
    }

    void OnApplicationQuit()
    {
        // Ensure the connection with server is closed immediatelly
        if (colyseus != null)
            colyseus.Close();
    }


}