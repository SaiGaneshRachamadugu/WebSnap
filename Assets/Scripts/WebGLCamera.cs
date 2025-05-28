using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Runtime.InteropServices;

public class WebGLCamera : MonoBehaviour
{
    public Texture2D capturedTexture;
    public UnityEngine.UI.RawImage displayImage;

    [Header("RapidAPI Credentials")]
    public string rapidApiKey = "54fc1f1090msh0b765c44bf20303p1584bfjsn5927b1f1d0a2";
    public string apiHost = "cartoon-yourself.p.rapidapi.com";
    public string url = "https://cartoon-yourself.p.rapidapi.com/facebody/api/portrait-animation/portrait-animation";

    [Header("Cartoon Style")]
    public string cartoonType = "anime";

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        StartCamera();
#endif
    }

    [DllImport("__Internal")] private static extern void StartCamera();
    [DllImport("__Internal")] private static extern void CaptureImage();
    [DllImport("__Internal")] private static extern void DownloadImage(string base64, int length);

    public void CapturePhoto()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        CaptureImage();
#else
        Debug.Log("CapturePhoto called in Editor. Only works in WebGL build.");
#endif
    }

    public void OnCapturedImage(string base64Image)
    {
        Debug.Log("OnCapturedImage received base64 image from JS");
        StartCoroutine(ProcessImage(base64Image));
    }

    IEnumerator ProcessImage(string base64Image)
    {
        Debug.Log("Sending image to cartoonify API...");

        byte[] imageBytes = System.Convert.FromBase64String(base64Image);

        WWWForm form = new WWWForm();
        form.AddBinaryData("image", imageBytes, "image.png", "image/png");
        form.AddField("type", cartoonType);

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        request.SetRequestHeader("X-RapidAPI-Key", rapidApiKey);
        request.SetRequestHeader("X-RapidAPI-Host", apiHost);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("API Error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
            yield break;
        }

        string responseText = request.downloadHandler.text;
        Debug.Log("API Response: " + responseText);

        CartoonResponse response = JsonUtility.FromJson<CartoonResponse>(responseText);

        if (response == null || response.data == null || string.IsNullOrEmpty(response.data.image_url))
        {
            Debug.LogError("API response missing 'image_url' field.");
            yield break;
        }

        string imageUrl = response.data.image_url;

        //download the image from the returned URL//
        UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return imageRequest.SendWebRequest();

        if (imageRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download cartoon image: " + imageRequest.error);
            yield break;
        }

        Texture2D cartoonTexture = DownloadHandlerTexture.GetContent(imageRequest);
        capturedTexture = cartoonTexture;
        //to display the image in the UI//
        displayImage.texture = cartoonTexture;

        Debug.Log("Cartoon image displayed");
    }

    public void DownloadCapturedImage()
    {
        if (capturedTexture == null)
        {
            Debug.LogWarning("No captured image to download");
            return;
        }

        byte[] bytes = capturedTexture.EncodeToPNG();
        string base64 = System.Convert.ToBase64String(bytes);

#if UNITY_WEBGL && !UNITY_EDITOR
        DownloadImage(base64, base64.Length);
#else
        Debug.Log("Download only works in WebGL.");
#endif
    }

    [System.Serializable]
    public class CartoonData
    {
        public string image_url;
    }

    [System.Serializable]
    public class CartoonResponse
    {
        public CartoonData data;
    }

}
