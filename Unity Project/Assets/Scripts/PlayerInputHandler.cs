using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private GameAPI gameAPI;
    [SerializeField] private float getRoundRetryIntervalSeconds = 1f;

    protected Dictionary<string, List<PlayerPress>> _playerPress = new Dictionary<string, List<PlayerPress>>();
    private bool _isRequestPending = false;
    private bool isFinished = true;

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

    public IEnumerator StartPlayerInputCollection(string currentGameId)
    {
        print("Starting player input collection...");
        while (true)
        {
            bool isDone = false;
            bool isFinished = false;

            GetRoundRequest getRoundRequest = new GetRoundRequest
            {
                game_id = currentGameId
            };

            gameAPI.GetRound(
                getRoundRequest,
                response =>
                {
                    isFinished = string.Equals(response.status, "finished", System.StringComparison.OrdinalIgnoreCase);
                    Debug.Log($"GetRound: round_id={response.round_id}, status={response.status}", this);
                    isDone = true;

                    _playerPress = response.presses_by_player ?? new Dictionary<string, List<PlayerPress>>();
                },
                error =>
                {
                    Debug.LogError($"GetRound failed: {error}", this);
                    isDone = true;
                });

            yield return new WaitUntil(() => isDone);

            if (isFinished)
            {
                yield break;
            }

            Debug.Log($"Round not finished yet. Retrying GetRound in {getRoundRetryIntervalSeconds:0.##} seconds...", this);
            yield return new WaitForSeconds(getRoundRetryIntervalSeconds);
        }
    }

    public void EndPlayerInputCollection()
    {
        isFinished = true;
    }

    public Dictionary<string, List<PlayerPress>> GetPlayerPress()
    {
        return _playerPress;
    }
}
