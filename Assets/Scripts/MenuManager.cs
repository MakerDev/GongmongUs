using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class MenuManager : MonoBehaviour
    {
        private static Toggle _bgmToggle;
        private static Toggle _fullscreenToggle;


        public static void RegisterToggles(Toggle bgmToggle, Toggle fullscreenToggle)
        {
            if (_bgmToggle != null)
            {
                _bgmToggle.onValueChanged.RemoveAllListeners();
                _fullscreenToggle.onValueChanged.RemoveAllListeners();
            }

            _bgmToggle = bgmToggle;
            _fullscreenToggle = fullscreenToggle;

            InitializeToggles();
        }

        private static void InitializeToggles()
        {
            _bgmToggle.isOn = SoundManager.Instance.IsPlayingBGM;
            _fullscreenToggle.isOn = Screen.fullScreen;

            _bgmToggle.onValueChanged.AddListener(SetBgm);
            _fullscreenToggle.onValueChanged.AddListener(SwitchFullScreen);
        }

        public static void SwitchFullScreen(bool toFullScreen)
        {
            if (toFullScreen)
            {
                Screen.SetResolution(1600, 1000, true);
            }
            else
            {
                Screen.SetResolution(1120, 700, false);
            }
        }

        public static void SetBgm(bool turnOn)
        {
            if (turnOn)
            {
                SoundManager.Instance.PlayBGM();                
            }
            else
            {
                SoundManager.Instance.MuteSound();
            }
        }
    }
}
