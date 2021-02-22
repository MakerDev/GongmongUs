using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingLightController : MonoBehaviour
{
    public static BuildingLightController Instance { get; private set; } = null;

    [SerializeField]
    private List<MeshRenderer> _floorRenderers = new List<MeshRenderer>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void SetPlayerFloor(int currentFloor)
    {
        foreach (var floorRenderer in _floorRenderers)
        {
            floorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        //6층은 계속 켜놓을까?

        if (currentFloor >= 6)
        {
            _floorRenderers[currentFloor - 1].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
        else
        {
            _floorRenderers[currentFloor - 1].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            //_floorRenderers[currentFloor].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }
}
