using KimLIb.ObjectPool.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace KimLIb.ObjectPool.Editor
{
    [CustomEditor(typeof(PoolItemSO))]
    public class PoolItemSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorView = default;

        private TextField _nameField;
        private Button _changeButton;
        private ObjectField _prefabField;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            editorView.CloneTree(root);
            
            //InspectorElement.FillDefaultInspector(root, serializedObject, this);
            
            _nameField = root.Q<TextField>("PoolingName");
            _changeButton = root.Q<Button>("ChangeBtn");
            _prefabField = root.Q<ObjectField>("PrefabField");

            _changeButton.clicked += HandleChangeButtonClick;
            _nameField.RegisterCallback<KeyDownEvent>(HandleKeyDownEvent);
            _prefabField.RegisterValueChangedCallback(HandlePrefabChangeEvent);

            return root;
        }

        private void HandlePrefabChangeEvent(ChangeEvent<Object> evt)
        {
            if (evt.newValue == null) return;
            
            GameObject newObject = evt.newValue as GameObject;
            Debug.Assert(newObject != null);
            
            PoolItemSO item = target as PoolItemSO;

            if (!newObject.TryGetComponent(out IPoolable poolable))
            {
                item.prefab = null;
                EditorUtility.SetDirty(item);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Error", "Poolable 컴포넌트를 찾을 수 없습니다", "Ok");
                return;
            }
            
            poolable.PoolItem = item;
            EditorUtility.SetDirty(newObject);
            AssetDatabase.SaveAssetIfDirty(newObject);
        }

        private void HandleChangeButtonClick()
        {
            string newName = _nameField.text;
            if (string.IsNullOrEmpty(newName))
            {
                EditorUtility.DisplayDialog("Error", "name is null", "Ok");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(target);

            string massage = AssetDatabase.RenameAsset(assetPath, newName);
            if (string.IsNullOrEmpty(massage))
            {
                target.name = newName;
            }
            else
            {
                EditorUtility.DisplayDialog("Error", massage, "OK");
            }
        }

        private void HandleKeyDownEvent(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return)
            {
                HandleChangeButtonClick();
            }
        }
    }
}