using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Betray Group", menuName = "UtilityAI/Actions/Betray Group")]
public class BetrayGroupAction : Action
{
    public override void Execute(NPCController npc)
    {
        npc.BetrayGroup();
    }
}
