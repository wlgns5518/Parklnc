using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Clips")]
    public AudioClip bgmClip;
    public AudioClip carClip;
    public AudioClip uiClickClip;
    public AudioClip carHitClip;

    private const string BGM_VOLUME_KEY = "BGM_VOLUME";
    private const string EFFECT_VOLUME_KEY = "EFFECT_VOLUME";

    private AudioSource bgmSource;
    private AudioSource effectSource;

    void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // AudioSource 컴포넌트 동적으로 추가
            bgmSource = gameObject.AddComponent<AudioSource>();
            effectSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 저장된 볼륨 불러오기
        float bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
        float effectVolume = PlayerPrefs.GetFloat(EFFECT_VOLUME_KEY, 1f);

        SetBGMVolume(bgmVolume);
        SetEffectVolume(effectVolume);

        // 배경음악이 할당되어 있으면 반복 재생
        if (bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    // 차량 소리 재생
    public void PlayCarSound()
    {
        if (carClip != null && !effectSource.isPlaying)
        {
            effectSource.PlayOneShot(carClip);
        }
    }
    public void PlayCarHitSound()
    {
        if(carHitClip != null)
        {
            effectSource.PlayOneShot(carHitClip);
        }
    }

    // UI 클릭 소리 재생
    public void PlayUIClickSound()
    {
        if (uiClickClip != null)
        {
            effectSource.PlayOneShot(uiClickClip);
        }
    }

    // 볼륨 설정 메서드
    public void SetBGMVolume(float volume)
    {
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, volume);
        PlayerPrefs.Save();
        if (bgmSource != null)
            bgmSource.volume = volume;
    }

    public void SetEffectVolume(float volume)
    {
        PlayerPrefs.SetFloat(EFFECT_VOLUME_KEY, volume);
        PlayerPrefs.Save();
        if (effectSource != null)
            effectSource.volume = volume;
    }

    // 현재 볼륨 값 반환
    public float GetBGMVolume()
    {
        return PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
    }

    public float GetEffectVolume()
    {
        return PlayerPrefs.GetFloat(EFFECT_VOLUME_KEY, 1f);
    }
}