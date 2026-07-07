using UnityEngine;

namespace KimLIb.ObjectPool.Runtime
{
    [CreateAssetMenu(fileName = "Pool item", menuName = "Object Pool/Pool Item", order = 10)]
    public class PoolItemSO : ScriptableObject
    {
        public string poolingName;
        public GameObject prefab;
        public int initCount;

        private void OnValidate()
        {
            if (prefab != null && !prefab.TryGetComponent(out IPoolable ipoolable))
            {
                Debug.LogError($"풀링 프리팹 오브젝트에 붙은 컴포넌트가 iPoolable을 구현해야합니다");
                prefab = null;
            }
        }
    }
}