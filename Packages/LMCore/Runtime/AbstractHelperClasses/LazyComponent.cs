using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.AbstractClasses
{
    public class LazyComponent<T>
    {
        private readonly GameObject go;

        public LazyComponent(GameObject go) {
            this.go = go;
        }

        private T component;

        public T Value
        {
            get
            {
                if (component == null)
                {
                    component = go.GetComponent<T>();
                }
                return component;
            }
        }

        public static implicit operator T(LazyComponent<T> lazy) => lazy.Value;
    }
}
