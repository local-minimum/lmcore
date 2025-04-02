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
        string _Name;

        [SerializeField, TextArea]
        string _Description;

        [SerializeField]
        ItemType _ItemType;

        [SerializeField]
        EquipmentType _Equipment = EquipmentType.None;

        [SerializeField]
        Sprite _UISprite;

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

        public override string Name => _Name;

        public override string Description => _Description;

        public override Sprite UISprite => _UISprite;
        public override Transform WorldRoot => _WorldRoot;

        public override bool Stackable => _StackSizeLimit > 1;

        public override int StackSizeLimit => _StackSizeLimit;

        public override ItemType Type => _ItemType;

        public override EquipmentType Equipment => _Equipment;
    }
}
