using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "secondTest", menuName = "TestData/SecondTest")]
public class SecondTest : ScriptableObject
{
    public Color someColor;
    public Mesh someMesh;
    public Material someMaterial;
    public string someString;
    public int[] arrayThatDontWork;
}
