// ===== Enhanced Framework - https://github.com/TetsuoYoshima/EnhancedFramework-Combat ===== //
// 
// Notes:
//
// ========================================================================================== //

using EnhancedEditor;
using EnhancedFramework.Core;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedFramework.Combat {
    /// <summary>
    /// Utility <see cref="Component"/> used to perform any <see cref="SkillTemplate"/> in a scene.
    /// </summary>
    [ScriptGizmos(false, true)]
    [AddComponentMenu(FrameworkUtility.MenuPath + "Combat/Skiller"), DisallowMultipleComponent]
    public sealed class Skiller : EnhancedPoolableObject {
        #region Global Members
        #if UNITY_EDITOR
        [Section("Skiller")]
        [SerializeField, Enhanced, ShowIf(nameof(header))] private bool header = false;
        #endif

        [SerializeField, Enhanced, DisplayName("Skills"), ReadOnly] private List<SkillInstance> skillInstances = new List<SkillInstance>();
        #endregion

        #region Enhanced Behaviour
        protected override void OnBehaviourDisabled() {
            base.OnBehaviourDisabled();

            // Dispose of all active skills.
            DisposeAllSkills();
        }
        #endregion

        #region Instance
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
            if (!skillInstances.Remove(_instance))
                return;

            // Send back to the pool once all operations complete.
            if (IsFromPool && (skillInstances.Count == 0)) {
                Release();
            }
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Stops and cancels all active <see cref="SkillInstance"/> from this <see cref="Skiller"/>.
        /// </summary>
        public void StopAllSkills() {
            ref List<SkillInstance> _instances = ref skillInstances;
            for (int i = _instances.Count; i-- > 0;) {
                _instances[i].Cancel();
            }
        }

        /// <summary>
        /// Disposes of all active <see cref="SkillInstance"/> - skills may either be canceled or continue with a disposable <see cref="Skiller"/>.
        /// </summary>
        public void DisposeAllSkills() {
            ref List<SkillInstance> _instances = ref skillInstances;
            for (int i = _instances.Count; i-- > 0;) {
                _instances[i].DisposeSkiller();
            }
        }

        /// <summary>
        /// <inheritdoc cref="DisposeAllSkills"/>
        /// <para/>
        /// Called when this <see cref="EnhancedPoolableObject"/> is deactivated - make sure it will not be send to the pool.
        /// </summary>
        /// <inheritdoc cref="DisposeAllSkills"/>
        private void DisposeAllSkills_Poolable() {
            ref List<SkillInstance> _instances = ref skillInstances;
            for (int i = _instances.Count; i-- > 0;) {
                SkillInstance _instance = _instances[i];

                // Remove instance first, so it cannot be unregistered and will send no request for releasing this object.
                _instances.RemoveAt(i);
                _instance.DisposeSkiller();
            }
        }
        #endregion

        #region Poolable
        protected override void OnDeactivation() {
            base.OnDeactivation();

            // Dispose of all active skills.
            DisposeAllSkills_Poolable();
        }
        #endregion
    }
}
