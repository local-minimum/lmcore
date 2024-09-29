using UnityEngine;

namespace LMCore.TiledDungeon.Actions
{
    public abstract class AbstractDungeonAction : MonoBehaviour
    {
        abstract public bool Available { get; }
        abstract public bool IsEasing { get; }
        abstract public void Abandon();
        abstract public void Finalise();
        abstract public void Play(System.Action onDoneCallback = null, System.Action<float> onProgress = null);

        abstract public void PlayFromCurrentProgress(System.Action onDoneCallback = null, System.Action<float> onProgress = null);

        public static float NewtonRaphson(System.Func<float, float> errorFunction, float acceptError, int maxDepth=20)
        {
            float bestProgress = 0;
            float bestError = float.MaxValue;

            float progress = 0.5f;
            float deltaScale = 0.001f;


            for (int i = 0; i < maxDepth; i++)
            {
                float error = errorFunction(progress);

                if (error < bestError)
                {
                    bestProgress = progress;
                    bestError = error;

                    if (bestError < acceptError)
                    {
                        break;
                    }
                }

                float stepSize = deltaScale * (progress > 0.5f ? -1 : 1);
                float slope = (error - errorFunction(progress + stepSize)) / stepSize;
                progress = progress - error / slope;
            }

            return bestProgress;
        }

    }
}
