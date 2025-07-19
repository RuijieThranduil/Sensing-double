using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class DataItem
{
    public string url;
}

[System.Serializable]
class ResponseData
{
    private long created;
    public DataItem[] data;
}

public enum ModelType
{
    dall_e_2,
    gpt_image_1
}

public class DalleAPI : MonoBehaviour
{
    [SerializeField] private string apiKey = ""; // �� Inspector ��������� API ��Կ
    public ModelType Model = ModelType.dall_e_2;
    
    /**
     * prompt => image *
     */
    public IEnumerator GenerateImage(string prompt, System.Action<Texture2D> onComplete)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Debug.LogError("Prompt cannot be empty.");
            yield break;
        }

        // ���� JSON ������
        string json = $"{{\"prompt\":\"{prompt}\",\"n\":1,\"size\":\"1024x1024\"}}";
        Debug.Log($"Sending request: {json}");

        // ���� HTTP ����
        string apiUrl = "https://api.openai.com/v1/images/generations";
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Response: {request.downloadHandler.text}");

            string responseText = request.downloadHandler.text;
            string imageUrl = ParseImageUrl(responseText);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                // ����ͼƬ
                StartCoroutine(DownloadImage(imageUrl, onComplete));
            }
            else
            {
                Debug.LogError("Failed to extract image URL from response.");
            }
        }
        else
        {
            Debug.LogError($"Error: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
        }
    }
    
    /**
     * prompt + mask + src => image *
     */
    public IEnumerator GenerateImageWithPromptAndImage(string prompt, System.Action<Texture2D> onComplete)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Debug.LogError("Prompt cannot be empty.");
            yield break;
        }
        WWWForm form = new WWWForm();
        form.AddField("prompt", prompt);
        form.AddField("n", "1");
        form.AddField("size", "1024x1024");
        
        Texture2D texture = new Texture2D(512,512); // 创建一个新的 Texture2D 对象
        texture.LoadImage(LoadImageBytes("/Resources/src.png")); // 从字节数组加载图片数据
        Texture2D stretchTexture = StretchTexture(texture,512,512);
        
        byte[] imgBytes = stretchTexture.EncodeToPNG();
        
        form.AddBinaryData("image", imgBytes);
        
        Debug.Log($"src 大小占用：{stretchTexture.GetRawTextureData().Length/1024/1024} mb"); 
        
        Texture2D texture2 = new Texture2D(512,512); // 创建一个新的 Texture2D 对象
        texture2.LoadImage(LoadImageBytes("/Resources/mask.png")); // 从字节数组加载图片数据
        Texture2D stretchTexture2 = StretchTexture(texture2,512,512);
        byte[] maskBytes = stretchTexture2.EncodeToPNG();
        
        form.AddBinaryData("mask", maskBytes);
        
        Debug.Log($"mask 大小占用：{stretchTexture2.GetRawTextureData().Length/1024/1024} mb"); 
        
        
        UnityWebRequest request = UnityWebRequest.Post("https://api.openai.com/v1/images/edits", form);
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Response: {request.downloadHandler.text}");

            string responseText = request.downloadHandler.text;
            string imageUrl = ParseImageUrl(responseText);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                // ����ͼƬ
                StartCoroutine(DownloadImage(imageUrl, onComplete));
            }
            else
            {
                Debug.LogError("Failed to extract image URL from response.");
            }
        }
        else
        {
            Debug.LogError($"Error: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
        }
    }
    
        /**
     * prompt + mask + src => image *
     */
    public IEnumerator GenerateImageWithMask(string prompt, Texture2D texInputImage, Texture2D texInputMask, System.Action<Texture2D> onComplete)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Debug.LogError("Prompt cannot be empty.");
            yield break;
        }
        WWWForm form = new WWWForm();
        form.AddField("prompt", prompt);
        
        string model = "dall-e-2";
        switch (Model)
        {
            case ModelType.dall_e_2:
                model = "dall-e-2";
                break;
            case ModelType.gpt_image_1:
                model = "gpt-image-1";
                break;
        }
        form.AddField("model", model);
        
        form.AddField("n", "1");
        form.AddField("quality", "high");
        form.AddField("size", "1024x1024");

        int size = 1024;
        Texture2D texture = new Texture2D(size,size); // 创建一个新的 Texture2D 对象
        texture.LoadImage(texInputImage.EncodeToPNG()); // 从字节数组加载图片数据
        Texture2D stretchTexture = StretchTexture(texture,size,size);
        
        byte[] imgBytes = stretchTexture.EncodeToPNG();
        
        form.AddBinaryData("image", imgBytes);
        
        Debug.Log($"src 大小占用：{stretchTexture.GetRawTextureData().Length/1024/1024} mb"); 
        
        Texture2D texture2 = new Texture2D(size,size); // 创建一个新的 Texture2D 对象
        texture2.LoadImage(texInputMask.EncodeToPNG()); // 从字节数组加载图片数据
        Texture2D stretchTexture2 = StretchTexture(texture2,size,size);
        byte[] maskBytes = stretchTexture2.EncodeToPNG();
        
        form.AddBinaryData("mask", maskBytes);
        
        Debug.Log($"mask 大小占用：{stretchTexture2.GetRawTextureData().Length/1024/1024} mb"); 
        
        UnityWebRequest request = UnityWebRequest.Post("https://api.openai.com/v1/images/edits", form);
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Response: {request.downloadHandler.text}");

            string responseText = request.downloadHandler.text;

            if (Model == ModelType.gpt_image_1)
            {
                Texture2D tex = ParseTextureFromJson(responseText);
                if (tex != null)
                {
                    onComplete?.Invoke(tex);
                }
                else
                {
                    Debug.LogError("Failed to parse image from response.");
                    onComplete?.Invoke(default);
                } 
            }
            else
            {
                string imageUrl = ParseImageUrl(responseText);
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    // ����ͼƬ
                    StartCoroutine(DownloadImage(imageUrl, onComplete));
                }
                else
                {
                    Debug.LogError("Failed to extract image URL from response.");
                    onComplete?.Invoke(default);
                }
            }
        }
        else
        {
            Debug.LogError($"Error: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            onComplete?.Invoke(default);
        }
    }

    Texture2D StretchTexture(Texture2D originalTexture, int newWidth, int newHeight)
    {
        Texture2D stretchedTexture = new Texture2D(newWidth, newHeight);

        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                float u = (float)x / newWidth;
                float v = (float)y / newHeight;

                int originalX = Mathf.RoundToInt(u * originalTexture.width);
                int originalY = Mathf.RoundToInt(v * originalTexture.height);

                Color pixel = originalTexture.GetPixel(originalX, originalY);
                stretchedTexture.SetPixel(x, y, pixel);
            }
        }

        stretchedTexture.Apply();

        return stretchedTexture;
    }
    
    byte[] LoadImageBytes(string imagePath)
    {
        string filePath = Application.dataPath + imagePath;
        return System.IO.File.ReadAllBytes(filePath);
    }
    
    private string ParseImageUrl(string jsonResponse)
    {
        // �򵥽��� JSON����ȡͼƬ URL
        var playerData = JsonUtility.FromJson<ResponseData>(jsonResponse);
        return playerData.data[0].url;
    }
    
    //gpt-image-1
    private Texture2D ParseTextureFromJson(string jsonResponse)
    {
        JObject json = JObject.Parse(jsonResponse);
        string base64Image = json["data"][0]["b64_json"].ToString();
        Texture2D texture = Base64ToTexture(base64Image);
        return texture;
    }
    
    // 将 Base64 字符串转换为 Texture2D
    private Texture2D Base64ToTexture(string base64String)
    {
        try
        {
            byte[] imageBytes = System.Convert.FromBase64String(base64String);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false); // 初始尺寸不重要，LoadImage 会自动调整
            texture.LoadImage(imageBytes); // 自动解析 PNG/JPG 数据
            texture.Apply(); // 应用纹理更改
            return texture;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Base64ToTexture Error: " + e.Message);
            return null;
        }
    }

    private IEnumerator DownloadImage(string url, System.Action<Texture2D> onComplete)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            onComplete?.Invoke(texture);
        }
        else
        {
            Debug.LogError($"Failed to download image: {request.error}");
            onComplete?.Invoke(default);
        }
    }
}
