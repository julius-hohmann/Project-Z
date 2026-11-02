using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Betrayal", menuName = "UtilityAI/Considerations/PlayerTypes/Betrayal")]
public class BetrayalConsideration : Consideration
{
    [SerializeField] public AnimationCurve responseCurve;

    //Time in a group until the full interest is reached, 0 = only the player type matters
    [SerializeField] public float timeUntilFullInterest;

    public override float ScoreConsideration(NPCController npc)
    {
        float interest = npc.playerTypeHandler.playerTypeData.betrayGroup;

        if (timeUntilFullInterest > 0f)
        {
            if (npc.groupHandler.currentGroup == null)
                interest = 0f;
            else
                interest *= Mathf.Clamp01(npc.groupHandler.TimeInGroup / timeUntilFullInterest);
        }

        score = responseCurve.Evaluate(interest);
        return score;
    }
}
