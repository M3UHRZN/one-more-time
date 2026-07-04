namespace OneMoreTime
{
    /// One airborne jump charge. Ground refreshes continuously; wall refreshes on contact entry.
    public sealed class DoubleJumpState
    {
        bool _wasOnWall;

        public bool IsAvailable { get; private set; } = true;

        public void ObserveContacts(bool grounded, bool onWall)
        {
            if (grounded || (onWall && !_wasOnWall))
                IsAvailable = true;

            _wasOnWall = onWall;
        }

        public bool TryConsume()
        {
            if (!IsAvailable) return false;
            IsAvailable = false;
            return true;
        }
    }
}
