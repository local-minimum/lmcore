using System;
using System.Collections.Generic;
using System.Linq;

namespace LMCore.IO
{
    [Flags]
    public enum GamePlayAction
    {
        None = 0,
        Select = 1,
        Interact = 2,
        Primary = 4,
        Secondary = 8,
        Tertiary = 16,
        Abort = 32,
        Inventory = 64,
        Map = 128,
        InspectCharacter = 256,
    };

    public static class GamePlayActionExtensions
    {
        private static List<GamePlayAction> Primitives = new List<GamePlayAction>() { 
            GamePlayAction.Select,
            GamePlayAction.Interact,
            GamePlayAction.Primary,
            GamePlayAction.Secondary,
            GamePlayAction.Tertiary,
            GamePlayAction.Abort,
            GamePlayAction.Inventory,
            GamePlayAction.Map,
            GamePlayAction.InspectCharacter,
        };
        public static IEnumerable<GamePlayAction> AsPrimitives(this GamePlayAction action) =>
            Primitives.Where(p => (p & action) == p);
    }
}
