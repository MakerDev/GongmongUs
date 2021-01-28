using FirstGearGames.Utilities.Networks;
using Mirror;
using UnityEngine;


namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms.Demos
{


    public class MovingPlatform : NetworkBehaviour
    {
        public float MaxChangeTime = 2f;
        public Vector3 _positionRange;
        public Vector3 _rotationRange;
        public float PosRate = 4f;
        public float RotRate = 60f;

        private Vector3 _startPos;

        private float _nextRotTime = 0f;
        private float _nextPosTime = 0f;

        private Quaternion _rotGoal;
        private Vector3 _posGoal;
        private bool _useGoals = true;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _startPos = transform.position;
        }


        private void Update()
        {
            if (!base.isServer)
                return;

            if (Input.GetKeyDown(KeyCode.X))
                _useGoals = !_useGoals;

            if (Time.time > _nextRotTime)
            {
                _nextRotTime = Time.time + Random.Range(0.5f, MaxChangeTime);
                _rotGoal = Quaternion.Euler(RandomV3(_rotationRange));
            }
            if (Time.time > _nextPosTime)
            {
                _nextPosTime = Time.time + Random.Range(0.5f, MaxChangeTime);
                _posGoal = _startPos + RandomV3(_positionRange);
            }

            Quaternion rotGoal = (_useGoals) ? _rotGoal : Quaternion.identity;
            Vector3 posGoal = (_useGoals) ? _posGoal : Vector3.zero;

            transform.position = Vector3.MoveTowards(transform.position, posGoal, Time.deltaTime * PosRate);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotGoal, Time.deltaTime * RotRate);

            //uint value = Quaternions.CompressQuaternion(transform.rotation);
            //Quaternion r = Quaternions.DecompressQuaternion(value);
            //Debug.Log("A " + ReturnRotationAsString(transform.rotation));
            //Debug.Log("B " + ReturnRotationAsString(r));
        }

        //private string ReturnRotationAsString(Quaternion rot)
        //{
        //    return (rot.w + ", " + rot.x + ", " + rot.y + ", " + rot.z);
        //}


        private Vector3 RandomV3(Vector3 range)
        {
            return new Vector3(
                Random.Range(-range.x, range.x),
                Random.Range(-range.y, range.y),
                Random.Range(-range.z, range.z)
                );
        }
    }


}