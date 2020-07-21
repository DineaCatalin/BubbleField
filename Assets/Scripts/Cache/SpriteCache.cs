using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class contains an array of sprites
// Each index cooresponds to the type of a bubble
public class SpriteCache : MonoBehaviour {

    private Dictionary<int, Sprite> colorMap;

    private static SpriteCache sharedInstance;

    public static SpriteCache SharedInstance
    {
        get
        {
            if (sharedInstance == null)
                sharedInstance = GameObject.Find("GameManager").GetComponent<SpriteCache>();

            return sharedInstance;
        }
    }

    [SerializeField] Sprite[] sprites; 

    public Sprite GetSpriteForType(int type)
    {
        return sprites[type];
    }
}
