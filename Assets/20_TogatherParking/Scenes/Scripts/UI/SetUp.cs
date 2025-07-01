using UnityEngine;
using UnityEngine.UI;

public class SetUp : MonoBehaviour
{
    public Slider bgmSlider;
    public Slider effectSlider;

    void Start()
    {
        // 슬라이더 초기값을 저장된 볼륨 값으로 설정
        if (bgmSlider != null)
            bgmSlider.value = SoundManager.Instance.GetBGMVolume();
        if (effectSlider != null)
            effectSlider.value = SoundManager.Instance.GetEffectVolume();

        // 슬라이더 값이 바뀔 때마다 볼륨 적용
        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener((value) => SoundManager.Instance.SetBGMVolume(value));
        if (effectSlider != null)
            effectSlider.onValueChanged.AddListener((value) => SoundManager.Instance.SetEffectVolume(value));
    }
}