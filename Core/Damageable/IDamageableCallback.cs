// ===== Enhanced Framework - https://github.com/TetsuoYoshima/EnhancedFramework-Combat ===== //
// 
// Notes:
//
// ========================================================================================== //

namespace EnhancedFramework.Combat {
    /// <summary>
    /// Interface used to receive callbacks for a <see cref="Damageable"/>
    /// and control whether certain operations can be applied or not.
    /// </summary>
    public interface IDamageableController {
        #region Content
        /// <inheritdoc cref="Damageable.CanApplyDamage"/>
        bool CanApplyDamage(Damageable _damageable, ref HealthChangeInfos _change);

        /// <inheritdoc cref="Damageable.CanApplyHeal"/>
        bool CanApplyHeal(Damageable _damageable, ref HealthChangeInfos _change);

        /// <inheritdoc cref="Damageable.CanDie"/>
        bool CanDie(Damageable _damageable, HealthModification _modification);

        /// <inheritdoc cref="Damageable.CanResurrect"/>
        bool CanResurrect(Damageable _damageable, HealthModification _modification);
        #endregion
    }

    /// <summary>
    /// Interface used to receive callbacks for a <see cref="Damageable"/>
    /// after certain operations have been applied and its state has changed.
    /// </summary>
    public interface IDamageableWatcher {
        #region Content
        /// <summary>
        /// Called whenever this object invulnerability state is changed.
        /// </summary>
        /// <param name="_isInvulnerable">Whether this object is currently invulnerable or not.</param>
        void OnSetInvulnerability(Damageable _damageable, bool _isInvulnerable);

        /// <param name="_maxHealth">New max health value of this object (with applied modifiers).</param>
        /// <inheritdoc cref="Damageable.OnMaxHealthChanged"/>
        void OnSetMaxHealth(Damageable _damageable, float _maxHealth);

        /// <summary>
        /// Called whenever this object armor value is changed.
        /// </summary>
        /// <param name="_armor">New armor value of this object (between 0 and 1).</param>
        void OnSetArmor(Damageable _damageable, float _armor);

        /// <inheritdoc cref="Damageable.OnSetHealth"/>
        void OnSetHealth(Damageable _damageable, HealthModification _modification);

        /// <inheritdoc cref="Damageable.OnDamaged"/>
        void OnDamaged(Damageable _damageable, HealthModification _modification);

        /// <inheritdoc cref="Damageable.OnHealed"/>
        void OnHealed(Damageable _damageable, HealthModification _modification);

        /// <inheritdoc cref="Damageable.OnDied"/>
        void OnDied(Damageable _damageable, HealthModification _modification);

        /// <inheritdoc cref="Damageable.OnResurrected"/>
        void OnResurrected(Damageable _damageable, HealthModification _modification);
        #endregion
    }
}
