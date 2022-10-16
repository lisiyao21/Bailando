using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
public class BoneController : MonoBehaviour
{
	[SerializeField] Animator animator;
	[SerializeField, Range(10, 120)] float FrameRate;
	[SerializeField] string Data_Path;
	[SerializeField] string File_Name;
	[SerializeField] int Data_Size;
	public List<Transform> BoneList = new List<Transform>();
	Vector3[] points = new Vector3[17];
	Vector3[] NormalizeBone = new Vector3[12];
	Quaternion[] init_rot;
	Vector3 init_position; 
    Quaternion[] init_inv; //Inverse
	int[] bones = new int[10] { 1, 2, 4, 5, 7, 8, 11, 12, 14, 15 }; 
    int[] child_bones = new int[10] { 2, 3, 5, 6, 8, 10, 12, 13, 15, 16 }; // bones
	int bone_num = 19;
	float scale_ratio = 0.001f;
    float heal_position = 0.05f;
    float head_angle = 0f;
	public int startFrame;


	float Timer;
	int[,] joints = new int[,]
	{ { 0, 1 }, { 1, 2 }, { 2, 3 }, { 0, 4 }, { 4, 5 }, { 5, 6 }, { 0, 7 }, { 7, 8 }, { 8, 9 }, { 9, 10 }, { 8, 11 }, { 11, 12 }, { 12, 13 }, { 8, 14 }, { 14, 15 }, { 15, 16 }
	};
	int[,] BoneJoint = new int[,]
	{ { 0, 2 }, { 2, 3 }, { 0, 5 }, { 5, 6 }, { 0, 7 }, { 7, 8 }, { 8, 9 }, { 9, 10 }, { 9, 12 }, { 12, 13 }, { 9, 15 }, { 15, 16 }
	};
	int NowFrame = 0;
	void Start()
	{
		GetBones();
		PointUpdate();
	}

	void Update()
	{
		PointUpdateByTime();
		SetBoneRot();
	}
	void GetBones()
	{
        init_rot = new Quaternion[bone_num];
        init_inv = new Quaternion[bone_num];

		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.Hips));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftFoot));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightLowerLeg));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightFoot));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.Spine));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.Chest));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.Neck));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.Head));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightUpperArm));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightLowerArm));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightHand));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftUpperArm));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftLowerArm));
		BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftHand));

		Vector3 init_forward = TriangleNormal(points[7],points[4],points[1]);
		init_inv[0] = Quaternion.Inverse(Quaternion.LookRotation(init_forward));

        init_position = BoneList[0].position;
        init_rot[0] = BoneList[0].rotation;
        for (int i = 0; i < bones.Length; i++) {
            int b = bones[i];
            int cb = child_bones[i];
        
           
            init_rot[b] = BoneList[b].rotation;
            
            init_inv[b] = Quaternion.Inverse(Quaternion.LookRotation(BoneList[b].position - BoneList[cb].position,init_forward));
            // Debug.Log($"{init_rot[b]},{init_inv[b]}");
		}
	}
	void PointUpdate()
	{
		if (NowFrame < Data_Size)
		{
			StreamReader fi = new StreamReader(Application.dataPath + Data_Path + File_Name + (NowFrame + startFrame).ToString() + ".txt");
			NowFrame++;
			string all = fi.ReadToEnd();
			if (all != "0")
			{
				string[] axis = all.Split(']');
				float[] x = axis[0].Replace("[", "").Replace(" ", "").Split(',').Where(s => s != "").Select(f => float.Parse(f)).ToArray();
				float[] y = axis[1].Replace("[", "").Replace(" ", "").Split(',').Where(s => s != "").Select(f => float.Parse(f)).ToArray();
				float[] z = axis[2].Replace("[", "").Replace(" ", "").Split(',').Where(s => s != "").Select(f => float.Parse(f)).ToArray();
				// float[] x = axis[0].Replace("[", "").Replace("\r\n", "").Replace("\n", "").Split(' ').Where(s => s != "").Select(f => float.Parse(f)).ToArray();
				// float[] y = axis[2].Replace("[", "").Replace("\r\n", "").Replace("\n", "").Split(' ').Where(s => s != "").Select(f => float.Parse(f)).ToArray();
				// float[] z = axis[1].Replace("[", "").Replace("\r\n", "").Replace("\n", "").Split(' ').Where(s => s != "").Select(f => float.Parse(f)).ToArray();
				
				for (int i = 0; i < 17; i++)
				{
					points[i] = new Vector3(x[i], y[i], z[i]);
				}
				for (int i = 0; i < 12; i++)
				{
					NormalizeBone[i] = (points[BoneJoint[i, 1]] - points[BoneJoint[i, 0]]).normalized;
				}
			}
			else
			{
				Debug.Log("All Data 0");
			}
		}
	}
	void PointUpdateByTime()
	{
		Timer += Time.deltaTime;
		if (Timer > (1 / FrameRate))
		{
			Timer = 0;
			PointUpdate();
		}
	}
	Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 d1 = a - b;
        Vector3 d2 = a - c;

        Vector3 dd = Vector3.Cross(d1, d2);
        dd.Normalize();

        return dd;
    }
	
	static Vector3 ThoraxCalc(Vector3 a1, Vector3 b1)
	{		
	Vector3 t = (a1-b1)/2;
	return t;
	}
	
	static Vector3 SpineCalc(Vector3 a2, Vector3 b2, Vector3 c2)
	{
		Vector3 s2 = (b2-c2)/2;
	Vector3 s=(a2-s2)/2;
	return s;
	}

	void SetBoneRot()
	{
		Vector3[] now_pos = points;

        Vector3 pos_forward = TriangleNormal(now_pos[7], now_pos[4], now_pos[1]);
		// 캐릭터의 위치를 업데이트
        BoneList[0].position = now_pos[0] * scale_ratio + new Vector3(init_position.x, heal_position, init_position.z);
        BoneList[0].rotation = Quaternion.LookRotation(pos_forward) * init_inv[0] * init_rot[0];

		Vector3 tmp = new Vector3(-1, 0, 0);
        for (int i = 0; i < bones.Length; i++) {
            int b = bones[i];
            int cb = child_bones[i];
            // Debug.Log($"{i},{b},{cb}");
			Debug.Log($"{BoneList[b].rotation = (Quaternion.LookRotation(now_pos[b] - now_pos[cb], pos_forward) * init_inv[b] * init_rot[b])}");
			if (i >= 0 && i <= 3){
				BoneList[b].Rotate(180,0,180);
			}
        }

        BoneList[8].rotation = Quaternion.AngleAxis(head_angle, BoneList[11].position - BoneList[14].position) * BoneList[8].rotation;
		
		for (int i = 0; i < 16; i++)
		{
			DrawLine(points[joints[i, 0]] * 0.001f + new Vector3(-1, 0.8f, 0), points[joints[i, 1]] * 0.001f + new Vector3(-1, 0.8f, 0), Color.blue);
			DrawRay(points[joints[i, 0]] * 0.001f + new Vector3(-1, 0.8f, 0), BoneList[i].right * 0.01f, Color.magenta);
			DrawRay(points[joints[i, 0]] * 0.001f + new Vector3(-1, 0.8f, 0), BoneList[i].forward * 0.01f, Color.green);
			DrawRay(points[joints[i, 0]] * 0.001f + new Vector3(-1, 0.8f, 0), BoneList[i].up * 0.01f, Color.cyan);
		}
		for (int i = 0; i < 12; i++)
		{
			DrawRay(points[BoneJoint[i, 0]] * 0.001f + new Vector3(1, 0.8f, 0), NormalizeBone[i] * 0.1f, Color.green);
		}
	}
	void DrawLine(Vector3 s, Vector3 e, Color c)
	{
		Debug.DrawLine(s, e, c);
	}
	void DrawRay(Vector3 s, Vector3 d, Color c)
	{
		Debug.DrawRay(s, d, c);
	}
}
enum PointsNum
{
	Hips,
	RightUpperLeg,
	RightLowerLeg,
	RightFoot,
	LeftUpperLeg,
	LeftLowerLeg,
	LeftFoot,
	Spine,
	Chest,
	Neck,
	Head,
	LeftUpperArm,
	LeftLowerArm,
	LeftHand,
	RightUpperArm,
	RightLowerArm,
	RightHand
}