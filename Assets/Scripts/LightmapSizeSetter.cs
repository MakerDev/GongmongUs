using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    //[ExecuteInEditMode]
    public class LightmapSizeSetter : MonoBehaviour
    {
        [SerializeField]
        private int _lightmatScale = 128;

        private void Start()
        {
            var renderers = GetComponentsInChildren<MeshRenderer>();

            foreach (var renderer in renderers)
            {
                //renderer.scaleInLightmap = _lightmatScale;
            }
        }
    }
}