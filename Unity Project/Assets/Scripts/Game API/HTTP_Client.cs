using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;

public class HTTP_Client : MonoBehaviour
{
    private const string AuthHeaderName = "x-api-password";

    [Header("Server")]
    [SerializeField] private string serverUrl = "https://onion.adgl.site/api/";

    [Header("Auth")]
    [SerializeField] private string authHeaderValue = "ultra-super-secret-password-fr";

    public void Get<T>(string endpoint, Action<T> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetCoroutine(endpoint, response =>
        {
            if (TryDecodeJson(response, out T decoded, onError))
                onSuccess?.Invoke(decoded);
        }, onError));
    }

    public void Post<TRequest, TResponse>(string endpoint, TRequest requestBody, Action<TResponse> onSuccess, Action<string> onError = null)
    {
        string jsonBody = JsonUtility.ToJson(requestBody);
        StartCoroutine(SendJsonCoroutine("POST", endpoint, jsonBody, response =>
        {
            if (TryDecodeJson(response, out TResponse decoded, onError))
                onSuccess?.Invoke(decoded);
        }, onError));
    }

    public bool TryDecodeJson<T>(string json, out T decoded, Action<string> onError = null)
    {
        decoded = default;

        if (string.IsNullOrWhiteSpace(json))
        {
            string errorMessage = "JSON decode failed: response body was empty.";
            onError?.Invoke(errorMessage);
            Debug.LogError(errorMessage);
            return false;
        }

        try
        {
            decoded = JsonConvert.DeserializeObject<T>(json);

            if (object.Equals(decoded, null))
            {
                string errorMessage = "JSON decode failed: parsed object was null.";
                onError?.Invoke(errorMessage);
                Debug.LogError(errorMessage);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            string errorMessage = $"JSON decode failed: {ex.Message}";
            onError?.Invoke(errorMessage);
            Debug.LogError(errorMessage);
            return false;
        }
    }

    private IEnumerator GetCoroutine(string endpoint, Action<string> onSuccess, Action<string> onError)
    {
        string url = serverUrl + endpoint;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            ApplyHeaders(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
                yield break;
            }

            string errorMessage = $"HTTP GET failed ({request.responseCode}): {request.error}\n{request.downloadHandler.text}";
            onError?.Invoke(errorMessage);
            Debug.LogError(errorMessage);
        }
    }

    private IEnumerator SendJsonCoroutine(string method, string endpoint, string jsonBody, Action<string> onSuccess, Action<string> onError)
    {
        string url = serverUrl + endpoint;
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody ?? "{}");

        using (UnityWebRequest request = new UnityWebRequest(url, method))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            ApplyHeaders(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
                yield break;
            }

            string errorMessage = $"HTTP {method} failed ({request.responseCode}): {request.error}\n{request.downloadHandler.text}";
            onError?.Invoke(errorMessage);
            Debug.LogError(errorMessage);
        }
    }

    private void ApplyHeaders(UnityWebRequest request)
    {
        if (!string.IsNullOrWhiteSpace(authHeaderValue))
        {
            request.SetRequestHeader(AuthHeaderName, authHeaderValue);
        }
    }
}