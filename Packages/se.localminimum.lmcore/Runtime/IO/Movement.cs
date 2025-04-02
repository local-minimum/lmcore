namespace LMCore.IO
{
    public enum Movement
    {
        None,
        Forward,
        Backward,
        StrafeLeft,
        StrafeRight,
        YawCCW,
        YawCW,
        Up,
        Down,
        PitchUp,
        PitchDown,
        RollCW,
        RollCCW,
        AbsNorth,
        AbsSouth,
        AbsWest,
        AbsEast,
        AbsUp,
        AbsDown
    };

    public static class MovementExtensions
    {

        public static bool IsTranslation(this Movement movement) =>
            movement == Movement.Forward
            || movement == Movement.Backward
            || movement == Movement.StrafeLeft
            || movement == Movement.StrafeRight
            || movement == Movement.Up
            || movement == Movement.Down
            || movement == Movement.AbsNorth
            || movement == Movement.AbsSouth
            || movement == Movement.AbsWest
            || movement == Movement.AbsEast
            || movement == Movement.AbsUp
            || movement == Movement.AbsDown;

        public static bool IsRotation(this Movement movement) =>
            movement == Movement.YawCCW
            || movement == Movement.YawCW
            || movement == Movement.PitchUp
            || movement == Movement.PitchDown
            || movement == Movement.RollCW
            || movement == Movement.RollCCW;
    }
}