using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTest : MonoBehaviour
{
    public int hp;
    public int attack;
    public int level;
    public Color tint;
    public TestScriptable scriptableObjectReference;
    [Range(0,360.0f)]
    public float viewAngle;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
