using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Recently Invited", menuName ="UtilityAI/Considerations/RecentlyInvited")]
public class RecentlyInvitedConsideration : Consideration
{
    [SerializeField] public AnimationCurve responseCurve;
    public override float ScoreConsideration(NPCController npc)
    {
        bool recentlyInvited = npc.groupHandler.RecentlyInvited.Contains(npc.threatHandler.GetClosestInVision("Human"));

        if (npc.groupHandler.currentGroup == null && !recentlyInvited)
            return npc.playerTypeHandler.playerTypeData.teamUp;
        score = responseCurve.Evaluate(Convert.ToInt32(recentlyInvited));
        return score;
    }
}
