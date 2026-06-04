using UnityEngine;

namespace Gazeus.DesafioMatch3.Data
{
    [CreateAssetMenu(fileName = "TilePrefabRepository", menuName = "Gameplay/TilePrefabRepository")]
    public class TilePrefabRepository : ScriptableObject
    {
        [SerializeField]
        private GameObject[] _tileTypePrefabList;

        public GameObject[] TileTypePrefabList => _tileTypePrefabList;
    }
}
