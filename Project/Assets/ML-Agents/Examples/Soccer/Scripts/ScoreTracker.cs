using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreTracker : MonoBehaviour
{
    private int blueGoals = 0;
    private int purpleGoals = 0;


    public TMP_Text blueScoreText;
    public TMP_Text purpleScoreText;

    
    public void GoalScored(Team team)
    {
        if (team == Team.Blue)
        {
            blueGoals++;
            blueScoreText.text = blueGoals.ToString();
        }
        else
        {
            purpleGoals++;
            purpleScoreText.text = purpleGoals.ToString();
        }
    }
}
