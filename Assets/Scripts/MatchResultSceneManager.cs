using Assets.Scripts.MatchMaking;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class MatchResultSceneManager : MonoBehaviour
    {
        [SerializeField]
        private Image _resultImage;

        [SerializeField]
        private Sprite _professorWinSprite;
        [SerializeField]
        private Sprite _professorLoseSprite;

        private void Start()
        {
            switch (MatchManager.Instance.MatchResult)
            {
                case MatchResult.ProfessorWins:
                    _resultImage.sprite = _professorWinSprite;
                    break;

                case MatchResult.StudentsWin:
                    _resultImage.sprite = _professorLoseSprite;
                    break;
                default:
                    break;
            }

            SoundManager.Instance.StopBGM();
            //TODO : Play SFX

            BackToGameScene();
        }

        private async void BackToGameScene()
        {
            await UniTask.Delay(3000);

            MatchManager.Instance.ClearMatchResult();
            SceneManager.LoadScene("GameScene");
        }
    }
}
