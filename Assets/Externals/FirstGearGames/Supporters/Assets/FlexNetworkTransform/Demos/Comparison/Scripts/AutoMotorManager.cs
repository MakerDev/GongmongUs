using FirstGearGames.Utilities.Networks;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms.Demos
{

    public class AutoMotorManager : NetworkBehaviour
    {
        public float MoveRate = 4f;
        public float MoveVariance = 0.3f;
        public Transform[] Targets = new Transform[0];
  
        public override void OnStartServer()
        {
            base.OnStartServer();
            StartCoroutine(__Move());
        }

        private IEnumerator __Move()
        {
            Vector3[] startPositions = new Vector3[Targets.Length];
            for (int i = 0; i < Targets.Length; i++)
            {
                if (Targets[i] != null)
                    startPositions[i] = Targets[i].position;
            }

            float xRange = 4f;
            float yRange = 2f;

            int waitSteps = 8;
            int step = 0;
            float adjustedRate = MoveRate * Random.Range(1f - MoveVariance, 1f + MoveVariance);
            Vector3 randomPosition = new Vector3(Random.Range(-xRange, xRange), Random.Range(-yRange, yRange), 0f);
            while (true)
            {
                bool notAtGoal = false;
                for (int i = 0; i < Targets.Length; i++)
                {
                    if (Targets[i] != null && Targets[i].gameObject.activeInHierarchy)
                    {
                        Vector3 goal = startPositions[i] + randomPosition;
                        Targets[i].position = Vector3.MoveTowards(Targets[i].position, goal, adjustedRate * Time.deltaTime);
                        if (Targets[i].position != goal)
                            notAtGoal = true;
                    }
                }

                //If all transforms are at goal.
                if (!notAtGoal)
                {
                    adjustedRate = MoveRate * Random.Range(1f - MoveVariance, 1f + MoveVariance);
                    randomPosition = new Vector3(Random.Range(-xRange, xRange), Random.Range(-yRange, yRange), 0f);

                    step++;
                    if (step == waitSteps)
                    {
                        step = 0;
                        yield return new WaitForSeconds(1f);
                    }
                }

                yield return null;
            }
        }

    }


}