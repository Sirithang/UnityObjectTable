using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "testScriptable", menuName = "TestData/TestScriptable")]
public class TestScriptable : ScriptableObject
{
    public int intValue;
    public float floatValue;
    public GameObject objectReference;
    public Object assetReference;
    public Vector3 vector3Value;
    public AnimationCurve curveValue;
}
