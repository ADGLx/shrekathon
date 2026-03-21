/**
 * PlayerGUI.cs used to manage GUI ellements on screen for each player. These include:
 * - Player name
 * - Player score
 * - Players button state (pressed / locked / normal)
 * - Player button text ( number of taps)
 */

using UnityEngine;

public class PlayerGUI : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI playerNameText;
    [SerializeField] private TMPro.TextMeshProUGUI playerScoreText;
    [SerializeField] private GameObject buttonIndicator;
    [SerializeField] private GameObject buttonText;

    public void SetPlayerName(string name)
    {
        playerNameText.text = name;
    }

    public void SetPlayerScore(int score)
    {
        playerScoreText.text = $"Score: {score}";
    }

    /// <summary>
    /// Sets the button state for the player. This includes changing the color of the button to indicate if it's being pressed, locked, or normal.
    /// </summary>
    /// <param name="isPressed"></param>
    /// <param name="isLocked"></param>
    public void SetButtonState(bool isPressed, bool isLocked)
    {
        if (isPressed && !isLocked)
        {
            //@todo: Change the button color to indicate it's being pressed
            print("Button is pressed");
        }
        else
        {
            //todo: Reset the button color to normal
            print("Button is normal (not pressed)");
        }

        if (isLocked)
        {
            //@todo: Make the button grayed out
            print("Button is locked");
        }
        else
        {
            //@todo: Reset the button color to normal
            print("Button is normal (not locked)");
        }
    }
}
