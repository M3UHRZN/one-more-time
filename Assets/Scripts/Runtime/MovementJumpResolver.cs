namespace OneMoreTime
{
    public enum MovementJumpSource { None, Ground, Wall }

    public static class MovementJumpResolver
    {
        public static MovementJumpSource Choose(bool wallRunActive,
            bool groundAvailable, bool wallAvailable)
        {
            if (wallRunActive && wallAvailable) return MovementJumpSource.Wall;
            if (groundAvailable) return MovementJumpSource.Ground;
            if (wallAvailable) return MovementJumpSource.Wall;
            return MovementJumpSource.None;
        }
    }
}
