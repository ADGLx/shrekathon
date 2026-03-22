using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private GameAPI gameAPI;
    [SerializeField] private float getRoundRetryIntervalSeconds = 0.1f;

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
        isFinished = false;
        while (true)
        {
            bool isDone = false;

            GetRoundRequest getRoundRequest = new GetRoundRequest
            {
                game_id = currentGameId
            };

            gameAPI.GetRound(
                getRoundRequest,
                response =>
                {
                    //Debug.Log($"[PlayerInputHandler] StartPlayerInputCollection - GetRound: round_id={response.round_id}, status={response.status}", this);
                    _playerPress = response.presses_by_player ?? new Dictionary<string, List<PlayerPress>>();
                    isDone = true;
                },
                error =>
                {
                    Debug.LogError($"[PlayerInputHandler] StartPlayerInputCollection - GetRound failed: {error}", this);
                    isDone = true;
                });

            yield return new WaitUntil(() => isDone);

            // Leaves loop permenanty when playerInput collection is finished
            if (isFinished)
                yield break;

            //Debug.Log($"[PlayerInputHandler] StartPlayerInputCollection - Round not finished yet. Retrying GetRound in {getRoundRetryIntervalSeconds:0.##} seconds...", this);
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
