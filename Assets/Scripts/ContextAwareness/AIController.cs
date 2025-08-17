using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

#region JSON Data Models

[Serializable]
public class ChatCompletionResponse
{
    [JsonProperty("choices")] 
    public List<Choice> Choices { get; set; }
}

[Serializable]
public class Choice
{
    [JsonProperty("message")]
    public Message Message { get; set; }
}

[Serializable]
public class Message
{
    [JsonProperty("Role")]
    public string Role { get; set; }
    
    [JsonProperty("content")]
    public string Content { get; set; }
}

#endregion

public class AIController : MonoBehaviour
{
    [Header("Environment Detection Reference")]
    [SerializeField] private EnvDetectionController envDetectionController;
    
    [Header("AI Reference")]
    [SerializeField] private ApiConfig apiConfig;
    private string apiUrl = "https://api.openai.com/v1/chat/completions";

    private void Start()
    {
        envDetectionController.OnWebcamScreenshotCollected += RecognizeUserEnv;
    }

    private void RecognizeUserEnv(Texture2D passthroughCamTexture2D)
    {
        Debug.Log($"~~~ received snapshot pixels: ({passthroughCamTexture2D.width}, {passthroughCamTexture2D.height})");
    }

    [Button]
    public void SendAIRequest()
    {
        StartCoroutine(GetChatResponse("", ParseChatResponse));
    }

    private IEnumerator GetChatResponse(string prompt, Action<string> callback)
    {
        if(string.IsNullOrEmpty(apiConfig.apiKey))
            yield break;
        
        var jsonData = new
        {
            model = "gpt-4o",
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant" },
                new { role = "user", content = "hello?" },
            },
            max_tokens = 500
        };
        string jsonString = JsonConvert.SerializeObject(jsonData);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonString);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiConfig.apiKey}");
        
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(request.error);
            callback?.Invoke(null);
        }
        else
        {
            callback?.Invoke(request.downloadHandler.text);
        }
    }

    private void ParseChatResponse(string response)
    {
        ChatCompletionResponse parsedResponse = JsonConvert.DeserializeObject<ChatCompletionResponse>(response);
        string aiReply = parsedResponse.Choices[0].Message.Content;
        Debug.Log("~~~ AI response: " + aiReply);
    }

}
