using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Agents join groups because they want to team up or because they see a chance to betray them later
[CreateAssetMenu(fileName = "Group Interest", menuName = "UtilityAI/Considerations/PlayerTypes/Group Interest")]
public class GroupInterestConsideration : Consideration
{
    [SerializeField] public AnimationCurve responseCurve;
    public override float ScoreConsideration(NPCController npc)
    {
        PlayerTypeData playerType = npc.playerTypeHandler.playerTypeData;

        score = responseCurve.Evaluate(Mathf.Max(playerType.teamUp, playerType.betrayGroup));
        return score;
    }
}
