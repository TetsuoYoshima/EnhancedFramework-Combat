// ===== Enhanced Framework - https://github.com/TetsuoYoshima/EnhancedFramework-Combat ===== //
// 
// Notes:
//
// ========================================================================================== //

using EnhancedEditor;
using EnhancedFramework.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EnhancedFramework.Combat {
    // ===== Pool ===== \\

    /// <summary>
    /// Utility class used to manage a specific <see cref="SkillInstance"/> type pool.
    /// </summary>
    internal sealed class SkillInstancePool<T> : IObjectPoolManager<T> where T : SkillInstance {
        #region Pool
        private readonly ObjectPool<T> pool = new ObjectPool<T>();
        private int instanceCount = -1;

        // -----------------------

        public T GetInstance() {
            Initialize();
            return pool.GetPoolInstance();
        }

        public bool ReleaseInstance(T _instance) {
            return pool.ReleasePoolInstance(_instance);
        }

        public void ClearPool(int _capacity = 1) {
            pool.ClearPool(_capacity);
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        private void Initialize() {
            if (instanceCount != -1)
                return;

            pool.Initialize(this);
            instanceCount = 0;
        }

        // -------------------------------------------
        // Manager
        // -------------------------------------------

        T IObjectPool<T>.GetPoolInstance() {
            return GetInstance();
        }

        bool IObjectPool<T>.ReleasePoolInstance(T _instance) {
            return ReleaseInstance(_instance);
        }

        void IObjectPool.ClearPool(int _capacity) {
            ClearPool(_capacity);
        }

        T IObjectPoolManager<T>.CreateInstance() {
            #if UNITY_EDITOR
            T _component = new GameObject($"SKL_{typeof(T).Name}_{++instanceCount:#00}").AddComponent<T>();
            #else
            T _component = new GameObject().AddComponent<T>();
            #endif

            SkillManager.Instance.ParentInstance(_component);
            return _component;
        }

        void IObjectPoolManager<T>.DestroyInstance(T _instance) {
            Object.Destroy(_instance.gameObject);
        }
        #endregion
    }

    // ===== Component ===== \\

    /// <summary>
    /// Base class to derive any <see cref="SkillBehaviour"/>-related instance from.
    /// </summary>
    public abstract class SkillInstance : EnhancedPoolableObject {
        public const string MenuPath = "Combat/Skill Instance/";
        public const string Suffix   = " [Skill Instance]";

        #region State
        /// <summary>
        /// Indicates the current state of this skill.
        /// </summary>
        public enum State {
            Inactive    = 0,

            Active      = 1,
            Paused      = 2,
        }
        #endregion

        #region Global Members
        [Section("Skill [Instance]")]

        [Tooltip("The current state of this skill")]
        [SerializeField, Enhanced, ReadOnly] private State state = State.Inactive;

        [Space(10f), HorizontalLine(SuperColor.Grey, 1f), Space(10f)]

        [Tooltip("The Skill template associated with this instance")]
        [SerializeField, Enhanced, ReadOnly] private SkillTemplate template = null;

        [Tooltip("The skiller instigator of this skill instance")]
        [SerializeField, Enhanced, ReadOnly] private Skiller skiller = null;

        // -----------------------

        /// <summary>
        /// Called when this skill is stopped, with a <see cref="bool"/> parameter indicating if it was successfully completed or not.
        /// </summary>
        public Action<SkillInstance, bool> OnSkillStopped = null;

        // -----------------------

        /// <summary>
        /// The <see cref="SkillTemplate"/> associated with this instance.
        /// </summary>
        public SkillTemplate Template {
            get { return template; }
        }

        /// <summary>
        /// The current state of this skill.
        /// </summary>
        public State Status {
            get { return state; }
        }
        #endregion

        #region Core Behaviour
        /// <summary>
        /// Initializes this instance with a given <see cref="SkillTemplate"/>, <see cref="Skiller"/> and target(s).
        /// </summary>
        internal void Initialize(SkillTemplate _template, Skiller _skiller, IList<Transform> _targets) {
            // Setup.
            template = _template;
            skiller  = _skiller;

            SetState(State.Active);

            // Registration.
            SkillManager.Instance.RegisterInstance(this);
            _skiller.RegisterInstance(this);

            // Callback.
            OnInitialized(_template, _skiller, _targets);
        }

        /// <summary>
        /// Updates this skill - called every frame while active.
        /// </summary>
        internal protected abstract void SkillUpdate(float _deltaTime);

        /// <summary>
        /// Called whenever this skill is stopped, whether it was canceled or completed.
        /// </summary>
        private void Stop(bool _completed) {
            // Unregistration.
            SkillManager.Instance.UnregisterInstance(this);
            skiller.UnregisterInstance(this);

            // Callback(s).
            OnSkillStopped?.Invoke(this, _completed);
            OnSkillStopped = null;

            OnStopped(_completed);

            // Clear.
            template = null;
            skiller  = null;

            SetState(State.Inactive);
            Release();
        }

        // -------------------------------------------
        // Callback(s)
        // -------------------------------------------

        /// <inheritdoc cref="Initialize"/>
        protected abstract void OnInitialized(SkillTemplate _template, Skiller _skiller, IList<Transform> _targets);

        /// <inheritdoc cref="Stop"/>
        protected abstract void OnStopped(bool _completed);
        #endregion

        #region Skiller
        /// <summary>
        /// Disposes this skill from its current associated <see cref="Skiller"/>.
        /// <br/> Either disconnect from it and pick a new <see cref="Skiller"/> from the pool, or cancel this skill.
        /// </summary>
        public void DisposeSkiller() {
            if (OnDisposeSkiller()) {
                DisconnectFromSkiller();
            } else {
                Cancel();
            }
        }

        /// <summary>
        /// Disconnects this instance from its current associated <see cref="Skiller"/>,
        /// <br/> then get a disposable <see cref="Skiller"/> from the pool and attaches itself to it.
        /// </summary>
        public void DisconnectFromSkiller() {
            // Pick a disposable skiller to use from the pool.
            Skiller _skiller = SkillManager.GetDisposableSkiller();
            if (!SwitchSkiller(_skiller)) {
                _skiller.Release();
            }
        }

        /// <summary>
        /// Disconnects this instance from its current associated <see cref="Skiller"/>,
        /// <br/> then attaches and registers itself to another given <see cref="Skiller"/>.
        /// </summary>
        /// <param name="_skiller">New <see cref="Skiller"/> to attach this instance to.</param>
        public bool SwitchSkiller(Skiller _skiller) {
            ref Skiller _currentSkiller = ref skiller;

            if (!OnSwitchSkiller(_currentSkiller, _skiller))
                return false;

            _currentSkiller.UnregisterInstance(this);
            _skiller.RegisterInstance(this);

            _currentSkiller = _skiller;
            return true;
        }

        // -------------------------------------------
        // Callback(s)
        // -------------------------------------------

        /// <summary>
        /// Called whenever this skill is being disposed from its associated <see cref="Skiller"/>.
        /// <br/> Use this to indicate if this instance should continue executing or should be canceled.
        /// </summary>
        /// <returns>True to continue executing (and pick a new disposable <see cref="Skiller"/> from the pool), false to cancel this skill.</returns>
        protected virtual bool OnDisposeSkiller() {
            return false;
        }

        /// <summary>
        /// Called whenever trying to change the <see cref="Skiller"/> associated with this instance.
        /// <br/> Use this to cancel the operation and cancel or complete this skill.
        /// </summary>
        /// <param name="_current">Current <see cref="Skiller"/> associated with this instance.</param>
        /// <param name="_new">New <see cref="Skiller"/> to associate with this instance.</param>
        /// <returns>True to switch <see cref="Skiller"/>, false to cancel the operation (use this if the skill was stopped).</returns>
        protected virtual bool OnSwitchSkiller(Skiller _current, Skiller _new) {
            return true;
        }
        #endregion

        #region State
        /// <summary>
        /// Pauses this skill.
        /// </summary>
        public bool Pause() {
            // Ignore if not playing.
            if (state != State.Active)
                return false;

            SetState(State.Paused);
            OnPaused();

            return true;
        }

        /// <summary>
        /// Resumes this skill.
        /// </summary>
        public bool Resume() {
            // Ignore if not paused.
            if (state != State.Paused)
                return false;

            SetState(State.Active);
            OnResumed();

            return true;
        }

        /// <summary>
        /// Cancels this skill.
        /// </summary>
        public bool Cancel() {
            // Ignore if inactive.
            if (state == State.Inactive)
                return false;

            OnCanceled();
            Stop(false);

            return true;
        }

        /// <summary>
        /// Completes this skill.
        /// </summary>
        public bool Complete() {
            // Ignore if inactive.
            if (state == State.Inactive)
                return false;

            OnCompleted();
            Stop(true);

            return true;
        }

        // -------------------------------------------
        // Callbacks
        // -------------------------------------------

        /// <summary>
        /// Called whenever this skill is paused.
        /// </summary>
        protected virtual void OnPaused() { }

        /// <summary>
        /// Called whenever this skill is resumed.
        /// </summary>
        protected virtual void OnResumed() { }

        /// <summary>
        /// Called whenever this skill is canceled.
        /// </summary>
        protected virtual void OnCanceled() { }

        /// <summary>
        /// Called whenever this skill is completed.
        /// </summary>
        protected virtual void OnCompleted() { }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Sets the <see cref="State"/> of this skill.
        /// </summary>
        /// <param name="_state">New state of this skill.</param>
        private void SetState(State _state) {
            state = _state;
        }
        #endregion

        // ===== Miscs ===== \\

        #region Pool
        public override void OnSentToPool() {
            base.OnSentToPool();

            // Security.
            Cancel();
        }
        #endregion
    }
}
