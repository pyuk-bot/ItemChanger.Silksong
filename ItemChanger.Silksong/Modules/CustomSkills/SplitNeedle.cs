using GlobalEnums;
using MonoDetour.DetourTypes;

namespace ItemChanger.Silksong.Modules.CustomSkills;

// TODO: track state in inventory

/// <summary>
/// Module to support removing the ability to swing needle in certain directions, and give those directions as Items.
/// </summary>
public class SplitNeedle : CustomSkillModule
{
#pragma warning disable IDE1006 // Naming Styles
    /// <summary>
    /// Property tracking whether the ability to slash left is obtained.
    /// </summary>
    public bool hasLeftslash { get; set; }
    /// <summary>
    /// Property tracking whether the ability to slash right is obtained.
    /// </summary>
    public bool hasRightslash { get; set; }
    /// <summary>
    /// Property tracking whether the ability to slash up is obtained.
    /// </summary>
    public bool hasUpslash { get; set; }
    /// <summary>
    /// Property tracking whether the ability to slash down is obtained. Not split into left and right parts.
    /// </summary>
    public bool hasDownslash { get; set; }
#pragma warning restore IDE1006 // Naming Styles

    public override IEnumerable<string> GettableSkillBools() =>
    [
        nameof(hasLeftslash),
        nameof(hasRightslash),
        nameof(hasUpslash),
        nameof(hasDownslash),
    ];

    public override bool GetBool(string boolName)
    {
        switch (boolName)
        {
            case nameof(hasLeftslash): return hasLeftslash;
            case nameof(hasRightslash): return hasRightslash;
            case nameof(hasUpslash): return hasUpslash;
            case nameof(hasDownslash): return hasDownslash;
            default: throw UnsupportedBoolName(boolName);
        }
    }

    public override IEnumerable<string> SettableSkillBools() =>
    [
        nameof(hasLeftslash),
        nameof(hasRightslash),
        nameof(hasUpslash),
        nameof(hasDownslash),
    ];

    public override void SetBool(string boolName, bool value)
    {
        switch (boolName)
        {
            case nameof(hasLeftslash): 
                hasLeftslash = value;
                break;
            case nameof(hasRightslash):
                hasRightslash = value;
                break;
            case nameof(hasUpslash):
                hasUpslash = value;
                break;
            case nameof(hasDownslash):
                hasDownslash = value;
                break;
            default: throw UnsupportedBoolName(boolName);
        }
    }

    protected override void DoLoad()
    {
        base.DoLoad();
        Using(Md.HeroController.Attack.ControlFlowPrefix(AbortAttack));
    }

    private ReturnFlow AbortAttack(HeroController self, ref AttackDirection dir)
    {
        bool willSlashRight = HeroWillActToRight(self, LPlusR.Neutral);
        switch (dir)
        {
            case AttackDirection.upward when !hasUpslash:
            case AttackDirection.downward when !hasDownslash:
            case AttackDirection.normal when willSlashRight && !hasRightslash || !willSlashRight && !hasLeftslash:
                return ReturnFlow.SkipOriginal;
        }
        return ReturnFlow.None;
    }

    /* 
    private static readonly MethodInfo? AttackMethod = typeof(HeroController).GetMethod(
        "Attack", BindingFlags.NonPublic | BindingFlags.Instance);

    private static ILHook? attackHook;


    private static void ModifyAttack(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitDelegate<Func<HeroController, int, bool>>(ShouldBlockAttack);
        ILLabel continueLabel = cursor.DefineLabel();
        cursor.Emit(OpCodes.Brfalse, continueLabel);
        cursor.Emit(OpCodes.Ret);
        cursor.MarkLabel(continueLabel);
    }
    

    private enum Direction { Left, Right, Up, Down }

    private static bool ShouldBlockAttack(HeroController hero, int attackDir)
    {
        if (Instance == null) return false;

        Direction dir = GetAttackDirection(hero, attackDir);
        return !CanAttackInDirection(dir);
    }

    private static Direction GetAttackDirection(HeroController hero, int attackDir)
    {
        if (attackDir == 1) return Direction.Up;
        if (attackDir == 2) return Direction.Down;
        return hero.cState.facingRight ? Direction.Right : Direction.Left;
    }

    private static bool CanAttackInDirection(Direction dir)
    {
        if (Instance == null) return true;

        return dir switch
        {
            Direction.Left => Instance.HasLeft,
            Direction.Right => Instance.HasRight,
            Direction.Up => Instance.HasUp,
            Direction.Down => Instance.HasDown,
            _ => true
        };
    }
    */
}
