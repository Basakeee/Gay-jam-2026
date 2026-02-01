using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSetting : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Image musicSlider;
    public Image sfxSlider;

    // กำหนดค่า Config
    private const float MIN_VAL = -10f;
    private const float MAX_VAL = 10f;
    private const float STEP_VAL = 2f; // ค่าที่จะเพิ่มหรือลดทีละ 2

    // ตัวแปรเก็บค่าปัจจุบัน (เริ่มที่ 0)
    private float _currentMasterVol = 0f;
    private float _currentMusicVol = 0f;
    private float _currentSFXVol = 0f;

    private void Start()
    {
        ResetToDefault();
    }

    // ---------------------- MASTER ----------------------
    public void ChangeMasterVolume(float volume)
    {
        // 1. ล็อคค่าให้อยู่ในช่วง -10 ถึง 10 เท่านั้น
        volume = Mathf.Clamp(volume, MIN_VAL, MAX_VAL);
        
        // 2. อัปเดตตัวแปรจำค่า
        _currentMasterVol = volume;

        // 3. ส่งค่าไป Mixer (ถ้า -10 ให้ Mute เป็น -80)
        float mixerValue = (volume <= MIN_VAL) ? -80f : volume;
        audioMixer.SetFloat("MasterVolume", mixerValue);
    }

    // ปุ่มบวก Master
    public void IncreaseMaster()
    {
        ChangeMasterVolume(_currentMasterVol + STEP_VAL);
    }

    // ปุ่มลบ Master
    public void DecreaseMaster()
    {
        ChangeMasterVolume(_currentMasterVol - STEP_VAL);
    }


    // ---------------------- MUSIC ----------------------
    public void ChangeMusicVolume(float volume)
    {
        volume = Mathf.Clamp(volume, MIN_VAL, MAX_VAL);
        _currentMusicVol = volume;

        float mixerValue = (volume <= MIN_VAL) ? -80f : volume;
        audioMixer.SetFloat("MusicVolume", mixerValue);

        if (musicSlider != null)
        {
            musicSlider.fillAmount = Mathf.InverseLerp(MIN_VAL, MAX_VAL, volume);
        }
    }

    public void IncreaseMusic()
    {
        ChangeMusicVolume(_currentMusicVol + STEP_VAL);
    }

    public void DecreaseMusic()
    {
        ChangeMusicVolume(_currentMusicVol - STEP_VAL);
    }


    // ---------------------- SFX ----------------------
    public void ChangeSFXVolume(float volume)
    {
        volume = Mathf.Clamp(volume, MIN_VAL, MAX_VAL);
        _currentSFXVol = volume;

        float mixerValue = (volume <= MIN_VAL) ? -80f : volume;
        audioMixer.SetFloat("SFXVolume", mixerValue);

        if (sfxSlider != null)
        {
            sfxSlider.fillAmount = Mathf.InverseLerp(MIN_VAL, MAX_VAL, volume);
        }
    }

    public void IncreaseSFX()
    {
        ChangeSFXVolume(_currentSFXVol + STEP_VAL);
    }

    public void DecreaseSFX()
    {
        ChangeSFXVolume(_currentSFXVol - STEP_VAL);
    }


    // ---------------------- OTHERS ----------------------
    public void ResetToDefault()
    {
        ChangeMasterVolume(0);
        ChangeMusicVolume(0);
        ChangeSFXVolume(0);
    }

    public void ToggleFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}