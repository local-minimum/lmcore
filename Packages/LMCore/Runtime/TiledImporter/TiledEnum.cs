﻿using System;

namespace LMCore.TiledImporter
{
    [Serializable]
    public class TiledEnum<T>
    {
        public string TypeName;
        public T Value;

        public override string ToString() => $"<{TypeName}({Value})>";
    }
}
