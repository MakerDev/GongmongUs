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
        public float RechargeSpeed { get; private set; } = 0.1f;
        public float ChargeAmount { get; private set; } = 1f;
        private string _skillName = "MainAction";

        public CooltimeAction(string skillName)
        {
            _skillName = skillName;
        }

        public virtual bool CanExecute()
        {
            if (ChargeAmount >= 1.0f)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Charge(float rechargeAmount)
        {
            if (ChargeAmount >= 1.0f)
            {
                ChargeAmount = 1.0f;
                return;
            }

            ChargeAmount += rechargeAmount;
        }

        protected void StartCooldown()
        {
            ChargeAmount = 0;
        }

        public virtual void TryExecute()
        {
        }
    }
}
