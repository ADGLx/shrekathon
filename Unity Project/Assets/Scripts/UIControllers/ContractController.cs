using UnityEngine;
using UnityEngine.UI;


public class ContractController : MonoBehaviour
{


    [Header("UI References")]
    [SerializeField] private Image Contract;

    public void Populate(PitchData data) {
      if (data == null) {
        Debug.LogError("[ContractManager] Populate called with null PitchData.");
        return;
      }

      Contract.sprite = data.contractSprite;

      Debug.Log($"[ContractManager] Populated contract: {data.contractTitle}");
    }
    public void Show()
  {
    gameObject.SetActive(true);
  }
    public void Hide()
  {
    gameObject.SetActive(false);
  }
}
