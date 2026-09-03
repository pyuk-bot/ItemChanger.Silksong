using Benchwarp.Data;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Modules;
using ItemChanger.Silksong.RawData;
using QuestPlaymakerActions;
using Silksong.FsmUtil;

namespace ItemChanger.Silksong.Modules;

/// <summary>
/// Module which edits main quest triggers to improve robustness for nonvanilla progression. See remarks for detailed changes.
/// </summary>
/// <remarks>
/// Removes the trigger for Quests.Black_Thread_Pt4_Return from Dock_04[left1] transition.
/// Modifies the first Lace Abyss conversation to trigger Quests.Black_Thread_Pt4_Return instead of Quests.Black_Thread_Pt3_Escape, 
/// effectively removing the Escape step of the quest.
/// Modifies the second Lace Abyss conversation, which replaces the first after obtaining Everbloom,
/// to complete Quests.Black_Thread_Pt4_Return,
/// ensuring the location is still accessible if the player obtains the Everbloom before triggering the first conversation
/// </remarks>
public class MainQuestReflowModule : Module
{
    protected override void DoLoad()
    {
        Using(new FsmEditGroup
        {
            { new(SceneNames.Abyss_05, "Lace Abyss Ghost Cutscene", "Lace Cutscene"), ReplaceEscapeQuestTrigger },
            { new(SceneNames.Abyss_05, "Abyss Dive Cutscene", "Dive Cutscene"), AddCompleteReturnQuestTrigger },
            { new(SceneNames.Dock_04, "left1", "Advance Quest"), RemoveReturnQuestTrigger },
        });
    }

    protected override void DoUnload() { }

    private void ReplaceEscapeQuestTrigger(PlayMakerFSM fsm)
    {
        // replace trigger to begin pt3 by trigger to begin pt4
        fsm.MustGetState("Return Control").GetFirstActionOfType<BeginQuest>()!.Quest = QuestManager.GetQuest(Quests.Black_Thread_Pt4_Return);
    }

    private void AddCompleteReturnQuestTrigger(PlayMakerFSM fsm)
    {
        // complete pt4 if the cutscene that normally triggers pt3 is replaced by the dive cutscene
        EndQuest endQuest = new()
        {
            Quest = QuestManager.GetQuest(Quests.Black_Thread_Pt4_Return),
            ConsumeCurrency = false
        };
        fsm.MustGetState("Return Control").GetFirstActionOfType<RemoveHeroInputBlocker>()!.InsertActionAfter(endQuest);
    }

    private void RemoveReturnQuestTrigger(PlayMakerFSM fsm)
    {
        // remove trigger to begin pt4
        fsm.MustGetState("State 1").RemoveTransitions();
    }
}
