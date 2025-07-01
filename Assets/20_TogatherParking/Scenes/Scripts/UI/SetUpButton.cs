using UnityEngine;

public class SetUpButton : MonoBehaviour
{
    [SerializeField]
    public GameObject SetupPanel;
    public void SetUpOff()
    {
        SoundManager.Instance.PlayUIClickSound();
        SetupPanel.SetActive(false);
    }
    public void SetUpOn()
    {
        SoundManager.Instance.PlayUIClickSound();
        SetupPanel.SetActive(true);
    }
}
