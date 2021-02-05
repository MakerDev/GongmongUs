using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.GamePlay.PlayerActions
{
    public class StunByAssignmentAction : IPlayerAction
    {
        public bool CanExecute()
        {
            return false;
        }

        public void TryExecute()
        {

        }
    }
}
