using Unity.Robotics.UrdfImporter.Control;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;



public class RobotController : MonoBehaviour
{
    [System.Serializable]
    public struct JointInfo
    {
        public GameObject robotPart;
        public  ArticulationJointType type;
    }

    public JointInfo[] jointsInfo;

    private void Start()
    {
        // 刪除物體上的 Controller script
        Controller existingController = GetComponent<Controller>();
        if (existingController != null)
        {
            // Debug.LogWarning("Existing Controller script found, removing it.");
            Destroy(existingController);
        }
        AutoAssignJoints();
    }

    private void ListAllQualifiedChildObjects(Transform parent, List<JointInfo> assignedJointsInfo)
    {
        foreach (Transform child in parent)
        {
            // 檢查是否包含 "Collisions" 和 "Visuals" 子物件
            Transform collisions = child.Find("Collisions");
            Transform visuals = child.Find("Visuals");
            ArticulationBody articulationBody = child.GetComponent<ArticulationBody>();
            if (articulationBody != null)
            {
                Debug.Log($"Joint {child.name} type: {articulationBody.jointType}");
            }
            if (child.name == "base_link")
            {
                // Debug.Log("Skipping base_link but checking its children");
                if (child.childCount > 0)
                {
                    //遞迴檢查子物件
                    ListAllQualifiedChildObjects(child, assignedJointsInfo);
                }
                continue; // 繼續到下一個子物件
            }

            if (collisions != null && visuals != null && articulationBody != null && articulationBody.jointType != ArticulationJointType.FixedJoint)
            {
                
                if (articulationBody.jointType == ArticulationJointType.RevoluteJoint )
                {
                
                    if (child.gameObject.GetComponent<RotateJointController>() == null)
                    {
                        child.gameObject.AddComponent<RotateJointController>();
                        ArticulationDrive xDrive = articulationBody.xDrive;
                        xDrive.stiffness = 1000000f;
                        xDrive.damping = 1000f;
                        articulationBody.xDrive = xDrive;
                    }
                }
                else if (articulationBody.jointType == ArticulationJointType.PrismaticJoint)
                {
                    if (child.gameObject.GetComponent<SlideJointController>() == null)
                    {
                        child.gameObject.AddComponent<SlideJointController>();
                        ArticulationDrive xDrive = articulationBody.xDrive;
                        xDrive.stiffness = 1000000f;
                        xDrive.damping = 1000f;
                        articulationBody.xDrive = xDrive;
                    }
                }

                JointInfo newJointInfo = new JointInfo
                {
                    robotPart = child.gameObject,
                    type = articulationBody.jointType
                };
                assignedJointsInfo.Add(newJointInfo);
            }

            // 如果該子物件還有子物件，遞迴遍歷
            if (child.childCount > 0)
            {
                ListAllQualifiedChildObjects(child, assignedJointsInfo);
            }
        }
    }


    private void AutoAssignJoints()
    {
        List<JointInfo> assignedJointsInfo = new List<JointInfo>();
        ListAllQualifiedChildObjects(transform, assignedJointsInfo);
        jointsInfo = assignedJointsInfo.ToArray();
    }

    public void StopAll()
    {
        for (int i = 0; i < jointsInfo.Length; i++)
        {
            GameObject robotPart = jointsInfo[i].robotPart;
            if (jointsInfo[i].type == ArticulationJointType.RevoluteJoint) {
                RotateJointController jointController = robotPart.GetComponent<RotateJointController>();
                if (jointController != null){
                    jointController.rotationState = RotationDirection.None;
                }
            }
            else if (jointsInfo[i].type == ArticulationJointType.PrismaticJoint) {
                SlideJointController slideController = robotPart.GetComponent<SlideJointController>();
                if (slideController != null){
                    slideController.slideState = SlideDirection.None;
                }
            }
        }
    }

    public void RotateJoint(int jointIndex, RotationDirection direction)
    {
        if (jointIndex >= 0 && jointIndex < jointsInfo.Length && jointsInfo[jointIndex].type == ArticulationJointType.RevoluteJoint) {
            JointInfo jointInfo = jointsInfo[jointIndex];
            RotateJointController jointController = jointInfo.robotPart.GetComponent<RotateJointController>();
            if (jointController != null) {
                jointController.rotationState = direction;
            }
        }
        else {
            Debug.LogWarning("Invalid joint index or joint is not revolute.");
        }
    }

    public void SlideJoint(int jointIndex, SlideDirection direction)
    {
        if (jointIndex >= 0 && jointIndex < jointsInfo.Length && jointsInfo[jointIndex].type == ArticulationJointType.PrismaticJoint) {

            JointInfo jointInfo = jointsInfo[jointIndex];
            SlideJointController slideController = jointInfo.robotPart.GetComponent<SlideJointController>();
            if (slideController != null) {
                slideController.slideState = direction;

            }
        }
        else{
            Debug.LogWarning("Invalid joint index or joint is not prismatic.");
        }
    }
}/////////

 ////   public MoveJoint (int jointIndex, )
