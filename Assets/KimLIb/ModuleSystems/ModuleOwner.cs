using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KimLIb.ModuleSystems
{
    public class ModuleOwner : MonoBehaviour
    {
        protected Dictionary<Type, IModule> _moduleDict;

        protected virtual void Awake()
        {
            _moduleDict = GetComponentsInChildren<IModule>().ToDictionary(module => module.GetType());
            InitializeModules();
            AfterInitializeModules();
        }
        
        protected virtual void InitializeModules()
        {
            foreach (IModule module in _moduleDict.Values)    
            {
                module.Initialize(this);
            }    
        }
        
        protected virtual void AfterInitializeModules()
        {
            foreach (IAfterInitModule afterInitModule in _moduleDict.Values.OfType<IAfterInitModule>())
            {
                afterInitModule.AfterInit();
            }
        }

        public T GetModule<T>()
        {
            if (_moduleDict.TryGetValue(typeof(T), out IModule module))
            {
                return (T)module;
            }
            
            IModule findModule = _moduleDict.Values.FirstOrDefault(moduleType => moduleType is T);
            if(findModule is T castedModule)
            {
                return castedModule;
            }
            return default;
        }
    }
}
