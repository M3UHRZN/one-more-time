using System;
using UnityEngine;

namespace OneMoreTime
{
    /// GDD §2/§3.2: bölüm kronometresini yönetir, bitişte koşu kalitesini kilitler.
    /// İnce orkestratör; hesap RunQuality'de, süre RunTimer'da.
    public class RunController : MonoBehaviour
    {
        [SerializeField] PlayerRespawner player;
        [SerializeField] float parTime = 60f;
        [SerializeField] RunQualityConfig runQuality = new RunQualityConfig();

        readonly RunTimer _timer = new RunTimer();

        public float Elapsed => _timer.Elapsed;
        public bool IsRunning => _timer.IsRunning;
        public float ParTime => parTime;
        public RunResult LastResult { get; private set; }
        public bool HasFinished { get; private set; }

        public event Action<RunResult> RunFinished;

        void Start() => BeginRun();

        void Update() => _timer.Tick(Time.deltaTime);

        /// Yeni koşu başlatır (bölüm başı / sonraki bölüm). #8/#11/#12 çağırır.
        public void BeginRun()
        {
            _timer.Begin();
            HasFinished = false;
        }

        public void Finish()
        {
            if (HasFinished) return;

            _timer.Stop();
            float seconds = _timer.Elapsed;
            int corpses = player.CorpseCount;
            float rightChance = RunQuality.RightOnTimeChance(seconds, corpses, parTime, runQuality);
            LastResult = new RunResult(seconds, corpses, rightChance);
            HasFinished = true;
            RunFinished?.Invoke(LastResult);
        }
    }
}
