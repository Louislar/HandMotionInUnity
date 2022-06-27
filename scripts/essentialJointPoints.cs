using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum essentialJointPoints : int
{
    Hip = 0,
    Spine,
    Chest,
    UpperChest,
    Head,

    lShoulder,
    lUpperArm,
    lLowerArm,
    lHand,

    rShoulder,
    rUpperArm,
    rLowerArm,
    rHand,

    lUpperLeg,
    lLowerLeg,
    lFoot,
    lToe,

    rUpperLeg,
    rLowerLeg,
    rFoot,
    rToe,

    Count
}

public static partial class ExtensionEnum
{
    public static int Int(this essentialJointPoints i)
    {
        return (int)i;
    }

    public static int ToHumanBodyBones(this essentialJointPoints i)
    {
        Dictionary<int, int> mappingDict = new Dictionary<int, int>()
        {
            {0,0},
            {1,7},
            {2, 8 },
            {3, 54 },
            {4, 10 },
            {5, 11 },
            {6, 13 },
            {7, 15 },
            {8, 17 },
            {9, 12 },
            {10, 14 },
            {11, 16 },
            {12, 18 },
            {13,  1},
            {14,  3},
            {15,  5},
            {16,  19},
            {17,  2},
            {18,  4},
            {19,  6},
            {20,  20}
        };
        return mappingDict[(int)i];
    }
}
