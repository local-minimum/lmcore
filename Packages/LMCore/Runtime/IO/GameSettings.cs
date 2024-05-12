using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.IO
{
    public static class GameSettings
    {
        private static readonly string SettingsRoot = "GameSettings";
        private static readonly string MovementsRoot = $"{SettingsRoot}.Movement";
        private static readonly string ActionsRoot = $"{SettingsRoot}.Actions";
        private static readonly string UIRoot = $"{SettingsRoot}.UI";
        private static readonly string CustomRoot = $"{SettingsRoot}.Custom";

        public class StringSetting
        {
            public delegate void OnChangeEvent(string value);

            public event OnChangeEvent OnChange;

            public readonly string Key;
            private string DefaultValue;

            public StringSetting(string key, string defaultValue = null)
            {
                Key = key;
                DefaultValue = defaultValue;
            }

            public string Value
            {
                get
                {
                    return PlayerPrefs.GetString(Key, DefaultValue);
                }

                set
                {
                    PlayerPrefs.SetString(Key, value ?? "");
                    OnChange?.Invoke(value);
                }
            }

            public void RestoreDefault()
            {
                PlayerPrefs.DeleteKey(Key);
                OnChange?.Invoke(DefaultValue);
            }

            public void StoreCurrrentValue()
            {
                Value = Value;
            }
        }

        public class BoolSetting
        {
            public delegate void OnChangeEvent(bool value);

            public event OnChangeEvent OnChange;

            public readonly string Key;
            private bool DefaultValue;

            public BoolSetting(string key, bool defaultValue = false)
            {
                Key = key;
                DefaultValue = defaultValue;
            }

            public bool Value
            {
                get
                {
                    return PlayerPrefs.GetInt(Key, DefaultValue ? 1 : 0) == 1;
                }

                set
                {
                    PlayerPrefs.SetInt(Key, value ? 1 : 0);
                    OnChange?.Invoke(value);
                }
            }

            public void RestoreDefault()
            {
                PlayerPrefs.DeleteKey(Key);
            }

            public void StoreCurrrentValue()
            {
                Value = Value;
            }
        }

        #region Movement
        public static readonly BoolSetting InstantMovement = new BoolSetting($"{MovementsRoot}.InstantMovemnt", false);

        private static readonly StringSetting MovementForward = new StringSetting($"{MovementsRoot}.{Movement.Forward}", "w");
        private static readonly StringSetting MovementBackward = new StringSetting($"{MovementsRoot}.{Movement.Backward}", "s");
        private static readonly StringSetting MovementLeft = new StringSetting($"{MovementsRoot}.{Movement.StrafeLeft}", "a");
        private static readonly StringSetting MovementRight = new StringSetting($"{MovementsRoot}.{Movement.StrafeRight}", "d");
        private static readonly StringSetting MovementTurnCW = new StringSetting($"{MovementsRoot}.{Movement.TurnCW}", "e");
        private static readonly StringSetting MovementTurnCCW = new StringSetting($"{MovementsRoot}.{Movement.TurnCCW}", "q");
        public static StringSetting GetMovementSetting(Movement movement) { 
            switch (movement)
            {
                case Movement.Forward: return MovementForward;
                case Movement.Backward: return MovementBackward;
                case Movement.StrafeLeft: return MovementLeft;
                case Movement.StrafeRight: return MovementRight;
                case Movement.TurnCW: return MovementTurnCW;  
                case Movement.TurnCCW: return MovementTurnCCW;
                default: return null;
            }
        }
        #endregion Movement

        #region Gameplay Action
        private static Dictionary<GamePlayAction, StringSetting> _actionSettings = new Dictionary<GamePlayAction, StringSetting>();
        public static IEnumerable<StringSetting> GetActionSettings(GamePlayAction action) => action
            .AsPrimitives()
            .Select(a => 
            {
                if (_actionSettings.ContainsKey(a)) return _actionSettings[a];

                var setting = new StringSetting($"{ActionsRoot}.{a}");
                _actionSettings[a] = setting;

                return setting;
            });
        #endregion Gameplay Action

        public static readonly BoolSetting MinimiapVisible = new BoolSetting($"{UIRoot}.Minimap.Visible", true);
        public static readonly BoolSetting KeyPressHUDVisible = new BoolSetting($"{UIRoot}.KeyPressHUD.Visible", true);

        public static void RestoreAllSettings()
        {
            InstantMovement.RestoreDefault();

            MinimiapVisible.RestoreDefault();
            KeyPressHUDVisible.RestoreDefault();
        }

        public static void StoreAllSettings()
        {
            InstantMovement.StoreCurrrentValue();

            MinimiapVisible.StoreCurrrentValue();
            KeyPressHUDVisible.StoreCurrrentValue();
        }

        #region Custom Settings
        private static Dictionary<string, BoolSetting> CustomBools = new Dictionary<string, BoolSetting>();
        public static BoolSetting GetCustomBool(string key, bool defaultValue = false)
        {
            var fullKey = $"{CustomRoot}.{key}";
            if (CustomBools.ContainsKey(fullKey))
            {
                return CustomBools[fullKey];
            }

            var setting = new BoolSetting(fullKey, defaultValue);

            CustomBools[fullKey] = setting;

            return setting;
        }

        private static Dictionary<string, StringSetting> CustomStrings = new Dictionary<string, StringSetting>();
        public static StringSetting GetCustomString(string key, string defaultValue = null)
        {
            var fullKey = $"{CustomRoot}.{key}";
            if (CustomStrings.ContainsKey(fullKey))
            {
                return CustomStrings[fullKey];
            }

            var setting = new StringSetting(fullKey, defaultValue);

            CustomStrings[fullKey] = setting;

            return setting;
        }
        #endregion Custom Settings
    }
}