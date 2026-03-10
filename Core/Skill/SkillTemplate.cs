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
    /// <see cref="ScriptableObject"/> wrapper used to configure any type of skill in the game.
    /// <br/> Can be used with a <see cref="Skiller"/> to perform its behaviour.
    /// </summary>
    [CreateAssetMenu(fileName = "SKT_SkillTemplate", menuName = FrameworkUtility.MenuPath + "Combat/Skill Template", order = FrameworkUtility.MenuOrder)]
    #pragma warning disable
    public sealed class SkillTemplate : EnhancedScriptableObject {
        #region Global Members
        #if UNITY_EDITOR
        [Section("Skill [Template]")]

        [Tooltip("Name of this skill - use this to easily label it [Editor only]")]
        [SerializeField, Enhanced, DisplayName("Name")] private string skillName = "New Skill";

        [Tooltip("Use this field to write any information about this skill [Editor only]")]
        [SerializeField, Enhanced, EnhancedTextArea(true)] private string comment = string.Empty;
        #endif

        [Space(10f)]

        [Tooltip("This skill configurable behaviour and its associated settings")]
        [SerializeField] private PolymorphValue<SkillBehaviour> behaviour = new PolymorphValue<SkillBehaviour>(typeof(NullSkillBehaviour), SerializedTypeConstraint.None);

        // -----------------------

        /// <summary>
        /// This skill configurable behaviour and its associated settings.
        /// </summary>
        public SkillBehaviour Behaviour {
            get { return behaviour; }
        }
        #endregion

        #region Core
        /// <inheritdoc cref="Perform(Skiller, IList{Transform})"/>
        public SkillInstance Perform(Skiller _skiller) {
            return Perform(_skiller, null as IList<Transform>);
        }

        /// <param name="_target">Object to use as target for this skill.</param>
        /// <inheritdoc cref="Perform(Skiller, IList{Transform})"/>
        public SkillInstance Perform(Skiller _skiller, Transform _target) {
            List<Transform> _buffer = BufferUtility.TransformList;
            _buffer.ReplaceBy(_target);

            return Perform(_skiller, _buffer);
        }

        /// <summary>
        /// Performs this skill and create a new instance of it.
        /// </summary>
        /// <param name="_skiller"><see cref="Skiller"/> component, instigator of this operation.</param>
        /// <param name="_targets">Objects to use as targets for this skill.</param>
        /// <returns>New <see cref="SkillInstance"/> of this skill.</returns>
        public SkillInstance Perform(Skiller _skiller, IList<Transform> _targets) {
            SkillInstance _instance = behaviour.Value.GetInstance();
            _instance.Initialize(this, _skiller, _targets);

            return _instance;
        }
        #endregion

        #region Utility
        /// <summary>
        /// Get this skill <see cref="SkillBehaviour"/> as a given type instance.
        /// </summary>
        public bool GetBehaviour<T>(out T _behaviour) where T : SkillBehaviour {
            return behaviour.GetValue(out _behaviour);
        }
        #endregion
    }
}
