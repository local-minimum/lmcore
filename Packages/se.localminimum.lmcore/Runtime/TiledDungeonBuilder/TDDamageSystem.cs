using LMCore.AbstractClasses;
using LMCore.Crawler;
using LMCore.TiledDungeon.DungeonFeatures;
using LMCore.TiledDungeon.Enemies;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public delegate void HurtPlayerEvent(TDPlayerCharacter target, int value, bool crit);
    public delegate void HealPlayerEvent(TDPlayerCharacter target, int value, bool crit);
    public delegate void KillPlayerEvent();
    public delegate void HurtEnemyEvent(TDEnemy enemy, int value, bool crit);
    public delegate void KillEnemyEvent(TDEnemy enemy);

    public class TDDamageSystem : Singleton<TDDamageSystem, TDDamageSystem>
    {
        public static event HealPlayerEvent OnHealPlayer;
        public static event HurtPlayerEvent OnHurtPlayer;
        public static event KillPlayerEvent OnKillPlayer;

        public static event HurtEnemyEvent OnHurtEnemy;
        public static event HurtEnemyEvent OnHealEnemy;
        public static event KillEnemyEvent OnKillEnemy;

        private void OnEnable()
        {
            Projectile.OnCollide += Projectile_OnCollide;
            TDSpikeTrap.OnHit += TDSpikeTrap_OnHit;
            TiledDungeon.OnDungeonLoad += TiledDungeon_OnDungeonLoad;
            TiledDungeon.OnDungeonUnload += TiledDungeon_OnDungeonUnload;
        }

        private void OnDisable()
        {
            Projectile.OnCollide -= Projectile_OnCollide;
            TDSpikeTrap.OnHit -= TDSpikeTrap_OnHit;
            TiledDungeon.OnDungeonLoad -= TiledDungeon_OnDungeonLoad;
            TiledDungeon.OnDungeonUnload -= TiledDungeon_OnDungeonUnload;
        }

        private new void OnDestroy()
        {
            TDDamageSystem.dungeon = null;
            base.OnDestroy();
        }

        private void TiledDungeon_OnDungeonUnload(TiledDungeon dungeon)
        {
            TDDamageSystem.dungeon = dungeon;
        }

        private void TiledDungeon_OnDungeonLoad(TiledDungeon dungeon, bool fromSave)
        {
            if (TDDamageSystem.dungeon == dungeon)
            {
                TDDamageSystem.dungeon = null;
            }
        }

        string PrefixLogMessage(string message) =>
            $"DamageSystem: {message}";

        /// <summary>
        /// Heal character by amount, return how much actually was healed.
        /// </summary>
        public int HealCharacter(TDPlayerCharacter character, int amount, bool crit = false)
        {
            Debug.Log(PrefixLogMessage($"Hurting {character.Name} for {amount}"));
            character.Heal(amount, out var healing);
            if (healing > 0)
            {
                OnHealPlayer?.Invoke(character, healing, crit);
            }
            return healing;
        }

        /// <summary>
        /// Hurt character by amount, return actually how much was hurt
        /// </summary>
        public int HurtCharacter(TDPlayerCharacter character, int damage, bool crit = false)
        {
            Debug.Log(PrefixLogMessage($"Hurting {character.Name} for {damage}"));
            character.Hurt(damage, out var hurt);
            if (hurt > 0)
            {
                OnHurtPlayer?.Invoke(character, hurt, crit);
            }
            return hurt;
        }

        private bool HurtPlayer(GridEntity entity, int damage)
        {
            if (entity == null) return false;

            var party = entity.GetComponentInChildren<TDPlayerEntity>();
            if (party != null)
            {
                foreach (var character in party.Members)
                {
                    var hurt = HurtCharacter(character, damage);
                    damage -= hurt;
                    if (damage <= 0)
                    {
                        break;
                    }
                }

                if (!party.Alive)
                {
                    Debug.Log(PrefixLogMessage("Player party is dead"));
                    HandleKillPlayer();
                }

                return true;
            }

            return false;
        }

        public bool HealEnemy(GridEntity entity, int amount)
        {
            if (entity == null) return false;

            var enemy = entity.GetComponentInChildren<TDEnemy>();

            if (enemy != null && enemy.Alive)
            {
                if (enemy.Stats.Heal(amount, out int healed))
                {
                    Debug.Log(PrefixLogMessage($"{enemy.name} got {healed} health"));
                    OnHealEnemy?.Invoke(enemy, healed, false);

                    return true;
                }
            }
            return false;
        }

        public bool HurtEnemy(GridEntity entity, int damage)
        {
            if (entity == null) return false;

            var enemy = entity.GetComponentInChildren<TDEnemy>();

            if (enemy != null)
            {
                if (enemy.Stats.Hurt(damage, out int hurt))
                {
                    Debug.Log(PrefixLogMessage($"{enemy.name} took {hurt} damage"));
                    OnHurtEnemy?.Invoke(enemy, hurt, false);

                    if (!enemy.Stats.IsAlive)
                    {
                        Debug.Log(PrefixLogMessage($"Enemy {enemy.name} is dead"));
                        HandleKillEnemy(enemy);
                    }
                    return true;
                }
            }

            return false;
        }

        private void HurtEntity(GridEntity entity, int damage)
        {
            if (HurtPlayer(entity, damage)) return;
            if (HurtEnemy(entity, damage)) return;
        }

        private void TDSpikeTrap_OnHit(TDSpikeTrap trap, GridEntity entity)
        {
            HurtEntity(entity, trap.Damage);
        }

        private void Projectile_OnCollide(Projectile projectile, GameObject victim)
        {
            HurtEntity(victim.GetComponentInParent<GridEntity>(), projectile.Damage);
        }

        static TiledDungeon dungeon = null;

        public static void HandleKillEnemy(TDEnemy enemy)
        {
            var entity = enemy.Entity;

            TDEnemyPool.instance.RegisterDeath(enemy);

            enemy.Stats.ResetArmorModifiers();
            // NOTE: Do this before removing the enemy from the dungeon!
            // Else it might not register out of danger zone correctly
            // Should not be as important when loading saves

            // This notices that enemy is dead and responds accordingly
            enemy.UpdateActivity();

            // Removes occupations & reservations and disables entity
            dungeon?.RemoveGridEntityFromDungeon(entity);

            OnKillEnemy?.Invoke(enemy);
        }

        public static void HandleKillPlayer()
        {
            OnKillPlayer?.Invoke();
        }
    }
}
