using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniMap : MonoBehaviour
{
    private static List<GameObject> _otherPlayers = new List<GameObject>();
    private static GameObject _localPlayer = null;
    
    private const int _xOffset = 43;
    private const int _yOffset = 42;

    /// <summary>
    /// If this is always on minimap which follows local player or the maps that are shown when pressing tap key.
    /// </summary>
    [SerializeField]
    private bool _isStatic = false;

    private int _floor = 1;

    [SerializeField]
    private Image _floorImage;
    [SerializeField]
    private List<Sprite> _floorSprites = new List<Sprite>();

    private int CurrentFloorOfLocalPlayer()
    {
        var y = _localPlayer.transform.position.y;

        if (y >= 3.2)
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
    }

    public void ShowNextFloor()
    {
        ShowFloor(((_floor + 1) % _floorSprites.Count) + 1);
    }

    public void ShowPreviousFloor()
    {
        ShowFloor(((_floor - 1) % _floorSprites.Count) + 1);
    }

    public static void ReigsterPlayerObjects(GameObject localPlayer, List<GameObject> otherPlayers)
    {
        _localPlayer = localPlayer;
        _otherPlayers.AddRange(otherPlayers);
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
            floor = CurrentFloorOfLocalPlayer();
        }

        DrawFloor(floor);
    }

    private void DrawFloor(int floor)
    {

    }
}
