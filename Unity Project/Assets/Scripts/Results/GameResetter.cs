using UnityEngine;
using UnityEngine.SceneManagement;

public class GameResetter : MonoBehaviour
{
    public void TotalReset()
    {
        // Destroy persistent singletons so they don't carry over into the new session
        if (RoundManager.Instance != null)         Destroy(RoundManager.Instance.gameObject);
        if (GameAPI.Instance != null)              Destroy(GameAPI.Instance.gameObject);
        if (PlayerInputHandler.Instance != null)   Destroy(PlayerInputHandler.Instance.gameObject);
        if (AudioManager.Instance != null)         Destroy(AudioManager.Instance.gameObject);

        SceneManager.LoadScene(0);
    }
}