using LMCore.Juice;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TiledDungeon
{
    // TODO: Player hook up interact 
    // TODO: Use configuration to present lock and button
    // TODO: Align behaviour with configuration
    public class TDDoor : MonoBehaviour
    {
        [SerializeField]
        Transform Door;

        [SerializeField]
        TemporalEasing<float> DoorSliding;

        [SerializeField, HideInInspector]
        bool isOpen = false;

        Vector3 doorReferencePosition;

        private void Start()
        {
            doorReferencePosition = Door.position;
            SyncDoor();
        }

        [ContextMenu("Interact")]
        public void Interact()
        {
            if (Door == null) { return; }

            if (DoorSliding.IsEasing)
            {
                DoorSliding.AbortEase();
            } else if (isOpen)
            {
                DoorSliding.EaseEndToStart();
            } else 
            {
                DoorSliding.EaseStartToEnd();
            }
        }

        void SyncDoor()
        {
            Door.transform.position = doorReferencePosition + Door.transform.right * (isOpen ? 1 : 0);
        }


        private void Update()
        {
            if (DoorSliding.IsEasing)
            {
                Door.transform.position = doorReferencePosition + Door.transform.right * DoorSliding.Evaluate();
                if (!DoorSliding.IsEasing )
                {
                    isOpen = !isOpen;
                }
            }
        }
    }
}
