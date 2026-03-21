using UnityEngine;
using UnityEngine.UI;


public class ContractManager : MonoBehaviour
{


    [Header("UI References")]
    [SerializeField] private Image Contract;

    public void populate(PitchData data) {
      if (data == null) {
        Debug.LogError("[ContractManager] Populate called with null PitchData.");
        return;
      }

      Contract.sprite = data.contractSprite;

      Debug.Log($"[ContractManager] Populated contract: {data.contractTitle}");
    }
}
