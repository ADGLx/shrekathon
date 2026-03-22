using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharacterController : MonoBehaviour
{
  [Header("UI References")]
    [SerializeField] private Image      portraitImage;
    [SerializeField] private TextMeshProUGUI nameLabel;

    [Header("Entrance Animation")]
    [SerializeField] private Animator   characterAnimator;


    public void Populate(PitchData data) {
      if (data == null) {
        Debug.LogError("[CharacterManager] Populate called with null PitchData.");
        return;
      }

      portraitImage.sprite = data.characterSprite;
      nameLabel.text = data.characterName;

      Debug.Log($"[CharacterManager] Populated character: {data.characterName}");
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
