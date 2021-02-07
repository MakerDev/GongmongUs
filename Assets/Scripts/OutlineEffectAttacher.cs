using cakeslice;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineEffectAttacher : NetworkBehaviour
{
    [SerializeField]
    private GameObject _cameraObject;

    private void Start()
    { 
        if (isLocalPlayer)
        {
            var outlineEffect = _cameraObject.AddComponent<OutlineEffect>();
            outlineEffect.lineColor0 = new Color(242, 255, 0);
            outlineEffect.fillAmount = 0;
            outlineEffect.useFillColor = true;
            outlineEffect.fillColor = new Color(252, 182, 0)
            {
                a = 0.2f
            };
            outlineEffect.lineThickness = 3.3f;
            outlineEffect.lineIntensity = 2.6f;
        }
    }
}
