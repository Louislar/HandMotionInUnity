using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class handLMsController : MonoBehaviour
{
    public jsonDeserializer jsonDeserializer;
    public avatarController avatarController;
    public class JointBone
    {
        public Vector3 Pos3D = new Vector3();   // observed data from sensor(e.g. MediaPipe, LeapMotion)
        public Quaternion rotation;
        public float flexionRotation;
        public float abductionRotation;

        // For Kalman filter 
        public Vector3 p = new Vector3();
        public Vector3 x = new Vector3();
        public Vector3 k = new Vector3();
        public Vector3 predict = new Vector3();
    }
    public class JointBoneRenderOB
    {
        // Visualize joints
        public GameObject SphereOB;
        public MeshRenderer Sphere;
        public GameObject LineObject;
        public LineRenderer Line;

        public JointBone start = null;
        public JointBone end = null;
    }
    public bool setWristOrigin;
    public bool NegX;
    public bool NegY;
    public bool NegZ;
    public float LMScale;
    public List<JointBoneRenderOB> JBRenderOBs = new List<JointBoneRenderOB>();
    public float renderScale;
    public bool renderNegX;
    public bool renderNegY;
    public bool renderNegZ;
    public float renderOffsetX;
    public float renderOffsetY;
    public float renderOffsetZ;
    
    public Material boneRenderMaterial;
    public Material jointRenderMaterial;

    public float kalmanParamQ;
    public float kalmanParamR;

    private float imgWidth = 848;
    private float imgHeight = 480;

    public JointBone[] jointBones = null;

    public bool updateHandRotationsToAvatar;

    [Header("Record hand LM settings")]
    public bool isRecordRotation;
    private List<MediaPipeHandLMs> recordedJointsRotation;
    public float recordLength;

    public void init()
    {
        jointBones = new JointBone[21];
        for (var i = 0; i < 21; i++) jointBones[i] = new JointBone();

        // Add bones 
        // Thumb
        addJointBone(0, 1);
        addJointBone(1, 2);
        addJointBone(2, 3);
        addJointBone(3, 4);
        // Index
        addJointBone(0, 5);
        addJointBone(5, 6);
        addJointBone(6, 7);
        addJointBone(7, 8);
        // Middle
        addJointBone(9, 10);
        addJointBone(10, 11);
        addJointBone(11, 12);
        // Ring
        addJointBone(13, 14);
        addJointBone(14, 15);
        addJointBone(15, 16);
        // Pinky
        addJointBone(0, 17);
        addJointBone(17, 18);
        addJointBone(18, 19);
        addJointBone(19, 20);
    }
    public void updateJointBoneLMs(MediaPipeHandLMs newLMs)
    {
        // Calibrate the normalized landmark data
        // They've been normalized by different scale, x -> width, y-> height
        // image width and height are (848, 480)
        float maxImgWidth = imgWidth - 1;
        float maxImgHeight = imgHeight - 1;
        float heightWidthRatio = maxImgHeight / maxImgWidth;

        // TODO: Make wrist position origin
        // TODO: negate x, y, z
        float negX = NegX ? -1 : 1;
        float negY = NegY ? -1 : 1;
        float negZ = NegZ ? -1 : 1;
        // scale x, y, z

        if(setWristOrigin)
        {
            for (int i = 0; i < newLMs.data.Count; ++i)
            {
                jointBones[i].Pos3D.x = LMScale * negX * (newLMs.data[i].x- newLMs.data[0].x);
                jointBones[i].Pos3D.y = LMScale * negY * (newLMs.data[i].y - newLMs.data[0].y) * heightWidthRatio;  // calibrate the normalized landmark
                jointBones[i].Pos3D.z = LMScale * negZ * (newLMs.data[i].z - newLMs.data[0].z);
                // Kalman filter
                KalmanFilter(jointBones[i]);
            }
        }
        else
        {
            for (int i = 0; i < newLMs.data.Count; ++i)
            {
                jointBones[i].Pos3D.x = LMScale * negX * newLMs.data[i].x;
                jointBones[i].Pos3D.y = LMScale * negY * newLMs.data[i].y * heightWidthRatio;  // calibrate the normalized landmark
                jointBones[i].Pos3D.z = LMScale * negZ * newLMs.data[i].z;
                // Kalman filter
                KalmanFilter(jointBones[i]);
            }
        }
    }
    /// <summary>
    /// 計算關節旋轉角度，wrist and MCP and PIP
    /// TODO: 目前還是使用Pos3D做計算，可以改用kalman filter估計後的predict看看效果會不會比較好
    /// </summary>
    public void updateJointRotation()
    {
        // wrist rotation(ref: https://github.com/TesseraktZero/UnityHandTrackingWithMediapipe)
        Transform wristTransform = JBRenderOBs[0].SphereOB.transform;
        Vector3 indexFinger = JBRenderOBs[5].SphereOB.transform.position;
        Vector3 middleFinger = JBRenderOBs[9].SphereOB.transform.position;

        Vector3 vectorToMiddle = middleFinger - wristTransform.position;
        Vector3 vectorToIndex = indexFinger - wristTransform.position;
        //to get ortho vector of middle finger from index finger
        // Vector3.OrthoNormalize(ref vectorToMiddle, ref vectorToIndex);

        //vector normal to wrist
        Vector3 normalVector = Vector3.Cross(vectorToIndex.normalized, vectorToMiddle.normalized);
        // Debug.DrawRay(wristTransform.position, normalVector, Color.white);
        // Debug.DrawRay(wristTransform.position, vectorToIndex, Color.yellow);
        wristTransform.rotation = Quaternion.LookRotation(normalVector, vectorToIndex);

        // index PIP(joint[6])
        Vector3 middleWristToMCP = jointBones[9].predict - jointBones[0].predict;
        Vector3 indexWristToMCP = jointBones[5].predict - jointBones[0].predict;
        Vector3 indexMCPToPIP = jointBones[6].predict - jointBones[5].predict;
        Vector3 indexPIPToDIP = jointBones[7].predict - jointBones[6].predict;
        float IndexPIPAngle = Vector3.Angle(indexMCPToPIP, indexPIPToDIP);
        // index MCP(joint[5])
        Vector3 indexMCPNormal = jointBones[9].predict - jointBones[5].predict;   // vector from index mcp to middle mcp
        Vector3 palmNormal = Vector3.Cross(indexWristToMCP.normalized, indexMCPNormal.normalized);
        //Debug.DrawRay(jointBones[5].predict, palmNormal, Color.red);
        //Debug.DrawRay(jointBones[5].predict, indexMCPNormal, Color.blue);
        // MCP rotation have two angles, one is along palm normal, another is along MCP normal(vector from middle to index)
        Vector3 projectToMCPNormal = Vector3.ProjectOnPlane(indexMCPToPIP, indexMCPNormal.normalized);
        Vector3 projectToPalmNormal = Vector3.ProjectOnPlane(indexMCPToPIP, palmNormal);
        // TODO Use Middle finger instead, 因為放鬆時食指並不是歪的，而是與中指平行的(但是改完發現效果好像沒有很好)
        // 其實原本效果就很糟(abduction會歪向一邊，當我的食指是放鬆時)
        float indexMCPAngle1 = Vector3.SignedAngle(indexWristToMCP, projectToMCPNormal, indexMCPNormal);
        float indexMCPAngle2 = Vector3.SignedAngle(indexWristToMCP, projectToPalmNormal, palmNormal);
        //Debug.DrawRay(jointBones[5].Pos3D, projectToMCPNormal, Color.black);
        //Debug.DrawRay(jointBones[5].Pos3D, projectToMCPNormal, Color.yellow);
        //print("index flexion: " + indexMCPAngle1.ToString());
        //print("abdcution: " + indexMCPAngle2.ToString());

        // middle PIP(joint[10])

        Vector3 middleMCPToPIP = jointBones[10].predict - jointBones[9].predict;
        Vector3 middlePIPToDIP = jointBones[11].predict - jointBones[10].predict;
        float middlePIPAngle = Vector3.Angle(middleMCPToPIP, middlePIPToDIP);
        // middle MCP(joint[9])
        Vector3 middlePalmNormal = Vector3.Cross(middleWristToMCP.normalized, indexMCPNormal.normalized);
        Vector3 middleProjectToMCPNormal = Vector3.ProjectOnPlane(middleMCPToPIP, indexMCPNormal.normalized);
        Vector3 middleProjectToPalmNormal = Vector3.ProjectOnPlane(middleMCPToPIP, middlePalmNormal);
        float middleMCPAngle1 = Vector3.SignedAngle(middleWristToMCP, middleProjectToMCPNormal, indexMCPNormal);
        float middleMCPAngle2 = Vector3.SignedAngle(middleWristToMCP, middleProjectToPalmNormal, middlePalmNormal);
        //Debug.DrawRay(jointBones[9].predict, middlePalmNormal, Color.red);
        //Debug.DrawRay(jointBones[9].predict, indexMCPNormal, Color.blue);
        //Debug.DrawRay(JBRenderOBs[8].SphereOB.transform.position, middleProjectToMCPNormal, Color.black);
        //Debug.DrawRay(JBRenderOBs[8].SphereOB.transform.position, middleProjectToPalmNormal, Color.yellow);
        //print("middle flexion: " + middleMCPAngle1.ToString());
        //print("abdcution: " + middleMCPAngle2.ToString());

        jointBones[6].flexionRotation = IndexPIPAngle;
        jointBones[5].flexionRotation = indexMCPAngle1;
        jointBones[5].abductionRotation = indexMCPAngle2;
        jointBones[10].flexionRotation = middlePIPAngle;
        jointBones[9].flexionRotation = middleMCPAngle1;
        jointBones[9].abductionRotation = middleMCPAngle2;
        // Convert the flexion and abduction to Eular angle and quaternion
        jointBones[6].rotation = Quaternion.Euler(IndexPIPAngle, 0.0f, 0.0f);
        jointBones[5].rotation = Quaternion.Euler(indexMCPAngle1, 0.0f, indexMCPAngle2);
        jointBones[10].rotation = Quaternion.Euler(middlePIPAngle, 0.0f, 0.0f);
        jointBones[9].rotation = Quaternion.Euler(middleMCPAngle1, 0.0f, middleMCPAngle2);
    }

    /// <summary>
    /// 更新手指的旋轉角度到avatar身上
    /// </summary>
    public void updateBodyJointRotation()
    {
        // index finger represent the right leg
        avatarController.updateRotation(
            jointBones[6].flexionRotation, jointBones[10].flexionRotation,
            jointBones[5].flexionRotation, jointBones[5].abductionRotation,
            jointBones[9].flexionRotation, jointBones[9].abductionRotation);
        // index represent the left leg
        //avatarController.updateRotation(
        //    jointBones[10].flexionRotation, jointBones[6].flexionRotation,
        //    jointBones[9].flexionRotation, jointBones[9].abductionRotation,
        //    jointBones[5].flexionRotation, jointBones[5].abductionRotation);
    }
    //public void updateJointBoneRenderOBs()
    //{
    //    foreach (var jb in JBRenderOBs)
    //    {
    //        var s = jb.start;
    //        var e = jb.end;

    //        jb.Line.SetPosition(0, new Vector3(s.Pos3D.x * renderScale + renderOffsetX, s.Pos3D.y * renderScale + renderOffsetY, s.Pos3D.z * renderScale + renderOffsetZ));
    //        jb.Line.SetPosition(1, new Vector3(e.Pos3D.x * renderScale + renderOffsetX, e.Pos3D.y * renderScale + renderOffsetY, e.Pos3D.z * renderScale + renderOffsetZ));
    //        jb.SphereOB.transform.position = new Vector3(s.Pos3D.x * renderScale + renderOffsetX, s.Pos3D.y * renderScale + renderOffsetY, s.Pos3D.z * renderScale + renderOffsetZ);
    //    }
    //}
    public void updateJointBoneRenderOBs()
    {
        foreach (var jb in JBRenderOBs)
        {
            var s = jb.start;
            var e = jb.end;

            jb.Line.SetPosition(0, new Vector3(s.predict.x * renderScale + renderOffsetX, s.predict.y * renderScale + renderOffsetY, s.predict.z * renderScale + renderOffsetZ));
            jb.Line.SetPosition(1, new Vector3(e.predict.x * renderScale + renderOffsetX, e.predict.y * renderScale + renderOffsetY, e.predict.z * renderScale + renderOffsetZ));
            jb.SphereOB.transform.position = new Vector3(s.predict.x * renderScale + renderOffsetX, s.predict.y * renderScale + renderOffsetY, s.predict.z * renderScale + renderOffsetZ);
        }
    }
    private void KalmanFilter(JointBone measurement)
    {
        MeasurementUpdate(measurement);
        measurement.predict.x = measurement.x.x + (measurement.Pos3D.x - measurement.x.x) * measurement.k.x;
        measurement.predict.y = measurement.x.y + (measurement.Pos3D.y - measurement.x.y) * measurement.k.y;
        measurement.predict.z = measurement.x.z + (measurement.Pos3D.z - measurement.x.z) * measurement.k.z;
        measurement.x = measurement.predict;
    }
    private void MeasurementUpdate(JointBone measurement)
    {
        measurement.k.x = (measurement.p.x + kalmanParamQ) / (measurement.p.x + kalmanParamQ + kalmanParamR);
        measurement.k.y = (measurement.p.y + kalmanParamQ) / (measurement.p.y + kalmanParamQ + kalmanParamR);
        measurement.k.z = (measurement.p.z + kalmanParamQ) / (measurement.p.z + kalmanParamQ + kalmanParamR);
        measurement.p.x = kalmanParamR * (measurement.p.x + kalmanParamQ) / (kalmanParamR + measurement.p.x + kalmanParamQ);
        measurement.p.y = kalmanParamR * (measurement.p.y + kalmanParamQ) / (kalmanParamR + measurement.p.y + kalmanParamQ);
        measurement.p.z = kalmanParamR * (measurement.p.z + kalmanParamQ) / (kalmanParamR + measurement.p.z + kalmanParamQ);
    }
    public void addJointBone(int s, int e)
    {
        var jbr = new JointBoneRenderOB
        {
            SphereOB = GameObject.CreatePrimitive(PrimitiveType.Sphere), 
            LineObject = new GameObject("Line"),
            start = jointBones[s],
            end = jointBones[e],
        };
        jbr.Line = jbr.LineObject.AddComponent<LineRenderer>();
        jbr.Line.startWidth = 0.03f;
        jbr.Line.endWidth = 0.01f;

        // define the number of vertex
        jbr.Line.positionCount = 2;
        jbr.Line.material = boneRenderMaterial;

        // Joint sphere setting
        jbr.SphereOB.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
        jbr.Sphere = jbr.SphereOB.GetComponent<MeshRenderer>();
        jbr.Sphere.material = jointRenderMaterial;

        JBRenderOBs.Add(jbr);
    }
    /// <summary>
    /// Update landmarks from the json file every n seconds
    /// </summary>
    /// <returns></returns>
    IEnumerator updateLMOnce()
    {
        int curIndex = 0;
        while (curIndex < jsonDeserializer.handLMs.results.Length)
        {
            updateJointBoneLMs(jsonDeserializer.handLMs.results[curIndex]);
            ++curIndex;
            yield return new WaitForSeconds(0.03f);
        }
        yield return null;
    }

    /// <summary>
    /// Record the rotation of the 4 joints to a json file
    /// </summary>
    /// <returns></returns>
    IEnumerator rotationRecord()
    {
        float recordTimeElapse = 0;
        List<int> collectJoints = new List<int>() {
            5, 6, 9, 10
        };
        recordedJointsRotation = new List<MediaPipeHandLMs>();
        while (recordTimeElapse < recordLength)
        {
            List<LMDataPoint> tmpDataPonts = new List<LMDataPoint>();
            foreach (int _aBoneIdx in collectJoints)
            {
                tmpDataPonts.Add(new LMDataPoint()
                {
                    x = jointBones[_aBoneIdx].rotation.eulerAngles.x,
                    y = jointBones[_aBoneIdx].rotation.eulerAngles.y,
                    z = jointBones[_aBoneIdx].rotation.eulerAngles.z
                });
            }
            recordedJointsRotation.Add(new MediaPipeHandLMs()
            {
                time = recordTimeElapse,
                data = tmpDataPonts
            });
            recordTimeElapse += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }
        jsonDeserializer jsonConverter = new jsonDeserializer();
        jsonConverter.serializeAndOutputFile(new MediaPipeResult() { results = recordedJointsRotation.ToArray() }, "jsonRotationData/handRotationRecord/leftFrontKick.json");
        yield return null;
    }

    // Start is called before the first frame update
    void Start()
    {
        init();
        updateJointBoneLMs(jsonDeserializer.handLMs.results[0]);
        updateJointBoneRenderOBs();
        StartCoroutine(updateLMOnce());
        if(isRecordRotation)
        {
            StartCoroutine(rotationRecord());
        }
    }

    // Update is called once per frame
    void Update()
    {
        updateJointRotation();
        updateJointBoneRenderOBs();
        if(updateHandRotationsToAvatar)
            updateBodyJointRotation();
    }
}
