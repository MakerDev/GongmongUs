
using Mirror;
using System.Collections;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkAnimators.Demos
{

    public class AnimateStressTest : NetworkBehaviour
    {
        private bool _useFNA = true;

        [Range(0, 2)]
        public int StressIntensity = 0;
        public bool DoubleParams = false;

        private FlexNetworkAnimator _fna;
        private NetworkAnimator _networkAnimator;
        private Animator _animator;

        private static int _boolTest = Animator.StringToHash("Toggle");
        private static int _intTest = Animator.StringToHash("Number");
        private static int _floatTest = Animator.StringToHash("Horizontal");
        private static int _triggerTest = Animator.StringToHash("Jump");
        private static int _boolTest2 = Animator.StringToHash("Toggle2");
        private static int _intTest2 = Animator.StringToHash("Number2");
        private static int _floatTest2 = Animator.StringToHash("Horizontal2");
        private static int _triggerTest2 = Animator.StringToHash("Jump2");

        private static int _playTest = Animator.StringToHash("PlayJump");


        private bool _lastBool = false;

        private float ReturnWaitRange()
        {
            if (StressIntensity == 0)
            {
                return Random.Range(0.5f, 2f);
            }
            else if (StressIntensity == 1)
            {
                return (Random.Range(0.2f, 0.75f));
            }
            else
            {
                return 0f;
            }
        }
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _fna = GetComponent<FlexNetworkAnimator>();
            _networkAnimator = GetComponent<NetworkAnimator>();
            _useFNA = (_fna != null);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            StartCoroutine(__RandomizeBool());
            StartCoroutine(__RandomizeFloat());
            StartCoroutine(__RandomizeSpeed());
            StartCoroutine(__RandomizeLayerWeight());
            StartCoroutine(__RandomizeInt());
            StartCoroutine(__RandomizeTrigger());
            StartCoroutine(__RandomizePlay());
        }


        private IEnumerator __RandomizeBool()
        {
            while (true)
            {
                _animator.SetBool(_boolTest, !_lastBool);
                if (DoubleParams)
                    _animator.SetBool(_boolTest, !_lastBool);

                _lastBool = !_lastBool;

                yield return new WaitForSeconds(ReturnWaitRange());
            }
        }

        private IEnumerator __RandomizeFloat()
        {
            while (true)
            {
                float next = Random.Range(-1f, 1f);
                _animator.SetFloat(_floatTest, next);
                if (DoubleParams)
                    _animator.SetFloat(_floatTest2, next);

                yield return new WaitForSeconds(ReturnWaitRange());
            }
        }

        private IEnumerator __RandomizeSpeed()
        {

            while (true)
            {
                float next = Random.Range(0f, 1f);
                _animator.speed = next;
                yield return new WaitForSeconds(ReturnWaitRange());
            }
        }

        private IEnumerator __RandomizeLayerWeight()
        {

            while (true)
            {
                float next = Random.Range(0f, 1f);
                _animator.SetLayerWeight(1, next);
                if (DoubleParams)
                    _animator.SetLayerWeight(2, next);

                yield return new WaitForSeconds(ReturnWaitRange());
            }
        }


        private IEnumerator __RandomizeInt()
        {
            while (true)
            {
                int next = Random.Range(-10, 10);
                _animator.SetInteger(_intTest, next);
                if (DoubleParams)
                    _animator.SetInteger(_intTest2, next);

                yield return new WaitForSeconds(ReturnWaitRange());
            }
        }

        private IEnumerator __RandomizeTrigger()
        {
            while (true)
            {
                if (_useFNA && _fna != null)
                {
                    _fna.SetTrigger(_triggerTest);
                    if (DoubleParams)
                        _fna.SetTrigger(_triggerTest2);
                }
                else if (_networkAnimator != null)
                {
                    _networkAnimator.SetTrigger(_triggerTest);
                    if (DoubleParams)
                        _networkAnimator.SetTrigger(_triggerTest2);
                }

                yield return new WaitForSeconds(ReturnWaitRange());
            }
        }

        private IEnumerator __RandomizePlay()
        {
            while (true)
            {
                
                if (_useFNA && _fna != null)
                {
                    _fna.Play(_playTest);
                }
                //NA checks every frame automatically.
                else if (_networkAnimator != null)
                {
                }

                float wait = ReturnWaitRange() * 3f;
                yield return new WaitForSeconds(wait);
            }
        }



    }


}