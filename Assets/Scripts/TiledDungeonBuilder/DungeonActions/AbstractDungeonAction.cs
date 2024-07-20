using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TiledDungeon.Actions
{
    public abstract class AbstractDungeonAction : MonoBehaviour
    {
        abstract public bool Available { get; }
        abstract public bool IsEasing { get; }
        abstract public void Abandon();
        abstract public void Finalise();
        abstract public void Play();
    }
}
