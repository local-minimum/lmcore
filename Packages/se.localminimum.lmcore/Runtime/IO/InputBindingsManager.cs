using LMCore.AbstractClasses;
using System.Collections.Generic;
using System.Linq;

namespace LMCore.IO
{
    public class InputBindingsManager : Singleton<InputBindingsManager, InputBindingsManager>
    {
        List<ActionBindingConf> _actionBindins;
        List<ActionBindingConf> actionBindins
        {
            get
            {
                if (_actionBindins == null)
                {
                    _actionBindins = GetComponentsInChildren<ActionBindingConf>().ToList();
                }
                return _actionBindins;
            }
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        public string GetActiveActionHint(
            GamePlayAction action,
            string decoration = "[%HINT%]",
            string missing = "<UNSET>")
        {
            var device = ActionMapToggler.LastDevice;

            var options = actionBindins
                .Where(a => a.Defines(action, 0) && a.For(device))
                .ToList();

            if (options.Count == 0) return missing;

            return decoration.Replace("%HINT%", options[0].HumanizedBinding());
        }
    }
}
