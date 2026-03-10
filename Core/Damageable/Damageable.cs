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

using Min     = EnhancedEditor.MinAttribute;
using Range   = EnhancedEditor.RangeAttribute;
using Delayed = EnhancedEditor.DelayedAttribute;

namespace EnhancedFramework.Combat {
    /// <summary>
    /// Health managing component, controlling any associated operations and allowing this object to take damages and get healed.
    /// </summary>
    [ScriptGizmos(false, true)]
    [AddComponentMenu(FrameworkUtility.MenuPath + "Combat/Damageable"), DisallowMultipleComponent]
    #pragma warning disable
    public sealed class Damageable : EnhancedBehaviour {
        #region Global Members
        public const float MinHealth = 0f;

        [Section("Damageable")]

        [Tooltip("Current health of this object")]
        [Enhanced, ProgressBar(nameof(MaxHealth), 25f, SuperColor.Crimson, true)]
        [SerializeField] private float health = 100f;

        [Tooltip("Health maximum allowed value (without modifiers)")]
        [SerializeField, Enhanced, Min(MinHealth + 1f), Delayed] private int maxHealth = 100;

        [Space(10f)]

        [Tooltip("Health percent of this object assigned on start")]
        [DisplayName("Start Health [%]")]
        [SerializeField, Enhanced, Range(0f, 1f)] private float startHealth = 1f;

        [Tooltip("Reduces inflicted damages up to this percentage value")]
        [DisplayName("Armor [%]")]
        [SerializeField, Enhanced, Range(0f, 1f)] private float armor = 0f;

        [Space(10f), HorizontalLine(SuperColor.Grey, 1f), Space(10f)]

        [Tooltip("Max health coefficient modifier, applied by multiplying by this value")]
        [DisplayName("Modif. [%]")]
        [SerializeField, Enhanced, ReadOnly] private float healthCoefModifier = 1f;

        [Tooltip("Max health flat modifier, applied by adding this value")]
        [DisplayName("Modif. [+]")]
        [SerializeField, Enhanced, ReadOnly] private int healthFlatModifier = 0;

        [Space(10f)]

        [Tooltip("Automatically fully restores this object health when it dies")]
        [SerializeField] private bool isImmortal = false;

        [Tooltip("Prevents this object from taking any damage while on")]
        [SerializeField, Enhanced, ReadOnly(true)] private bool isInvulnerable = false;

        [Tooltip("An object dies when its health falls down below zero")]
        [SerializeField, Enhanced, ReadOnly(true)] private bool isDead = false;

        // -----------------------

        /// <summary>
        /// Current health of this object.
        /// </summary>
        public float Health {
            get { return health; }
        }

        /// <summary>
        /// Maximum possible health of this value.
        /// </summary>
        public float MaxHealth {
            get {
                float _value = (maxHealth + healthFlatModifier) * healthCoefModifier;
                return Mathf.Max(MinHealth + 1f, _value);
            }
        }

        /// <summary>
        /// Current health percent, between 0 and 1.
        /// </summary>
        public float HealthPercent {
            get {
                float _health = health;
                float _max    = MaxHealth;

                if (_health == MinHealth)
                    return 0f;

                if (_health == _max)
                    return 1f;

                return _health / _max;
            }
        }

        /// <summary>
        /// Health range of this object, between <see cref="MinHealth"/> and <see cref="MaxHealth"/>.
        /// </summary>
        public Vector2 HealthRange {
            get { return new Vector2(MinHealth, MaxHealth); }
        }

        /// <summary>
        /// Reduces inflicted damages up to this percentage value.
        /// </summary>
        public float Armor {
            get { return armor; }
        }

        /// <summary>
        /// Prevents this object from taking any damage while true.
        /// </summary>
        public bool IsInvulnerable {
            get { return isInvulnerable; }
        }

        /// <summary>
        /// Automatically fully restores this object health back when it dies.
        /// </summary>
        public bool IsImmortal {
            get { return isImmortal; }
        }

        /// <summary>
        /// An object dies when its health falls down below zero.
        /// </summary>
        public bool IsDead {
            get { return isDead; }
        }
        #endregion

        #region Enhanced Behaviour
        private void Awake() {
            // Init values.
            InitStartHealth();
            AddArmor(armor, SelfArmorId);
        }
        #endregion

        #region Registration
        private List<IDamageableController> controllers = new List<IDamageableController>();
        private List<IDamageableWatcher>    watchers    = new List<IDamageableWatcher>();

        // -------------------------------------------
        // Controller
        // -------------------------------------------

        /// <summary>
        /// Registers a new <see cref="IDamageableController"/> on this object.
        /// </summary>
        /// <param name="_controller"><see cref="IDamageableController"/> to register.</param>
        public void RegisterController(IDamageableController _controller) {
            controllers.Add(_controller);
        }

        /// <summary>
        /// Unregisters a given <see cref="IDamageableController"/> from this object.
        /// </summary>
        /// <param name="_controller"><see cref="IDamageableController"/> to unregister.</param>
        public void UnregisterController(IDamageableController _controller) {
            controllers.Remove(_controller);
        }

        // -------------------------------------------
        // Watcher
        // -------------------------------------------

        /// <summary>
        /// Registers a new <see cref="IDamageableWatcher"/> on this object.
        /// </summary>
        /// <param name="_watcher"><see cref="IDamageableWatcher"/> to register.</param>
        public void RegisterWatcher(IDamageableWatcher _watcher) {
            watchers.Add(_watcher);
        }

        /// <summary>
        /// Unregisters a given <see cref="IDamageableWatcher"/> from this object.
        /// </summary>
        /// <param name="_watcher"><see cref="IDamageableWatcher"/> to unregister.</param>
        public void UnregisterWatcher(IDamageableWatcher _watcher) {
            watchers.Remove(_watcher);
        }
        #endregion

        // ===== Special ===== \\

        #region Invulnerability
        private const int InvulnerabilityId = 0;

        private readonly CallbackCooldown<Cooldown> invulnerabilityCooldown = new CallbackCooldown<Cooldown>();
        private readonly Set<int> invulnerabilityBuffer = new Set<int>();

        private Action onRemoveInvulnerability = null;

        // -----------------------

        /// <summary>
        /// Makes this object invulnerable for a given amount of time (cannot take any damage).
        /// </summary>
        /// <param name="_duration">Time during which to make this object invulnerable, in seconds.</param>
        public void MakeInvulnerable(float _duration) {
            const float MinDuration = .00001f;
            if ((_duration < MinDuration) || (invulnerabilityCooldown.Remain > _duration))
                return;

            ToggleInvulnerability(true, InvulnerabilityId);

            onRemoveInvulnerability ??= OnRemoveInvulnerability;
            invulnerabilityCooldown.Reload(_duration, onRemoveInvulnerability);

            // ----- Local Method ----- \\

            void OnRemoveInvulnerability() {
                ToggleInvulnerability(false, InvulnerabilityId);
            }
        }

        /// <summary>
        /// Toggles this object invulnerability from a specific source id (cannot take any damage).
        /// <br/> Becomes invulnerable if at least one invulnerability state is registered from any id,
        /// and only stops once all are unregistered.
        /// </summary>
        /// <param name="_isInvulnerable">Whether to activate or deactivate invulnerability.</param>
        /// <param name="_id">Source id from this request.</param>
        public void ToggleInvulnerability(bool _isInvulnerable, int _id) {
            // Buffer.
            if (_isInvulnerable) {

                invulnerabilityBuffer.Add(_id);

            } else {

                invulnerabilityBuffer.Remove(_id);
                _isInvulnerable = invulnerabilityBuffer.Count != 0;

                // Cancel cooldown.
                if (_id == InvulnerabilityId) {
                    invulnerabilityCooldown.Cancel();
                }
            }

            // Same value.
            if (isInvulnerable == _isInvulnerable)
                return;

            // Update.
            SetInvulnerability(_isInvulnerable);
        }

        /// <summary>
        /// Removes this object invulnerability from all sources.
        /// </summary>
        public void RemoveInvulnerability() {
            if (!isInvulnerable)
                return;

            invulnerabilityBuffer.Clear();
            invulnerabilityCooldown.Cancel();

            SetInvulnerability(false);
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        /// <summary>
        /// Set this object invulnerability state.
        /// </summary>
        private void SetInvulnerability(bool _isInvulnerable) {
            isInvulnerable = _isInvulnerable;

            // Watcher(s).
            ref List<IDamageableWatcher> _watchers = ref watchers;
            for (int i = _watchers.Count; i-- > 0;) {
                _watchers[i].OnSetInvulnerability(this, _isInvulnerable);
            }
        }
        #endregion

        #region Armor
        private const int SelfArmorId = 0;
        private PairCollection<int, float> armorBuffer = new PairCollection<int, float>();

        // -------------------------------------------
        // Buffer
        // -------------------------------------------

        /// <summary>
        /// Adds a given armor value, associated with a given id (use the same id to modify or remove this armor).
        /// </summary>
        /// <param name="_armor">Armor value to add (between 0 and 1).</param>
        /// <param name="_id">Unique identifier associated with this armor value.</param>
        public void AddArmor(float _armor, int _id) {
            armorBuffer.Set(_id, _armor);
            RefreshArmor();
        }

        /// <summary>
        /// Removes a given armor value, associated with a given id (the same id that was used to add it).
        /// </summary>
        /// <param name="_id">Identifier associated with the armor value to remove.</param>
        public void RemoveArmor(int _id) {
            if (armorBuffer.Remove(_id)) {
                RefreshArmor();
            }
        }

        // -------------------------------------------
        // General
        // -------------------------------------------

        /// <summary>
        /// Clears all registered armor modifiers and set its armor value back to 0.
        /// </summary>
        /// <param name="_preserveBase">Only preserves this object pre-configured armor value in the inspector, and removes all subsequent added values.</param>
        private void ResetArmor(bool _preserveBase = true) {
            ref List<Pair<int, float>> _buffer = ref armorBuffer.collection;
            float _armor = 0f;

            if (_preserveBase) {
                // Keep base armor.
                for (int i = _buffer.Count; i-- > 0;) {

                    if (_buffer[i].First == SelfArmorId) {
                        _armor = _buffer[i].Second;
                    } else {
                        _buffer.RemoveAt(i);
                    }
                }
            } else {
                // Clear all.
                _buffer.Clear();
            }

            SetArmor(_armor);
        }

        /// <summary>
        /// Refreshes this object armor value, used to reduce inflicted damages up to this percentage value.
        /// </summary>
        private void RefreshArmor() {
            ref List<Pair<int, float>> _buffer = ref armorBuffer.collection;
            float _armor = 0f;

            for (int i = _buffer.Count; i-- > 0;) {
                _armor += _buffer[i].Second;
            }

            SetArmor(Mathf.Clamp01(_armor));
        }

        /// <summary>
        /// Sets this object armor value, used to reduce inflicted damages up to this percentage value.
        /// </summary>
        /// <param name="_armor">New armor value, between 0 and 1.</param>
        private void SetArmor(float _armor) {
            // Same value.
            if (armor == _armor)
                return;

            armor = _armor;

            // Callback.
            OnArmorChanged(_armor);
        }

        // -------------------------------------------
        // Callback(s)
        // -------------------------------------------

        /// <summary>
        /// Called whenever this object armor value is changed.
        /// </summary>
        private void OnArmorChanged(float _armor) {
            // Watcher(s).
            ref List<IDamageableWatcher> _watchers = ref watchers;
            for (int i = _watchers.Count; i-- > 0;) {
                _watchers[i].OnSetArmor(this, _armor);
            }
        }
        #endregion

        #region Modifiers
        private readonly PairCollection<int, float> coefModifiers = new PairCollection<int, float>();
        private readonly PairCollection<int, int>   flatModifiers = new PairCollection<int, int>();

        // -------------------------------------------
        // Coef
        // -------------------------------------------

        /// <summary>
        /// Applies a given health coefficient modifier, associated with a given id (use the same id to modify or pop and remove this modifier).
        /// <inheritdoc cref="ModifierDoc"/>
        /// </summary>
        /// <param name="_healthCoef">Max health coef to apply (value must be above 0).</param>
        /// <inheritdoc cref="ModifierDoc"/>
        public void PushModifierCoef(float _healthCoef, int _id, HealthAdjustMode _mode, HealthModificationOptions _options) {
            if (_healthCoef <= 0f) {
                this.LogErrorMessage("Cannot applied a negative or null health coefficient - value must be above 0");
                return;
            }

            coefModifiers.Set(_id, _healthCoef);
            RefreshModifiers(_mode, _options);
        }

        /// <summary>
        /// Removes a given health coefficient modifier, associated with a given id (the same id that was used to push it).
        /// <inheritdoc cref="ModifierDoc"/>
        /// </summary>
        /// <inheritdoc cref="ModifierDoc"/>
        public void PopModifierCoef(int _id, HealthAdjustMode _mode, HealthModificationOptions _options) {
            if (coefModifiers.Remove(_id)) {
                RefreshModifiers(_mode, _options);
            }
        }

        // -------------------------------------------
        // Flat
        // -------------------------------------------

        /// <summary>
        /// Applies a given health "flat" modifier, associated with a given id (use the same id to modify or pop and remove this modifier).
        /// <inheritdoc cref="ModifierDoc"/>
        /// </summary>
        /// <param name="_healthValue">Max health value to add.</param>
        /// <inheritdoc cref="ModifierDoc"/>
        public void PushModifierFlat(int _healthValue, int _id, HealthAdjustMode _mode, HealthModificationOptions _options) {
            flatModifiers.Set(_id, _healthValue);
            RefreshModifiers(_mode, _options);
        }

        /// <summary>
        /// Removes a given health "flat" modifier, associated with a given id (the same id that was used to push it).
        /// <inheritdoc cref="ModifierDoc"/>
        /// </summary>
        /// <inheritdoc cref="ModifierDoc"/>
        public void PopModifierFlat(int _id, HealthAdjustMode _mode, HealthModificationOptions _options) {
            if (flatModifiers.Remove(_id)) {
                RefreshModifiers(_mode, _options);
            }
        }

        // -------------------------------------------
        // General
        // -------------------------------------------

        /// <summary>
        /// Clears all registered modifiers on this object, both coef and flat.
        /// </summary>
        /// <inheritdoc cref="ModifierDoc"/>
        public void ClearModifiers(HealthAdjustMode _mode, HealthModificationOptions _options) {
            coefModifiers.Clear();
            flatModifiers.Clear();

            SetModifiers(1f, 0, _mode, _options);
        }

        /// <summary>
        /// Refreshes this object health modifier values.
        /// </summary>
        private void RefreshModifiers(HealthAdjustMode _mode, HealthModificationOptions _options) {

            float _coef = GetCoefModifier(ref coefModifiers.collection, 1f);
            int   _flat = GetFlatModifier(ref flatModifiers.collection, 0);

            SetModifiers(_coef, _flat, _mode, _options);

            // ----- Local Methods ----- \\

            float GetCoefModifier(ref List<Pair<int, float>> _buffer, float _value) {

                for (int i = _buffer.Count; i-- > 0;) {
                    _value *= _buffer[i].Second;
                }

                return _value;
            }

            int GetFlatModifier(ref List<Pair<int, int>> _buffer, int _value) {

                for (int i = _buffer.Count; i-- > 0;) {
                    _value += _buffer[i].Second;
                }

                return _value;
            }
        }

        /// <summary>
        /// Set this object health modifier values.
        /// </summary>
        private void SetModifiers(float _coef, int _flat, HealthAdjustMode _mode, HealthModificationOptions _options) {
            // Same values.
            if ((healthCoefModifier == _coef) && (healthFlatModifier == _flat))
                return;

            // Update.
            float _oldMax = MaxHealth;

            healthCoefModifier = _coef;
            healthFlatModifier = _flat;

            // Callback.
            OnMaxHealthChanged(_oldMax, _mode, _options);
        }

        // -------------------------------------------
        // Doc
        // -------------------------------------------

        /// <summary>
        /// <br/> Then modify the current health value accordingly.
        /// <para/> Modifiers are used to modify this object max health value.
        /// </summary>
        /// <param name="_id">Id associated with this modifier.</param>
        /// <param name="_options">Options used to determine how to modify the current health value accordingly.</param>
        /// <param name="_mode">Mode used to modify the current health value accordingly.</param>
        private void ModifierDoc(int _id, HealthAdjustMode _mode, HealthModificationOptions _options) { }
        #endregion

        #region Max Health
        /// <summary>
        /// Sets this object max health value (without modifiers).
        /// </summary>
        /// <param name="_maxHealth">New max health value, without applied modifiers.</param>
        /// <inheritdoc cref="ModifierDoc"/>
        public void SetMaxHealth(int _maxHealth, HealthAdjustMode _mode, HealthModificationOptions _options) {
            if (maxHealth == _maxHealth)
                return;

            // Update.
            float _oldMax = MaxHealth;
            maxHealth = Mathf.Max((int)MinHealth + 1, _maxHealth);

            // Callback.
            OnMaxHealthChanged(_oldMax, _mode, _options);
        }

        /// <summary>
        /// Sets this object max health and initializes its current health according to its start percent.
        /// <br/> (see: <see cref="startHealth"/>)
        /// </summary>
        /// <param name="_maxHealth">Max health of this object (without modifiers).</param>
        public void InitMaxHealth(int _maxHealth) {
            SetMaxHealth(_maxHealth, HealthAdjustMode.None, HealthModificationOptions.None);
            InitStartHealth();
        }

        // -------------------------------------------
        // Callback(s)
        // -------------------------------------------

        /// <summary>
        /// Called whenever the max health value of this object is changed.
        /// </summary>
        private void OnMaxHealthChanged(float _oldValue, HealthAdjustMode _mode, HealthModificationOptions _options) {
            float _max = MaxHealth;

            switch (_mode) {
                // Preserve current health percent.
                case HealthAdjustMode.Percent:

                    float _percent = health / _oldValue;
                    SetHealthPercent(_percent, _options);

                    break;

                // Adjust with flat difference.
                case HealthAdjustMode.Flat:

                    float _difference = _max - _oldValue;
                    SetHealth(health + _difference, _options);

                    break;

                // Clamp below max value.
                case HealthAdjustMode.Clamp:
                    if (health > _max) {
                        SetHealth(_max, HealthModificationType.None);
                    }

                    break;

                case HealthAdjustMode.None:
                default:
                    break;
            }

            // Watcher(s).
            ref List<IDamageableWatcher> _watchers = ref watchers;
            for (int i = _watchers.Count; i-- > 0;) {
                _watchers[i].OnSetMaxHealth(this, _max);
            }
        }
        #endregion

        // ===== Health ===== \\

        #region Health
        /// <param name="_options">Options used to determine which operations are available and how to modify the current health.</param>
        /// <inheritdoc cref="SetHealth(float, HealthModificationType)"/>
        public HealthModification SetHealth(float _health, HealthModificationOptions _options) {

            ref float _currentHealth = ref health;

            // Increase.
            if (_health > _currentHealth) {

                if (_options.HasFlagUnsafe(HealthModificationOptions.Increase)) {
                    return SetHealth(_health, GetType(HealthModificationType.Heal));
                }
            }
            // Decrease.
            else if (_health < _currentHealth) {

                if (_options.HasFlagUnsafe(HealthModificationOptions.Decrease)) {
                    if (!_options.HasFlagUnsafe(HealthModificationOptions.Kill)) {
                        _health = Mathf.Max(_health, Mathf.Min(_currentHealth, MinHealth + 1f));
                    }

                    return SetHealth(_health, GetType(HealthModificationType.Damage));
                }

                // Make sure the current health is not above max value.
                float _maxHealth = MaxHealth;
                if (_currentHealth > _maxHealth) {
                    return SetHealth(_maxHealth, HealthModificationType.None);
                }
            }

            return HealthModification.None;

            // ----- Local Method ----- \\

            HealthModificationType GetType(HealthModificationType _default) {
                return _options.HasFlagUnsafe(HealthModificationOptions.Feedbacks) ? _default : HealthModificationType.None;
            }
        }

        /// <param name="_health">Health value to assign to this object.</param>
        /// <inheritdoc cref="SetHealth(HealthChangeInfos, HealthModificationType)"/>
        public HealthModification SetHealth(float _health, HealthModificationType _type = HealthModificationType.None) {
            return SetHealth(new HealthChangeInfos(_health - health), _type);
        }

        /// <summary>
        /// Set the current health of this object.
        /// </summary>
        /// <param name="_change">Detailed informations about the health modification to apply.</param>
        /// <param name="_type">This modification type.</param>
        /// <returns>Applied modification data wrapper.</returns>
        public HealthModification SetHealth(HealthChangeInfos _change, HealthModificationType _type = HealthModificationType.None) {

            float _health = health;

            // Force kill.
            if (_type == HealthModificationType.Kill) {
                _change.Change = MinHealth - _health;
            }

            float _desiredValue = _health + _change.Change;
            float _newValue     = Mathf.Clamp(_desiredValue, MinHealth, MaxHealth);

            HealthModification _modification = new HealthModification(_health, _newValue, _desiredValue, _change, _type);
            ApplyHealth(ref _modification);

            return _modification;
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Set the current health percent of this object (according to its max health).
        /// </summary>
        /// <param name="_percent">Health percent to assign, according to this object max health.</param>
        /// <inheritdoc cref="SetHealth(float, HealthModificationOptions)"/>
        private HealthModification SetHealthPercent(float _percent, HealthModificationOptions _options) {
            return SetHealth(_percent * MaxHealth, _options);
        }

        /// <summary>
        /// Set this object health according to its configured <see cref="startHealth"/> percent value and its current max health.
        /// </summary>
        public void InitStartHealth() {
            SetHealth(MaxHealth * startHealth, HealthModificationType.None);
        }

        /// <summary>
        /// Sets this object start health percent value.
        /// </summary>
        /// <param name="_startHealth">Start health percent value to assign.</param>
        /// <param name="_init">Immediatly calls <see cref="InitStartHealth"/> after setting this value.</param>
        public void SetStartHealth(float _startHealth, bool _init = false) {
            startHealth = _startHealth;
            if (_init) {
                InitStartHealth();
            }
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        /// <summary>
        /// Applies a given health modification and set its current value.
        /// <br/> Also manages this object death and resurrection.
        /// </summary>
        /// <returns>True if the modification could be successfully applied, false otherwise.</returns>
        private bool ApplyHealth(ref HealthModification _modification) {
            ref float _health = ref _modification.NewValue;
            ref bool  _isDead = ref isDead;

            // The object is dead - cannot modify health until resurrection.
            if (_isDead) {

                // Do not resurrect.
                if (((_modification.Type != HealthModificationType.Resurrect) && (_health == MinHealth)) || !CanResurrect(_modification)) {
                    _health = MinHealth;
                    return false;
                }
            }

            health = _health;

            // Resurrection.
            if (_isDead) {
                _isDead = false;
                OnResurrected(_modification);
            }
            // Death.
            else if ((_health == MinHealth) && CanDie(_modification)) {
                _isDead = true;
                OnDied(_modification);
            }

            // Callback(s).
            OnSetHealth(_modification);

            switch (_modification.Type) {

                case HealthModificationType.Damage:
                    OnDamaged(_modification);
                    break;

                case HealthModificationType.Heal:
                    OnHealed(_modification);
                    break;

                case HealthModificationType.Resurrect:
                case HealthModificationType.Kill:
                case HealthModificationType.None:
                default:
                    break;
            }

            return true;
        }

        // -------------------------------------------
        // Callback(s)
        // -------------------------------------------

        /// <summary>
        /// Called whenever this object current health is changed.
        /// </summary>
        private void OnSetHealth(HealthModification _modification) {
            // Watcher(s).
            ref List<IDamageableWatcher> _watchers = ref watchers;
            for (int i = _watchers.Count; i-- > 0;) {
                _watchers[i].OnSetHealth(this, _modification);
            }
        }
        #endregion

        #region Damage
        /// <param name="_damage">Amount of damage to inflict.</param>
        /// <inheritdoc cref="Damage(HealthChangeInfos, out HealthModification)"/>
        public bool Damage(float _damage) {
            return Damage(new HealthChangeInfos(-_damage), out _);
        }

        /// <summary>
        /// Inflicts damages to this object.
        /// </summary>
        /// <param name="_change">Detailed informations about the damages to inflict.</param>
        /// <param name="_modification">Applied modification data wrapper.</param>
        /// <returns>True if any damage could be applied, false otherwise.</returns>
        public bool Damage(HealthChangeInfos _change, out HealthModification _modification) {
            ref float _damage = ref _change.Change;
            _damage *= 1f - armor;

            // Check.
            if (!CanApplyDamage(ref _change)) {
                _modification = HealthModification.None;
                return false;
            }

            // Apply.
            _modification = SetHealth(_change, HealthModificationType.Damage);
            return true;
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Can damages be apply on this object?
        /// </summary>
        /// <returns>True if any damage can be apply, false otherwise.</returns>
        /// <inheritdoc cref="Damage"/>
        public bool CanApplyDamage(ref HealthChangeInfos _change) {
            if (isInvulnerable || isDead)
                return false;

            // Controller(s).
            ref List<IDamageableController> _controllers = ref controllers;
            for (int i = _controllers.Count; i-- > 0;) {

                if (!_controllers[i].CanApplyDamage(this, ref _change))
                    return false;
            }

            return true;
        }

        // -------------------------------------------
        // Callback(s)
        // -------------------------------------------

        /// <summary>
        /// Called whenever this object takes damage.
        /// </summary>
        private void OnDamaged(HealthModification _modification) {
            // Watcher(s).
            ref List<IDamageableWatcher> _watchers = ref watchers;
            for (int i = _watchers.Count; i-- > 0;) {
                _watchers[i].OnDamaged(this, _modification);
            }
        }
        #endregion

        #region Heal
        /// <param name="_heal">Amount of heal to apply.</param>
        /// <inheritdoc cref="Heal(HealthChangeInfos, out HealthModification)"/>
        public bool Heal(float _heal) {
            return Heal(new HealthChangeInfos(_heal), out _);
        }

        /// <summary>
        /// Heals this object and restores its health.
        /// </summary>
        /// <param name="_change">Detailed informations about the heal to apply.</param>
        /// <param name="_modification">Applied modification data wrapper.</param>
        /// <returns>True if any heal could be applied, false otherwise.</returns>
        public bool Heal(HealthChangeInfos _change, out HealthModification _modification) {

            // Check.
            if (!CanApplyHeal(ref _change)) {
                _modification = HealthModification.None;
                return false;
            }

            // Apply.
            _modification = SetHealth(_change, HealthModificationType.Heal);
            return true;
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Can heal be apply on this object?
        /// </summary>
        /// <returns>True if any damage can be apply, false otherwise.</returns>
        /// <inheritdoc cref="Heal"/>
        public bool CanApplyHeal(ref HealthChangeInfos _change) {
            if (health == MaxHealth)
                return false;

            // Controller(s).
            ref List<IDamageableController> _controllers = ref controllers;
            for (int i = _controllers.Count; i-- > 0;) {

                if (!_controllers[i].CanApplyHeal(this, ref _change))
                    return false;
            }

            return true;
        }

        // -------------------------------------------
        // Callback(s)
        // -------------------------------------------

        /// <summary>
        /// Called whenever this object gets healed.
        /// </summary>
        private void OnHealed(HealthModification _modification) {
            // Watcher(s).
            ref List<IDamageableWatcher> _watchers = ref watchers;
            for (int i = _watchers.Count; i-- > 0;) {
                _watchers[i].OnHealed(this, _modification);
            }
        }
        #endregion
        
        #region Death
        /// <inheritdoc cref="Kill(HealthChangeInfos, out HealthModification)"/>
        public bool Kill() {
            return Kill(new HealthChangeInfos(0f), out _);
        }

        /// <summary>
        /// Kills this object and make its health falls down to 0.
        /// </summary>
        /// <param name="_change">Detailed informations about the operation to apply.</param>
        /// <param name="_modification">Applied modification data wrapper.</param>
        /// <returns>True if this object could be killed, false otherwise.</returns>
        public bool Kill(HealthChangeInfos _change, out HealthModification _modification) {
            // Already dead.
            if (isDead) {
                _modification = HealthModification.None;
                return true;
            }

            _modification = SetHealth(_change, HealthModificationType.Kill);
            return isDead;
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Can this object die?
        /// </summary>
        /// <returns>True if this object can die, false otherwise.</returns>
        /// <param name="_modification">Applied health modification data wrapper.</param>
        public bool CanDie(HealthModification _modification) {
            if (isDead)
                return false;

            // Controller(s).
            ref List<IDamageableController> _controllers = ref controllers;
            for (int i = _controllers.Count; i-- > 0;) {

                if (!_controllers[i].CanDie(this, _modification))
                    return false;
            }

            return true;
        }

        // -------------------------------------------
        // Callback(s)
        // -------------------------------------------

        /// <summary>
        /// Called whenever this object dies.
        /// </summary>
        private void OnDied(HealthModification _modification) {
            // Automatically fully restores health.
            if (isImmortal) {
                SetHealthPercent(1f, HealthModificationOptions.All);
                return;
            }

            RemoveInvulnerability();

            // Watcher(s).
            ref List<IDamageableWatcher> _watchers = ref watchers;
            for (int i = _watchers.Count; i-- > 0;) {
                _watchers[i].OnDied(this, _modification);
            }
        }
        #endregion

        #region Resurrection
        /// <param name="_heal">Amount of heal to apply for resurrection.</param>
        /// <inheritdoc cref="Resurrect(HealthChangeInfos, out HealthModification)"/>
        public bool Resurrect(float _heal) {
            return Resurrect(new HealthChangeInfos(_heal), out _);
        }

        /// <summary>
        /// Resurrects this object from the dead.
        /// </summary>
        /// <param name="_change">Detailed informations about the operation to apply.</param>
        /// <param name="_modification">Applied modification data wrapper.</param>
        /// <returns>True if this object could be resurrected, false otherwise.</returns>
        public bool Resurrect(HealthChangeInfos _change, out HealthModification _modification) {
            // Already alive.
            if (!isDead) {
                _modification = HealthModification.None;
                return true;
            }

            ref float _heal = ref _change.Change;
            _heal = Mathf.Max(_heal, 0f);

            _modification = SetHealth(_change, HealthModificationType.Resurrect);
            return !isDead;
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Can this object be resurrected from the dead?
        /// </summary>
        /// <returns>True if this object can resurrect, false otherwise.</returns>
        /// <param name="_modification">Applied health modification data wrapper.</param>
        public bool CanResurrect(HealthModification _modification) {
            if (!isDead)
                return false;

            // Controller(s).
            ref List<IDamageableController> _controllers = ref controllers;
            for (int i = _controllers.Count; i-- > 0;) {

                if (!_controllers[i].CanResurrect(this, _modification))
                    return false;
            }

            return true;
        }

        // -------------------------------------------
        // Callback(s)
        // -------------------------------------------

        /// <summary>
        /// Called whenever this object resurrects from the dead.
        /// </summary>
        private void OnResurrected(HealthModification _modification) {
            // Watcher(s).
            ref List<IDamageableWatcher> _watchers = ref watchers;
            for (int i = _watchers.Count; i-- > 0;) {
                _watchers[i].OnResurrected(this, _modification);
            }
        }
        #endregion

        // ===== Miscs ===== \\

        #region Saveable
        // ----------------------------
        // Save is not automatic,
        // but can be performed manually from any component
        // by casting this object as an ISaveable and calling the associated methods.
        // ----------------------------

        protected override void OnSerialize(ObjectSaveData _data) {
            base.OnSerialize(_data);

            // Data error.
            if (!_data.GetFloatData(true, true, out SaveDataType<float> _floatData) || !_data.GetIntData(true, true, out SaveDataType<int> _intData))
                return;

            // Health - current, start, max, isDead.
            _floatData.Serialize(startHealth);
            _floatData.Serialize(health);
            _intData  .Serialize(maxHealth);
            _intData  .Serialize(isDead.ToInt());

            // Modifiers & armor.
            SerializeBuffer(ref coefModifiers.collection, ref _intData, ref _floatData);
            SerializeBuffer(ref flatModifiers.collection, ref _intData, ref _intData  );
            SerializeBuffer(ref armorBuffer  .collection, ref _intData, ref _floatData);

            // Invulnerability.
            SerializeInvulnerability(ref invulnerabilityBuffer.collection, ref _intData, ref _floatData);

            // ----- Local Methods ----- \\

            void SerializeBuffer<T>(ref List<Pair<int, T>> _buffer, ref SaveDataType<int> _intData, ref SaveDataType<T> _tData) {

                int _count = _buffer.Count;
                _intData.Serialize(_count);

                for (int i = 0; i < _count; i++) {
                    Pair<int, T> _value = _buffer[i];

                    _intData.Serialize(_value.First);
                    _tData  .Serialize(_value.Second);
                }
            }

            void SerializeInvulnerability(ref List<int> _invulnerabilityBuffer, ref SaveDataType<int> _intData, ref SaveDataType<float> _floatData) {

                int _count = _invulnerabilityBuffer.Count;
                _intData.Serialize(_count);

                for (int i = 0; i < _count; i++) {
                    int _value = _invulnerabilityBuffer[i];
                    if (_value != InvulnerabilityId) {
                        _intData.Serialize(_value);
                    }
                }

                _floatData.Serialize(invulnerabilityCooldown.Remain);
            }
        }

        protected override void OnDeserialize(ObjectSaveData _data) {
            base.OnDeserialize(_data);

            // No data.
            if (!_data.GetFloatData(true, true, out SaveDataType<float> _floatData) || !_data.GetIntData(true, true, out SaveDataType<int> _intData))
                return;

            // Health - current, start, max, isDead.
            if (!_floatData.Deserialize(out float _startHealth) || !_floatData.Deserialize(out float _health) ||
                !_intData  .Deserialize(out int   _maxHealth  ) || !_intData  .Deserialize(out int _isDead  ))
                return;

            SetStartHealth(_startHealth, false);
            SetMaxHealth(_maxHealth, HealthAdjustMode.None, HealthModificationOptions.None);

            // Modifiers & armor.
            DeserializeBuffer(ref coefModifiers.collection, ref _intData, ref _floatData);
            DeserializeBuffer(ref flatModifiers.collection, ref _intData, ref _intData  );
            DeserializeBuffer(ref armorBuffer  .collection, ref _intData, ref _floatData);

            RefreshModifiers(HealthAdjustMode.None, HealthModificationOptions.None);
            RefreshArmor();

            // Health - once modifiers are applied.
            if (_isDead.ToBool()) {

                Kill();

            } else {

                if (isDead) {
                    Resurrect(_health);
                } else {
                    SetHealth(_health, HealthModificationOptions.None);
                }
            }

            // Invulnerability.
            DeserializeInvulnerability(ref _intData, ref _floatData);

            // ----- Local Methods ----- \\

            void DeserializeBuffer<T>(ref List<Pair<int, T>> _buffer, ref SaveDataType<int> _intData, ref SaveDataType<T> _tData) {

                int _count = _intData.Deserialize();
                for (int i = 0; i < _count; i++) {
                    _buffer.Add(new Pair<int, T>(_intData.Deserialize(), _tData.Deserialize()));
                }
            }

            void DeserializeInvulnerability(ref SaveDataType<int> _intData, ref SaveDataType<float> _floatData) {

                int _count = _intData.Deserialize();
                for (int i = 0; i < _count; i++) {
                    ToggleInvulnerability(true, _intData.Deserialize());
                }

                if (_floatData.Deserialize(out float _invulnerabilityCooldown)) {
                    MakeInvulnerable(_invulnerabilityCooldown);
                }
            }
        }
        #endregion

        #region Logger
        public override Color GetLogMessageColor(LogType _type) {
            return SuperColor.Crimson.Get();
        }
        #endregion
    }
}
