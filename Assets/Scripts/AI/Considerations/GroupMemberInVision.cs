using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GroupMemberInVision", menuName = "UtilityAI/Considerations/GroupMemberInVision")]
public class GroupMemberInVision : Consideration
{
    [SerializeField] public AnimationCurve responseCurve;
    public override float ScoreConsideration(NPCController npc)
    {
        score = responseCurve.Evaluate(Convert.ToInt32(npc.threatHandler.GetClosestGroupMemberInVision() != null));
        return score;
    }
}
