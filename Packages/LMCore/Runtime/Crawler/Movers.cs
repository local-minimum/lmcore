using System.Collections.Generic;
using UnityEngine;

namespace LMCore.Crawler
{
    public delegate void ActivateMoverEvent(IEntityMover mover);
    public delegate void DeactivateMoverEvent(IEntityMover mover);

    public static class Movers
    {
        public static event ActivateMoverEvent OnActivateMover;
        public static event DeactivateMoverEvent OnDeactivateMover;

        public static readonly HashSet<IEntityMover> movers = new ();

        public static void Activate(IEntityMover mover)
        {
            movers.Add(mover);

            OnActivateMover?.Invoke(mover);
        }

        public static void Deactivate(IEntityMover mover)
        {
            if (movers.Contains(mover))
            {
                movers.Remove(mover);

                OnDeactivateMover?.Invoke(mover);

            }
        }
    }
}
