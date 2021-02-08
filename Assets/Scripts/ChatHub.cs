using Mirror;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class ChatHub : NetworkBehaviour
    {
        public static ChatHub Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void BroadcastMessage(string message, string sender, ChatType chatType)
        {
            CmdBroadcastMessage(message, sender, chatType);
        }

        [Command(ignoreAuthority = true)]
        public void CmdBroadcastMessage(string message, string sender, ChatType chatType)
        {
            RpcPrintMessage(message, sender, chatType);
        }

        [ClientRpc]
        public void RpcPrintMessage(string message, string sender, ChatType chatType)
        {
            //Print to local chat panel
            GameManager.Instance.PrintMessage(message, sender, chatType);
        }
    }
}