using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TiledDungeon
{
    public class TDSpikeTrap : MonoBehaviour
    {
        [SerializeField]
        GameObject[] Spikes;

        [SerializeField]
        bool Spikeless = false;
        void Start()
        {
            if (Spikeless)
            {
                foreach (var spike in Spikes)
                {
                    spike.SetActive(false);
                }
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
