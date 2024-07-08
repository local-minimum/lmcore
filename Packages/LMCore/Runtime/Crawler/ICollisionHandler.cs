using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Crawler
{
    public interface ICollisionHandler
    {
        public void Collision(GridEntity entity); 
    }
}
