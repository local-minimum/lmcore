using UnityEngine;

namespace TiledImporter
{
    [SerializeField]
    public class TiledEnum<T>
    {
        public string TypeName;
        public T Value;
    }
}
