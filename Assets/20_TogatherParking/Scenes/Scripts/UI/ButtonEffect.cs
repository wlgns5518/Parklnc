using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ButtonEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float pressedScale = 0.9f; // 눌렀을 때 크기 비율
    [SerializeField] private float scaleDuration = 0.08f; // 크기 전환 시간

    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StartScaleCoroutine(originalScale * pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StartScaleCoroutine(originalScale);
    }

    private void StartScaleCoroutine(Vector3 targetScale)
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(targetScale));
    }

    private IEnumerator ScaleTo(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < scaleDuration)
        {
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / scaleDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        transform.localScale = targetScale;
    }
}