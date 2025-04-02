using System;

namespace LMCore.DevConsole
{
    [AttributeUsage(AttributeTargets.Method)]
    public class Command : Attribute
    {
        public readonly string[] Context;
        public readonly string Description;

        public Command(string[] context, string description)
        {
            Context = context;
            Description = description;
        }
    }
}
