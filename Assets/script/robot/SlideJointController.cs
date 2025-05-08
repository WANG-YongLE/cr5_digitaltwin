using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
public enum SlideDirection { None = 0, Right = 1, Left = -1 };

public class SlideJointController : MonoBehaviour
{
    public SlideDirection slideState = SlideDirection.None;
    public float speed = 0.001f;
    private ArticulationBody articulation;

    void Start()
    {
        articulation = GetComponent<ArticulationBody>();
    }

    void Update()
    {
    
        if (slideState != SlideDirection.None)
        {
 //           Debug.Log($"SlideState: {slideState}");
            float slideChange = (float)slideState * speed* Time.fixedDeltaTime;
            float slideGoal = CurrentPrimaryAxisPosition() + slideChange;
            SlideTo(slideGoal);
        }
    }

    void SlideTo(float target)
    {
        if (articulation != null)
        {
            var drive = articulation.xDrive;
            drive.target= target;
        //    Debug.Log($"SlideTo: {target}");
            articulation.xDrive = drive;
        }
    }
    public float CurrentPrimaryAxisPosition()
    {
        return articulation.xDrive.target;
    }
}