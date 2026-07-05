using System.Collections.Generic;
using UnityEngine;

namespace KimLIb.ObjectPool.Runtime
{
    [CreateAssetMenu(fileName = "Pool Manager", menuName = "Object Pool/Pool Manager", order = 0)]
    public class PoolManagerSO : ScriptableObject
    {
        public List<PoolItemSO> itemList = new();

        private Dictionary<PoolItemSO, Pool> _pools;
        private Transform _rootTrm;

        public void InitializePool(Transform rootTrm)
        {
            _rootTrm = rootTrm;
            _pools = new Dictionary<PoolItemSO, Pool>();

            foreach (PoolItemSO item in itemList)
            {
                IPoolable poolable = item.prefab.GetComponent<IPoolable>();
                Debug.Assert(poolable != null, $"얘 풀에이블 아님{item.name}");

                Pool pool = new Pool(item, _rootTrm, item.initCount);
                _pools.Add(item, pool);
            }
        }

        public T Pop<T>(PoolItemSO type) where T : IPoolable
        {
            Debug.Assert(_rootTrm != null, "부모 없으니까 초기화하기 바람");

            if (_pools.TryGetValue(type, out Pool pool))
            {
                return (T)pool.Pop();
            }

            return default;
        }

        public void Push(IPoolable item)
        {
            if (item == null || item.PoolItem == null)
            {
                Debug.LogError("반환할 풀 아이템 정보가 없습니다.");
                return;
            }

            if (_pools.TryGetValue(item.PoolItem, out Pool pool))
            {
                pool.Push(item);
            }
        }
    }
}