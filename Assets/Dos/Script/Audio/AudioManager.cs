using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public AudioSource sfxSource;
    public AudioSource musicSource;

    private void Awake()
    {
        if(instance == null)
            instance = this;
    }

    public void PlayOneShotSFX(AudioClip clip = null)
    {
        if(clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }
    public void PlayBGM(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.Play();
    }

    
}
