using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Image   playerPortrait;
    [SerializeField] private TMP_Text playerPointsText;

    void Start()
    {
        if (playerPortrait == null)
            Debug.LogError("[PlayerController] Could not find Image component in children.");
        if (playerPointsText == null)
            Debug.LogError("[PlayerController] Could not find TMP_Text component in children.");
    }

    public void Populate(PlayerIconData data)
    {
        if (data == null)
        {
            Debug.LogError("[PlayerController] Populate called with null PlayerIconData.");
            return;
        }

        playerPortrait.sprite = data.characterSprite;
        playerPointsText.text = "0";

        Debug.Log($"[PlayerController] Populated character: {data.characterName} with sprite: {data.characterSprite.name}");
        gameObject.SetActive(true);
    }

    public void UpdatePlayerPoints(int points)
    {
        playerPointsText.text = points.ToString();
    }

    public void SetPlayerStatus(bool isPressed, bool isLocked)
    {
        ClearEffects();

        if (isPressed)
            PressedImage();
        else if (isLocked)
            LockImage();
    }

    void ClearEffects()
    {
        playerPortrait.color = Color.white;
    }

    void PressedImage()
    {
        playerPortrait.color = Color.green;
    }

    void LockImage()
    {
        playerPortrait.color = Color.gray;
    }
}