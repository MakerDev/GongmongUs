using Assets.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniMapMob : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _infoText;

    private Player _player;
    private int _currentFloor;

    public void SetPlayer(Player player)
    {
        _player = player;
        _currentFloor = MiniMap.GetCurrentFloor(player.transform.position.y);
        _infoText.text = $"{_currentFloor}층:{_player.PlayerName}";
    }

    private void Update()
    {
        var currentFloor = MiniMap.GetCurrentFloor(_player.transform.position.y);

        if (currentFloor == _currentFloor)
        {
            return;
        }

        _currentFloor = currentFloor;
        _infoText.text = $"{_currentFloor}층:{_player.PlayerName}";
    }
}

