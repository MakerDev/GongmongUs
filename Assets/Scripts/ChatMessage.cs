using System;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public enum ChatType
    {
        Info,
        Player,
        KillInfo,
        None,
    }

    [Serializable]
    public class ChatMessage
    {
        public string Message;
        public string Sender;
        public Text TextObject;

        public ChatType ChatType = ChatType.Info;
    }
}