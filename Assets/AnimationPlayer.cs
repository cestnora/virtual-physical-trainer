using System;
using System.Collections;
using UnityEngine;

public class AnimationPlayer : MonoBehaviour
{
    // Reference to the Dialogue component; assign in Inspector
    public DialogueBox dialogue;

    // Reference to the Animator want to control; assign in Inspector
    public Animator targetAnimator;

    // Optionally, reference to the continue button to visually disable it (if needed)
    // public Button continueButton;

    // private bool isPaused = false;
    private bool canClickNext = true; // controls whether the next click is accepted

    void Start()
    {
        // If no Animator is assigned manually, try to get one on this GameObject
        if (targetAnimator == null)
        {
            targetAnimator = GetComponent<Animator>();
            if (targetAnimator == null)
            {
                Debug.LogWarning("No Animator assigned or found on " + gameObject.name);
            }
        }
    }

    // Method for next button, now with continuous synchronization
    public void OnNextButtonPressed()
    {
        if (!canClickNext)
            return; // ignore extra clicks

        canClickNext = false;

        if (dialogue != null)
        {
            dialogue.ShowNextCaption();
        }
        else
        {
            Debug.LogWarning("Dialogue reference not set on " + gameObject.name);
        }

        if (targetAnimator != null)
        {
            targetAnimator.SetTrigger("Next");
            StartCoroutine(ResetTrigger("Next"));
            StartCoroutine(WaitForAnimationFinish("Next"));
        }
        else
        {
            Debug.LogWarning("No Animator to control on " + gameObject.name);
            canClickNext = true;
        }
    }

    // Restart animation and go back to the idle state ("Watch Again")
    public void OnWatchAgainButtonPressed()
    {
        if (dialogue != null)
        {
            dialogue.RestartDialogue();
        }
        else
        {
            Debug.LogWarning("Dialogue reference not set on " + gameObject.name);
        }

        if (targetAnimator != null)
        {
            // "Idle" trigger set up in Animator's AnyState transitions
            targetAnimator.SetTrigger("Start");
        }
        else
        {
            Debug.LogWarning("No Animator to control on " + gameObject.name);
        }
    }

    // Generic exercise button method: Pass a string (e.g., "WarmUp", "PushUp", "FullBody", etc.)
    public void OnExerciseButtonPressed(string exerciseType)
    {
        if (!canClickNext)
            return;

        canClickNext = false;

        if (dialogue != null)
        {
            if (Enum.TryParse<DialogueBox.ExerciseType>(exerciseType, true, out var exercise))
            {
                dialogue.ShowSelectedExercises(exercise);
            }
            else
            {
                Debug.LogError("Invalid exercise type: " + exerciseType);
            }
        }
        else
        {
            Debug.LogWarning("Dialogue reference not set on " + gameObject.name);
        }

        if (targetAnimator != null)
        {
            targetAnimator.SetTrigger(exerciseType);
            StartCoroutine(ResetTrigger(exerciseType));
            StartCoroutine(WaitForAnimationFinish(exerciseType));
        }
        else
        {
            Debug.LogWarning("No Animator to control on " + gameObject.name);
            canClickNext = true;
        }
    }

    // Coroutine to reset a trigger after one frame
    private IEnumerator ResetTrigger(string triggerName)
    {
        yield return null;
        targetAnimator.ResetTrigger(triggerName);
    }

    // Coroutine that continuously checks until the current animation state is finished
    // For "FullBody", use a fixed wait if needed
    private IEnumerator WaitForAnimationFinish(string triggerName)
    {
        yield return null; // let the animator update its state

        float timeout = 3.0f; // maximum wait time in seconds to avoid an infinite loop
        float elapsed = 0f;

        if (targetAnimator == null)
        {
            canClickNext = true;
            yield break;
        }

       // Continuously check the current state's normalized time
       AnimatorStateInfo stateInfo = targetAnimator.GetCurrentAnimatorStateInfo(0);
       while (stateInfo.normalizedTime < 1.0f && elapsed < timeout)
       {
            yield return null;
            elapsed += Time.deltaTime;
            stateInfo = targetAnimator.GetCurrentAnimatorStateInfo(0);
       }

        canClickNext = true;
    }

    // Optional: Toggle pause using the Space key
    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        isPaused = !isPaused;
    //        if (targetAnimator != null)
    //        {
    //            targetAnimator.SetBool("isPaused", isPaused);
    //            targetAnimator.SetFloat("Speed", isPaused ? 0 : 1);
    //        }
    //    }
    //}
}
