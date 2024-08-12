using LMCore.AbstractClasses;
using LMCore.Extensions;
using LMCore.IO;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LMCore.UI
{
    public class KeyPressHUD : Singleton<KeyPressHUD, KeyPressHUD>
    {
        [SerializeField]
        private GameObject[] ExtraActions;

        [SerializeField]
        private GameObject RotateCCW;

        [SerializeField]
        private GameObject MoveForward;

        [SerializeField]
        private GameObject RotateCW;

        [SerializeField]
        private GameObject StrafeLeft;

        [SerializeField]
        private GameObject MoveBackward;

        [SerializeField]
        private GameObject StrafeRight;

        [SerializeField]
        private Color DefaultColor;

        [SerializeField]
        private Color ActiveColor;

        [SerializeField]
        private Color PressedColor;

        [SerializeField, Range(0, 1)]
        private float easeToDefaultTime = 0.1f;

        private struct Ease
        {
            public readonly GameObject Target;
            public readonly Color StartColor;
            private float StartTime;
            private float Duration;

            public Ease(GameObject target, Color startColor, float duration)
            {
                Target = target;
                StartColor = startColor;
                StartTime = Time.timeSinceLevelLoad;
                Duration = duration;
            }

            public float CalculateProgress() => Mathf.Clamp01((Time.timeSinceLevelLoad - StartTime) / Duration);
        }

        private List<Ease> eases = new List<Ease>();

        private void ApplyEffect(GameObject go, Color color)
        {
            foreach (Image image in go.GetComponentsInChildren<Image>())
            {
                image.color = color;
            }

            foreach (TextMeshProUGUI text in go.GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.color = color;
            }
        }

        private GameObject GetByMovement(Movement movement)
        {
            switch (movement)
            {
                case Movement.Forward:
                    return MoveForward;

                case Movement.Backward:
                    return MoveBackward;

                case Movement.StrafeLeft:
                    return StrafeLeft;

                case Movement.StrafeRight:
                    return StrafeRight;

                case Movement.YawCCW:
                    return RotateCCW;

                case Movement.YawCW:
                    return RotateCW;

                default:
                    return null;
            }
        }

        private List<Movement> pressed = new List<Movement>();

        private void SyncPressed()
        {
            for (int i = 0, n = pressed.Count; i < n; i++)
            {
                var movement = pressed[i];
                var go = GetByMovement(movement);
                if (go == null) continue;

                ApplyEffect(go, i == n - 1 ? ActiveColor : PressedColor);
            }
        }

        public void Press(Movement movement)
        {
            var go = GetByMovement(movement);
            if (go == null) return;

            pressed.Add(movement);

            eases = eases.Where(e => e.Target != go).ToList();

            SyncPressed();
        }

        public void Release(Movement movement)
        {
            var go = GetByMovement(movement);
            if (go == null) return;

            if (pressed.Count > 0)
            {
                eases.Add(new Ease(go, pressed.Last() == movement ? ActiveColor : PressedColor, easeToDefaultTime));
            }

            pressed.Remove(movement);

            SyncPressed();
        }

        private void Start()
        {
            ApplyEffect(RotateCCW, DefaultColor);
            ApplyEffect(MoveForward, DefaultColor);
            ApplyEffect(RotateCW, DefaultColor);
            ApplyEffect(StrafeLeft, DefaultColor);
            ApplyEffect(MoveBackward, DefaultColor);
            ApplyEffect(StrafeRight, DefaultColor);
            foreach (var action in ExtraActions)
            {
                ApplyEffect(action, DefaultColor);
            }

            Visible = GameSettings.KeyPressHUDVisible.Value;
        }

        private void OnEnable()
        {
            GameSettings.KeyPressHUDVisible.OnChange += KeyPressHUDVisible_OnChange;
        }

        private void OnDisable()
        {
            GameSettings.KeyPressHUDVisible.OnChange -= KeyPressHUDVisible_OnChange;
        }

        private void KeyPressHUDVisible_OnChange(bool value)
        {
            Visible = value;
        }

        private void Update()
        {
            if (eases.Count == 0) return;

            var nextEases = new List<Ease>();

            foreach (var ease in eases)
            {
                var progress = ease.CalculateProgress();

                ApplyEffect(ease.Target, Color.Lerp(ease.StartColor, DefaultColor, progress));

                if (progress < 1)
                {
                    nextEases.Add(ease);
                }
            }

            eases = nextEases;
        }

        public bool Visible
        {
            set
            {
                if (value)
                {
                    transform.ShowAllChildren();
                }
                else
                {
                    transform.HideAllChildren();
                };
            }
        }
    }
}