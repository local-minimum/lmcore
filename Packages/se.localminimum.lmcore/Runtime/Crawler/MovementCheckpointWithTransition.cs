namespace LMCore.Crawler
{
    public class MovementCheckpointWithTransition
    {
        public MovementCheckpoint Checkpoint { get; set; }
        public MovementTransition Transition { get; set; }

        public override string ToString() =>
            $"{Transition} - {Checkpoint}";
    }
}
