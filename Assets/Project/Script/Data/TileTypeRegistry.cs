using UnityEngine;
using UnityEngine.Serialization;

namespace Gazeus.DesafioMatch3.Data
{
    [CreateAssetMenu(fileName = "TileTypeRegistry", menuName = "Gameplay/Tile Type Registry")]
    public class TileTypeRegistry : ScriptableObject
    {
        [FormerlySerializedAs("_tileTypePrefabList")]
        [FormerlySerializedAs("_prefabs")]
        [SerializeField]
        private GameObject[] _prefabColors;

        [SerializeField]
        private GameObject _prefabJoker;

        [SerializeField]
        private GameObject _prefabBomb;

        [SerializeField]
        private GameObject _prefabSkull;

        private GameObject[] _prefabLookup;

        public int ColorCount => _prefabColors?.Length ?? 0;

        public int JokerTypeId => ColorCount;

        public int BombTypeId => ColorCount + 1;

        public int SkullTypeId => ColorCount + 2;

        public int TotalTypeCount => ColorCount + 3;

        public bool IsConfigured =>
            ColorCount > 0 &&
            _prefabJoker != null &&
            _prefabBomb != null &&
            _prefabSkull != null;

        public GameObject[] GetPrefabLookupTable()
        {
            if (!IsConfigured)
            {
                return null;
            }

            if (_prefabLookup == null || _prefabLookup.Length != TotalTypeCount)
            {
                RebuildPrefabLookup();
            }

            return _prefabLookup;
        }

        public GameObject GetPrefab(int type)
        {
            GameObject[] lookup = GetPrefabLookupTable();
            if (lookup == null || type < 0 || type >= lookup.Length)
            {
                return null;
            }

            return lookup[type];
        }

        public bool IsEmpty(int type) => type < 0;

        public bool IsColor(int type) => type >= 0 && type < ColorCount;

        public TileKind GetKind(int type)
        {
            if (IsColor(type))
            {
                return TileKind.Color;
            }

            if (type == JokerTypeId)
            {
                return TileKind.Joker;
            }

            if (type == BombTypeId)
            {
                return TileKind.BombJoker;
            }

            if (type == SkullTypeId)
            {
                return TileKind.Skull;
            }

            return TileKind.Color;
        }

        public bool IsJoker(int type) => type == JokerTypeId;

        public bool IsBomb(int type) => type == BombTypeId;

        public bool IsSkull(int type) => type == SkullTypeId;

        public bool IsSpecial(int type) => IsJoker(type) || IsBomb(type) || IsSkull(type);

        private void OnValidate() => _prefabLookup = null;

        private void RebuildPrefabLookup()
        {
            _prefabLookup = new GameObject[TotalTypeCount];

            for (int i = 0; i < ColorCount; i++)
            {
                _prefabLookup[i] = _prefabColors[i];
            }

            _prefabLookup[JokerTypeId] = _prefabJoker;
            _prefabLookup[BombTypeId] = _prefabBomb;
            _prefabLookup[SkullTypeId] = _prefabSkull;
        }
    }
}
