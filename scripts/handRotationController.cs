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

    // 多種mapping的rotation結果
    private List<string> multipleRotResultsFileNames;
    private List<MediaPipeResult> mappedRotResults;


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

    /// <summary>
    /// 依序apply/播放多個mapped rotations到手上，
    /// 用於需要record多種mapping function的position結果，
    /// 避免需要錄製多種mapping functions時需要重複開始與結束
    /// </summary>
    /// <returns></returns>
    IEnumerator updateMultipleJointsRotationsSequential()
    {

        yield return null;
    }

    /// <summary>
    /// 產生True與False排列組合的string序列
    /// e.g. True, False, True
    /// </summary>
    /// <param name="size">產生的序列長度</param>
    /// <returns></returns>
    public List<string> boolPermutation(int size=1)
    {
        //float totalCount = Mathf.Pow(2, size);
        List<string> permutationList = new List<string>();
        for(int i=0;i<size;++i)
        {
            if (permutationList.Count == 0)
            {
                permutationList.Add("True, ");
                permutationList.Add("False, ");
                continue;
            }
            int curLength = permutationList.Count;
            List<string> newPermutationList = new List<string>();
            for (int j=0;j<curLength;++j)
            {
                newPermutationList.Add(permutationList[j] + "True, ");
                newPermutationList.Add(permutationList[j] + "False, ");
            }
            permutationList = newPermutationList;
        }
        // 消除最後的", "，並且加上括號
        for (int i = 0; i < permutationList.Count; ++i) permutationList[i] = permutationList[i].Substring(0, permutationList[i].Length - 2);
        for (int i = 0; i < permutationList.Count; ++i) permutationList[i] = "("+permutationList[i]+")";
        //foreach (string str in permutationList) print(str);
        return permutationList;
    }


    // Start is called before the first frame update
    void Start()
    {
        //rotationResult = jsonDeserializer.readAndParseRotation("jsonRotationData/handRotationAfterMapping/leftFrontKick.json");
        rotationResult = jsonDeserializer.readAndParseRotation(
            //"jsonRotationData/handRotationAfterMapping/leftFrontKickCombinations/leftFrontKick(True, False, True, False, False, False).json"
            //"jsonRotationData/handRotationAfterMapping/leftFrontKickCombinations/leftFrontKick(True, False, True, True, True, True).json"
            "jsonRotationData/handRotationAfterMapping/leftFrontKickCombinations/leftFrontKick(True, True, True, True, True, True).json"
            );
        // Read multiple mapping rotation result
        List<string> boolPermutationStrings = boolPermutation(6);
        string rootFileName = "jsonRotationData/handRotationAfterMapping/leftFrontKickCombinations/leftFrontKick";
        // TODO: auto-generate所有true false組合的檔名，最後所有檔名再補上".json"
        multipleRotResultsFileNames = new List<string> {
            "jsonRotationData/handRotationAfterMapping/leftFrontKickCombinations/leftFrontKick(True, True, True, True, True, True).json",
            "jsonRotationData/handRotationAfterMapping/leftFrontKickCombinations/leftFrontKick(True, False, True, True, True, True).json",
            "jsonRotationData/handRotationAfterMapping/leftFrontKickCombinations/leftFrontKick(True, True, True, True, True, True).json"
        };
        foreach (string str in multipleRotResultsFileNames) print(str);
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
