using System.Collections.Generic;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public delegate void GainXPEvent(TDPlayerCharacter character, int amount);
    public delegate void GainLevelEvent(TDPlayerCharacter character, int level, int startXp, int levelMaxXp);

    public class TDExperienceSystem : MonoBehaviour
    {
        public static event GainXPEvent OnGainXP;
        public static event GainLevelEvent OnGainLevel;

        [SerializeField, Range(0, 1), Tooltip("Each level over enemy decays xp by this factor")]
        float OverLeveledPenelty = 0.9f;

        [SerializeField, Range(0, 1), Tooltip("Each level under enemy increases xp by this factor")]
        float UnderLeveledPenelty = 0.9f;

        [SerializeField]
        TDPlayerEntity party;

        [SerializeField]
        List<int> _levelingThresholds = new List<int>();

        [SerializeField]
        bool AllowOverleveling;

        string PrefixLogMessage(string message) =>
            $"XP System: {message}";

        public int CalculateLevel(int xp, out int startXp, out int endXp)
        {
            var n = _levelingThresholds.Count;
            if (n == 0)
            {
                startXp = 0;
                endXp = 0;
                return 1;
            }

            var idx = _levelingThresholds.FindIndex(threshold => xp < threshold);

            var lastThreshold = _levelingThresholds[n - 1];
            var lastDelta = lastThreshold - _levelingThresholds[Mathf.Max(0, n - 2)];

            // More XP than highest level
            if (idx < 0)
            {
                if (!AllowOverleveling || n < 2)
                {
                    startXp = lastThreshold;
                    endXp = lastThreshold;
                    return n;
                }

                var overshoot = xp - lastDelta;
                var overshootLevels = overshoot / lastDelta;

                startXp = lastThreshold + (overshootLevels * lastDelta);
                endXp = startXp + lastDelta;
                return n + overshootLevels;
            }

            endXp = _levelingThresholds[idx];
            if (idx == 0)
            {
                startXp = 0;
            }
            else
            {
                startXp = _levelingThresholds[idx - 1];
            }

            return idx + 1;
        }

        private void OnEnable()
        {
            TDDamageSystem.OnKillEnemy += TDDamageSystem_OnKillEnemy;
        }

        private void OnDisable()
        {
            TDDamageSystem.OnKillEnemy -= TDDamageSystem_OnKillEnemy;
        }

        public void GainXP(TDPlayerCharacter character, int gain)
        {
            if (gain <= 0)
            {
                Debug.Log(PrefixLogMessage($"XP gain for {character.name} must be positive (was {gain})"));
                return;
            }

            character.XP += gain;
            Debug.Log(PrefixLogMessage($"{character.Name} gains {gain} XP"));
            OnGainXP?.Invoke(character, gain);

            var newLevel = CalculateLevel(character.XP, out var levelStart, out var levelEnd);
            if (newLevel > character.Level)
            {
                character.Level = newLevel;
                Debug.Log(PrefixLogMessage($"{character.Name} reaches level {newLevel}"));
                OnGainLevel?.Invoke(character, newLevel, levelStart, levelEnd);
            }
        }

        private void TDDamageSystem_OnKillEnemy(Enemies.TDEnemy enemy)
        {
            foreach (var character in party.Members)
            {
                if (!character.Alive) continue;

                var levelDelta = character.Level - enemy.Level;
                int gain = 0;

                if (levelDelta == 0)
                {
                    gain = enemy.XP;
                }
                else if (levelDelta > 1)
                {
                    gain = Mathf.FloorToInt(enemy.XP * Mathf.Pow(OverLeveledPenelty, levelDelta));
                }
                else
                {
                    gain = Mathf.CeilToInt(enemy.XP * Mathf.Pow(UnderLeveledPenelty + 1f, levelDelta));
                }

                if (gain > 0)
                {
                    GainXP(character, gain);
                }
            }
        }
    }
}
