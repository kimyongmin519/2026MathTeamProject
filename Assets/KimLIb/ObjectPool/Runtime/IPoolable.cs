using UnityEngine;

namespace KimLIb.ObjectPool.Runtime
{
    public interface IPoolable
    {
        public PoolItemSO PoolItem { get; set; }
        public GameObject GameObject { get; }
        public void ResetItem();
    }
}