using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameController : MonoBehaviour
{
    [SerializeField] private RayCastShooter shooter;

    private Animator textAnimator;

    [SerializeField] private Text popUpText;            // Text that will be animated for combos, level ups etc

    private bool mouseDown;

    private void Awake()
    {
        textAnimator = popUpText.GetComponent<Animator>();

        // Listen to the showCombo event
        EventManager.AddListener("showCombo", new System.Action<EventParam>(ShowCombo));
    }

    void Update()
    {
        // Handle touch
        // Forward commands to the raycast shooter
        if (Input.touches.Length > 0)
        {
            Touch touch = Input.touches[0];

            if (touch.phase == TouchPhase.Began)
            {
                OnTouchBegan(touch.position);
            }
            else if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
            {
                OnTouchEnded(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                OnTouchMove(touch.position);
            }

            return;
        }

        // Used for testing on MAC
#if UNITY_STANDALONE_OSX
        else if (Input.GetMouseButtonDown(0))
        {
            mouseDown = true;
            OnTouchBegan(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            mouseDown = false;
            OnTouchEnded(Input.mousePosition);
        }
        else if (mouseDown)
        {
            OnTouchMove(Input.mousePosition);
        }
#endif

    }

    void OnTouchBegan(Vector2 touch)
    {
        shooter.OnTouchBegan(touch);
    }

    void OnTouchEnded(Vector2 touch)
    {
        Vector2 point = Camera.main.ScreenToWorldPoint(touch);
        if (Vector2.Distance(point, shooter.transform.position) < 0.2f)
        {
            shooter.HideAimRay();
        }
        else
        {
            shooter.OnTouchEnded(touch);
        }
    }

    void OnTouchMove(Vector2 touch) 
    {
        shooter.OnTouchMove(touch);
    }

    int step = 5;

    // Calls the ShowText function
    // This function has an EventParam because it is triggered by the EventManager
    void ShowCombo(EventParam param)
    {
        StartCoroutine(ShowText(param._string));
    }

    // Trigger the text animation of the text that will indicate combos, level ups, etc.
    // The animator starts the transition to the animation state when step is positive and goes
    // back to the idle state when step is negative
    IEnumerator ShowText(string text)
    {
        this.popUpText.text = text;

        // Set step to be positive so that the animator will transition from idle to the animation state
        step = Mathf.Abs(step);
        textAnimator.SetInteger("step", step);

        // SOUND
        AudioManager.SharedInstance.Play("combo");

        yield return new WaitForSeconds(1f);

        // Set step negative to go back to idle state
        step = -step;
        textAnimator.SetInteger("step", step);
    }

    // Close the APP
    public void OnExitButtonPressed()
    {
        Application.Quit();
    }
}
