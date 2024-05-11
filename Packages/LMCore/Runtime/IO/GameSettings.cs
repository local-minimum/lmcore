using UnityEngine;

namespace LMCore.IO
{
    public static class GameSettings
    {
        private static readonly string SettingsRoot = "GameSettings";
        private static readonly string MovementsRoot = $"{SettingsRoot}.Movement";
        private static readonly string UIRoot = $"{SettingsRoot}.UI";

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
                    PlayerPrefs.SetString(Key, value);
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
    }
}