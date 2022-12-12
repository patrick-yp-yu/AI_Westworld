using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class Message
{
    public string Text;
    public TMP_Text TextObject;
    public MessageType MessageType;
}

public enum MessageType
{
    User, Bot
}
 
public class Messager : MonoBehaviour
{
    public GameObject chatPanel, textObject;
    public TMP_InputField chatBox;
    public TMP_Text toggle;
    public Color UserColor, BotColor;
    List<Message> Messages = new List<Message>();

    char mode = '0';
    
    private ObjectGeneration objectGeneration;

    string prefix = "http://127.0.0.1:5000/";

    // Start is called before the first frame update
    void Start()
    {
        objectGeneration = new ObjectGeneration();

        // Initializing list of prefab object names to send to server
        string[] files = Directory.GetFiles(@"Assets/Resources", "*.prefab");
        string allPrefabNames = "/prefab?list=";
        foreach(string file in files)
        {
            allPrefabNames = allPrefabNames + Path.GetFileNameWithoutExtension(file) + ",";
        }
        if (!string.IsNullOrEmpty(allPrefabNames)) {
            allPrefabNames = allPrefabNames[..^1];
        }
        Debug.Log($"Files: {allPrefabNames}");
        StartCoroutine(GetRequest(allPrefabNames));
    }

    // Handling requests to server
    IEnumerator GetRequest(string uri)
    {
        uri = prefix+uri;
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();
 
            string[] pages = uri.Split('/');
            int page = pages.Length - 1;
 
            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                    Debug.LogError(pages[page] + ": Connection Error: " + webRequest.error);
                    AddMessage("Error: Not connected to server!", MessageType.Bot);
                    break;
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                    ParseResponse(webRequest.downloadHandler.text, MessageType.Bot);
                    break;
            }
        }
    }

    // Parsing JSON response from server into text message and the response data structure
    public void ParseResponse(string messageText, MessageType messageType) 
    {
        if (messageText[0] != '0') { // '0' response means request was for initializing object list and should not be parsed
            Response response = JsonUtility.FromJson<Response>(messageText);
            // objectGeneration.PickObject(response.Object);
            objectGeneration.PickPrefab(response.Object);
            AddMessage($"Bot: {response.Text.Trim()}", messageType);
        }
    }

    // Print a text message to the chat window (with MessageTypes of User or Bot)
    public void AddMessage (string messageText, MessageType messageType) 
    {
        if (Messages.Count >= 25)
        {
            Destroy(Messages[0].TextObject.gameObject);
            Messages.Remove(Messages[0]);
        }

        var newMessage = new Message { Text = messageText };
        var newText = Instantiate(textObject, chatPanel.transform);

        newMessage.TextObject = newText.GetComponent<TMP_Text>();
        newMessage.TextObject.text = messageText;
        newMessage.TextObject.color = messageType == MessageType.User ? UserColor : BotColor;

        Messages.Add(newMessage);
    }

    // Send the contents of the chat box to be processed by the server
    public void SendMessageToBot()
    {
        var userMessage = chatBox.text.Trim();
        
        if (!string.IsNullOrEmpty(userMessage))
        {
            Debug.Log($"Bot: [USER] {userMessage}");
            AddMessage($"User: {userMessage}", MessageType.User);

            var serverMessage = mode+userMessage;
            StartCoroutine(GetRequest(serverMessage));
            chatBox.Select();
            chatBox.text = "";
        }
    }

    // Toggle the mode of response, which can be modified to include modes as needed
    public void ToggleMode() {
        if (toggle.text == "Mode 0") {
            toggle.text = "Mode 1";
            mode = '1';
        } else if (toggle.text == "Mode 1") {
            toggle.text = "Mode 2";
            mode = '2';
        } else if (toggle.text == "Mode 2") {
            toggle.text = "Mode 0";
            mode = '0';
        } 
    }
 
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) // The "T" key is used to activate the chat window
        {
            chatBox.ActivateInputField();
        }
        if (Input.GetKeyDown(KeyCode.Return)) // The "return" key is used to send a user's message
        {
           SendMessageToBot();
        }
        if (Input.GetKeyDown(KeyCode.Tab)) // The "tab" key is used to toggle the response mode
        {
            ToggleMode();
        }
        if (Input.GetKeyDown(KeyCode.Escape)) // The "escape" key is used to terminate the program
        {
            Application.Quit();
        }
    }
}