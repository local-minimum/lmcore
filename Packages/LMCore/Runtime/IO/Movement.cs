namespace LMCore.IO
{
    public enum Movement
    { None, Forward, Backward, StrafeLeft, StrafeRight, TurnCCW, TurnCW };

    public static class MovementExtensions {

        public static bool IsTranslation(this Movement movement) => movement != Movement.None && movement != Movement.TurnCCW && movement != Movement.TurnCW;
        public static bool IsRotation(this Movement movement) => movement == Movement.TurnCCW || movement == Movement.TurnCW;
    }
}