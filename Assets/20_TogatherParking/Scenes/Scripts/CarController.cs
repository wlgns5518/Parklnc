using UnityEngine;
using System;

public class CarController : MonoBehaviour
{
    public static CarController Instance { get; private set; } // 싱글턴 패턴
    private CarMovement selectedCar;

    //  선택된 차량이 변경될 때 호출되는 이벤트
    public event Action<CarMovement> OnCarSelected;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SelectCar(CarMovement car)
    {
        if (selectedCar != car)
        {
            selectedCar = car;
            Debug.Log("차 선택됨: " + car.gameObject.name);

            //  이벤트 호출: 차량이 변경될 때만 실행
            OnCarSelected?.Invoke(selectedCar);
        }
    }

    public void DeselectCar()
    {
        if (selectedCar != null)
        {
            Debug.Log("차량 선택 해제됨: " + selectedCar.gameObject.name);
            selectedCar = null;

            //  이벤트 호출: 선택 해제 시 null 전달
            OnCarSelected?.Invoke(null);
        }
    }

    public bool HasSelectedCar(CarMovement car)
    {
        return selectedCar == car;
    }
}
