using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.UI
{
    public delegate void MenuEvent(AbsMenu menu);
    public delegate void HideMenusEvent();

    public abstract class AbsMenu : MonoBehaviour
    {
        /// <summary>
        /// Called with menu that gets focused
        /// </summary>
        public static event MenuEvent OnShowMenu;
        /// <summary>
        /// Called when a menu loses focus
        /// </summary>
        public static event MenuEvent OnExitMenu;
        /// <summary>
        /// Called when no menu is active anymore
        /// </summary>
        public static event HideMenusEvent OnHideMenus;

        public abstract bool PausesGameplay { get; }

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
        private static List<AbsMenu> ActiveMenus = new List<AbsMenu>();

        /// <summary>
        /// Reference to the currently focused menu
        /// </summary>
        public static AbsMenu FocusedMenu => ActiveMenus.LastOrDefault();

        /// <summary>
        /// If any menu is showing
        /// </summary>
        public static bool ShowingMenus => ActiveMenus.Count > 0;

        /// <summary>
        /// If any menu in the stack pauses gameplay, we are paused
        /// </summary>
        public static bool PausingGameplay => ActiveMenus.Any(menu => menu.PausesGameplay);

        /// <summary>
        /// If the menu has been show, not neccesarily being focused
        /// </summary>
        public bool ActiveMenu => ActiveMenus.Contains(this);

        /// <summary>
        /// If current menu is the one being focused
        /// </summary>
        public bool Focused => ActiveMenus.LastOrDefault() == this;

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

            var previous = ActiveMenus.LastOrDefault();
            ActiveMenus.Add(this);
            Focus();
            previous?.Blur();

            OnShowMenu?.Invoke(this);
        }

        /// <summary>
        /// Exit the current menu giving focus to the previous one in the stack
        /// </summary>
        public void Exit()
        {
            var idx = ActiveMenus.LastIndexOf(this);
            if (idx != -1)
            {
                var currentLast = ActiveMenus.LastOrDefault();
                ActiveMenus.RemoveAt(idx);
                var newLast = ActiveMenus.LastOrDefault();
                if (newLast == null)
                {
                    OnHideMenus?.Invoke();
                }
                else if (currentLast != newLast)
                {
                    newLast?.Focus();
                    OnShowMenu?.Invoke(newLast);
                }
                else
                {
                    Debug.LogWarning(PrefixLogMessage("Exited myself even though I wasn't in focus, this might be fine"));
                }
            }
            else
            {
                Debug.LogWarning(PrefixLogMessage("Shouldn't have exited myself since I wasn't registered in the stack"));
            }
            Blur();
            OnExitMenu?.Invoke(this);
        }

        abstract public string MenuId { get; }

        private static Dictionary<string, AbsMenu> AllMenus = new Dictionary<string, AbsMenu>();
        public static AbsMenu GetMenu(string menuId) => AllMenus[menuId];

        private void Awake()
        {
            if (string.IsNullOrEmpty(MenuId))
            {
                Debug.LogError(PrefixLogMessage("We don't have a name"));
                Destroy(gameObject);
            }
            else if (AllMenus.ContainsKey(MenuId))
            {
                Debug.LogError(PrefixLogMessage("We already exist"));
                Destroy(gameObject);
            }
            else
            {
                AllMenus[MenuId] = this;
            }
        }

        private void OnDestroy()
        {
            if (AllMenus.ContainsKey(MenuId))
            {
                AllMenus.Remove(MenuId);
            }
        }
    }
}
