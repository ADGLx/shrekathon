using System;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    [SerializeField] private DealManager dealController;
    [SerializeField] private PitchData[] pitchData;
    [SerializeField] private int numberOfRounds;
    private int currentRound;
    void Start()
    {
        pitchData = Resources.LoadAll<PitchData>("PitchData");
        currentRound = 0;
        StartRound();
    }

    void StartRound() {
        dealController.Load(pitchData[0]);  // TODO: load based on pitchOrderData

        WaitUntil dealIsLoaded = new WaitUntil(() => dealController.CurrentData != null);
        StartCoroutine(dealIsLoaded);

        dealController.displayDeal();
    }

    void EndRound()
    {
        if (numberOfRounds >= currentRound)
            print("TODO: End of Game Logic;");
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