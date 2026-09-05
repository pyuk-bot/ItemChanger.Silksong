using Benchwarp.Data;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using ItemChanger.Modules;
using ItemChanger.Serialization;
using PrepatcherPlugin;
using Silksong.FsmUtil;
using Silksong.UnityHelper.Extensions;
using UnityEngine.SceneManagement;

namespace ItemChanger.Silksong.Modules;

/// <summary>
/// Module which makes several changes to the Slab kidnapping sequence:
/// - Wardenflies always spawn throughout Pharloom, including after already being kidnapped
/// - Getting kidnapped does not remove items
/// </summary>
/// <remarks>
/// In vanilla, the general behavior is as follows:
/// - Wardenflies appear after defeating Bell Beast, collecting Cling Grip, and awakening the Citadel.
/// - Wardenflies permanently die when killed (tracked by a PD field set by EnemyDeathEffects, and separately by scenedata).
///   - A special sequence occurs if Hornet is captured when cursed, in which the curse kills the Wardenfly.
///   - Hornet then wakes at location and a cursed corpse appears. This is not a Hornet death.
/// - Wardenflies otherwise disappear when: act 3 starts, upper slab is visited, or the slab sequence is finished by regaining the cloak.
/// - Special notes on vanilla behavior:
///   - Background Moorwing encounter or alt-location Moorwing fight takes priority over Wardenfly spawn in Greymoor_05.
/// 
/// By default, the module removes all conditions to trigger the Slab sequence, except that Hornet must be uncursed.
/// </remarks>
[SingletonModule]
public class SlabKidnappingModule : Module
{
    /// <summary>
    /// An <see cref="IValueProvider{T}"/> describing whether Slab Wardens should be available throughout Pharloom.
    /// Defaults to constant true.
    /// </summary>
    public IValueProvider<bool> SlabCaptureIsAvailable { get; init; } = new BoxedBool { Value = true };

    /// <summary>
    /// An <see cref="IValueProvider{T}"/> describing whether Slab Wardens are able to capture Hornet while she is
    /// cursed. Defaults to constant false.
    /// </summary>
    public IValueProvider<bool> SlabCaptureWhileCursed { get; init; } = new BoxedBool { Value = false };

    protected override void DoLoad()
    {
        Using(new SceneEditGroup
        {
            { SceneNames.Bone_East_04c, ForceJailerDocks },
            { SceneNames.Shadow_21, ForceJailerBilewater },
            { SceneNames.Bone_East_04c, RemoveWardenflyDeactivators },
            { SceneNames.Shadow_21, RemoveWardenflyDeactivators },
            { SceneNames.Greymoor_05, RemoveWardenflyDeactivators },
        });
        Using(new FsmEditGroup
        {
            { new(SilksongHost.Wildcard, "Slab Fly Large Cage", "Control"), HookWardenflyFsm } ,
            { new(SceneNames.Greymoor_05, "Scene Control", "Scene Control"), ForceJailerGreymoor },
        });
    }

    protected override void DoUnload()
    {
    }

    private void ForceJailerDocks(Scene scene)
    {
        // This scene uses a TestGameObjectActivator to enable the jailer + disable ant enemies
        GameObject sceneControl = scene.FindGameObjectByName("Scene Control")!;
        sceneControl.RemoveComponent<TestGameObjectActivator>(); // hasWalljump && !blackThreadWorld && !boneEastJailerClearedOut && !slab_cloak_battle_completed && !visitedUpperSlab

        sceneControl.FindChild(name: "Slab Jailer Scene")!.SetActive(SlabCaptureIsAvailable.Value);
        sceneControl.FindChild(name: "Bone Hunters Scene")!.SetActive(!SlabCaptureIsAvailable.Value);
    }

    private void ForceJailerBilewater(Scene scene)
    {
        // This scene uses a PlayerDataTestResponse to enable the jailer + disable bilewater enemies
        GameObject sceneControl = scene.FindGameObjectByName("Scene Control")!;
        sceneControl.RemoveComponent<PlayerDataTestResponse>(); // !(slab_cloak_battle_completed || blackThreadWorld || visitedUpperSlab)

        sceneControl.FindChild(name: "Slab Jailer Scene")!.SetActive(SlabCaptureIsAvailable.Value);
        sceneControl.FindChild(name: "Muckmen Control")!.SetActive(!SlabCaptureIsAvailable.Value);
    }

    private void ForceJailerGreymoor(PlayMakerFSM fsm)
    {
        // This scene uses a FSM to control whether to spawn the jailer, regular enemies, or Moorwing.
        // Default behaviour: spawn the jailer according to SlabCaptureIsAvailable, except when Moorwing is present.
        FsmState enemySuiteState = fsm.MustGetState("Enemy Suite");
        enemySuiteState.Actions = [];
        // vanilla: if !hasWalljump || blackThreadWorld || !citadelWoken || greymoor05_killedJailer
        //             || visitedUpperSlab || slab_cloak_battle_completed
        //             then send FARMERS. else spawn JAILER.
        enemySuiteState.AddLambdaMethod(_ => fsm.SendEvent(SlabCaptureIsAvailable.Value ? "JAILER" : "FARMERS"));

        FsmState jailCartState = fsm.MustGetState("Jail Cart?");
        jailCartState.InsertLambdaMethod(0, _ =>
        {
            if (SlabCaptureIsAvailable.Value)
                fsm.SendEvent("CART PRESENT");
        });

        FsmState roostingState = fsm.MustGetState("Roosting");
        if (SlabCaptureIsAvailable.Value)
        {
            // vanilla: a separate set of enemies under the Roosting Enemies parent object are spawned when Moorwing is roosting
            // Prevent them from spawning when the wardenfly is present, as one of of the Roosting Enemies patrols the same area
            roostingState.RemoveLastActionOfType<ActivateGameObject>();
            roostingState.RemoveLastActionOfType<SetParent>();
        }
        roostingState.AddTransition("SPAWN JAILER", "Jailer");
        roostingState.AddLambdaMethod(_ =>
        {
            if (SlabCaptureIsAvailable.Value)
                fsm.SendEvent("SPAWN JAILER");
        });
    }

    private void RemoveWardenflyDeactivators(Scene scene)
    {
        GameObject? jailerObj = scene.FindGameObjectByName("Slab Fly Large Cage");
        if (jailerObj == null)
        {
            LogWarn($"Did not find expected wardenfly in {scene.name}.");
            return;
        }

        // remove components that may automatically deactivate the wardenfly.
        // These run in Start, so the fsm hook is potentially too late to remove them.
        jailerObj.RemoveComponents<DeactivateIfPlayerdataTrue>(); // visitedUpperSlab
        jailerObj.RemoveComponents<DeactivateIfPlayerdataFalse>(); // UnlockedFastTravel
        jailerObj.RemoveComponents<PersistentBoolItem>(); // scene data check for permanent kill
    }

    private void HookWardenflyFsm(PlayMakerFSM fsm)
    {
        // Rewire wardenflies spawn logic
        FsmState initState = fsm.MustGetState("Init");
        initState.RemoveTransition("FINISHED");
        initState.AddTransition("HERE", "Dormant");
        initState.AddTransition("DEAD", "Cursed Dead");
        initState.AddTransition("NOT HERE", "Not Here");

        string isWardenDeadVariableName = fsm.GetStringVariable("Cursed Death Bool").Value;

        initState.AddLambdaMethod(_ =>
        {
            if (SlabCaptureIsAvailable.Value)
                fsm.SendEvent("HERE");
            else if (PlayerData.instance.GetBool(isWardenDeadVariableName) && !SlabCaptureWhileCursed.Value)
                fsm.SendEvent("DEAD");
            else
                fsm.SendEvent("NOT HERE");
        });

        // Ignore cursed state
        FsmState curseCheckState = fsm.MustGetState("Is Cursed?");
        curseCheckState.GetFirstActionOfType<BoolTest>()!.isTrue = null;
        curseCheckState.AddMethod(_ => fsm.SendEvent(SlabCaptureWhileCursed.Value ? "FALSE" : "TRUE"));

        // Suppress the usual slab capture function that takes all items.
        FsmState capturedState = fsm.MustGetState("Start Caged Sequence");
        capturedState.RemoveFirstActionOfType<CallStaticMethod>();
        capturedState.InsertLambdaMethod(6, _ =>
        {
            // This is identical to the default slab sequence (HeroSlabCapture::ApplyCaptured) with
            // item stealing and cloakless crest removed
            HeroController.instance.MaxHealth();
            GameManager.instance.SetDeathRespawnSimple("Caged Respawn Marker", 0, false);
            PlayerDataAccess.respawnScene = SceneNames.Slab_03;
            PlayerDataAccess.mapZone = MapZone.THE_SLAB;
            DeliveryQuestItem.BreakAllNoEffects();
        });
    }
}