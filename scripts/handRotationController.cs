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

    // �h��mapping��rotation���G
    private List<string> multipleRotResultsFileNames;
    private List<MediaPipeResult> mappedRotResults;


    /// <summary>
    /// ��s��������ਤ�ר�avatar���W
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
    /// ��sjoint���ਤ�סA�ϥ�Ū�������׸�T
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
    /// �̧�apply/����h��mapped rotations���W�A
    /// �Ω�ݭnrecord�h��mapping function��position���G�A
    /// �קK�ݭn���s�h��mapping functions�ɻݭn���ƶ}�l�P����
    /// </summary>
    /// <returns></returns>
    IEnumerator updateMultipleJointsRotationsSequential()
    {

        yield return null;
    }

    /// <summary>
    /// ����True�PFalse�ƦC�զX��string�ǦC
    /// e.g. True, False, True
    /// </summary>
    /// <param name="size">���ͪ��ǦC����</param>
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
        // �����̫᪺", "�A�åB�[�W�A��
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
        // TODO: auto-generate�Ҧ�true false�զX���ɦW�A�̫�Ҧ��ɦW�A�ɤW".json"
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
