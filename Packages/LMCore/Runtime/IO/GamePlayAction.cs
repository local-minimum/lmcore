using System;

namespace LMCore.IO
{
    [Flags]
    public enum GamePlayAction
    {
        None = 0,
        Select = 2^0,
        Interact = 2^1,
        Primary = 2^2,
        Secondary = 2^3,
        Tertiary = 2^4,
    };
}
