using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCastShooter : MonoBehaviour
{
    // Template used for the dots of the aiming line
    public GameObject dotPerfab;

    [SerializeField]
    private float bubbleSpeed = 6.66f;

    
    private float bubbleProgress;                     // Used to track where to draw the bulletbubble
    private float bubbleIncrement;

    
    private bool shooterActive = true;                // Used to track when the player is able to shoot or not
    private bool isBubbleMoving = false;              

    public int numShotsBeforeNewLine = 10;            // After numShotsBeforeNewLines we will add a new line on the grid
    private int firedBubbles = 0;                     // Keep track of how many bubbles we have fired

    public Bubble bulletBubble;                       // Bubble that will be shot by the shooter

    [SerializeField][Range(0,10)]
    int maxShooterBubbleType = 6;

    [SerializeField]
    private int rayCastLimit = 5;                     // How many times the rayCast will be performed in the maximum case. Without this the game
                                                      // can crash when the user shoots a ball horizontally as it will never stop raycasting because it will just hit sidewalls

    [SerializeField]
    private int maxDots = 26;                         // Max number of drawn aim dots

    [SerializeField]
    private float dotGap = 0.32f;                     // Gap between the dots

    // For PC/Mac 
    private bool mouseDown;

    

    // Position where the RayCast will hit the side walls
    // Used to draw the shooting line
    private List<Vector2> dotPositions;

    // This is our ObjectPool for the dots
    private GameObject[] dotPool;

	void Start()
    {
        // Create a n array of dots and a list of positions for the dots
        dotPositions = new List<Vector2>();
        dotPool = new GameObject[maxDots];

        isBubbleMoving = false;

        SetNextBubble();
        ResetBulletPosition();

        var gradient = 1.0f / maxDots;
        var alpha = 1.0f;

        for(int i = 0; i < maxDots; i++)
        {
            var dot = Instantiate(dotPerfab) as GameObject;     // Instantiate a dot
            dot.transform.parent = this.transform;

            // Set the alpha of each dot to be less then the previous so that our aiming line seems to dissapear
            SpriteRenderer spriteRenderer = dot.GetComponent<SpriteRenderer>();
            Color color = spriteRenderer.color;
            alpha -= gradient;
            color.a = alpha;
            spriteRenderer.color = color;

            dot.SetActive(false);                               // Set it as inactive for later use
            dotPool[i] = dot;                                   // And add it to the Pool
        }

        EventManager.AddListener("enableShooter", new System.Action<EventParam>(EnableShooter));
    }

    void Update()
    {

        if (isBubbleMoving)
        {
            shooterActive = false;

            // Toggle bubbles collider so the raycast won't hit it
            bulletBubble.ToggleCollider(true);

            bubbleProgress += bubbleIncrement;

            if (bubbleProgress > 1)
            {

                dotPositions.RemoveAt(0);

                // BulletBubble has reached it's destination
                if (dotPositions.Count < 2) 
                {
                    isBubbleMoving = false;
                    ResetBulletPosition();
                    SetNextBubble();
              
                    return;
                }
                else
                {
                    shooterActive = false;
                    StartBubblePath();
                }
            }

            // Set bulletbubble position
            float posX = dotPositions[0].x + bubbleProgress * (dotPositions[1].x - dotPositions[0].x);
            float posY = dotPositions[0].y + bubbleProgress * (dotPositions[1].y - dotPositions[0].y);
            bulletBubble.transform.position = new Vector3(posX, posY, 0);
        }
    }


    // Activate the bubbletbubble and give it a new type
    void SetNextBubble()
    {
        int type = Random.Range(0, maxShooterBubbleType);
        bulletBubble.SetType(type);

        bulletBubble.gameObject.SetActive(true);

        // If we have passed numShotsBeforeNewLine add a new line
        firedBubbles++;
        if(firedBubbles > numShotsBeforeNewLine)
        {
            firedBubbles = 0;
            BubbleGrid.SharedInstance.AddNewLine();
        }

    }

    public void OnTouchBegan(Vector2 touch)
    {
        if(shooterActive)
            bulletBubble.ToggleCollider(false);
    }

    public void OnTouchEnded(Vector2 touch)
    {
        if(shooterActive)
        {
            if (dotPositions == null || dotPositions.Count < 2)
                return;

            HideAimRay();

            bubbleProgress = 0.0f;
            isBubbleMoving = true;

            bulletBubble.gameObject.SetActive(true);
            ResetBulletPosition();

            AudioManager.SharedInstance.Play("launch");

            StartBubblePath();
        }
        
    }

    public void OnTouchMove(Vector2 touch)
    {
        if(shooterActive)
        {
            if (dotPositions == null)
                return;

            dotPositions.Clear();

            HideAimRay();

            Vector2 point = Camera.main.ScreenToWorldPoint(touch);

            var direction = new Vector2(point.x - transform.position.x, point.y - transform.position.y);

            RaycastHit2D rayHit = Physics2D.Raycast(transform.position, direction);

            if (rayHit.collider != null)
            {
                dotPositions.Add(transform.position);

                if (rayHit.collider.tag == "SideWall")
                {
                    DoRayCast(rayHit, direction);
                }
                else
                {
                    dotPositions.Add(rayHit.point);
                    DrawPaths();
                }
            }
        }
       
    }

    // Do a raycast from rayHit 
    void DoRayCast(RaycastHit2D rayHit, Vector2 direction)
    {
        if (dotPositions.Count >= rayCastLimit)
            return;

        dotPositions.Add(rayHit.point);

        var normal = Mathf.Atan2(rayHit.normal.y, rayHit.normal.x);
        var newDirection = normal + (normal - Mathf.Atan2(direction.y, direction.x));
        var reflection = new Vector2(-Mathf.Cos(newDirection), -Mathf.Sin(newDirection));
        var newCastPoint = rayHit.point + (2 * reflection);

        // Keep shooting raycasts as long as we hit Sidewalls
        var hit2 = Physics2D.Raycast(newCastPoint, reflection);

        // We hit something again
        if (hit2.collider != null)
        {
            if(hit2.collider.tag == "SideWall")
            {
                DoRayCast(hit2, reflection);
            }
            else
            {
                dotPositions.Add(hit2.point);
                DrawPaths();
            }
        }
        else
        {
            DrawPaths();
        }
    }

    // Hide the current dots and draw the new dots based on the new direction
    // from the user input
    void DrawPaths()
    {

        if(dotPositions.Count > 1) 
        {
            HideAimRay();

            int index = 0;

            for (int i = 1; i < dotPositions.Count; i++)
            {
                DrawTrail(i - 1, i, ref index);
            }
                
        }
    }

    // Draw the dots between 2 points -> ray cast point and hit point
    void DrawTrail(int start, int end, ref int index)
    {
        var pathLength = Vector2.Distance(dotPositions[start], dotPositions[end]);
        int numDots = Mathf.RoundToInt((float)pathLength / dotGap);

        float dotProgress = 1.0f / numDots;

        var p = 0.0f;

        while(p < 1)
        {
            var px = dotPositions[start].x + p * (dotPositions[end].x - dotPositions[start].x);
            var py = dotPositions[start].y + p * (dotPositions[end].y - dotPositions[start].y);

            if (index < maxDots)
            {
                
                var dot = dotPool[index];
                dot.transform.position = new Vector3(px, py, 0);
                dot.SetActive(true);

                index++;
            }

            p += dotProgress;
        }

    }

    void StartBubblePath()
    {
        Vector2 start = dotPositions[0];
        Vector2 end = dotPositions[1];

        float length = Vector2.Distance(start, end);

        float iterations = length / ( 1 / bubbleSpeed );

        bubbleProgress = 0.0f;

        // How much a bubble will be incremented during 1 iteration
        bubbleIncrement = 1.0f / iterations;
    }

    // Sets all the dots from the dot pool inactive
    public void HideAimRay()
    {
        for (int i = 0; i < dotPool.Length; i++)
            dotPool[i].SetActive(false);
    }

    void ResetBulletPosition()
    {
        bulletBubble.transform.position = transform.position;
        shooterActive = true;
    }

    void DisableShooter()
    {
        shooterActive = false;
    }

    void EnableShooter(EventParam eventParam)
    {
        shooterActive = true;
    }

}
