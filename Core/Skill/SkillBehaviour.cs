// ===== Enhanced Framework - https://github.com/TetsuoYoshima/EnhancedFramework-Combat ===== //
// 
// Notes:
//
// ========================================================================================== //

using EnhancedFramework.Core;
using System;

namespace EnhancedFramework.Combat {
    /// <summary>
    /// Base non-generic class to derive any <see cref="SkillTemplate"/>-related behaviour from.
    /// </summary>
    [Serializable]
    public abstract class SkillBehaviour {
        #region Global Members
        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <summary>
        /// Prevents inheriting from this class in other assemblies.
        /// </summary>
        private protected SkillBehaviour() { }
        #endregion

        #region Pool
        /// <summary>
        /// Get an new instance of this skill from the pool.
        /// </summary>
        /// <inheritdoc cref="ObjectPool{T}.GetPoolInstance"/>
        public abstract SkillInstance GetInstance();

        /// <summary>
        /// Releases a given instance of this skill and send it back to the pool.
        /// </summary>
        /// <inheritdoc cref="ObjectPool{T}.ReleasePoolInstance"/>
        public abstract bool ReleaseInstance(SkillInstance _instance);

        /// <summary>
        /// Clears this skill pool content.
        /// </summary>
        /// <inheritdoc cref="ObjectPool{T}.ClearPool"/>
        public abstract void ClearPool(int _capacity = 1);
        #endregion
    }

    /// <summary>
    /// Base generic <see cref="SkillBehaviour"/> class to inherit any skill from.
    /// <br/> Comes with an associated <see cref="SkillInstance"/> pre-configured pool.
    /// </summary>
    /// <typeparam name="T">This behaviour associated <see cref="SkillInstance"/> type.</typeparam>
    [Serializable]
    public abstract class SkillBehaviour<T> : SkillBehaviour where T : SkillInstance {
        #region Pool
        private static readonly SkillInstancePool<T> pool = new SkillInstancePool<T>();

        // -----------------------

        /// <inheritdoc cref="GetInstance"/>
        public T GetSkillInstance() {
            return pool.GetInstance();
        }

        /// <inheritdoc cref="ReleaseInstance"/>
        public bool ReleaseSkillInstance(T _instance) {
            return pool.ReleaseInstance(_instance);
        }

        // -------------------------------------------
        // Override
        // -------------------------------------------

        /// <inheritdoc/>
        public override SkillInstance GetInstance() {
            return GetSkillInstance();
        }

        /// <inheritdoc/>
        public override bool ReleaseInstance(SkillInstance _instance) {
            return ReleaseSkillInstance(_instance as T);
        }

        /// <inheritdoc/>
        public override void ClearPool(int _capacity = 1) {
            pool.ClearPool(_capacity);
        }
        #endregion
    }    
}
