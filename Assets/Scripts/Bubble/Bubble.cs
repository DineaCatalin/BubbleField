using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// The number on the bubble is 2ˆ(type+1)
// Bubbles with the same type merge and bumb up the exponent
// until they reach the last type (2048) and explode
public enum BubbleType
{
    EXP_1 = 0, // 2
    EXP_2,     // 4
    EXP_3,     // 8
    EXP_4,     // 16
    EXP_5,     // 32
    EXP_6,     // 64
    EXP_7,     // 128
    EXP_8,     // 256
    EXP_9,     // 512
    EXP_10,    // 1024
    EXP_11     // 2048
}

public class Bubble : MonoBehaviour
{
    [SerializeField] private int type;

    // Max allowed type -> 2048
    private int maxType = 10; 

    // Helpers for grid algorithms 
    public bool visited;
    public bool connected;

    // Position of the bubble in the grid
    public int row;
    public int column;

    // Transform position of the bubble -> to know where to spawn particle effects
    [HideInInspector] public Vector3 position;

    // Cache the SpriteRenderer and the collider
    private CircleCollider2D circleCollider;
    private SpriteRenderer spriteRenderer;


    

    public int Type
    {
        get
        {
            return type;
        }
    }

    private void Awake()
    {
        visited = false;
        connected = true;
        circleCollider = this.gameObject.GetComponent<CircleCollider2D>();
        spriteRenderer = this.gameObject.GetComponent<SpriteRenderer>();
    }

    // If the type is max explode if not change the sprite to the appropriate type
    public void SetType(int type)
    {
        if (type > 10)
            BubbleGrid.SharedInstance.DestoryBubbleCluster(this);

        this.type = type;
        spriteRenderer.sprite = SpriteCache.SharedInstance.GetSpriteForType(type);
    }

    // If the type is max explode if not change the sprite to the appropriate type
    public bool IncreaseType(int extra)
    {
        this.type = type + extra;

        if (this.type >= maxType)
        {
            return false;
        }

        spriteRenderer.sprite = SpriteCache.SharedInstance.GetSpriteForType(type);
        return true;
    }

    // Set the position of the bubble in the game world with the help of the grid
    public void SetPosition(BubbleGrid grid, int row, int column)
    {
        this.row = row;
        this.column = column;

        position = new Vector3((column * grid.TILE_SIZE) - grid.GRID_OFFSET_X, grid.GRID_OFFSET_Y - (row * grid.TILE_SIZE), 0);

        if(column % 2 == 0)
        {
            position.y -= grid.TILE_SIZE * 0.5f;
        }

        transform.position = position;
    }

    // If the bubble was hit by the bullet the bullet will attach to the Grid and start a matching sequence
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Bullet")
        {
            Bubble bulletBubble = collision.gameObject.GetComponent<Bubble>();
            if (bulletBubble == null)
                return;

            BubbleGrid.SharedInstance.AttachBubble(this, bulletBubble);
        }
    }

    public void ToggleCollider(bool enabled)
    {
        circleCollider.enabled = enabled;
    }


}

