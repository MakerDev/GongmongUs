using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField]
    private AudioSource _backgroundMusic;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }       
    }

    private void Start()
    {
        _backgroundMusic.Play();
    }

    public void PlayeBGM(AudioClip audioClip)
    {
        if (_backgroundMusic == null)
        {
            var backgroundMusicObject = new GameObject($"BGM");           
            _backgroundMusic = backgroundMusicObject.AddComponent<AudioSource>();
        }
        else
        {
            _backgroundMusic.Stop();
        }

        _backgroundMusic.clip = audioClip;
        _backgroundMusic.loop = true;
        _backgroundMusic.Play();
    }

    public void StopBGM()
    {
        if (_backgroundMusic == null)
        {
            return;
        }

        _backgroundMusic.Stop();
    }

    public void SFXPlay(string sfxName, AudioClip audioClip)
    {
        var tempSoundObject = new GameObject($"{sfxName}Sound");
        var audioSource = tempSoundObject.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.Play();

        Destroy(tempSoundObject, audioClip.length);
    }
}
