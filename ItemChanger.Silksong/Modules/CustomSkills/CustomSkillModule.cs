using ItemChanger.Modules;
using Md.HeroController;
using UnityEngine.InputSystem.Controls;

namespace ItemChanger.Silksong.Modules.CustomSkills;

/// <summary>
/// Base class for modules which define custom skills. Handles interop with <see cref="CustomSkillPlayerDataModule"/> to set up PlayerData hooks for the skills.
/// </summary>
public abstract class CustomSkillModule : Module
{
    /// <summary>
    /// Lists the boolNames supported by get operations on <see cref="GetBool(string)"/>.
    /// Can include base PD bools, to override their behavior.
    /// </summary>
    public abstract IEnumerable<string> GettableSkillBools();
    /// <summary>
    /// Lists the boolNames supported by set operations on <see cref="SetBool(string, bool)"/>.
    /// Can Include base PD bools, to monitor (but not override) their behavior.
    /// </summary>
    /// <returns></returns>
    public abstract IEnumerable<string> SettableSkillBools();
    /// <summary>
    /// Gets the skill bool associated with the boolName.
    /// </summary>
    /// <exception cref="ArgumentException">The boolName is not in <see cref="GettableSkillBools"/>.</exception>
    public abstract bool GetBool(string boolName);
    /// <summary>
    /// Sets the skill bool associated with the boolName.
    /// </summary>
    /// <exception cref="ArgumentException">The boolName is not in <see cref="SettableSkillBools"/>.</exception>
    public abstract void SetBool(string boolName, bool value);

    protected override void DoLoad() => ActiveProfile!.Modules.GetOrAdd<CustomSkillPlayerDataModule>().Register(this);
    protected override void DoUnload() { }
    protected ArgumentException UnsupportedBoolName(string boolName) => new($"Bool {boolName} is not supported by module {GetType().Name}.", nameof(boolName));
    
    protected enum LPlusR
    {
        Neutral,
        Left,
        Right,
    }
    /// <summary>
    /// Determines which direction Hornet will act in (left or right) for split skill modules
    /// </summary>
    /// <param name="hc">HeroController instance</param>
    /// <param name="LeftPlusRightBias">
    /// The direction Hornet will act in when the player is holding both left and right at the same time.
    /// If Hornet will always act to the left regardless of her facing direction, then this should be `LPlusR.Left`,
    /// and similar for the right. If Hornet will always act in her facing direction, then this should be `LPlusR.Neutral`.
    /// Test in-game to see which bias is appropriate for a given skill. If the skill has multiple biases depending on context,
    /// override this function to handle them appropriately.
    /// </param>
    /// <returns>
    /// `true` if Hornet will act to the right, `false` if Hornet will act to the left
    /// </returns>
    protected virtual bool HeroWillActToRight(HeroController hc, LPlusR LeftPlusRightBias)
    {
        // Check directional input to prevent turning around on the same frame the action is performed
        bool holdingRight = hc.inputHandler.inputActions.Right.IsPressed;
        bool holdingLeft = hc.inputHandler.inputActions.Left.IsPressed;
        // Also check wall sliding state because Hornet faces into the wall when sliding
        // Wall sliding forces lateral actions to always come out in the opposite direction from hc.cState's facing direction
        // Can't just use hc.wallSlidingL/R because those are both false during a walldash, which also inverts actions
        // Scuttlebracing up walls doesn't set these values, but it also doesn't invert action direction, so it's fine
        if (hc.cState.wallSliding || hc.cState.wallScrambling)
        {
            return !hc.cState.facingRight;
        }
        else if (holdingRight)
        {
            switch (LeftPlusRightBias)
            {
                case LPlusR.Left: return !holdingLeft;
                case LPlusR.Neutral: return !holdingLeft || hc.cState.facingRight;
                case LPlusR.Right: return true;
                default: throw new ArgumentException($"Unsupported value {LeftPlusRightBias} given for LeftPlusRightBias");
            }
        }
        else
        {
            return !holdingLeft && hc.cState.facingRight;
        }
    }
}
