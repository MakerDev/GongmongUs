using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.GamePlay.PlayerActions
{
    public class CooltimeAction : IPlayerAction
    {
        private float _rechargeSpeed = 0.5f; //charge 50%/sec
        private float _chargeAmount = 1f;
        private SkillUI _skillUI = null;
        private string _skillName = "MainAction";

        public CooltimeAction(string skillName)
        {
            _skillName = skillName;
        }

        public virtual bool CanExecute()
        {
            return true;

            if (_chargeAmount >= 1.0f)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected void StartCooldown()
        {
            _chargeAmount = 0;
        }

        public virtual void TryExecute()
        {
        }
    }
}
