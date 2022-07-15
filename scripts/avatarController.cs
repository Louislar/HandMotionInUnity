using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class avatarController : MonoBehaviour
{
    public Animator avatarAnim;
    public Material skMaterial;
    private VNectModel.JointPoint[] jointPoints;
    private VNectModel vNectModel;
    private LMDataPoint[] lmDataPoints;
    public float adjUpperLegFlexion;
    public float adjIndexUpperLegAbduction;
    public bool isRotationFromHand;
    [Header("Rotation recording settings")]
    public bool isRecordHumanMotion;
    public float recordLength;
    private List<MediaPipeHandLMs> avatarRotationData;
    private Transform tmpTrans;
    [Header("Position recording settings")]
    public bool isRecordHumanPosition;
    public bool isRecordMultiHumanPositions;
    public List<string> HumanRotationFileNMs;
    public float positionRecordLength;
    private List<MediaPipeHandLMs> avatarPositionData;
    [Header("Position read in settings")]
    public bool isReadApplyHumanPositions;
    public int readPositionsJointsNum;
    public Vector3 originHipPosition;
    private string readInHumanPositionFile;
    private MediaPipeResult readInHumanPositionResult;
    private handLMsController.JointBone[] SynthesisJointsPos;

    /// <summary>
    /// init joint points array with "essentialJointPoints" enum
    /// This will prepare a data array for output as a json file
    /// TODO: 使用提供的enum mapping方法，用for迴圈將整件事完成
    /// </summary>
    /// <returns></returns>
    public LMDataPoint[] init(Animator anim = null)
    {
        lmDataPoints = new LMDataPoint[(int)essentialJointPoints.Count];
        for (var i = 0; i < (int)essentialJointPoints.Count; ++i) lmDataPoints[i] = new LMDataPoint();

        if (anim == null)
            return null;

        

        // Right Arm
        lmDataPointCopy(lmDataPoints[(int)essentialJointPoints.rShoulder], anim.GetBoneTransform(HumanBodyBones.RightShoulder).position);
        lmDataPointCopy(lmDataPoints[(int)essentialJointPoints.rUpperArm], anim.GetBoneTransform(HumanBodyBones.RightUpperArm).position);
        lmDataPointCopy(lmDataPoints[(int)essentialJointPoints.rLowerArm], anim.GetBoneTransform(HumanBodyBones.RightLowerArm).position);
        lmDataPointCopy(lmDataPoints[(int)essentialJointPoints.rHand], anim.GetBoneTransform(HumanBodyBones.RightHand).position);
        // Left Arm
        lmDataPointCopy(lmDataPoints[(int)essentialJointPoints.lShoulder], anim.GetBoneTransform(HumanBodyBones.LeftShoulder).position);
        lmDataPointCopy(lmDataPoints[(int)essentialJointPoints.lUpperArm], anim.GetBoneTransform(HumanBodyBones.LeftUpperArm).position);
        lmDataPointCopy(lmDataPoints[(int)essentialJointPoints.lLowerArm], anim.GetBoneTransform(HumanBodyBones.LeftLowerArm).position);
        lmDataPointCopy(lmDataPoints[(int)essentialJointPoints.lHand], anim.GetBoneTransform(HumanBodyBones.LeftHand).position);

        //// Right Leg
        //lmDataPoints[(int)essentialJointPoints.rUpperLeg].Transform = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        //lmDataPoints[(int)essentialJointPoints.rLowerLeg].Transform = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        //lmDataPoints[(int)essentialJointPoints.rFoot].Transform = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        //lmDataPoints[(int)essentialJointPoints.rToe].Transform = anim.GetBoneTransform(HumanBodyBones.RightToes);
        //// Left Leg
        //lmDataPoints[(int)essentialJointPoints.rUpperLeg].Transform = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        //lmDataPoints[(int)essentialJointPoints.rLowerLeg].Transform = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        //lmDataPoints[(int)essentialJointPoints.rFoot].Transform = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        //lmDataPoints[(int)essentialJointPoints.rToe].Transform = anim.GetBoneTransform(HumanBodyBones.LeftToes);

        //// etc
        //lmDataPoints[(int)essentialJointPoints.Hip].Transform = anim.GetBoneTransform(HumanBodyBones.Hips);
        //lmDataPoints[(int)essentialJointPoints.Spine].Transform = anim.GetBoneTransform(HumanBodyBones.Spine);
        //lmDataPoints[(int)essentialJointPoints.Chest].Transform = anim.GetBoneTransform(HumanBodyBones.Chest);
        //lmDataPoints[(int)essentialJointPoints.UpperChest].Transform = anim.GetBoneTransform(HumanBodyBones.UpperChest);
        //lmDataPoints[(int)essentialJointPoints.Head].Transform = anim.GetBoneTransform(HumanBodyBones.Head);
        return null;
    }
    public void lmDataPointCopy(LMDataPoint lmdp, Vector3 v3)
    {
        lmdp.x = v3.x;
        lmdp.y = v3.y;
        lmdp.z = v3.z;
    }
    /// <summary>
    /// Update the rotation of joints in the data array by hand LM rotation             
    /// TODO: 將jointPoints參考的enum改成自己定義的essentialJointPoints
    /// </summary>
    public void updateRotation(float leftKneeRotation, float rightKneeRotation, float leftUpperLeg1, float leftUpperLeg2, float rightUpperLeg1, float rightUpperLeg2)
    {
        leftUpperLeg1 -= adjUpperLegFlexion;    // 30
        rightUpperLeg1 -= adjUpperLegFlexion;
        leftUpperLeg2 -= adjIndexUpperLegAbduction; // 食指在正常狀況下就會有些微的abduction, 20

        // knee == lowerLeg
        jointPoints[PositionIndex.lShin.Int()].InitRotation = Quaternion.Euler(leftKneeRotation, 0.0f, 0.0f);
        jointPoints[PositionIndex.rShin.Int()].InitRotation = Quaternion.Euler(rightKneeRotation, 0.0f, 0.0f);

        // UpperLeg1 = along x axis, UpperLeg2 = along z axis 
        jointPoints[PositionIndex.lThighBend.Int()].InitRotation = Quaternion.Euler(leftUpperLeg1, 0.0f, leftUpperLeg2);
        jointPoints[PositionIndex.rThighBend.Int()].InitRotation = Quaternion.Euler(rightUpperLeg1, 0.0f, rightUpperLeg2);
    }

    /// <summary>
    /// 紀錄human avatar特定關節的rotation資料
    /// 最後再利用json格式儲存
    /// </summary>
    /// <returns></returns>
    public IEnumerator rotationRecorder()
    {
        // TODO: 讓hip的rotation資料也能夠被記錄到json檔當中(暫緩)
        float recordTimeElapse = 0;
        List<HumanBodyBones> collectJoints = new List<HumanBodyBones>() {
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg
        };
        avatarRotationData = new List<MediaPipeHandLMs>();
        while (recordTimeElapse<recordLength)
        {
            List<LMDataPoint> tmpDataPonts = new List<LMDataPoint>();
            foreach (HumanBodyBones _aBone in collectJoints)
            {
                tmpDataPonts.Add(new LMDataPoint()
                {
                    x = avatarAnim.GetBoneTransform(_aBone).localEulerAngles.x,
                    y = avatarAnim.GetBoneTransform(_aBone).localEulerAngles.y,
                    z = avatarAnim.GetBoneTransform(_aBone).localEulerAngles.z
                });
            }
            avatarRotationData.Add(new MediaPipeHandLMs()
            {
                time = recordTimeElapse,
                data = tmpDataPonts
            });
            recordTimeElapse += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }
        jsonDeserializer jsonConverter = new jsonDeserializer();
        jsonConverter.serializeAndOutputFile(new MediaPipeResult() { results=avatarRotationData.ToArray() }, "jsonRotationData/Walk_Strafe_Right.json");
        //print($"get bone: {avatarAnim.GetBoneTransform(HumanBodyBones.LeftUpperLeg).localEulerAngles.x}");
        yield return null;
    }

    /// <summary>
    /// 紀錄human avatar關節點的position
    /// </summary>
    /// <returns></returns>
    public IEnumerator positionRecorder()
    {
        // TODO: Finish this function，upper leg的position似乎不用輸出
        // 但是，應該需要將position校正到hip為原點(放到python裡面再做)
        // 需要能夠指定錄製時間長度
        float recordTimeElapse = 0;
        //List<HumanBodyBones> collectJoints = new List<HumanBodyBones>() {
        //    HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, 
        //    HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
        //    HumanBodyBones.Hips
        //};
        // 輸出所有在synthesis階段需要用到的骨架
        List<HumanBodyBones> collectJoints = new List<HumanBodyBones>() {
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
            HumanBodyBones.Hips,
            HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
            HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand
        };
        avatarPositionData = new List<MediaPipeHandLMs>();
        while (recordTimeElapse < positionRecordLength)
        {
            List<LMDataPoint> tmpDataPonts = new List<LMDataPoint>();
            foreach (HumanBodyBones _aBone in collectJoints)
            {
                tmpDataPonts.Add(new LMDataPoint()
                {
                    x = avatarAnim.GetBoneTransform(_aBone).position.x,
                    y = avatarAnim.GetBoneTransform(_aBone).position.y,
                    z = avatarAnim.GetBoneTransform(_aBone).position.z
                });
            }
            avatarPositionData.Add(new MediaPipeHandLMs()
            {
                time = recordTimeElapse,
                data = tmpDataPonts
            });
            recordTimeElapse += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }
        jsonDeserializer jsonConverter = new jsonDeserializer();
        //jsonConverter.serializeAndOutputFile(
        //    new MediaPipeResult() { results = avatarPositionData.ToArray() },
        //    "jsonPositionData/leftFrontKickCombinations/previousGenData/leftFrontKickPosition(True, True, True, True, True, True).json"
        //    );
        jsonConverter.serializeAndOutputFile(
            new MediaPipeResult() { results = avatarPositionData.ToArray() },
            //"jsonPositionData/bodyMotionPosition/leftFrontKickPosition.json"
            "jsonPositionData/bodyMotionPosition/leftFrontKickPositionFullJoints.json"  // 輸出所有身體joints使用的檔名
            );
        // print($"get bone: {avatarAnim.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position.x}");
        yield return null;
    }

    /// <summary>
    /// 根據hand rotation controller讀取不同檔案的旋轉資料，
    /// 記錄成多筆position的檔案
    /// </summary>
    /// <returns></returns>
    public IEnumerator multiplePositionsRecorder()
    {
        // 需要知道handRotationController切換讀取檔案的數量，使用changeCurRecordPosFileNM(string fileName)
        // 固定每個檔案紀錄的時間，並且事前給定錄製檔案數量
        int fileRecordCount = HumanRotationFileNMs.Count;
        while(fileRecordCount==0)
        {
            fileRecordCount = HumanRotationFileNMs.Count;
            yield return null;
        }
        int curFileIdx = 0;
        // 每0.05f紀錄一次資料，總共600次，30秒
        List<List<MediaPipeHandLMs>> multiPosData = new List<List<MediaPipeHandLMs>>();
        List<HumanBodyBones> collectJoints = new List<HumanBodyBones>() {
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
            HumanBodyBones.Hips
        };
        while (curFileIdx<fileRecordCount)
        {
            // 紀錄單一檔案的資料
            float recordTimeElapse = 0;
            avatarPositionData = new List<MediaPipeHandLMs>();
            while (recordTimeElapse < positionRecordLength)
            {
                List<LMDataPoint> tmpDataPonts = new List<LMDataPoint>();
                foreach (HumanBodyBones _aBone in collectJoints)
                {
                    tmpDataPonts.Add(new LMDataPoint()
                    {
                        x = avatarAnim.GetBoneTransform(_aBone).position.x,
                        y = avatarAnim.GetBoneTransform(_aBone).position.y,
                        z = avatarAnim.GetBoneTransform(_aBone).position.z
                    });
                }
                avatarPositionData.Add(new MediaPipeHandLMs()
                {
                    time = recordTimeElapse,
                    data = tmpDataPonts
                });
                recordTimeElapse += 0.05f;
                yield return new WaitForSeconds(0.05f);
            }

            multiPosData.Add(avatarPositionData);
            ++curFileIdx;
        }

        // Output multiple json files
        jsonDeserializer jsonConverter = new jsonDeserializer();
        for(int i=0; i< fileRecordCount; ++i)
        {
            jsonConverter.serializeAndOutputFile(
            new MediaPipeResult() { results = multiPosData[i].ToArray() },
            HumanRotationFileNMs[i]
            );
        }
        print("Finish record multiple position files");
        yield return null;
    }

    /// <summary>
    /// 所有需要紀錄的position檔案名稱
    /// 用於一次性紀錄多個position檔案時使用
    /// 搭配multiplePositionsRecorder()使用
    /// </summary>
    public void changeCurRecordPosFileNM(List<string> fileNames)
    {
        HumanRotationFileNMs = new List<string>();
        foreach (string str in fileNames)
        {
            // 更新檔案名稱
            string fileName = Path.GetFileName(str);
            // 改變檔案名稱 + "_position" + ".json"
            // TODO: 指定folder儲存position的輸出資料
            HumanRotationFileNMs.Add(
                Path.Combine(Application.dataPath, "jsonPositionData/leftFrontKickCombinations/" + fileName)
                );
            // print(curHumanRotationFileNM);
        }
    }

    /// <summary>
    /// 更新一次synthesis後的position data
    /// </summary>
    public void updateSynthesisPositionOnce(MediaPipeHandLMs newRots)
    {
        for (int i = 0; i < newRots.data.Count; ++i)
        {
            SynthesisJointsPos[i].Pos3D.x = newRots.data[i].x;
            SynthesisJointsPos[i].Pos3D.y = newRots.data[i].y;
            SynthesisJointsPos[i].Pos3D.z = newRots.data[i].z;
        }
    }

    /// <summary>
    /// TODO: 更新synthesis完成後的position data到avatar上
    /// </summary>
    /// <returns></returns>
    public IEnumerator updateSynthesisPosition()
    {
        int curIndex = 0;
        while (curIndex < readInHumanPositionResult.results.Length)
        {
            updateSynthesisPositionOnce(readInHumanPositionResult.results[curIndex]);
            ++curIndex;
            yield return new WaitForSeconds(0.05f);
        }
        yield return null;
    }

    // Start is called before the first frame update
    void Start()
    {
        tmpTrans = avatarAnim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        print(avatarAnim.GetBoneTransform(HumanBodyBones.RightUpperArm));
        vNectModel = new VNectModel();
        vNectModel.ShowSkeleton = true;
        vNectModel.SkeletonX = -1.0f;
        vNectModel.SkeletonY = 0.0f;
        vNectModel.SkeletonZ = 0.0f;
        vNectModel.SkeletonScale = 0.8f;
        vNectModel.SkeletonMaterial = skMaterial;
        jointPoints = vNectModel.Init(avatarAnim);

        // 讀取synthesis後的position data
        // TODO: initialize the synthesis joint points array
        SynthesisJointsPos = new handLMsController.JointBone[readPositionsJointsNum];
        for (int i=0; i < readPositionsJointsNum; ++i) { SynthesisJointsPos[i] = new handLMsController.JointBone(); }
        jsonDeserializer jsonDeserializer = new jsonDeserializer();
        readInHumanPositionFile = "jsonPositionData/afterSynthesis/leftFrontKick.json";
        readInHumanPositionResult = jsonDeserializer.readAndParseRotation(
            readInHumanPositionFile
            );
        // For test 讀取原本從Unity output的DB position
        //readInHumanPositionFile = "jsonPositionData/bodyMotionPosition/leftFrontKickPositionFullJoints.json";
        //string jsonText = File.ReadAllText(Path.Combine(Application.dataPath, readInHumanPositionFile));
        //readInHumanPositionResult = JsonUtility.FromJson<MediaPipeResult>(jsonText);
        // For test end
        // visualize synthesis後的position data
        if (isReadApplyHumanPositions)
        {
            StartCoroutine(updateSynthesisPosition());
        }

        if (isRecordHumanMotion)
        {
            StartCoroutine(rotationRecorder());
        }
        //drawSkeletonOnce();
        if(isRecordHumanPosition)
        {
            StartCoroutine(positionRecorder());
        }
        if (isRecordMultiHumanPositions)
        {
            StartCoroutine(multiplePositionsRecorder());
        }
    }

    // Update is called once per frame
    void Update()
    {
        //vNectModel.PoseUpdate(); // Pos3D有改變才會有動作
        drawSkeletonOnce(); // Draw the skeleton movement from avatar to another skeletal object
        //print(tmpTrans.localEulerAngles);
    }

    private void OnAnimatorIK(int layerIndex)
    {

        if (isRotationFromHand)
        {
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightShoulder, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightUpperArm, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightLowerArm, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightHand, Quaternion.Euler(new Vector3()));

            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftShoulder, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftUpperArm, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftLowerArm, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftHand, Quaternion.Euler(new Vector3()));

            //avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightUpperLeg, Quaternion.Euler(new Vector3()));
            //avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightLowerLeg, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightFoot, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightToes, Quaternion.Euler(new Vector3()));

            //avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftUpperLeg, Quaternion.Euler(new Vector3()));
            //avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftLowerLeg, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftFoot, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftToes, Quaternion.Euler(new Vector3()));

            // update rotation by rotation from other source(e.g. hand landmark)
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftUpperLeg, jointPoints[PositionIndex.lThighBend.Int()].InitRotation);
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftLowerLeg, jointPoints[PositionIndex.lShin.Int()].InitRotation);
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightUpperLeg, jointPoints[PositionIndex.rThighBend.Int()].InitRotation);
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightLowerLeg, jointPoints[PositionIndex.rShin.Int()].InitRotation);
        }
        // 更新讀取的synthesis human positions到human avatar身上
        if(isReadApplyHumanPositions)
        {
            
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightShoulder, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightUpperArm, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightLowerArm, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightHand, Quaternion.Euler(new Vector3()));

            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftShoulder, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftUpperArm, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftLowerArm, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftHand, Quaternion.Euler(new Vector3()));

            //avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightUpperLeg, Quaternion.Euler(new Vector3()));
            //avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightLowerLeg, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightFoot, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightToes, Quaternion.Euler(new Vector3()));

            //avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftUpperLeg, Quaternion.Euler(new Vector3()));
            //avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftLowerLeg, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftFoot, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftToes, Quaternion.Euler(new Vector3()));

            print("synthesis position: " + (SynthesisJointsPos[2].Pos3D + originHipPosition).ToString());
            //設定IK goal position的方法
            avatarAnim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1.0f);
            avatarAnim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1.0f);
            avatarAnim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);
            avatarAnim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1.0f);
            avatarAnim.SetIKPosition(AvatarIKGoal.LeftFoot, SynthesisJointsPos[2].Pos3D + originHipPosition);
            avatarAnim.SetIKPosition(AvatarIKGoal.RightFoot, SynthesisJointsPos[5].Pos3D + originHipPosition);
            avatarAnim.SetIKPosition(AvatarIKGoal.LeftHand, SynthesisJointsPos[11].Pos3D + originHipPosition);
            avatarAnim.SetIKPosition(AvatarIKGoal.RightHand, SynthesisJointsPos[14].Pos3D + originHipPosition);
        }
    }

    public void drawSkeletonOnce()
    {
        foreach (var sk in vNectModel.Skeletons)
        {
            var s = sk.start;
            var e = sk.end;

            sk.Line.SetPosition(0, new Vector3(s.Transform.position.x * vNectModel.SkeletonScale + vNectModel.SkeletonX, s.Transform.position.y * vNectModel.SkeletonScale + vNectModel.SkeletonY, s.Transform.position.z * vNectModel.SkeletonScale + vNectModel.SkeletonZ));
            sk.Line.SetPosition(1, new Vector3(e.Transform.position.x * vNectModel.SkeletonScale + vNectModel.SkeletonX, e.Transform.position.y * vNectModel.SkeletonScale + vNectModel.SkeletonY, e.Transform.position.z * vNectModel.SkeletonScale + vNectModel.SkeletonZ));
        }
    }
}
