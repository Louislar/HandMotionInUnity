using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class handRotationController : MonoBehaviour
{
    public avatarController avatarController;
    public jsonDeserializer jsonDeserializer;
    private MediaPipeResult rotationResult;
    private handLMsController.JointBone[] jointBones;

    public bool updateRotationToAvatar;

    /// <summary>
    /// 更新手指的旋轉角度到avatar身上
    /// </summary>
    public void updateBodyJointRotation()
    {
        avatarController.updateRotation(
            jointBones[1].flexionRotation, jointBones[3].flexionRotation,
            jointBones[0].flexionRotation, jointBones[0].abductionRotation, 
            jointBones[2].flexionRotation, jointBones[2].abductionRotation
            );
    }

    /// <summary>
    /// 更新joint旋轉角度，使用讀取的角度資訊
    /// </summary>
    /// <param name="newRots"></param>
    public void updateJointRotations(MediaPipeHandLMs newRots)
    {
        for (int i = 0; i < newRots.data.Count; ++i)
        {
            jointBones[i].flexionRotation = newRots.data[i].x;
            jointBones[i].abductionRotation = newRots.data[i].z;
        }
            
    }

    IEnumerator updateRotationOnce()
    {
        int curIndex = 0;
        while (curIndex < rotationResult.results.Length)
        {
            updateJointRotations(rotationResult.results[curIndex]);
            ++curIndex;
            yield return new WaitForSeconds(0.05f);
        }
        yield return null;
    }


    // Start is called before the first frame update
    void Start()
    {
        //rotationResult = jsonDeserializer.readAndParseRotation("jsonRotationData/handRotationAfterMapping/leftFrontKick.json");
        rotationResult = jsonDeserializer.readAndParseRotation(
            "jsonRotationData/handRotationAfterMapping/leftFrontKickCombinations/leftFrontKick(True, False, True, False, False, False).json"
            );
        jointBones = new handLMsController.JointBone[4];
        for (var i = 0; i < 4; i++) jointBones[i] = new handLMsController.JointBone();
        StartCoroutine(updateRotationOnce());
    }

    // Update is called once per frame
    void Update()
    {
        if(updateRotationToAvatar)
        {
            updateBodyJointRotation();
        }
    }
}
