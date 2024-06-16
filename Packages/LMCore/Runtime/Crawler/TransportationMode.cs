using System;

namespace LMCore.Crawler
{
    [Flags]
    public enum TransportationMode
    {
        None = 0, 
        Swimming = 2, 
        Walking = 4, 
        Flying = 8, 
        Teleporting = 16,
    }
}
