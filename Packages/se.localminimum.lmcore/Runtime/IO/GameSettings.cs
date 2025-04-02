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
        private static readonly string Gameplay = $"{SettingsRoot}.Gameplay";
        private static readonly string Camera = $"{SettingsRoot}.Camera";
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

            public override string ToString() =>
                $"['{Key}':'{Value}' {(PlayerPrefs.HasKey(Key) ? "Stored" : "Mem")} Default: {DefaultValue}]";
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

        public class FloatSetting
        {
            public delegate void OnChangeEvent(float value);

            public event OnChangeEvent OnChange;

            public readonly string Key;
            private float DefaultValue;

            public FloatSetting(string key, float defaultValue = 0f)
            {
                Key = key;
                DefaultValue = defaultValue;
            }

            public float Value
            {
                get
                {
                    return PlayerPrefs.GetFloat(Key, DefaultValue);
                }

                set
                {
                    PlayerPrefs.SetFloat(Key, value);
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

        #region Camera
        public static readonly FloatSetting FOV = new FloatSetting($"{Camera}.FOV", 60f);
        #endregion

        #region Gameplay
        public static readonly BoolSetting Autoloot = new BoolSetting($"{Gameplay}.Autoloot", true);
        #endregion

        #region Movement
        public static readonly BoolSetting InstantMovement = new BoolSetting($"{MovementsRoot}.InstantMovement", false);

        private static Dictionary<Movement, StringSetting> _movementSettings = new Dictionary<Movement, StringSetting>();
        public static StringSetting GetMovementSetting(Movement movement, string defaultValue = null)
        {
            if (_movementSettings.ContainsKey(movement)) return _movementSettings[movement];

            var setting = new StringSetting($"{MovementsRoot}.{movement}", defaultValue);
            _movementSettings.Add(movement, setting);
            return setting;
        }
        #endregion Movement

        #region Gameplay Action
        private static Dictionary<GamePlayAction, StringSetting> _actionSettings = new Dictionary<GamePlayAction, StringSetting>();
        public static IEnumerable<StringSetting> GetActionSettings(GamePlayAction action, string defaultValue = null) => action
            .AsPrimitives()
            .Select(a =>
            {
                if (_actionSettings.ContainsKey(a)) return _actionSettings[a];

                var setting = new StringSetting($"{ActionsRoot}.{a}", defaultValue);
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