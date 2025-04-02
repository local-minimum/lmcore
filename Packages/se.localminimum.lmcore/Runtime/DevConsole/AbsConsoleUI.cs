using UnityEngine;

namespace LMCore.DevConsole
{
    public delegate void InputEvent(string input);

    public abstract class AbsConsoleUI : MonoBehaviour
    {
        public abstract event InputEvent OnInput;

        public abstract bool Focused { get; }

        public abstract bool Showing { get; }

        public abstract void Show();
        public abstract void Hide(bool instant = false);

        public abstract void Output(string message, LogLevel level);
    }
}
