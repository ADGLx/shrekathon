using UnityEngine.Networking;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class WebServerManager : MonoBehaviour
{
    [SerializeField] private string apiUrl = "url";
    [SerializeField] private string apiPassword = "ultra-super-secret-password-fr";

    // Singleton Boilerplate | https://csharpindepth.com/articles/singleton
    private static WebServerManager _instance;
    public static WebServerManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Class methods
    private bool _isRequestPending = false;

    public async Task<GameData> CreateGame(int numPlayers)
    {
        if (_isRequestPending) return null;
        _isRequestPending = true;

        try
        {
            string json = $"{{\"ammount_of_players\": {numPlayers}}}";
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request = new UnityWebRequest($"{apiUrl}/create-game", "POST");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("x-api-password", apiPassword);
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                return null;

            return JsonUtility.FromJson<GameData>(request.downloadHandler.text);
        }
        finally
        {
            _isRequestPending = false;
        }
    }

    public async Task<GameData> GetGame(string gameId)
    {
        if (_isRequestPending) return null;
        _isRequestPending = true;

        try
        {
            string json = $"{{\"game_id\": \"{gameId}\"}}";
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request = new UnityWebRequest($"{apiUrl}/get-game", "POST");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("x-api-password", apiPassword);
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                return null;

            return JsonUtility.FromJson<GameData>(request.downloadHandler.text);
        }
        finally
        {
            _isRequestPending = false;
        }
    }

    public async Task<GameData> StartRound(string gameId, int timeLimitMiliseconds)
    {
        if (_isRequestPending) return null;
        _isRequestPending = true;

        try
        {
            string json = $"{{\"game_id\": \"{gameId}\", \"time_limit_ms\": {timeLimitMiliseconds}}}";
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request = new UnityWebRequest($"{apiUrl}/start-round", "POST");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("x-api-password", apiPassword);
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                return null;

            return JsonUtility.FromJson<GameData>(request.downloadHandler.text);
        }
        finally
        {
            _isRequestPending = false;
        }
    }

    public async Task<GameData> GetRound(string gameId)
    {
        if (_isRequestPending) return null;
        _isRequestPending = true;

        try
        {
            string json = $"{{\"game_id\": \"{gameId}\"}}";
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request = new UnityWebRequest($"{apiUrl}/get-round", "POST");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("x-api-password", apiPassword);
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                return null;

            return JsonUtility.FromJson<GameData>(request.downloadHandler.text);
        }
        finally
        {
            _isRequestPending = false;
        }
    }

    public async Task<GameData> EndRound(string gameId)
    {
        if (_isRequestPending) return null;
        _isRequestPending = true;

        try
        {
            string json = $"{{\"game_id\": \"{gameId}\"}}";
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request = new UnityWebRequest($"{apiUrl}/end-round", "POST");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("x-api-password", apiPassword);
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                return null;

            return JsonUtility.FromJson<GameData>(request.downloadHandler.text);
        }
        finally
        {
            _isRequestPending = false;
        }
    }

    public async Task<GameData> EndGame(string gameId)
    {
        if (_isRequestPending) return null;
        _isRequestPending = true;

        try
        {
            string json = $"{{\"game_id\": \"{gameId}\"}}";
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request = new UnityWebRequest($"{apiUrl}/end-game", "POST");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("x-api-password", apiPassword);
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                return null;

            return JsonUtility.FromJson<GameData>(request.downloadHandler.text);
        }
        finally
        {
            _isRequestPending = false;
        }
    }
}
