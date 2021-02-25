﻿using Assets.Scripts;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniMap : MonoBehaviour
{
    private List<Player> _allPlayers = new List<Player>();
    private Player _localPlayer = null;

    [SerializeField]
    private float _xOffset = 43f;
    [SerializeField]
    private float _zOffset = 32f;

    /// <summary>
    /// If this is always on minimap which follows local player or the maps that are shown when pressing tap key.
    /// </summary>
    [SerializeField]
    private bool _isStatic = false;

    private int _floor = 0;

    [SerializeField]
    private Image _floorImage;
    [SerializeField]
    private List<Sprite> _floorSprites = new List<Sprite>();

    /// <summary>
    /// 화면에 표시되는 점들
    /// </summary>
    private Dictionary<Player, GameObject> _minimapMobs = new Dictionary<Player, GameObject>();

    [SerializeField]
    private GameObject _normalPlayerMobPrefab;
    [SerializeField]
    private GameObject _infectedPlayerMobPrefab;
    [SerializeField]
    private GameObject _monsterPlayerMobPrefab;
    [SerializeField]
    private GameObject _localPlayerPrefab;

    [SerializeField]
    private RectTransform _mobSpawnPoint;
    [SerializeField]
    private TextMeshProUGUI _currentFloorText;

    private const float REAL_WIDTH = 100f;
    private const float REAL_HEIGHT = 110f;

    [SerializeField]
    private float _xMultiplier = 1f;
    [SerializeField]
    private float _zMultiplier = 1f;

    [SerializeField]
    private float _imageWidth = 1f;
    [SerializeField]
    private float _imageHeight = 1f;

    private void Start()
    {
        _imageWidth = _floorImage.rectTransform.rect.width;
        _imageHeight = _floorImage.rectTransform.rect.height;

        _xMultiplier = _imageWidth / REAL_WIDTH;
        _zMultiplier = _imageHeight / REAL_HEIGHT;
    }

    private void RemoveMinimabMob(Player player)
    {
        if (_minimapMobs.ContainsKey(player) == false)
        {
            return;
        }

        Destroy(_minimapMobs[player]);
        _minimapMobs.Remove(player);
    }

    public void NotifyStateChanged(Player player)
    {
        RemoveMinimabMob(player);
        InstantiateMob(player);
    }

    private void InstantiateMob(Player player)
    {
        GameObject mob;

        if (player.isLocalPlayer)
        {
            mob = Instantiate(_localPlayerPrefab, _mobSpawnPoint.transform);
        }
        else
        {
            switch (player.State)
            {
                case PlayerState.Professor:
                    mob = Instantiate(_monsterPlayerMobPrefab, _mobSpawnPoint.transform);
                    break;
                case PlayerState.Assistant:
                    mob = Instantiate(_infectedPlayerMobPrefab, _mobSpawnPoint.transform);
                    break;
                case PlayerState.Student:
                    mob = Instantiate(_normalPlayerMobPrefab, _mobSpawnPoint.transform);
                    break;
                default:
                    mob = Instantiate(_normalPlayerMobPrefab, _mobSpawnPoint.transform);
                    break;
            }
        }

        mob.GetComponent<MiniMapMob>().SetPlayer(player);

        _minimapMobs.Add(player, mob);
    }

    public void ReigsterPlayerObjects(IEnumerable<Player> allPlayers)
    {
        _allPlayers.AddRange(allPlayers);

        //TODO : Connect proper sprites to players
        foreach (var player in allPlayers)
        {
            if (player.isLocalPlayer)
            {
                _localPlayer = player;
            }

            InstantiateMob(player);
        }
    }

    public void RemovePlayer(Player player)
    {
        _allPlayers.Remove(player);

        RemoveMinimabMob(player);
    }

    public static int GetCurrentFloor(float y)
    {
        if (y >= 30.15)
        {
            return 6;
        }
        else if (y >= 25.15)
        {
            return 5;
        }
        else if (y >= 18.74)
        {
            return 4;
        }
        else if (y >= 12.09)
        {
            return 3;
        }
        else if (y >= 5.2)
        {
            return 2;
        }
        else
        {
            return 1;
        }
    }

    public void ShowFloor(int floor)
    {
        _floor = floor;
        _floorImage.sprite = _floorSprites[floor - 1];
        _currentFloorText.text = $"{floor}층";
        BuildingLightController.Instance?.SetPlayerFloor(floor);
    }

    public void ShowNextFloor()
    {
        var nextFloor = _floor + 1;

        if (nextFloor > _floorSprites.Count)
        {
            nextFloor = 1;
        }

        ShowFloor(nextFloor);
    }

    public void ShowPreviousFloor()
    {
        if (_floor == 1)
        {
            ShowFloor(_floorSprites.Count);
        }
        else
        {
            ShowFloor(_floor - 1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (enabled == false || _localPlayer == null)
        {
            return;
        }

        var floor = _floor;

        if (_isStatic)
        {
            floor = GetCurrentFloor(_localPlayer.transform.position.y);
        }

        DrawFloor(floor);
    }

    private void DrawFloor(int floor)
    {
        if (_floor != floor)
        {
            ShowFloor(floor);
        }

        //테스트 할 때는 로컬플레이어 표시
        for (int i = 0; i < _allPlayers.Count; i++)
        {
            var player = _allPlayers[i];

            if (player == null)
            {
                continue;
            }

            var playerPosition = player.transform.position;
            var x = (playerPosition.x + _xOffset) * _xMultiplier;
            var z = (playerPosition.z + _zOffset) * _zMultiplier;

            var playerMob = _minimapMobs[player];
            var localPosition = new Vector3(x, z, 0);
            playerMob.transform.localPosition = localPosition;
        }
    }
}
