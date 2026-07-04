using System.Collections.Generic;
using UnityEngine;

namespace OneMoreTime
{
    /// GDD §3.1: bölüm boyunca kalıcı ceset listesi. Yalnızca kayıpta temizlenir (#11).
    /// Saf, test edilebilir; GameObject'lerin kendisini bilir ama MonoBehaviour değildir.
    public class CorpseRegistry
    {
        readonly List<GameObject> _corpses = new List<GameObject>();

        public IReadOnlyList<GameObject> Corpses => _corpses;
        public int Count => _corpses.Count;

        public void Register(GameObject corpse) => _corpses.Add(corpse);

        public void ClearAll()
        {
            for (int i = 0; i < _corpses.Count; i++)
                Object.Destroy(_corpses[i]);

            _corpses.Clear();
        }
    }
}
