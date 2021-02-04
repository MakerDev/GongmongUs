using Assets.Scripts.MiniGames;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.GamePlay.PlayerActions
{
    public class StartMiniGameAction : IPlayerAction
    {
        public bool CanExecute()
        {
            return Interactable.EnteredInteractable != null;
        }

        public void Execute()
        {
            //Just double check for sure.
            if (CanExecute())
            {
                Interactable.EnteredInteractable.StartMiniGame();
            }
        }
    }
}
