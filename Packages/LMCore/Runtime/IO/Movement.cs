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
    };

    public static class MovementExtensions {

        public static bool IsTranslation(this Movement movement) =>
            movement == Movement.Forward 
            || movement == Movement.Backward 
            || movement == Movement.StrafeLeft
            || movement == Movement.StrafeRight
            || movement == Movement.Up
            || movement == Movement.Down;
        public static bool IsRotation(this Movement movement) =>
            movement == Movement.YawCCW 
            || movement == Movement.YawCW 
            || movement == Movement.PitchUp 
            || movement == Movement.PitchDown
            || movement == Movement.RollCW
            || movement == Movement.RollCCW;
    }
}