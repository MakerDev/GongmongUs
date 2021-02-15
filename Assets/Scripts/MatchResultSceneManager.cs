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
        private Text _matchResultText;

        private void Start()
        {
            switch (MatchManager.Instance.MatchResult)
            {
                case MatchResult.ProfessorWins:
                    _matchResultText.text = "교수님이.. 승리하였습니다..";
                    break;
                case MatchResult.StudentsWin:
                    _matchResultText.text = "학생들이.. 승리하였습니다..!";
                    break;
                default:
                    break;
            }

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
