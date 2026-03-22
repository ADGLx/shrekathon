using UnityEngine;

[DisallowMultipleComponent]
public class UIDanceAnimator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField, Min(0f)] private float speed = 1.5f;
    [SerializeField] private float rotationAmplitude = 6f;
    [SerializeField] private float bobAmplitude = 4f;
    [SerializeField, Min(0f)] private float bobFrequencyMultiplier = 1.3f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Variation")]
    [SerializeField] private bool randomizePhaseOnEnable = true;
    [SerializeField] private float phaseOffset;

    private RectTransform rectTransform;
    private Quaternion baseRotation;
    private Vector2 baseAnchoredPosition;

    private void Awake()
    {
        rectTransform = transform as RectTransform;

        if (rectTransform == null)
        {
            Debug.LogWarning("[UIDanceAnimator] Requires a RectTransform.");
            enabled = false;
            return;
        }

        CacheBaseTransform();
    }

    private void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (rectTransform == null)
            return;

        CacheBaseTransform();

        if (randomizePhaseOnEnable)
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void OnDisable()
    {
        if (rectTransform == null)
            return;

        rectTransform.localRotation = baseRotation;
        rectTransform.anchoredPosition = baseAnchoredPosition;
    }

    private void Update()
    {
        if (rectTransform == null)
            return;

        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float wave = (time * speed) + phaseOffset;

        float zRotation = Mathf.Sin(wave) * rotationAmplitude;
        rectTransform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, zRotation);

        float bobWave = Mathf.Sin(wave * bobFrequencyMultiplier);
        float yOffset = bobWave * bobAmplitude;
        rectTransform.anchoredPosition = baseAnchoredPosition + new Vector2(0f, yOffset);
    }

    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Max(0f, newSpeed);
    }

    public void Recenter()
    {
        CacheBaseTransform();
        rectTransform.localRotation = baseRotation;
        rectTransform.anchoredPosition = baseAnchoredPosition;
    }

    private void CacheBaseTransform()
    {
        baseRotation = rectTransform.localRotation;
        baseAnchoredPosition = rectTransform.anchoredPosition;
    }
}
