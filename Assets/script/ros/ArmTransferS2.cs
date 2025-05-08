using System.Collections.Generic; // 加入 List 所需的命名空間
using UnityEngine;
using WebSocketSharp;
using System;
using TMPro;

public class ArmTransferS2 : MonoBehaviour
{
    public ConnectRosBridge connectRos;
    public float[] jointPositions;
    public bool haveGripper = true;
    string inputTopic = "/robot_arm";
    List<float> data = new List<float>(); // 動態調整大小的 List
    public GameObject robot;
    RobotController robotController;
    const int ROTATION_THRESHOLD = 3;
    const float SLIDE_THRESHOLD = 0.0003f;
    //只有當這個角度差大於或等於 ROTATION_THRESHOLD (也就是 3 度) 時，才會判斷需要進行旋轉，並根據目標角度和當前角度的比較結果返回 1 (正向旋轉) 或 -1 (負向旋轉)。
    //如果角度差小於 3 度，則認為已經足夠接近目標角度，返回 0，表示不需要旋轉。
    bool manual;

    void Start()
    {
        robotController = robot.GetComponent<RobotController>();
        connectRos.ws.OnMessage += OnWebSocketMessage;
        SubscribeToTopic(inputTopic);
    }

    void Update()
    {
        for (int i = robotController.jointsInfo.Length - 1; i >= 0; i--)
        {   

            if (i < data.Count)
            {
               

                if (robotController.jointsInfo[i].type == ArticulationJointType.RevoluteJoint)
                {   

                    float inputVal = CountRotationInputVal(i);
                    RotationDirection direction = GetRotationDirection(inputVal);
                    robotController.RotateJoint(i, direction);
                }
                else if (robotController.jointsInfo[i].type == ArticulationJointType.PrismaticJoint)
                {
                    float inputVal = CountSlideInputVal(i);
                    SlideDirection direction = GetSlideDirection(inputVal);
                    robotController.SlideJoint(i, direction);
                }
            }
        }
    }

    private void OnWebSocketMessage(object sender, MessageEventArgs e)
    {
        string jsonString = e.Data;
        var genericMessage = JsonUtility.FromJson<GenericRosMessage>(jsonString);
        if (genericMessage.topic == inputTopic)
        {
           
            RobotNewsMessageJointTrajectory message = JsonUtility.FromJson<RobotNewsMessageJointTrajectory>(jsonString);
    //       ("Received message on topic: " + genericMessage.topic + " \n with message: " + message.msg.positions);
            HandleJointTrajectoryMessage(message);
        }
    }

    private void HandleJointTrajectoryMessage(RobotNewsMessageJointTrajectory message)
    {
        jointPositions = message.msg.positions;
        // 清空並重新設置 data 列表
       // Debug.Log($"Joint Positions: [{string.Join(", ", jointPositions)}]");
        data.Clear();

        for (int i = 0; i < robotController.jointsInfo.Length; i++)
        {
            if (i < jointPositions.Length) data.Add(jointPositions[i]);

        }

    }

    private void SubscribeToTopic(string topic)
    {
        string typeMsg = "trajectory_msgs/msg/JointTrajectoryPoint";
        string subscribeMessage = "{\"op\":\"subscribe\",\"id\":\"1\",\"topic\":\"" + topic + "\",\"type\":\"" + typeMsg + "\"}";
        
        connectRos.ws.Send(subscribeMessage);
    }

    float CountRotationInputVal(int index)
    {
  
        RobotController.JointInfo jointInfo = robotController.jointsInfo[index];
        RotateJointController jointController = jointInfo.robotPart.GetComponent<RotateJointController>();
      
        if (jointController != null)
        {
     
            float currentAngle = jointController.CurrentPrimaryAxisRotation();
            float targetAngle = data[index];
            float mappedTargetAngle = MapTargetAngle(currentAngle, targetAngle);

            if (Math.Abs(mappedTargetAngle - currentAngle) >= ROTATION_THRESHOLD)
            {
  
                
                if (mappedTargetAngle > currentAngle) return 1;
                if (currentAngle > mappedTargetAngle) return -1;
            }
            return 0;
        }
        return 0;
    }

    float CountSlideInputVal(int index)
    {
        RobotController.JointInfo jointInfo = robotController.jointsInfo[index];
        SlideJointController slideController = jointInfo.robotPart.GetComponent<SlideJointController>();
        if (slideController != null)
        {
            float currentPosition = slideController.CurrentPrimaryAxisPosition();
            float targetPosition = data[index];
        //    Debug.Log($"currentPosition: {currentPosition}, targetPosition: {targetPosition}");
            if (Mathf.Abs(targetPosition - currentPosition) >= SLIDE_THRESHOLD)
            {
                if (targetPosition > currentPosition) return 1;
                if (currentPosition > targetPosition) return -1;
            }
            return 0;
        }
        return 0;
    }

    float MapTargetAngle(float currentRotation, float targetAngle)
    {
        float normalizedCurrentRotation = currentRotation % 360f;
        if (normalizedCurrentRotation < 0) normalizedCurrentRotation += 360f;

        int cycleOffset = Mathf.FloorToInt(currentRotation / 360f);
        float adjustedTargetAngle = targetAngle + (cycleOffset * 360f);

        if (Mathf.Abs(adjustedTargetAngle - currentRotation) > 180f)
        {
            if (adjustedTargetAngle > currentRotation)
                adjustedTargetAngle -= 360f;
            else
                adjustedTargetAngle += 360f;
        }
        return adjustedTargetAngle;
    }

    static RotationDirection GetRotationDirection(float inputVal)
    {
        if (inputVal > 0)
            return RotationDirection.Positive;
        else if (inputVal < 0)
            return RotationDirection.Negative;
        else
            return RotationDirection.None;
    }

    static SlideDirection GetSlideDirection(float inputVal)
    {
        if (inputVal > 0)//None = 0, Positive = 1, Negative = -1
            return SlideDirection.Right;
        else if (inputVal < 0)
            return SlideDirection.Left;
        else
            return SlideDirection.None;
    }

    public float[] GetCurrentJointPositions()
    {
        return data.ToArray();
    }

    public void UpdateJointPositions(float[] positions){
        data.Clear();
        for (int i = 0; i < robotController.jointsInfo.Length; i++)
        {
            data.Add(positions[i]);
        }
    }

    [System.Serializable]
    public class GenericRosMessage
    {
        public string op;
        public string topic;
    }

    [System.Serializable]
    public class RobotNewsMessageJointTrajectory
    {
        public string op;
        public string topic;
        public JointTrajectoryPointMessage msg;
    }

    [System.Serializable]
    public class JointTrajectoryPointMessage
    {
        public float[] positions;
        public float[] velocities;
        public float[] accelerations;
        public float[] effort;
        public TimeFromStart time_from_start;
    }

    [System.Serializable]
    public class TimeFromStart
    {
        public int sec;
        public int nanosec;
    }
}
