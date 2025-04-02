using System.Linq;
using UnityEngine;

namespace LMCore.IO
{
    public class ActionBindingConf : AbsBindingConf
    {
        [SerializeField]
        GamePlayAction gameplayAction;
        public bool Defines(GamePlayAction action, int bindingIndex) =>
            gameplayAction == action && this.bindingIndex == bindingIndex;

        // TODO: Allow for more than one binding stored
        // Iteration is over flags not actually over different bindings stored
        public override GameSettings.StringSetting Settings =>
            GameSettings
            .GetActionSettings(gameplayAction)
            .FirstOrDefault();


        private void OnValidate()
        {
            if (gameplayAction.AsPrimitives().Count() > 1)
            {
                Debug.Log("Cannot have more than one value selected at once");
                gameplayAction = GamePlayAction.None;
            }
        }
    }
}
