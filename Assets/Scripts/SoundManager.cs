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

    private bool _isPlayingBGM = true;
    public bool IsPlayingBGM
    {
        get { return _isPlayingBGM; }
        private set
        {
            _isPlayingBGM = value;

            if (_isPlayingBGM)
            {
                _backgroundMusic.Play();
            }
            else
            {
                _backgroundMusic.Stop();
            }
        }
    }

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
            IsPlayingBGM = !IsPlayingBGM;
        }
    }

    public void SetBGM(string name)
    {
        var audioClip = _audioClips.FirstOrDefault(x => x.name == name);

        if (_backgroundMusic == null)
        {
            var backgroundMusicObject = new GameObject($"BGM");
            _backgroundMusic = backgroundMusicObject.AddComponent<AudioSource>();
        }

        _backgroundMusic.clip = audioClip;
        _backgroundMusic.loop = true;
        _backgroundMusic.volume = 0.1f;

        if (IsPlayingBGM)
        {
            _backgroundMusic.Play();
        }
    }

    public void PlayeBGM()
    {       
        IsPlayingBGM = true;
    }

    public void StopBGM()
    {
        if (_backgroundMusic == null)
        {
            return;
        }

        IsPlayingBGM = false;
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
