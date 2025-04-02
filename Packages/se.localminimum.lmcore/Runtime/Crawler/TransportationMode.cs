using System;

namespace LMCore.Crawler
{
    [Serializable]
    [Flags]
    public enum TransportationMode
    {
        None = 0,
        Swimming = 2,
        Walking = 4,
        Flying = 8,
        Climbing = 16,
        Teleporting = 32,
        Squeezing = 64,
    }

    public static class TransportationModeExtensions
    {
        public static TransportationMode RemoveFlag(this TransportationMode mode, TransportationMode flag) =>
            mode & ~flag;

        public static TransportationMode AddFlag(this TransportationMode mode, TransportationMode flag) =>
            mode | flag;
    }
}
