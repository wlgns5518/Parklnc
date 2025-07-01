using UnityEngine;

public class Button : MonoBehaviour
{
    public void NewGameButton()
    {
        GameManager.Instance.NewGame();
        SoundManager.Instance.PlayUIClickSound();
    }
    public void ContinueGameButton()
    {
        GameManager.Instance.ContinueGame();
        SoundManager.Instance.PlayUIClickSound();
    }
    public void GameExitButton()
    {
        Application.Quit();
        SoundManager.Instance.PlayUIClickSound();
    }
    public void ExitButton()
    {
        GameManager.Instance.ExitGame();
        SoundManager.Instance.PlayUIClickSound();
    }
}
