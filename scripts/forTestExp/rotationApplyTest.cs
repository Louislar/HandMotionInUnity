using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotationApplyTest : MonoBehaviour
{
    public enum RotationApplyType:int
    {
        noApply=0,
        animator, 
        gameObject, 
        humanPose
    }
    public Animator avatarAnim;
    public List<GameObject> avatarJointsGO;
    public RotationApplyType rotationApplyType;
    public Vector3 curRotation;
    public float rotationsIncreaseSpeed;


    private void OnAnimatorIK(int layerIndex)
    {
        if (rotationApplyType==RotationApplyType.animator)
        {
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightShoulder, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightUpperArm, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightLowerArm, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightHand, Quaternion.Euler(new Vector3()));

            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftShoulder, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftUpperArm, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftLowerArm, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftHand, Quaternion.Euler(new Vector3()));

            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightUpperLeg, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightLowerLeg, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightFoot, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.RightToes, Quaternion.Euler(new Vector3()));

            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftUpperLeg, Quaternion.Euler(curRotation));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftLowerLeg, Quaternion.Euler(curRotation));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftFoot, Quaternion.Euler(new Vector3()));
            avatarAnim.SetBoneLocalRotation(HumanBodyBones.LeftToes, Quaternion.Euler(new Vector3()));
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        curRotation = new Vector3();
    }

    // Update is called once per frame
    void Update()
    {
        curRotation.x += rotationsIncreaseSpeed;
    }
}
