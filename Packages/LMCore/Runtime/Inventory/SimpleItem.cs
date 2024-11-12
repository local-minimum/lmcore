using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Inventory
{
    public class SimpleItem : AbsItem
    {
        [SerializeField, HideInInspector]
        int _StackSizeLimit = 1;

        [SerializeField, HideInInspector]
        string _Id;

        [SerializeField, HideInInspector]
        string _Origin;

        [SerializeField]
        ItemType _ItemType;

        [SerializeField]
        RectTransform _UIRoot;

        [SerializeField]
        Transform _WorldRoot;

        public void Configure(string id, string origin, int stackSizeLimit = 1)
        {
            _Id = id;
            _Origin = origin;
            _StackSizeLimit = stackSizeLimit;
        }

        public override string Id => _Id;

        public override string Origin => _Origin;

        public override RectTransform UIRoot => _UIRoot;

        public override Transform WorldRoot => _WorldRoot;

        public override bool Stackable => _StackSizeLimit > 1;

        public override int StackSizeLimit => _StackSizeLimit;

        public override ItemType Type => _ItemType;
    }
}
