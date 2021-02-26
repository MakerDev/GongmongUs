using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class MenuManager : MonoBehaviour
    {
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
