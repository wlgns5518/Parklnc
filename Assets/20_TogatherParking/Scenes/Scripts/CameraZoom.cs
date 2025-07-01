using UnityEngine;
using System.Collections;

public class CameraZoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    public Transform carTransform; // 자동차 Transform (인스펙터에서 직접 할당)
    public float zoomInDistance = 5f;   // 줌인 시 거리
    public float zoomSpeed = 2f;        // 줌 속도
    public float waitAfterZoomIn = 0.5f; // 줌인 후 대기 시간

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 offsetDirection;

    void Start()
    {
        if (carTransform != null && GameManager.Instance.CurrentLevel == 1)
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            offsetDirection = (transform.position - carTransform.position).normalized;
            StartCoroutine(ZoomInAndReturn());
        }
    }

    private IEnumerator ZoomInAndReturn()
    {
        // 자동차를 기준으로 offset 방향에서 zoomInDistance만큼 떨어진 위치 계산
        Vector3 zoomInTarget = carTransform.position + offsetDirection * zoomInDistance;
        // 줌인: 카메라가 자동차를 바라보며 이동
        while (Vector3.Distance(transform.position, zoomInTarget) > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, zoomInTarget, Time.deltaTime * zoomSpeed);
            transform.LookAt(carTransform.position);
            yield return null;
        }
        transform.position = zoomInTarget;
        transform.LookAt(carTransform.position);

        // 잠깐 대기
        yield return new WaitForSeconds(waitAfterZoomIn);

        // 원래 위치로 복귀 (회전도 원래대로)
        while (Vector3.Distance(transform.position, originalPosition) > 0.1f || Quaternion.Angle(transform.rotation, originalRotation) > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, originalPosition, Time.deltaTime * zoomSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, Time.deltaTime * zoomSpeed);
            yield return null;
        }
        transform.position = originalPosition;
        transform.rotation = originalRotation;
    }
}