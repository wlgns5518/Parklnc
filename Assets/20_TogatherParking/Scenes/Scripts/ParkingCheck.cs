using UnityEngine;

public class ParkingCheck : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == 3)
        {
            GameManager.Instance.NextLevel();
        }
    }
}