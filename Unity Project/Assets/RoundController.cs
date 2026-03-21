using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

public class RoundController : MonoBehaviour
{
    [SerializeField] private PitchObject pitchObject;
    [SerializeField] private PitchData[] pitchData;
    [SerializeField] private JsonObject pitchOrderData;
    void Start()
    {
        pitchData = Resources.LoadAll<PitchData>("PitchData");
        

        pitchObject.Load(pitchData[0]);  // TODO: load based on pitchOrderData
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
