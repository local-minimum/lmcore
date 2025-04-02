using UnityEngine;

namespace LMCore.IO
{
    public class MovementBindingConf : AbsBindingConf
    {
        [SerializeField]
        Movement movement;


        public bool Defines(Movement movement, int bindingIndex) =>
            this.movement == movement && this.bindingIndex == bindingIndex;

        // TODO: Allow for more than one binding stored
        public override GameSettings.StringSetting Settings =>
            GameSettings.GetMovementSetting(movement);
    }
}
