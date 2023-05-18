using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// OpenAI package
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

public class ChatController : MonoBehaviour
{
    // OpenAI
    public TMP_Text chatContent;  // Read Content in Scroll View
    public TMP_InputField userInput;    // User input field

    private OpenAIAPI api;
    private List<ChatMessage> messageList;  // Record of the whole Chat to chatGPT
    private List<ChatMessage> messageList0; // Chat to chatGPT


    // Start is called before the first frame update
    void Start()
    {

        // Read ChatGPT key way1:
        // string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User);

        // if (apiKey == null)
        //     Debug.Log("Error! Cannot load OPENAI_API_KEY correctly.");
        // else
        //     Debug.Log("OPENAI_API_KEY= " + apiKey);

        // api = new OpenAI_API.OpenAIAPI(apiKey);

        // Read ChatGPT key way2:
        api = new OpenAI_API.OpenAIAPI("your OPENAI_API_KEY");
        InitializeRole(); 
    }

    private void InitializeRole()
    {
        messageList0 = new List<ChatMessage> {
            new ChatMessage(ChatMessageRole.System, 
            @"Start a role play. Now, your name is 'Jarvis'. You are an expert story teller. 
                You will work closely with a player.
            ")
        };

        messageList = messageList0;        
    }     

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) // The "T" key is used to activate the chat window
        {
            userInput.ActivateInputField();
        }
        if (Input.GetKeyDown(KeyCode.Return)) // The "return" key is used to send a user's message
        {
           SendMessageToBot();
        }

        if (Input.GetKeyDown(KeyCode.Escape)) // The "escape" key is used to terminate the program
        {
            Application.Quit();
        }        
    }

    public async void SendMessageToBot()
    {
        // Fill the user message from the input field
        ChatMessage userMessage = new ChatMessage();
        userMessage.Role = ChatMessageRole.User;    // assign role
        userMessage.Content = userInput.text;       // assign content

        // Avoid long messages  
        if (userMessage.Content.Length > 200)
        {
            userMessage.Content = userMessage.Content.Substring(0, 200);
        }
 
        // Add the message to the list
        messageList.Add(userMessage);

        Debug.Log(string.Format("{0}: {1}", messageList[0].rawRole, messageList[0].Content));
        Debug.Log(string.Format("{0}: {1}", userMessage.rawRole, userMessage.Content));

        // Update the text field 
        // chatContent.text = string.Format("You: {0}", userMessage.Content);   // alternative way
        chatContent.text = "<color=#1E88E5>You: " + userMessage.Content + "</color>";

        // Clear the input field
        userInput.text = "";

        // Send chat to OpenAI
        var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest() {
            Model = Model.ChatGPTTurbo, 
            Temperature = 0.7,  // 0 = deterministic, 1 = change wildly
            MaxTokens = 100,
            Messages = messageList 
        });

        // Get the response message
        ChatMessage responseMessage = new ChatMessage();
        responseMessage.Role = chatResult.Choices[0].Message.Role;
        responseMessage.Content = chatResult.Choices[0].Message.Content;
        Debug.Log(string.Format("{0}: {1}", responseMessage.rawRole, responseMessage.Content));

        // Add the response to the list
        messageList.Add(responseMessage);

        // Udate the text field
        // chatContent.text = string.Format("You:\n\t{0}\n\n\nNPC_1:\n\t{1}", userMessage.Content, responseMessage.Content);    // alternative way
        chatContent.text += "<color=#D81B60>\n\nChatBot:\n\t" + responseMessage.Content + "</color>";

    }
}
