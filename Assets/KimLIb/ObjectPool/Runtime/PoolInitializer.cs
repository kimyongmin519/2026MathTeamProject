using UnityEngine;

namespace KimLIb.ObjectPool.Runtime
{
    public class PoolInitializer : MonoBehaviour
    {
        [field:SerializeField] public PoolManagerSO PoolManager { get; private set; }

        private void Awake()
        {
            PoolManager.InitializePool(transform);

            PoolInitializer[] initializers = FindObjectsByType<PoolInitializer>(FindObjectsSortMode.None);
            Debug.Assert(initializers.Length == 1, "풀 이니셜라이저는 오직 씬에 하나만 존재할 수 있습니다");
        }
        
        public T Pop<T>(PoolItemSO type) where T : IPoolable => PoolManager.Pop<T>(type);
        public void Push(IPoolable item) => PoolManager.Push(item);
    }
}