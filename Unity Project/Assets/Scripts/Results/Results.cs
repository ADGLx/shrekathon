using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Results : MonoBehaviour
{
    [SerializeField] private Image winnerImage;
    [SerializeField] private TMP_Text winnerNameText;
    [SerializeField] private TMP_Text winnerPointsText;

    void Start()
    {
        if (GameAPI.Instance == null)
        {
            Debug.LogError("[Results] GameAPI.Instance is null — cannot populate results screen.", this);
            return;
        }
        EndGameData data = GameAPI.Instance.CurrentEndGameData;
        if (data == null)
        {
            Debug.LogError("[Results] CurrentEndGameData is null — cannot populate results screen.", this);
            return;
        }

        winnerImage.sprite = data.winnerSprite;
        winnerNameText.text = data.winnerName;
        winnerPointsText.text = data.winnerPoints.ToString();
    }
}