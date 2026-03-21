using System;
using Newtonsoft.Json;
using UnityEngine;

public class GameAPI : MonoBehaviour
{
    public static GameAPI Instance;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [SerializeField] private HTTP_Client HTTP_Client;

    public void GetGame(GetGameRequest request, Action<GetGameResponse> onSuccess = null, Action<string> onError = null)
    {
        PostWithDefaults<GetGameRequest, GetGameResponse>("get-game", request, "GetGame", onSuccess, onError);
    }

    public void CreateGame(CreateGameRequest request, Action<CreateGameResponse> onSuccess = null, Action<string> onError = null)
    {
        PostWithDefaults<CreateGameRequest, CreateGameResponse>("create-game", request, "CreateGame", onSuccess, onError);
    }

    public void StartRound(StartRoundRequest request, Action<StartRoundResponse> onSuccess = null, Action<string> onError = null)
    {
        PostWithDefaults<StartRoundRequest, StartRoundResponse>("start-round", request, "StartRound", onSuccess, onError);
    }

    public void GetRound(GetRoundRequest request, Action<GetRoundResponse> onSuccess = null, Action<string> onError = null)
    {
        PostWithDefaults<GetRoundRequest, GetRoundResponse>("get-round", request, "GetRound", onSuccess, onError);
    }

    public void EndGame(EndGameRequest request, Action<EndGameResponse> onSuccess = null, Action<string> onError = null)
    {
        PostWithDefaults<EndGameRequest, EndGameResponse>("end-game", request, "EndGame", onSuccess, onError);
    }

    private void PostWithDefaults<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        string operation,
        Action<TResponse> onSuccess = null,
        Action<string> onError = null)
    {
        if (HTTP_Client == null)
        {
            string missingClientMessage = $"{operation} failed: HTTP_Client reference is missing on GameAPI.";
            Debug.LogError(missingClientMessage, this);
            onError?.Invoke(missingClientMessage);
            return;
        }

        HTTP_Client.Post<TRequest, TResponse>(
            endpoint,
            request,
            response =>
            {
                string payload = SerializeForLog(response);
                Debug.Log($"{operation} succeeded. Response: {payload}", this);
                onSuccess?.Invoke(response);
            },
            error =>
            {
                Debug.LogError($"{operation} failed: {error}", this);
                onError?.Invoke(error);
            });
    }

    private string SerializeForLog<T>(T value)
    {
        if (value == null)
        {
            return "null";
        }

        try
        {
            return JsonConvert.SerializeObject(value);
        }
        catch (Exception ex)
        {
            return $"<unable to serialize: {ex.Message}>";
        }
    }
}