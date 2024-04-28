using UnityEngine;

namespace LMCore.IO
{
    public static class GameSettings
    {

        static readonly string SettingsRoot = "GameSettings";
        static readonly string MovementsRoot = $"{SettingsRoot}.Movement";
        static readonly string UIRoot = $"{SettingsRoot}.UI";

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
