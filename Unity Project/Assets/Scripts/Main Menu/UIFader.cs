using UnityEngine;
using System.Collections;

public class UIFader : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDuration = 0.3f;

    public void FadeIn()
    {
        gameObject.SetActive(true);
        StartCoroutine(Fade(0, 1));
    }

    public void FadeOut()
    {
        StartCoroutine(Fade(1, 0, true));
    }

    IEnumerator Fade(float start, float end, bool disableOnEnd = false)
    {
        float time = 0;
        canvasGroup.alpha = start;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = end;

        if (disableOnEnd)
            gameObject.SetActive(false);
    }
}