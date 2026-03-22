using UnityEngine;

[DisallowMultipleComponent]
public class UICircularRotationAnimator : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float degreesPerSecond = 60f;
    [SerializeField] private bool clockwise = true;
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = transform as RectTransform;

        if (rectTransform == null)
        {
            Debug.LogWarning("[UICircularRotationAnimator] Requires a RectTransform.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (rectTransform == null)
            return;

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float direction = clockwise ? -1f : 1f;
        float deltaAngle = degreesPerSecond * direction * deltaTime;

        rectTransform.Rotate(0f, 0f, deltaAngle, Space.Self);
    }

    public void SetSpeed(float newDegreesPerSecond)
    {
        degreesPerSecond = newDegreesPerSecond;
    }

    public void SetClockwise(bool shouldRotateClockwise)
    {
        clockwise = shouldRotateClockwise;
    }
}
