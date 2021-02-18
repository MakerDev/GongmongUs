using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField]
    private AudioSource _backgroundMusic;

    [SerializeField]
    private List<AudioClip> _audioClips = new List<AudioClip>();
    [SerializeField]
    private List<AudioClip> _sfxClips = new List<AudioClip>();

    public bool IsPlayingBGM { get; private set; } = true;

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (IsPlayingBGM)
            {
                _backgroundMusic.Stop();
            }
            else
            {
                _backgroundMusic.Play();
            }

            IsPlayingBGM = !IsPlayingBGM;
        }
    }

    public void PlayeBGM(string name)
    {
        var audioClip = _audioClips.FirstOrDefault(x => x.name == name);

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
        _backgroundMusic.volume = 0.7f;
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
