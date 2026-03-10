// ===== Enhanced Framework - https://github.com/TetsuoYoshima/EnhancedFramework-Combat ===== //
// 
// Notes:
//
// ========================================================================================== //

using EnhancedEditor;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedFramework.Combat {
    /// <summary>
    /// <see cref="NullSkillBehaviour"/>-related <see cref="SkillInstance"/>.
    /// </summary>
    [ScriptGizmos(false, true)]
    [AddComponentMenu(MenuPath + "<Null>" + Suffix), DisallowMultipleComponent]
    public sealed class NullSkillInstance : SkillInstance {
        #region Core Behaviour
        protected internal override void SkillUpdate(float _deltaTime) { }

        // -------------------------------------------
        // Callback(s)
        // -------------------------------------------

        protected override void OnInitialized(SkillTemplate _template, Skiller _skiller, IList<Transform> _targets) {
            this.LogMessage("Null skill initialized - Completing");
            Complete();
        }

        protected override void OnStopped(bool _completed) {
            this.LogMessage("Null skill stopped - Completed: " + _completed);
        }
        #endregion
    }
}
