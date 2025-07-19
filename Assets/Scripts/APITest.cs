using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class APITest : MonoBehaviour
{
    [SerializeField] private string apiUrl = "https://api.openai.com/v1/images/generations";
    [SerializeField] private string apiKey = "your_api_key_here"; // �� Inspector ��������� OpenAI API ��Կ

    void Start()
    {
        StartCoroutine(TestConnection());
    }

    private IEnumerator TestConnection()
    {
        // ���� JSON ������
        string json = "{\"prompt\":\"A futuristic city with flying cars\",\"n\":1,\"size\":\"1024x1024\"}";
        Debug.Log($"Sending test request: {json}");

        // ���� HTTP ����
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        // ���ó�ʱʱ��
        request.timeout = 15;

        // ��������
        yield return request.SendWebRequest();

        // ���������
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
