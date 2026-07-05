using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreTime
{
    /// GDD §3.1: ölüm anında statik ceset küpü bırakır, oyuncuyu bölüm başına ışınlar.
    /// Feda tuşu kalıcıdır — dikensiz de ceset merdiveni kurmak GDD §3.6 sinerjisinin parçasıdır.
    public class PlayerRespawner : MonoBehaviour
    {
        [SerializeField] Rigidbody body;
        [SerializeField] Transform spawnPoint;
        [SerializeField] GameObject corpsePrefab;
        [SerializeField] float spawnYOffset = 0f;
        [SerializeField] Key selfDestructKey = Key.R;

        readonly CorpseRegistry _registry = new CorpseRegistry();

        /// #7 koşu kalitesi hesabının okuyacağı ceset sayısı.
        public int CorpseCount => _registry.Count;

        void Awake()
        {
            if (!body) body = GetComponent<Rigidbody>();
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current[selfDestructKey].wasPressedThisFrame)
                Kill();
        }

        public void Kill()
        {
            Vector3 deathPosition = body.position + Vector3.up * spawnYOffset;
            GameObject corpse = Instantiate(corpsePrefab, deathPosition, Quaternion.identity);
            _registry.Register(corpse);

            GameAudioEvents.RaisePlayerDied(deathPosition);
            TeleportToSpawn();
        }

        /// Kayıpta (NOT THIS TIME, jetonsuz) bölüm başına dönüş — ceset SPAWN ETMEZ.
        public void ResetToSpawn() => TeleportToSpawn();

        void TeleportToSpawn()
        {
            body.position = spawnPoint.position;
            body.linearVelocity = Vector3.zero;
        }

        /// #11: kayıpta (NOT THIS TIME, jetonsuz) tüm cesetler silinir.
        public void ClearCorpses() => _registry.ClearAll();
    }
}
