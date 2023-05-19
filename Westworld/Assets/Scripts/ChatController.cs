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
    private List<ChatMessage> messageList0; // Chat to chatGPT(storyteller)
    private List<ChatMessage> messageList1; // Chat to Character1
    private List<ChatMessage> messageList2; // Chat to Character2

    // player
    private GameObject player = null;

    // NPC
    private int talk2whom = 0;  // 0=ChatGPT, 1=Cow, 2=Sheep
    public TMP_Text talkingCharacter;

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
        api = new OpenAI_API.OpenAIAPI("your key");
        InitializeRole(); 

        // Get player's transform
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    private void InitializeRole()
    {
        messageList0 = new List<ChatMessage> {
            new ChatMessage(ChatMessageRole.System, 
            @"Start a role play. Now, your name is 'Storyteller'. You are an expert story teller. 
                Develop a short situation between two characters: 'Conan' and 'Shannon' in a farm scene, and the result is that 
                Conan feels angry and Shannon feels sad. 
                I will be a player to interact with the two characters to solve this situation. 
                The player should provide at least 2 positive feedbacks in the conversations so that 
                both Conan and Shannon will feel relieved. 
            ")
        };

        // messageList1 = new List<ChatMessage> {
        //     new ChatMessage(ChatMessageRole.System, 
        //     @"Start a role play. You are the character 'Conan' in the story. You feels angry, and the level of your angry is 5 out of 5. 
        //         A player will talk to you. If the conversation is positive, you will feel much better. 
        //         If the conversation is negative, you will still remain angry. 
        //     ")
        // };

        // messageList2 = new List<ChatMessage> {
        //     new ChatMessage(ChatMessageRole.System, 
        //     @"Start a role play. You are the character 'Shannon' in the story. You feels sad, and the level of your sadness is 5 out of 5. 
        //         A player will talk to you. If the conversation is positive, you will feel much better. 
        //         If the conversation is negative, you will still remain sad. 
        //     ")
        // };

    }     

    // Update is called once per frame
    void Update()
    {
        WhomToTalkTo();
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

    private void WhomToTalkTo()
    {
        // Check whether encounter NPCs
        float interactRange = 3f;
        Collider[] colliderArray = Physics.OverlapSphere(player.transform.position, interactRange);
        foreach (Collider collider in colliderArray) 
        {
            if (collider.CompareTag("NPC_Cow"))
            {
                talk2whom = 1;
                Debug.Log("Talk to Cow");
            }
            else if (collider.CompareTag("NPC_Sheep"))
            {
                talk2whom = 2;
                Debug.Log("Talk to Sheep");
            }
        }

        switch (talk2whom)
        {
            case 0: 
                talkingCharacter.text = "Talking to ChatGPT now ...";    
                break;
            case 1: 
                talkingCharacter.text = "Talking to Cow now ...";    
                break; 
            case 2: 
                talkingCharacter.text = "Talking to Sheep now ...";    
                break; 
            default: 
                talkingCharacter.text = "Talking to ChatGPT now ...";    
                break;
        }

    }

    public async void SendMessageToBot()
    {
        
        List<ChatMessage> message2GPT;  // message record for this prompt
        message2GPT = messageList0;     // Read previous record

        // Check whom am I takling to
        // ChatMessage msg2character = new ChatMessage();
        // msg2character.Role = ChatMessageRole.System;
        string character;
        string msg2character; 
        switch (talk2whom)
        {
            case 0: 
                character = "ChatGPT";
                msg2character = @"
                    I am talking to the Storyteller. You should response me as the Storyteller. ";     
                break;
            
            case 1:
                character = "Conan"; 
                msg2character = @"
                    I am talking to the first character: 'Conan'.
                    You are roleplaying as 'Conan', and you should response the following message as 'Conan'. ";     
                break;
            
            case 2: 
                character = "Shannon";
                msg2character = @"
                    I am talking to the second character: 'Shannon'.
                    You are roleplaying as 'Shannon', and you should response the following message as 'Shannon'. ";                   
                    break;
            default: 
                character = "ChatGPT";
                msg2character = @"
                    I am talking to the Storyteller. You should response me as the Storyteller. ";     
                break;
        }       

        // Fill the user message from the input field
        ChatMessage userMessage = new ChatMessage();
        userMessage.Role = ChatMessageRole.User;    // assign role
        userMessage.Content = msg2character + userInput.text;       // assign content

        // // Avoid long messages  
        // if (userMessage.Content.Length > 500)
        // {
        //     userMessage.Content = userMessage.Content.Substring(0, 500);
        // }
 
        // Add the user's input to the list
        message2GPT.Add(userMessage);

        // Debug.Log(string.Format("{0}: {1}", message2GPT[0].rawRole, message2GPT[0].Content));
        Debug.Log(string.Format("{0}: {1}", userMessage.rawRole, userMessage.Content));

        // Update the Chat Panel content 
        chatContent.text = "<color=#FFB000>You: " + userInput.text + "</color>";

        // Clear user input field
        userInput.text = "";

        // Send chat to OpenAI
        var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest() {
            Model = Model.ChatGPTTurbo, 
            Temperature = 0.7,  // 0 = deterministic, 1 = change wildly
            MaxTokens = 100,
            Messages = message2GPT 
        });

        // Get the response message
        ChatMessage responseMessage = new ChatMessage();
        responseMessage.Role = chatResult.Choices[0].Message.Role;
        responseMessage.Content = chatResult.Choices[0].Message.Content;
        Debug.Log(string.Format("{0}: {1}", responseMessage.rawRole, responseMessage.Content));

        // message2GPT.Add(responseMessage);

        // Udate the text field
        chatContent.text += "<color=#648FFF>\n\n" + character + ":\n" + responseMessage.Content + "</color>";

        // Add back ChatGPT's response to the corresponding list
        // StoryTeller should know all messages
        messageList0.Add(userMessage);     
        messageList0.Add(responseMessage);  

    }
}
