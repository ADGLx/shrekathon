using System;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

public class RoundController : MonoBehaviour
{
    [SerializeField] private DealManager dealManager;
    [SerializeField] private PitchData[] pitchData;
    [SerializeField] private int rounds;
    private int currentRound;
    void Start()
    {
        pitchData = Resources.LoadAll<PitchData>("PitchData");
        currentRound = 0;
    }
    void playRound() {
        dealManager.Load(pitchData[0]);  // TODO: load based on pitchOrderData

        WaitUntil dealIsLoaded = new WaitUntil(() => dealManager.CurrentData != null);
        StartCoroutine(dealIsLoaded);

        dealManager.displayDeal();
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
