using Gazeus.DesafioMatch3.Misc;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class PooledTile : MonoBehaviour, IPoolKey<string>
    {
        [SerializeField]
        private string _poolKey;

        public string PoolKey
        {
            get => _poolKey;
            set => _poolKey = value;
        }

        public string TypeId
        {
            get => _poolKey;
            set => _poolKey = value;
        }
    }
}
