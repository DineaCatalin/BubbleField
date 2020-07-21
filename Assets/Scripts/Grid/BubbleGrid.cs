using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BubbleGrid : MonoBehaviour
{
    // Control the structure of the grid
    public int ROWS = 8;
    public int COLUMNS = 6;
    public float TILE_SIZE = 0.68f;
    public float changeTypeRate = 0.5f;
    public int inititalLineCount = 5;

    // Bubble that will be used to Instantiate all the bubbles in the grid
    public Bubble bubbleTemplate;

    [HideInInspector] public float GRID_OFFSET_X = 0;
    [HideInInspector] public float GRID_OFFSET_Y = 0;
    [HideInInspector] public Bubble[,] bubbleGrid;
    [SerializeField] private int typePoolLength = 1000;
    [Range(0, 10)]
    [SerializeField] private int maxGeneratedBubbleType = 4;

    // This is the number of the ROW that cannot be passed
    // When there is a bubble in this row the last n = rowsDeletedOnReset rows will be deleted
    [SerializeField] private int restartLine;
    [SerializeField] private int rowsDeletedOnReset;

    // List that is used to determine the type of the bubbles generated
    private List<int> typePool;
    private int lastType;

    private CameraShake cameraShake;

    // List that hold the bubbles that match the current bubble that is being processed in the sequence
    private List<Bubble> matchList;

    // Deplay between the steps of the processing sequence 
    [SerializeField] float delay = 0.5f;

    // What combo we currently have
    int combo = 0;

    // Track score
    int score;

    // Singleton
    private static BubbleGrid sharedInstace;
    public static BubbleGrid SharedInstance { get { return sharedInstace; } }

    void Awake()
    {
        if (sharedInstace != null && sharedInstace != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            sharedInstace = this;
        }
    }

    // Use this for initialization
    void Start()
    {
        // Generate a random bubble 
        lastType = Random.Range(0, maxGeneratedBubbleType);
        typePool = new List<int>();
        matchList = new List<Bubble>();

        // Cache a camera shake to shake the screen when a cluster is being blown
        cameraShake = Camera.main.GetComponent<CameraShake>();

        // Populate type pool
        GenerateTypePool();

        Shuffle(typePool);

        BuildGrid();
    }

    // Sequence that processes bubble interactions
    private IEnumerator StartBubbleSequence(Bubble startBubble)
    {
        bool moreMatches = true;

        // Does the attached bubble have a match?
        int numMatches = 0;

        // While we have macthes
        while (moreMatches)
        {
            // Detect matches
            DetectBubbleCluster(startBubble);

            // If we have a cluster fuse all of it
            if (matchList.Count > 2)
            {
                startBubble = MergeBubbleCluster(matchList);
                
                combo++;
                numMatches++;

                yield return new WaitForSeconds(delay);
            }

            // If we have only 2 bubbles fuse them
            else if (matchList.Count > 1)
            {
                startBubble = MergeBubbles(startBubble, matchList[1]);

                if (startBubble == null)
                    moreMatches = false;

                combo++;
                numMatches++;

                yield return new WaitForSeconds(delay);
            }

            // There is only 1 bubble left so we can stop
            else
            {
                // If there are no matches until now play the specific sound
                if (numMatches == 0)
                {
                    Debug.Log("dasdsadadasd");
                    AudioManager.SharedInstance.Play("bubbleMerge");
                }
                    

                moreMatches = false;
                yield return new WaitForSeconds(delay);
            }

            // Hide the bubbles that are not connected to the sealing
            RemoveDisconnetedBubbles();
        }

        // Check if we did a combo
        CheckCombo();

        // Check if there are still bubbles in the 1st row
        // If there are not it means the player has a PERFECT sequence
        CheckPerfectSequence();

        yield return new WaitForSeconds(delay);

        // Reset bullet here
        EventManager.TriggerEvent("enableShooter", new EventParam());
    }

    // If we have a combo trigger an event to display it
    void CheckCombo()
    {
        if (combo > 1)
        {
            // Show the combo
            EventParam param = new EventParam();
            param._string = combo.ToString() + "X";
            EventManager.TriggerEvent("showCombo", param);
        }

        // Reset combo 
        combo = 0;
    }

    // If there are no more bubbles on the screen trigger an event to
    // display perfect on the screen
    void CheckPerfectSequence()
    {
        bool perfect = true;

        for (int column = 0; column < COLUMNS; column++)
        {
            // Bubble is still active in first row so no perfect sequence
            if(bubbleGrid[0, column].gameObject.activeSelf)
            {
                perfect = false;
            }
        }

        if(perfect)
        {
            EventParam param = new EventParam();
            param._string = "PERFECT";
            EventManager.TriggerEvent("showCombo", param);
        }
    }

    // Detects all the bubbles with the same type as @param bubble that can merge with bubble
    // The result is added to the matchList
    void DetectBubbleCluster(Bubble bubble)
    {
        matchList.Clear();

        for (int row = 0; row < ROWS; row++)
        {
            for (int column = 0; column < COLUMNS; column++)
            {
                bubbleGrid[row, column].visited = false;
            }
        }

        matchList.AddRange(GetMatchesForBubble(bubble));

        while (true)
        {
            bool allVisited = true;
            for (int i = matchList.Count - 1; i >= 0; i--)
            {
                Bubble match = matchList[i];
                if (!match.visited)
                {
                    AddMatches(GetMatchesForBubble(match));
                    allVisited = false;
                }
            }

            if (allVisited)
                return;
        }
    }

   

    // Adds all matches that are not contained in the matchlist
    void AddMatches(List<Bubble> matches)
    {
        foreach (Bubble match in matches)
        {
            if (!matchList.Contains(match))
                matchList.Add(match);
        }
    }

    public void AddNewLine()
    {
        bool overMaxRow = false;
        bool firstRowEmpty = true;

        // Check if we have bubbles in the upper row
        for (int i = 0; i < COLUMNS; i++)
        {
            if (bubbleGrid[0, i].gameObject.activeSelf)
            {
                firstRowEmpty = false;
                break;
            }

        }

        // If we do shift all the rows down by 1
        if (!firstRowEmpty)
        {
            int row = ROWS - 2;
            while (row >= 0)
            {
                for (int i = 0; i < COLUMNS; i++)
                {
                    if (bubbleGrid[row, i].gameObject.activeSelf)
                    {

                        bubbleGrid[row + 1, i].gameObject.SetActive(true);
                        bubbleGrid[row + 1, i].SetType(bubbleGrid[row, i].Type);
                        typePool.RemoveAt(0);

                    }
                    else
                    {
                        bubbleGrid[row + 1, i].gameObject.SetActive(false);
                    }

                }

                row--;
            }
        }

        // Add bubbles to the 1st row that is now empty after the shift
        for (int i = 0; i < COLUMNS; i++)
        {
            // Activate bubbles in the 1st row
            bubbleGrid[0, i].SetType(typePool[0]);
            typePool.RemoveAt(0);
            bubbleGrid[0, i].gameObject.SetActive(true);

            // Check delimiter line to see if we have to blow up some bubbles
            // This happens when the bubbles have reached the limit line
            if (bubbleGrid[restartLine, i].gameObject.activeSelf)
            {
                Debug.Log("Bubble in last line (" + restartLine + ", " + i + ") is active");
                overMaxRow = true;
            }
                
        }

        // If we have surpassed the max allowed row destory some rows so that the player has space
        if (overMaxRow)
            DestroyExtraRows(rowsDeletedOnReset);
    }

    
    //
    // Methods for manipulating bubbles on the grid
    //

    // Destroy the assimilated Bubble and increase the Type of the asimilator Bubble
    // Ex: 2 -> 4 or 16->32 etc.
    public Bubble MergeBubbles(Bubble assimilated, Bubble assimilator)
    {
        // Handle score
        score += (int)Mathf.Pow(2, assimilated.Type + 1);
        EventParam param = new EventParam();
        param._int = score;
        EventManager.TriggerEvent("addScore", param);
        score = 0;

        DestroyBubble(assimilated);

        // Bubble can't be increased any more so it will pop
        if (!assimilator.IncreaseType(1))
        {
            // Pop the maxed out bubble and it's neighbours
            DestoryBubbleCluster(assimilator);
            return null;
        }

        return assimilator;
    }

    public Bubble MergeBubbleCluster(List<Bubble> cluster)
    {
        Bubble endBubble = cluster[cluster.Count - 1];

        // Handle Score
        score += (int)Mathf.Pow(2, cluster[0].Type + 1) * cluster.Count;
        EventParam param = new EventParam();
        param._int = score;
        EventManager.TriggerEvent("addScore", param);

        // Check all bubbles in the cluster and chose the position of the merger to the one that can
        // merge again afterwards if there is one. If not take the last bubble in the list
        foreach (Bubble bubble in cluster)
        {
            var list = GetMatchesForBubble(bubbleGrid[bubble.row, bubble.column], cluster.Count);
            if (list.Count > 1)
                endBubble = bubbleGrid[bubble.row, bubble.column];

            DestroyBubble(bubble);
        }

        if(endBubble.IncreaseType(cluster.Count))
        {
            bubbleGrid[endBubble.row, endBubble.column].gameObject.SetActive(true);
            return endBubble;
        }
        else   // Resulting bubble is at max capacity so explode it and all it's neighbours
        {
            DestoryBubbleCluster(endBubble);
            return null;
        }

        
    }

    // Add a bubble to the grid
    // Called when the bullet hits the grid
    public void AttachBubble(Bubble collidedBubble, Bubble bulletBubble)
    {
        List <Bubble> emptyNeighbours = GetEmptyNeighbours(collidedBubble);

        Bubble minBubble = null;

        if (emptyNeighbours.Count != 0)
        {
            float minDistance = 2f;

            foreach (Bubble bubble in emptyNeighbours)
            {
                
                float dist = Vector2.Distance(bubble.transform.position, bulletBubble.transform.position);

                if (dist < minDistance)
                {
                    minDistance = dist;
                    minBubble = bubble;
                }
            }
            bulletBubble.gameObject.SetActive(false);
            Debug.Log("Minbubble is : " + minBubble);
            bubbleGrid[minBubble.row, minBubble.column].SetType(bulletBubble.Type);
            bubbleGrid[minBubble.row, minBubble.column].gameObject.SetActive(true);
        }

        StartCoroutine(StartBubbleSequence(minBubble));
    }

    //
    // Methods for hiding bubbles
    //

    // Destroy @row rows
    // Triggered when the max allowed row has been reached
    void DestroyExtraRows(int row)
    {
        for (int i = ROWS - row - 1; i < ROWS; i++)
        {
            for (int j = 0; j < COLUMNS; j++)
            {
                DestroyBubble(bubbleGrid[i, j]);
            }
        }

        // SOUND
        AudioManager.SharedInstance.Play("clusterMerge");

        EventParam param = new EventParam();
        param._string = "KEEP ON";
        EventManager.TriggerEvent("showCombo", param);
    }

    // Hide a bubble, shake the camera and spawn some particles
    void DestroyBubble(Bubble bubble)
    {
        ParticleManager.SharedInstance.PlayParticle(ParticleType.BUBBLE_DESTROYED, bubble);
        bubbleGrid[bubble.row, bubble.column].gameObject.SetActive(false);

        // SOUND
        AudioManager.SharedInstance.Play("clusterMerge");

        cameraShake.Shake(0.02f, 0.1f);
    }

    public void DestoryBubbleCluster(Bubble explode)
    {
        List<Bubble> bubbles = GetActiveNeighbours(explode);

        DestroyBubble(explode);

        // Shake it!
        cameraShake.Shake(0.08f, 1.75f);

        foreach (Bubble bubble in bubbles)
        {
            DestroyBubble(bubble);
        }

    }

    void RemoveDisconnetedBubbles()
    {
        // Used to see if bubbles were removed during this step
        // If there were we are going to trigger a camera shake
        bool pop = false;

        CheckForDisconnected();

        // Remove disconnected bubbles
        for (int row = 0; row < ROWS; row++)
        {
            for (int column = 0; column < COLUMNS; column++)
            {
                Bubble b = bubbleGrid[row, column];
                if (!b.connected && b.gameObject.activeSelf)
                {
                    score += (int)Mathf.Pow(2, b.Type + 1);
                    DestroyBubble(b);
                        //b.gameObject.SetActive(false);
                    pop = true;
                }

            }
        }

        EventParam param = new EventParam();
        param._int = score;
        EventManager.TriggerEvent("addScore", param);
        score = 0;

        if (pop)
        {
            cameraShake.Shake(0.03f, 0.1f);
            // SOUND
        }

    }

    //
    // Methods for searching bubbles
    //

    // Look which bouble has the neighbour that will result from the merge
    // So that it can merge again 
    List<Bubble> GetMatchesForBubble(Bubble bubble, int _combo = 0)
    {
        bubble.visited = true;
        List<Bubble> result = new List<Bubble>() { bubble };
        List<Bubble> activeNeighbours = GetActiveNeighbours(bubble);

        foreach (Bubble neighbour in activeNeighbours)
        {
            if (neighbour.Type == bubble.Type + _combo)
            {
                result.Add(neighbour);
            }
        }

        return result;
    }

    List<Bubble> GetActiveNeighbours(Bubble bubble)
    {
        List<Bubble> neighbours = new List<Bubble>();

        // Check the right side
        if (bubble.column + 1 < COLUMNS)
        {
            if (bubbleGrid[bubble.row, bubble.column + 1].gameObject.activeSelf)
                neighbours.Add(bubbleGrid[bubble.row, bubble.column + 1]);
        }

        // Check the left side
        if (bubble.column - 1 >= 0)
        {
            if (bubbleGrid[bubble.row, bubble.column - 1].gameObject.activeSelf)
                neighbours.Add(bubbleGrid[bubble.row, bubble.column - 1]);
        }

        // Check the top side
        if (bubble.row - 1 >= 0)
        {
            if (bubbleGrid[bubble.row - 1, bubble.column].gameObject.activeSelf)
                neighbours.Add(bubbleGrid[bubble.row - 1, bubble.column]);
        }

        // Check the bottom side
        if (bubble.row + 1 < ROWS)
        {
            if (bubbleGrid[bubble.row + 1, bubble.column].gameObject.activeSelf)
                neighbours.Add(bubbleGrid[bubble.row + 1, bubble.column]);
        }

        if (bubble.column % 2 == 0)
        {
            //bottom-left
            if (bubble.row + 1 < ROWS && bubble.column - 1 >= 0)
            {

                if (bubbleGrid[bubble.row + 1, bubble.column - 1].gameObject.activeSelf)

                    neighbours.Add(bubbleGrid[bubble.row + 1, bubble.column - 1]);

            }


            //bottom-right

            if (bubble.row + 1 < ROWS && bubble.column + 1 < COLUMNS)
            {
                if (bubbleGrid[bubble.row + 1, bubble.column + 1].gameObject.activeSelf)
                    neighbours.Add(bubbleGrid[bubble.row + 1, bubble.column + 1]);
            }
        }
        else
        {
            //top-left
            if (bubble.row - 1 >= 0 && bubble.column - 1 >= 0)
            {

                if (bubbleGrid[bubble.row - 1, bubble.column - 1].gameObject.activeSelf)

                    neighbours.Add(bubbleGrid[bubble.row - 1, bubble.column - 1]);

            }


            //top-right

            if (bubble.row - 1 >= 0 && bubble.column + 1 < COLUMNS)
            {
                if (bubbleGrid[bubble.row - 1, bubble.column + 1].gameObject.activeSelf)
                    neighbours.Add(bubbleGrid[bubble.row - 1, bubble.column + 1]);
            }
        }

        return neighbours;
    }

    List<Bubble> GetEmptyNeighbours(Bubble bubble)
    {
        List<Bubble> empty = new List<Bubble>();

        // Check right side
        if (bubble.column + 1 < COLUMNS)
        {
            if (!bubbleGrid[bubble.row, bubble.column + 1].gameObject.activeSelf)
                empty.Add(bubbleGrid[bubble.row, bubble.column + 1]);
        }

        // Check left side
        if (bubble.column - 1 >= 0)
        {
            if (!bubbleGrid[bubble.row, bubble.column - 1].gameObject.activeSelf)
                empty.Add(bubbleGrid[bubble.row, bubble.column - 1]);
        }

        // Check top side
        if (bubble.row - 1 >= 0)
        {

            if (!bubbleGrid[bubble.row - 1, bubble.column].gameObject.activeSelf)

                empty.Add(bubbleGrid[bubble.row - 1, bubble.column]);

        }

        //bottom
        if (bubble.row + 1 < ROWS)
        {
            if (!bubbleGrid[bubble.row + 1, bubble.column].gameObject.activeSelf)
                empty.Add(bubbleGrid[bubble.row + 1, bubble.column]);
        }

        if (bubble.column % 2 == 0)
        {

            //top-left
            if (bubble.row - 1 >= 0 && bubble.column - 1 >= 0)
            {
                if (!bubbleGrid[bubble.row - 1, bubble.column - 1].gameObject.activeSelf)
                    empty.Add(bubbleGrid[bubble.row - 1, bubble.column - 1]);
            }

            //top-right
            if (bubble.row - 1 >= 0 && bubble.column + 1 < COLUMNS)
            {
                if (!bubbleGrid[bubble.row - 1, bubble.column + 1].gameObject.activeSelf)
                    empty.Add(bubbleGrid[bubble.row - 1, bubble.column + 1]);
            }
        }
        else
        {
            //bottom-left
            if (bubble.row + 1 < ROWS && bubble.column - 1 >= 0)
            {
                if (!bubbleGrid[bubble.row + 1, bubble.column - 1].gameObject.activeSelf)
                    empty.Add(bubbleGrid[bubble.row + 1, bubble.column - 1]);
            }

            //bottom-right
            if (bubble.row + 1 < ROWS && bubble.column + 1 < COLUMNS)
            {
                if (!bubbleGrid[bubble.row + 1, bubble.column + 1].gameObject.activeSelf)
                    empty.Add(bubbleGrid[bubble.row + 1, bubble.column + 1]);
            }

        }


        return empty;
    }

    void CheckForDisconnected()
    {
        // Set all bubbles as disconnected
        for (int row = 0; row < ROWS; row++)
        {
            for (int column = 0; column < COLUMNS; column++)
            {
                bubbleGrid[row, column].connected = false;
            }
        }

        // Connect visible bubbles in first row
        for (int column = 0; column < COLUMNS; column++)
        {
            if (bubbleGrid[0, column].gameObject.activeSelf)
            {
                bubbleGrid[0, column].connected = true;
            }
        }

        // Check the rest of the Bubbles
        for (int row = 1; row < ROWS; row++)
        {
            for (int column = 0; column < COLUMNS; column++)
            {
                Bubble bubble = bubbleGrid[row, column];
                if (bubble.gameObject.activeSelf)
                {
                    List<Bubble> neighbours = GetActiveNeighbours(bubble);
                    bool connected = false;

                    // Check if the neighbours are connected to other bubbles
                    foreach (Bubble neighbour in neighbours)
                    {
                        if (neighbour.connected)
                            connected = true;
                    }

                    if (connected)
                    {
                        bubble.connected = true;
                        foreach (Bubble neighbour in neighbours)
                        {
                            if (neighbour.gameObject.activeSelf)
                                neighbour.connected = true;
                        }
                    }
                }
            }
        }
    }


    //
    //  Methods for building the grid and the type pool
    //

    void BuildGrid()
    {
        // Container for all the bubbles
        bubbleGrid = new Bubble[ROWS, COLUMNS];

        // Cache grid position
        Vector3 origin = transform.position;

        // Helpers to position the bubbles
        GRID_OFFSET_X = (COLUMNS * TILE_SIZE) * 0.5f;
        GRID_OFFSET_Y = (ROWS * TILE_SIZE) * 0.5f;
        GRID_OFFSET_X -= TILE_SIZE * 0.5f;
        GRID_OFFSET_Y -= TILE_SIZE * 0.5f;

        // This will help us keep track when to stop drawing the bubbles
        int vizibleBubbleDelimiter = 0;

        for (int row = 0; row < ROWS; row++)
        {
            for (int column = 0; column < COLUMNS; column++)
            {
                Bubble bubble = Instantiate(bubbleTemplate);

                // The grid's transform will be parent for all the transforms of the bubbles
                bubble.transform.parent = gameObject.transform;
                bubble.SetPosition(this, row, column);

                if (vizibleBubbleDelimiter >= inititalLineCount)
                {
                    bubble.gameObject.SetActive(false);
                }
                else
                {
                    bubble.SetType(typePool[0]);
                    typePool.RemoveAt(0);
                }

                bubbleGrid[row, column] = bubble;

            }

            vizibleBubbleDelimiter += 1;
        }
    }

    // Generate the pool of types
    // The bigger changeTypeRate is the more identical bubbles we have
    void GenerateTypePool()
    {

        for (int i = 0; i < typePoolLength; i++)
        {
            float random = Random.Range(0.0f, 1.0f);

            if (random > changeTypeRate)
            {
                lastType = Random.Range(0, maxGeneratedBubbleType);
            }

            typePool.Add(lastType);
        }
    }
    

    private static readonly System.Random RNG = new System.Random();

    // Shuffle the type list
    public static void Shuffle(IList types)
    {
        int count = types.Count;

        while (count > 1)
        {
            count--;
            int k = RNG.Next(count + 1);

            BubbleType type = (BubbleType)types[k];

            types[k] = types[count];

            types[count] = type;
        }
    }

}
