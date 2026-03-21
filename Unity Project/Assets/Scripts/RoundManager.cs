using System;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [SerializeField] protected CharacterController characterController;
    [SerializeField] protected ContractController contractController;
    [SerializeField] private PitchData[] pitchData;
    [SerializeField] private int numberOfRounds;
    private int currentRound;
    void Start()
    {
        pitchData = Resources.LoadAll<PitchData>("PitchData");
        currentRound = 0;
        StartRound();
    }

    public void StartRound()
    {
        PitchData currentPitch = pitchData[currentRound];
        currentRound++;

        // Handles isnstantiation
        DealManager dealManager;
        if (currentPitch.gameType == "BRINK")
            dealManager = gameObject.AddComponent<BrinkDealManager>();
        else
            throw new Exception($"Unsupported game type: {currentPitch.gameType}");

        dealManager.GetComponent<DealManager>().Init(characterController, contractController);
        dealManager.Load(pitchData[0]);
        WaitUntil dealIsLoaded = new WaitUntil(() => dealManager.CurrentData != null);
        StartCoroutine(dealIsLoaded);

        dealManager.OnDestroyed += EndRound();
        dealManager.displayDeal();
    }

    System.Action EndRound()
    {
        return () => {
            if (numberOfRounds >= currentRound)
                print("TODO: End of Game Logic;");
            else
                StartRound();
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
/**
* // In the spawning class
* RoundInstance round = gameObject.AddComponent<RoundInstance>();
* round.Init(30f);
*/