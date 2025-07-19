using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class APITest : MonoBehaviour
{
    [SerializeField] private string apiUrl = "https://api.openai.com/v1/images/generations";
    [SerializeField] private string apiKey = "your_api_key_here"; // 在 Inspector 中输入你的 OpenAI API 密钥

    void Start()
    {
        StartCoroutine(TestConnection());
    }

    private IEnumerator TestConnection()
    {
        // 构造 JSON 请求体
        string json = "{\"prompt\":\"A futuristic city with flying cars\",\"n\":1,\"size\":\"1024x1024\"}";
        Debug.Log($"Sending test request: {json}");

        // 创建 HTTP 请求
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        // 设置超时时间
        request.timeout = 15;

        // 发送请求
        yield return request.SendWebRequest();

        // 检查请求结果
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Successfully connected to OpenAI API.");
            Debug.Log($"Response: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"Connection failed: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
        }
    }
}
