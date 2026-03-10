// ===== Enhanced Framework - https://github.com/TetsuoYoshima/EnhancedFramework-Combat ===== //
// 
// Notes:
//
// ========================================================================================== //

using EnhancedEditor;
using EnhancedFramework.Core;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EnhancedFramework.Combat {
    /// <summary>
    /// Singleton class used to manage various global skill-related operations.
    /// </summary>
    [ScriptGizmos(false, true)]
    [AddComponentMenu(FrameworkUtility.MenuPath + "Combat/Skill Manager"), DisallowMultipleComponent]
    public sealed class SkillManager : EnhancedSingleton<SkillManager>, IUpdate, IObjectPoolManager<Skiller> {
        public override UpdateRegistration UpdateRegistration => base.UpdateRegistration | UpdateRegistration.Init | UpdateRegistration.Update;
        
        #region Global Members
        [Section("Skill Manager")]

        [Tooltip("Root transform where to attach skill instances")]
        [SerializeField, Enhanced, Required] private Transform instanceRoot = null;

        [Tooltip("Root transform where to attach disposable skillers from the pool")]
        [SerializeField, Enhanced, Required] private Transform skillerRoot  = null;

        [Space(10f)]

        [Tooltip("Sorts all skill instances in the hierarchy [editor only]")]
        [SerializeField] private bool cleanHierarchy = true;

        #if UNITY_EDITOR
        [Space(10f), HorizontalLine(SuperColor.Grey, 1f), Space(10f)]
        [SerializeField, Enhanced, ShowIf(nameof(separator))] private bool separator = false;
        #endif

        [SerializeField, Enhanced, ReadOnly] private List<SkillInstance> skillInstances = new List<SkillInstance>();
        #endregion

        #region Enhanced Behaviour
        protected override void OnInit() {
            base.OnInit();

            // Pool init.
            disposableSkillerPool.Initialize(this);
        }

        void IUpdate.Update() {
            // Instances update.
            List<SkillInstance> _instances = GetSkillInstances(out int _count);
            if (_count == 0)
                return;

            float _deltaTime = DeltaTime;
            for (int i = _count; i-- > 0;) {
                _instances[i].SkillUpdate(_deltaTime);
            }
        }
        #endregion

        #region Instance
        private readonly List<SkillInstance> pendingInstances = new List<SkillInstance>();
        
        // -------------------------------------------
        // Registration
        // -------------------------------------------

        /// <summary>
        /// Registers a new active <see cref="SkillInstance"/>.
        /// </summary>
        public void RegisterInstance(SkillInstance _instance) {
            skillInstances.Add(_instance);
        }

        /// <summary>
        /// Unregisters a given <see cref="SkillInstance"/>.
        /// </summary>
        public void UnregisterInstance(SkillInstance _instance) {
            pendingInstances.Add(_instance);
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Stops and cancels all currently active <see cref="SkillInstance"/>.
        /// </summary>
        public void StopAllSkills() {
            List<SkillInstance> _instances = GetSkillInstances(out int _count);
            for (int i = _count; i-- > 0;) {
                _instances[i].Cancel();
            }
        }

        /// <summary>
        /// Parents a given <see cref="SkillInstance"/> to its associated parent <see cref="Transform"/>.
        /// </summary>
        public void ParentInstance(SkillInstance _instance) {
            #if UNITY_EDITOR
            if (cleanHierarchy) {

                string _name = _instance.GetType().Name;
                if (!instanceRoot.FindChildResursive(_name, out Transform _parent, true, false)) {

                    _parent = new GameObject($"ROOT_{_name}").transform;
                    _parent.SetParent(instanceRoot);
                    _parent.ResetLocal();
                }

                _instance.transform.SetParent(instanceRoot);
                _instance.transform.SetAsLastSibling();

                return;
            }
            #endif

            _instance.transform.SetParent(instanceRoot);
        }

        /// <summary>
        /// Get all currently active <see cref="SkillInstance"/>.
        /// </summary>
        /// <param name="_count">Total count of active <see cref="SkillInstance"/> in the collection.</param>
        /// <returns>All active <see cref="SkillInstance"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<SkillInstance> GetSkillInstances(out int _count) {
            ref List<SkillInstance> _instances = ref skillInstances;
            _count = _instances.Count;

            if (_count != 0) {
                _count = BufferUtility.RemovePending(_instances, _count, pendingInstances);
            }

            return _instances;
        }
        #endregion

        #region Skiller
        private static readonly ObjectPool<Skiller> disposableSkillerPool = new ObjectPool<Skiller>();

        // -------------------------------------------
        // Disposable Pool
        // -------------------------------------------

        /// <summary>
        /// Get a new disposable <see cref="Skiller"/> from the pool.
        /// </summary>
        public static Skiller GetDisposableSkiller() {
            return disposableSkillerPool.GetPoolInstance();
        }

        /// <summary>
        /// Releases a given disposable <see cref="Skiller"/> and send it back to the pool.
        /// </summary>
        /// <inheritdoc cref="IObjectPool.ReleasePoolInstance"/>
        public static bool ReleaseDisposableSkiller(Skiller _instance) {
            return disposableSkillerPool.ReleasePoolInstance(_instance);
        }

        /// <summary>
        /// Clears the current disposable <see cref="Skiller"/> pool content.
        /// </summary>
        /// <inheritdoc cref="IObjectPool.ClearPool"/>
        public static void ClearDisposableSkillerPool(int _capacity = 1) {
            disposableSkillerPool.ClearPool(_capacity);
        }

        // -------------------------------------------
        // Pool Manager
        // -------------------------------------------

        Skiller IObjectPool<Skiller>.GetPoolInstance() {
            return GetDisposableSkiller();
        }

        bool IObjectPool<Skiller>.ReleasePoolInstance(Skiller _instance) {
            return ReleaseDisposableSkiller(_instance);
        }

        void IObjectPool.ClearPool(int _capacity) {
            ClearDisposableSkillerPool(_capacity);
        }

        Skiller IObjectPoolManager<Skiller>.CreateInstance() {
            #if UNITY_EDITOR
            Skiller _instance = new GameObject($"SKR_Skiller_{(skillerRoot.childCount + 1):#00}").AddComponent<Skiller>();
            #else
            Skiller _instance = new GameObject().AddComponent<Skiller>();
            #endif

            _instance.transform.SetParent(skillerRoot);
            return _instance;
        }

        void IObjectPoolManager<Skiller>.DestroyInstance(Skiller _instance) {
            Destroy(_instance.gameObject);
        }
        #endregion
    }
}
