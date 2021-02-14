using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.GamePlay.PlayerActions
{
    public class SkillUI : MonoBehaviour
    {
        public static Dictionary<string, SkillUI> SkillUIs { get;  private set; } = new Dictionary<string, SkillUI>();

        private void Start()
        {
            SkillUIs.Add(_actionName, this);
        }

        [SerializeField]
        private RectTransform _cooldownImage;
        [SerializeField]
        private string _actionName = "MainAction";

        public void SetCharge(float charge)
        {
            _cooldownImage.localScale = new Vector3(1, charge, 1);
        }
    }
}
