using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static GameManager Instance { get; private set; }

    // 현재 레벨
    public int CurrentLevel { get; private set; } = 1;

    private void Awake()
    {
        CurrentLevel = PlayerPrefs.GetInt("SavedLevel", 1);
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 레벨 증가 및 현재 씬 재로드
    public void NextLevel()
    {
        CurrentLevel++;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    // 레벨 초기화
    public void NewGame()
    {
        CurrentLevel = 1;
        SceneManager.LoadScene("TogeatherParking");
    }
    public void ContinueGame()
    {
        CurrentLevel = PlayerPrefs.GetInt("SavedLevel", 1);
        SceneManager.LoadScene("TogeatherParking");
    }
    public void ExitGame()
    {
        // PlayerPrefs에 현재 레벨 저장
        PlayerPrefs.SetInt("SavedLevel", CurrentLevel);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Home");
    }
    private void OnDisable()
    {
        // PlayerPrefs에 현재 레벨 저장
        PlayerPrefs.SetInt("SavedLevel", CurrentLevel);
        PlayerPrefs.Save();
    }
}