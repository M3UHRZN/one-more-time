namespace OneMoreTime
{
    /// GDD §3.2: bölüm kronometresi. Ölüm/respawn buna dokunmaz — yalnızca Begin/Stop
    /// çağıranlar (RunController) kontrol eder. Saf, test edilebilir.
    public class RunTimer
    {
        public bool IsRunning { get; private set; }
        public float Elapsed { get; private set; }

        public void Begin()
        {
            Elapsed = 0f;
            IsRunning = true;
        }

        public void Tick(float dt)
        {
            if (IsRunning) Elapsed += dt;
        }

        public void Stop() => IsRunning = false;
    }
}
