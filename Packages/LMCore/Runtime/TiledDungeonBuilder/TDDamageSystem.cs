using LMCore.AbstractClasses;
using LMCore.Crawler;
using LMCore.TiledDungeon.DungeonFeatures;
using UnityEngine;

namespace LMCore.TiledDungeon
{
    public class TDDamageSystem : Singleton<TDDamageSystem, TDDamageSystem> 
    {
        private void OnEnable()
        {
            Projectile.OnCollide += Projectile_OnCollide;
            TDSpikeTrap.OnHit += TDSpikeTrap_OnHit;
        }

        private void OnDisable()
        {
            Projectile.OnCollide -= Projectile_OnCollide;
            TDSpikeTrap.OnHit -= TDSpikeTrap_OnHit;
        }

        string PrefixLogMessage(string message) =>
            $"DamageSystem: {message}";

        private void HurtPlayer(GridEntity entity, int damage)
        {
            if (entity == null) return;

            var player = entity.GetComponentInChildren<TDPlayerEntity>();
            if (player != null)
            {
                foreach (var character in player.Party)
                {
                    character.Hurt(damage); 
                }
            }
        }

        private void TDSpikeTrap_OnHit(TDSpikeTrap trap, GridEntity entity)
        {
            HurtPlayer(entity, trap.Damage);
        }

        private void Projectile_OnCollide(Projectile projectile, GameObject victim)
        {
            HurtPlayer(victim.GetComponentInParent<GridEntity>(), projectile.Damage); 
        }
    }
}
