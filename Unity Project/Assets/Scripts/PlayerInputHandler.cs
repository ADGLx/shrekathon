using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInputHandler : MonoBehaviour
{
    public const int DefaultLongTapThresholdMs = 2000;

    [SerializeField] private GameAPI gameAPI;
    [SerializeField] private float finalRoundRetryDelaySeconds = 2f;

    protected Dictionary<string, List<PlayerPress>> _playerPress = new Dictionary<string, List<PlayerPress>>();
    private bool _isRequestPending = false;

    public static PlayerInputHandler Instance { get; private set; }

    private void Awake()
    {
        this.gameAPI = GameObject.Find("GameAPI").GetComponent<GameAPI>();
        print($"Game API found:{gameAPI.ToString()}");

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);   // kill duplicate (e.g. if scene reloads)
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator FetchRoundInputOnce(string currentGameId)
    {
        if (_isRequestPending)
        {
            Debug.Log("[PlayerInputHandler] FetchRoundInputOnce skipped because a request is already pending.", this);
            yield break;
        }

        if (gameAPI == null)
        {
            Debug.LogError("[PlayerInputHandler] FetchRoundInputOnce failed because GameAPI is missing.", this);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(currentGameId))
        {
            Debug.LogError("[PlayerInputHandler] FetchRoundInputOnce failed because game_id is missing.", this);
            yield break;
        }

        bool firstIsDone = false;
        bool firstIsFinished = false;

        _isRequestPending = true;
        Debug.Log($"[PlayerInputHandler] Timer finished. Fetching final round input (attempt 1) for game_id={currentGameId}.", this);
        gameAPI.GetRound(
            new GetRoundRequest { game_id = currentGameId },
            response =>
            {
                _playerPress = response.presses_by_player ?? new Dictionary<string, List<PlayerPress>>();
                firstIsFinished = IsRoundFinished(response?.status);
                Debug.Log($"[PlayerInputHandler] Final round input attempt 1 received. status={response?.status ?? "<null>"}, playersWithPresses={_playerPress.Count}", this);
                LogTapSnapshot("attempt 1", response?.status, _playerPress);
                _isRequestPending = false;
                firstIsDone = true;
            },
            error =>
            {
                Debug.LogError($"[PlayerInputHandler] FetchRoundInputOnce attempt 1 failed: {error}", this);
                _isRequestPending = false;
                firstIsDone = true;
            });

        yield return new WaitUntil(() => firstIsDone);

        if (firstIsFinished)
            yield break;

        Debug.Log($"[PlayerInputHandler] Round status was not finished. Waiting {finalRoundRetryDelaySeconds:0.##}s before one fallback GetRound call.", this);
        yield return new WaitForSeconds(finalRoundRetryDelaySeconds);

        bool secondIsDone = false;
        _isRequestPending = true;
        Debug.Log($"[PlayerInputHandler] Fetching final round input (attempt 2 fallback) for game_id={currentGameId}.", this);
        gameAPI.GetRound(
            new GetRoundRequest { game_id = currentGameId },
            response =>
            {
                _playerPress = response.presses_by_player ?? new Dictionary<string, List<PlayerPress>>();
                bool secondIsFinished = IsRoundFinished(response?.status);
                Debug.Log($"[PlayerInputHandler] Final round input attempt 2 received. status={response?.status ?? "<null>"}, finished={secondIsFinished}, playersWithPresses={_playerPress.Count}", this);
                LogTapSnapshot("attempt 2", response?.status, _playerPress);
                _isRequestPending = false;
                secondIsDone = true;
            },
            error =>
            {
                Debug.LogError($"[PlayerInputHandler] FetchRoundInputOnce attempt 2 failed: {error}", this);
                _isRequestPending = false;
                secondIsDone = true;
            });

        yield return new WaitUntil(() => secondIsDone);
    }

    private bool IsRoundFinished(string status)
    {
        return string.Equals(status, "finished", System.StringComparison.OrdinalIgnoreCase);
    }

    private void LogTapSnapshot(string attemptLabel, string status, Dictionary<string, List<PlayerPress>> playerPress)
    {
        int totalTaps = 0;
        foreach (var kvp in playerPress)
        {
            List<PlayerPress> presses = kvp.Value ?? new List<PlayerPress>();
            int tapCount = presses.Count;
            totalTaps += tapCount;

            int longestHoldMs = GetLongestHoldDurationMs(kvp.Key, playerPress);
            int longTapCount = GetLongTapCount(kvp.Key, DefaultLongTapThresholdMs, playerPress);
            Debug.Log($"[PlayerInputHandler] Server taps ({attemptLabel}) - player={kvp.Key}, tapCount={tapCount}, longestHoldMs={longestHoldMs}, longTapCount(>={DefaultLongTapThresholdMs}ms)={longTapCount}", this);
        }

        Debug.Log($"[PlayerInputHandler] Server taps summary ({attemptLabel}) - status={status ?? "<null>"}, players={playerPress.Count}, totalTaps={totalTaps}", this);
    }

    public int GetLongestHoldDurationMs(string playerKey)
    {
        return GetLongestHoldDurationMs(playerKey, _playerPress);
    }

    public int GetLongTapCount(string playerKey, int thresholdMs)
    {
        return GetLongTapCount(playerKey, thresholdMs, _playerPress);
    }

    private int GetLongestHoldDurationMs(string playerKey, Dictionary<string, List<PlayerPress>> playerPress)
    {
        if (string.IsNullOrWhiteSpace(playerKey) || playerPress == null || !playerPress.TryGetValue(playerKey, out List<PlayerPress> presses) || presses == null)
            return 0;

        int longestMs = 0;
        foreach (PlayerPress press in presses)
        {
            int durationMs = GetPressDurationMs(press);
            if (durationMs > longestMs)
                longestMs = durationMs;
        }

        return longestMs;
    }

    private int GetLongTapCount(string playerKey, int thresholdMs, Dictionary<string, List<PlayerPress>> playerPress)
    {
        if (thresholdMs <= 0 || string.IsNullOrWhiteSpace(playerKey) || playerPress == null || !playerPress.TryGetValue(playerKey, out List<PlayerPress> presses) || presses == null)
            return 0;

        int longTapCount = 0;
        foreach (PlayerPress press in presses)
            if (GetPressDurationMs(press) >= thresholdMs)
                longTapCount++;

        return longTapCount;
    }

    private int GetPressDurationMs(PlayerPress press)
    {
        if (press == null)
            return 0;

        if (press.end_offset_ms <= 0)
            return 0;

        return Mathf.Max(0, press.end_offset_ms - press.start_offset_ms);
    }

    public IEnumerator StartPlayerInputCollection(string currentGameId)
    {
        // Legacy entrypoint retained for compatibility. It now performs one final fetch.
        yield return FetchRoundInputOnce(currentGameId);
    }

    public void EndPlayerInputCollection()
    {
        // No-op: polling was removed in favor of one-time fetch after timer completion.
    }

    public Dictionary<string, List<PlayerPress>> GetPlayerPress()
    {
        return _playerPress;
    }
}
