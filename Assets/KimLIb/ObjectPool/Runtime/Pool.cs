using System.Collections.Generic;
using UnityEngine;

namespace KimLIb.ObjectPool.Runtime
{
    public class Pool
    {
        private readonly Stack<IPoolable> _pool;
        private readonly Transform _parent;
        private readonly GameObject _prefab;
        private readonly PoolItemSO _poolItem;

        public Pool(
            PoolItemSO poolItem,
            Transform parent,
            int initCount)
        {
            _poolItem = poolItem;
            _parent = parent;
            _prefab = poolItem.prefab;
            _pool = new Stack<IPoolable>(initCount);

            for (int i = 0; i < initCount; i++)
            {
                IPoolable poolable = CreateItem();
                poolable.GameObject.SetActive(false);
                _pool.Push(poolable);
            }
        }

        private IPoolable CreateItem()
        {
            GameObject instance =
                Object.Instantiate(_prefab, _parent);

            IPoolable poolable =
                instance.GetComponent<IPoolable>();

            Debug.Assert(
                poolable != null,
                $"{instance.name}에 IPoolable이 없습니다.");

            poolable.PoolItem = _poolItem;
            return poolable;
        }

        public IPoolable Pop()
        {
            IPoolable item;

            if (_pool.Count == 0)
            {
                item = CreateItem();
            }
            else
            {
                item = _pool.Pop();
            }

            item.GameObject.SetActive(true);
            item.ResetItem();

            return item;
        }

        public void Push(IPoolable item)
        {
            item.GameObject.SetActive(false);
            _pool.Push(item);
        }
    }
}