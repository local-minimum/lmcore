using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.UI
{
    public delegate void ShowMenuEvent(AbsMenu menu);
    public delegate void HideMenusEvent();

    public abstract class AbsMenu : MonoBehaviour
    {
        public static event ShowMenuEvent OnShowMenu;
        public static event HideMenusEvent OnHideMenus;

        /// <summary>
        /// Function called when a menu becomes the focused menu
        /// </summary>
        protected abstract void Focus();
        
        /// <summary>
        /// Function called to clean up when another menu takes focus
        /// or menu is exited
        /// </summary>
        protected abstract void Blur();

        /// <summary>
        /// This works as a stack where last shown is put lats and 
        /// exiting the most recent menu gives focus to the new
        /// top of the stack
        /// </summary>
        private static List<AbsMenu> menus = new List<AbsMenu>();

        protected string PrefixLogMessage(string message) =>
            $"Menu {name}: {message}";

        /// <summary>
        /// Call when the menu gets invoked by the player.
        ///
        /// Causes Focus to be called on this menu and Blur to be
        /// called on the previously focused menu, if such exists
        /// 
        /// This is not called if menu regains focus later on by
        /// the player exiting another menu getting back to this menu,
        /// then only Focus is called.
        /// </summary>
        public void Show()
        {

            var previous = menus.LastOrDefault();
            menus.Add(this);
            Focus();
            previous?.Blur();

            OnShowMenu?.Invoke(this);
        }

        /// <summary>
        /// Exit the current menu giving focus to the previous one in the stack
        /// </summary>
        public void Exit()
        {
            var idx = menus.LastIndexOf(this);
            if (idx != -1)
            {
                var currentLast = menus.LastOrDefault();
                menus.RemoveAt(idx);
                var newLast = menus.LastOrDefault();
                if (newLast == null)
                {
                    OnHideMenus?.Invoke();
                }
                else if (currentLast != newLast)
                {
                    newLast?.Focus();
                    OnShowMenu?.Invoke(newLast);
                } else
                {
                    Debug.LogWarning(PrefixLogMessage("Exited myself even though I wasn't in focus, this might be fine"));
                }
            } else
            {
                Debug.LogWarning(PrefixLogMessage("Shouldn't have exited myself since I wasn't registered in the stack"));
            }
            Blur();

        }
    }
}
