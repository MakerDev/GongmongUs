using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.MiniGames
{
    public class TaskSubmissionGame : MiniGame
    {
        [SerializeField]
        private InputField _userAnswerField;
        [SerializeField]
        private Text _wrongText;

        private string _taskName = "대학생활계획서";
        private string _answer = "대학생활계획서_2020123123_단벡이";
        //TODO : Complet this feature.
        private string _formatString = "";

        public void Submit()
        {
            //if (_userAnswerField.text == _answer)
            if(true)
            {
                _wrongText.text = "";
                MiniGameResult.Passed = true;
                CompleteMiniGame(MiniGameResult);
            }
            else
            {
                _wrongText.text = "Wrong Answer!";
            }
        }
    }
}