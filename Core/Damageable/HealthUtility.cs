// ===== Enhanced Framework - https://github.com/TetsuoYoshima/EnhancedFramework-Combat ===== //
// 
// Notes:
//
// ========================================================================================== //

using EnhancedEditor;
using System;
using UnityEngine;

namespace EnhancedFramework.Combat {
    // ===== Enum ===== \\

    /// <summary>
    /// Options used to determine which operations are available and how to modify a <see cref="Damageable"/> health.
    /// </summary>
    public enum HealthModificationOptions {
        [Tooltip("Disabled any health modification")]
        None = 0,

        [Separator(SeparatorPosition.Top)]

        [Tooltip("Health can be increase above is current value")]
        Increase = 1 << 0,

        [Tooltip("Health can be decrease below is current value")]
        Decrease = 1 << 1,

        [Tooltip("Health can fall down to 0 and triggers this object death")]
        Kill     = 1 << 2,

        [Separator(SeparatorPosition.Top)]

        [Tooltip("Triggers the associated feedbacks (heal on increase, damage on decrase)")]
        Feedbacks = 1 << 10,

        [Tooltip("All options enabled at once")]
        [Ethereal]
        All = Increase | Decrease | Kill | Feedbacks
    }

    /// <summary>
    /// <see cref="Damageable"/>-related health-modification type.
    /// </summary>
    public enum HealthModificationType {
        None    = 0,

        [Separator(SeparatorPosition.Top)]

        Heal    = 1,
        Damage  = 2,

        [Separator(SeparatorPosition.Top)]

        Kill      = 91,
        Resurrect = 92,
    }

    /// <summary>
    /// <see cref="HealthChangeInfos"/> source type (for exemple, indicates if another component caused the operation).
    /// </summary>
    public enum HealthChangeSource {
        None = 0,

        [Separator(SeparatorPosition.Top)]

        [Tooltip("Use a specific Component as the instigator of the operation")]
        Component = 1,
    }

    /// <summary>
    /// Mode used to determine how to modify a <see cref="Damageable"/> current health according to its max value.
    /// </summary>
    public enum HealthAdjustMode {
        [Tooltip("Do not modify the current health of this object [not recommended]")]
        None = 0,

        [Separator(SeparatorPosition.Top)]

        [Tooltip("Only clamp current health so that it does not exceed its max value")]
        Clamp   = 1,

        [Tooltip("Faithfully preserves the current health percent (if max health is multiplied by 2, multiply the current health by 2)")]
        Percent = 2,

        [Tooltip("Adds the max health difference value to the current health (if increased by 50, adds 50 to the current health)")]
        Flat    = 3,
    }

    // ===== Data Struct ===== \\

    /// <summary>
    /// <see cref="Damageable"/>-related detailed data struct for any health modification operation.
    /// </summary>
    [Serializable]
    public struct HealthModification {
        #region Content
        /// <summary>
        /// Creates a new default empty instance of this data struct.
        /// </summary>
        public static HealthModification None => new HealthModification(0f, 0f, 0f, HealthChangeInfos.None, HealthModificationType.None);

        // -----------------------

        /// <summary>
        /// Detailed informations about the health modification to apply.
        /// </summary>
        public HealthChangeInfos ChangeInfos;

        /// <summary>
        /// This modification associated <see cref="HealthModificationType"/>.
        /// </summary>
        public HealthModificationType Type;

        /// <summary>
        /// Object previous health value, before modification.
        /// </summary>
        public float PreviousValue;

        /// <summary>
        /// Object new health value, after modification.
        /// </summary>
        public float NewValue;

        /// <summary>
        /// Desired health modification.
        /// <para/>
        /// For example, we might want to decrease this object health by 10.
        /// <br/> Although, if this object only has 7 health, its actual health modification will only be of 7 (clamped to 0).
        /// <para/> In this case, this <see cref="DesiredModification"/> value will be of 10.
        /// </summary>
        public float DesiredModification;

        // -----------------------

        /// <summary>
        /// Actual applied health modification.
        /// <para/>
        /// For example, we might want to decrease this object health by 10.
        /// <br/> Although, if this object only has 7 health, its actual health modification will only be of 7 (clamped to 0).
        /// <para/> In this case, this <see cref="AppliedModification"/> value will be of 7.
        /// </summary>
        public readonly float AppliedModification {
            get { return NewValue - PreviousValue; }
        }

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <param name="_previousValue"><inheritdoc cref="PreviousValue" path="/summary"/></param>
        /// <param name="_newValue"><inheritdoc cref="NewValue" path="/summary"/></param>
        /// <param name="_desiredValue">Desired unclamped new health value.</param>
        /// <param name="_change"><inheritdoc cref="ChangeInfos" path="/summary"/></param>
        /// <param name="_type"><inheritdoc cref="Type" path="/summary"/></param>
        /// <inheritdoc cref="HealthModification"/>
        public HealthModification(float _previousValue, float _newValue, float _desiredValue, HealthChangeInfos _change, HealthModificationType _type) {
            const float MaxModificationValue = 999999f;

            DesiredModification = Mathf.Clamp(_desiredValue - _previousValue, -MaxModificationValue, MaxModificationValue);
            PreviousValue = _previousValue;
            NewValue      = _newValue;

            ChangeInfos = _change;
            Type        = _type;
        }
        #endregion
    }

    /// <summary>
    /// <see cref="Damageable"/>-related data struct for modifying the current health of an object.
    /// </summary>
    [Serializable]
    public struct HealthChangeInfos {
        #region Content
        /// <summary>
        /// Creates a new default empty instance of this data struct.
        /// </summary>
        public static HealthChangeInfos None => new HealthChangeInfos(0f);

        // -----------------------

        /// <summary>
        /// This modification operation source type - indicates if caused by another object.
        /// </summary>
        public HealthChangeSource SourceType;

        /// <summary>
        /// Source <see cref="Component"/> instigator of this operation (if inflicting damages, this object that caused the hit).
        /// </summary>
        public Component SourceComponent;

        /// <summary>
        /// Health modification value to apply (5 for increasing by 5, -3 for decreasing by 3).
        /// </summary>
        public float Change;

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <inheritdoc cref="HealthChangeInfos(float, HealthChangeSource, Component)"/>
        public HealthChangeInfos(float _change) : this(_change, HealthChangeSource.None, null) { }

        /// <param name="_source"><inheritdoc cref="SourceComponent" path="/summary"/></param>
        /// <inheritdoc cref="HealthChangeInfos(float, HealthChangeSource, Component)"/>
        public HealthChangeInfos(float _change, Component _source) : this(_change, HealthChangeSource.Component, _source) { }

        /// <param name="_change"><inheritdoc cref="Change" path="/summary"/></param>
        /// <param name="_source"><inheritdoc cref="SourceType" path="/summary"/></param>
        /// <param name="_component"><inheritdoc cref="SourceComponent" path="/summary"/></param>
        /// <inheritdoc cref="HealthChangeInfos"/>
        private HealthChangeInfos(float _change, HealthChangeSource _source, Component _component) {
            SourceType = _source;
            Change     = _change;

            SourceComponent = _component;
        }
        #endregion
    }
}
