using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LMCore.DevConsole
{
    public delegate void ContextChangeEvent(string[] context);

    public class DevConsole : MonoBehaviour
    {
        public static event ContextChangeEvent OnContextChange;
        private static DevConsole _instance;

        #region Outputting
        public static void Log(string message)
        {
            if (_instance != null) _instance.Output(message, LogLevel.Log);
        }

        public static void Warn(string message)
        {
            if (_instance != null) _instance.Output(message, LogLevel.Warning);
        }

        public static void Error(string message)
        {
            if (_instance != null) _instance.Output(message, LogLevel.Error);
        }

        private void Output(string message, LogLevel level = LogLevel.Log)
        {
            if (console == null) return;
            console.Output(message, level);
        }
        #endregion

        [SerializeField]
        AbsConsoleUI console;

        [SerializeField]
        string favoritesRoot = "LMCore.DevConsole.Fav";

        [SerializeField]
        int maxFavourites = 10;

        public static bool focused
        {
            get
            {
                if (_instance == null || _instance.console == null) return false;

                return _instance.console.Focused;
            }
        }

        public static void Show()
        {
            if (_instance == null || _instance.console == null) return;

            _instance._Show();
        }

        [ContextMenu("Show")]
        private void _Show() =>
            console.Show();

        public static bool Showing => _instance != null &&
            _instance.console != null &&
            _instance.console.Showing;

        public static void Hide(bool instant = false)
        {
            if (_instance == null || _instance.console == null) return;

            _instance._Hide(instant);
        }

        [ContextMenu("Hide")]
        private void __Hide() => _Hide();

        private void _Hide(bool instant = false) =>
            console.Hide(instant);

        [SerializeField, TextArea]
        private string HelpMessage;

        private string GenericHelpMessage => "Welcome to the Developer Console.\n" +
            "/n" +
            "Reserved commands:\n" +
            " - '/?'    Shows this message\n" +
            " - '/list' Lists all available commands\n" +
            " - '/set'  Store a favorite command\n" +
            " - '/fav'  Use or list stored favorites\n" +
            " - '/root' Returns to root commands\n" +
            " - '/up'   Exit current command context\n" +
            " - '/hide' Hide the console";

        private char reservedCharacter = '/';

        private static List<string> context = new List<string>();

        public static void AddContext(string scope)
        {
            if (_instance == null) return;

            scope = scope.Trim();
            if (string.IsNullOrEmpty(scope) || scope.StartsWith(_instance.reservedCharacter))
            {
                return;
            }

            context.Add(scope);
            OnContextChange?.Invoke(context.ToArray());
        }

        public static void RemoveOuterContext(string toRemove)
        {
            if (context.Count < 1) return;
            if (context[context.Count - 1] == toRemove)
            {
                context.RemoveAt(context.Count - 1);
                OnContextChange?.Invoke(context.ToArray());
            }
        }

        #region CommandDiscovery
        private static bool InActiveContext(Command command)
        {
            if (command.Context.Length != context.Count + 1) return false;

            for (int i = 0; i < command.Context.Length - 1; i++)
            {
                if (command.Context[i] != context[i]) return false;
            }

            return true;
        }

        private struct CommandInfo
        {
            public Command command;
            public MethodInfo info;
        }

        private static List<CommandInfo> AllCommands;

        private static IEnumerable<CommandInfo> GetCommands()
        {
            if (AllCommands == null)
            {
                return null;
            }

            return AllCommands
                .Where(c => InActiveContext(c.command));
        }
        #endregion

        #region User Input
        private static void Execute(string commandName, string payload)
        {
            var command = GetCommands()
                ?.Where(c => c.command.Context.Last() == commandName)
                ?.Select(c => c.info)
                ?.FirstOrDefault();

            if (command != null)
            {
                command.Invoke(null, new object[] { payload });
            }
            else
            {
                Error($"Command '{commandName}' not known");
                ListCommands();
            }
        }

        private void ExecuteReserved(string commandName, string payload)
        {
            switch (commandName)
            {
                case "/hide":
                    Output("Closing console");
                    Hide();
                    break;
                case "/root":
                    if (context.Count > 0)
                    {
                        Output("Exiting command contexts");
                        context.Clear();
                        OnContextChange?.Invoke(context.ToArray());
                        ListCommands();
                    }
                    else
                    {
                        Output("Already at root");
                    }
                    break;
                case "/up":
                    if (context.Count == 0)
                    {
                        Output("Already at root");
                    }
                    else if (context.Count == 1)
                    {
                        context.Clear();
                        Output("Exiting command contexts");
                        OnContextChange?.Invoke(context.ToArray());
                        ListCommands();
                    }
                    else
                    {
                        context.RemoveAt(context.Count - 1);
                        Output($"New context: {string.Join(",", context)}");
                        OnContextChange?.Invoke(context.ToArray());
                        ListCommands();
                    }
                    break;
                case "/list":
                    ListCommands();
                    break;
                case "":
                case "/?":
                    Output(
                        string.IsNullOrEmpty(HelpMessage) ? GenericHelpMessage : HelpMessage,
                        LogLevel.Log);
                    break;
                case "/set":
                    if (string.IsNullOrEmpty(payload))
                    {
                        Output("Create new favourite commands:\n" +
                            "/set [favourite-number] [command]\n" +
                            "To unset a favourite:\n" +
                            "/set [favourite-number]");
                    }
                    else
                    {
                        var favNumberStr = SplitCommandAndPayload(payload, out string newFavourite);
                        if (ParseInt(favNumberStr, out int favNumber))
                        {
                            if (favNumber < 0 || favNumber >= maxFavourites)
                            {
                                Output($"Favourites must be in range 0 - {maxFavourites - 1}", LogLevel.Error);
                            }
                            else
                            {
                                var key = $"{favoritesRoot}-{favNumber}";
                                if (string.IsNullOrEmpty(newFavourite))
                                {
                                    if (PlayerPrefs.HasKey(key))
                                    {
                                        PlayerPrefs.DeleteKey(key);
                                        Output($"Favourite {favNumber} removed");
                                    }
                                    else
                                    {
                                        Output($"Favourite {favNumber} does not exist", LogLevel.Warning);
                                    }
                                }
                                else
                                {
                                    PlayerPrefs.SetString(key, newFavourite);
                                    Output($"Command '{newFavourite}' stored.\n" +
                                        $"Type '/fav {favNumber}' to execute it.");
                                }
                            }
                        }
                    }
                    break;
                case "/fav":
                    if (string.IsNullOrEmpty(payload))
                    {
                        var maxWidthFavNumber = maxFavourites.ToString().Count();
                        Output(string.Join(
                            "\n",
                            Enumerable
                                .Range(0, maxFavourites)
                                .Select(favNumber =>
                                {
                                    var key = $"{favoritesRoot}-{favNumber}";
                                    var favStr = favNumber.ToString().PadLeft(maxWidthFavNumber);
                                    if (PlayerPrefs.HasKey(key))
                                    {
                                        return $"{favStr}: {PlayerPrefs.GetString(key)}";
                                    }
                                    else
                                    {
                                        return $"{favStr}: <i>[EMPTY]</i>";
                                    }
                                })));

                    }
                    else
                    {
                        var favStr = SplitCommandAndPayload(payload, out string _);
                        if (ParseInt(favStr, out int favNumber))
                        {
                            var key = $"{favoritesRoot}-{favNumber}";
                            if (PlayerPrefs.HasKey(key))
                            {
                                _ProcessInput(PlayerPrefs.GetString(key));
                            }
                            else
                            {
                                Output($"There's nothing stored on favourite {favNumber}", LogLevel.Error);
                            }
                        }
                        else
                        {
                            Output($"'{favStr}' is not a number\nUsage: /fav [number]", LogLevel.Error);
                        }
                    }
                    break;
                default:
                    Output($"Command '{commandName}' not recognized", LogLevel.Error);
                    Output(
                        string.IsNullOrEmpty(HelpMessage) ? GenericHelpMessage : HelpMessage,
                        LogLevel.Log);
                    break;

            }
        }

        public static void ListCommands()
        {
            var commands = GetCommands()
                ?.Select(c => new { command = c.command.Context.LastOrDefault(), description = c.command.Description })
                ?.Where(c => c.command != null)
                ?.ToList();

            if (commands == null)
            {
                Warn("Commands are still being loaded...");
                return;
            }

            if (commands.Count == 0)
            {
                Warn("There are no commands available in the current context");
                return;
            }

            var maxLength = commands.Max(c => c.command.Length);

            Log(string.Join(
                "\n",
                commands.Select(c => $" - {c.command.PadRight(maxLength)}    {c.description}")
            ));
        }

        #region Parsers
        public static bool ParseInt(string input, out int value) =>
            int.TryParse(
                input?.Trim(),
                out value
                );

        private static Regex vector3intPattern = new Regex("\\(?(-?\\d+), ?(-?\\d+)(?:, ?(-?\\d+)|)\\)?");

        public static bool ParseVector3Int(string input, out Vector3Int value)
        {
            switch (input.ToLower())
            {
                case "forward":
                    value = Vector3Int.forward;
                    return true;
                case "back":
                case "backward":
                    value = Vector3Int.back;
                    return true;
                case "up":
                    value = Vector3Int.up;
                    return true;
                case "down":
                    value = Vector3Int.down;
                    return true;
                case "left":
                    value = Vector3Int.left;
                    return true;
                case "right":
                    value = Vector3Int.right;
                    return true;
                case "zero":
                    value = Vector3Int.zero;
                    return true;
                default:
                    var match = vector3intPattern.Match(input);
                    if (match == null || !match.Success)
                    {
                        value = Vector3Int.zero;
                        return false;
                    }

                    if (match.Groups.Count == 3)
                    {
                        value = new Vector3Int(
                            int.Parse(match.Groups[1].Value),
                            int.Parse(match.Groups[2].Value)
                            );
                        return true;
                    }

                    value = new Vector3Int(
                        int.Parse(match.Groups[1].Value),
                        int.Parse(match.Groups[2].Value),
                        int.Parse(match.Groups[3].Value));
                    return true;

            }
        }
        #endregion

        public static void ProcessInput(string input, bool omitEmpty = true)
        {
            if (omitEmpty && string.IsNullOrEmpty(input.Trim())) return;

            if (_instance == null) return;

            _instance._ProcessInput(input);
        }

        public static string SplitCommandAndPayload(string input, out string payload)
        {
            if (input == null)
            {
                payload = null;
                return null;
            }

            input = input.TrimStart();

            var spacepos = input.IndexOf(" ");
            var command = input;
            payload = null;

            if (spacepos > 0)
            {
                command = input.Substring(0, spacepos);
                payload = input.Substring(spacepos + 1);
            }

            return command;
        }

        private void _ProcessInput(string input)
        {
            var command = SplitCommandAndPayload(input, out var payload);

            if (string.IsNullOrEmpty(input))
            {
                ListCommands();
                return;
            }

            if (string.IsNullOrEmpty(command))
            {
                ListCommands();
            }
            else if (command.StartsWith(reservedCharacter))
            {
                ExecuteReserved(command, payload);
            }
            else
            {
                Execute(command, payload);
            }
        }
        #endregion

        bool firstMessage = true;

        private void OnEnable()
        {
            if (AllCommands == null)
            {
                StartCoroutine(LoadCommands());
            }

            bool addEvent = console != null;
            if (_instance != null && _instance != this)
            {
                _instance.gameObject.SetActive(false);
            }
            else if (_instance == this)
            {
                addEvent = false;
            }

            _instance = this;

            if (addEvent)
            {
                console.OnInput += _ProcessInput;
            }

            if (firstMessage)
            {
                ExecuteReserved("", null);
                firstMessage = false;
            }
        }

        IEnumerator<WaitForSeconds> LoadCommands()
        {
            AllCommands = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                .Where(m => Attribute.IsDefined(m, typeof(Command)) && m.IsStatic)
                .Select(m => new CommandInfo() { command = m.GetCustomAttribute<Command>(), info = m })
                .ToList();

            yield return new WaitForSeconds(0);
        }

        private void OnDisable()
        {
            _instance = null;
            if (console != null)
            {
                console.OnInput -= _ProcessInput;
            }
        }

        private void Start()
        {
            Hide(true);
        }
    }
}
