using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelSystem : MonoBehaviour {

    [SerializeField]
    Text scoreText;

    [SerializeField]
    Text currentLevelUIText;

    [SerializeField]
    Text nextLevelUIText;

    [SerializeField]
    Slider slider;

    // Score that we have in a level
    int relativeScore;

    // Score that we have in total
    int totalScore;         

    // Score that we have to reach to get to the next level
    int nextLevelScore;

    // Current level
    int level;

    // How many points we need to get to the next level
    [SerializeField]
    private int levelScore = 1000;

    [SerializeField]
    private int levelMultiplier;

	// Use this for initialization
	void Start ()
    {
        relativeScore = 0;
        level = 1;
        nextLevelScore = levelScore;

        EventManager.AddListener("addScore", new System.Action<EventParam>(AddScore));

        currentLevelUIText.text = "1";
        nextLevelUIText.text = "2";
    }

    public void AddScore(EventParam param)
    {
        relativeScore += param._int;
        totalScore += param._int;

        scoreText.text = totalScore.ToString();
        slider.value = (float)relativeScore / nextLevelScore;
  

        if (relativeScore >= nextLevelScore)
        {
            // Raise next level
            level++;
            relativeScore = 0;
            slider.value = 0;

            // Raise event for level up
            param._string = "LEVEL UP";

            EventManager.TriggerEvent("showCombo", param);

            nextLevelScore = level * levelScore;

            // Set UI
            currentLevelUIText.text = level.ToString();
            nextLevelUIText.text = (level+1).ToString();
        }
    }
}
