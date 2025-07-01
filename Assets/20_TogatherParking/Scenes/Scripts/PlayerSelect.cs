using System.Collections;
using UnityEngine;

public class PlayerSelect : MonoBehaviour
{
    private Transform target;
    private Vector3 distance = new Vector3(0, 5, 0);

    private void Start()
    {
        if (CarController.Instance != null)
        {
            // 차량 선택 이벤트에 등록
            CarController.Instance.OnCarSelected += UpdateTarget;
        }
    }

    private void OnDestroy()
    {
        if (CarController.Instance != null)
        {
            // 이벤트 해제 (메모리 누수 방지)
            CarController.Instance.OnCarSelected -= UpdateTarget;
        }
    }

    //  차량이 변경될 때만 호출되는 함수
    private void UpdateTarget(CarMovement newCar)
    {
        target = newCar != null ? newCar.transform : null;
        if(target == null)
        {
            // 코루틴을 사용해 기본 위치로 부드럽게 이동
            StartCoroutine(MoveToDefaultPosition());
        }
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = Vector3.Lerp(transform.position, target.position + distance, Time.deltaTime * 5f);
        }
    }
    //  선택된 차량이 없을 때 기본 위치로 서서히 이동하는 코루틴
    private IEnumerator MoveToDefaultPosition()
    {
        while (Vector3.Distance(transform.position, distance) > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, distance, Time.deltaTime * 5f);
            yield return null;
        }
        transform.position = distance; // 마지막 보정
    }
}
