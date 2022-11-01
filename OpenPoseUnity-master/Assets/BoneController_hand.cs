using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
public class BoneController_hand : MonoBehaviour
{
	[SerializeField] Animator animator;
	[SerializeField, Range(10, 120)] float FrameRate;
	[SerializeField] string Data_Path;
	[SerializeField] string File_Name;
	[SerializeField] int Data_Size;
	public List<Transform> BoneList = new List<Transform>();
	Vector3[] points = new Vector3[38]; // BoneList의 길이와 동일
	Vector3[] NormalizeBone = new Vector3[12]; // 이게 머 ?
	Quaternion[] init_rot;
	Vector3 init_position;
	Quaternion[] init_inv; //Inverse
						   // int[] bones = new int[10] { 1, 2, 4, 5, 7, 8, 11, 12, 14, 15 }; // 여기 수정
						   // int[] child_bones = new int[10] { 2, 3, 5, 6, 8, 10, 12, 13, 15, 16 }; // bones 여기도 수정


	//int[] bones = new int[34] { 0,1, 2, 3, 4, 2, 6, 7, 2, 9, 10, 2, 12, 13, 2, 15, 16, 18, 19, 20, 21, 22, 20, 24, 25, 20, 27, 28, 20, 30, 31, 20, 33, 34 };
	int [] bones = new int[34] {1, 2, 3, 4, 5, 3, 7, 8, 3, 10, 11, 3, 13, 14, 3, 16, 17, 19, 20, 21, 22, 23, 21, 25, 26, 21, 28, 29, 21, 31, 32, 21, 34, 35};

	//int[] child_bones = new int[34] { 1,2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35 };
	int[] child_bones = new int[34] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36 };
	// int[] bones = new int[10] {17, 18, 20, 21, 23, 24, 26, 27, 29, 30};
	// int[] child_bones = new int[10] {18, 19, 21, 22, 24, 25, 27, 28, 30, 31};

	// int[] bones = new int[15] {17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31};
	// int[] child_bones = new int[15] {17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31};


	int bone_num = 39;
	float scale_ratio = 0.01f; // 어따 씀 ?
	float heal_position = 0.05f; // 어따 씀 ?
	float head_angle = 15f; // 어따 씀 ? 


	float Timer;
	int[,] joints = new int[,] // 이건 머 ?
	{ { 0, 1 }, { 1, 2 }, { 2, 3 }, { 0, 4 }, { 4, 5 }, { 5, 6 }, { 0, 7 }, { 7, 8 }, { 8, 9 }, { 9, 10 }, { 8, 11 }, { 11, 12 }, { 12, 13 }, { 8, 14 }, { 14, 15 }, { 15, 16 }
	};
	int[,] BoneJoint = new int[,] // 이건 머 ? 
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

        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.Hips));
        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg));
        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg));

        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftFoot));
        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg));
        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightLowerLeg));

        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightFoot));
        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.Spine));
        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.Chest));

        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.Neck));
        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.Head));
        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightUpperArm));

        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightLowerArm));
        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightHand));
        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftUpperArm));

        // BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftLowerArm));

        // 1028 backup
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.Chest));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftUpperArm));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftLowerArm));

        //
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftHand));
        //
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal));
        //
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal));
        //
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal));
        //
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftRingProximal));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftRingDistal));
        //
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal));

        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightUpperArm));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightLowerArm));

        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightHand));
        //
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightThumbProximal));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightThumbDistal));
        //
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightIndexProximal));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightIndexDistal));
        //
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal));
        //
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightRingProximal));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightRingDistal));
        //
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightLittleProximal));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.RightLittleDistal));
        BoneList.Add(animator.GetBoneTransform(HumanBodyBones.Head));

        // a - b, a - c 벡터들의 normalized된 법선벡터 (points[1], poinsts[19], points[37]의 의미는?)
        // 캐릭터가 매 프레임마다 이상적으로 바라보아야 할 정면에 대한 방향벡터
        Vector3 init_forward = TriangleNormal(points[1], points[19], points[37]);
		
		init_inv[0] = Quaternion.Inverse(Quaternion.LookRotation(init_forward)); // forward, Vector3.up (0, 1, 0)

		init_position = BoneList[0].position; // 가슴 position
		init_rot[0] = BoneList[0].rotation; // 가슴 rotation

		for (int i = 0; i < bones.Length; i++)
		{
			int b = bones[i];
			int cb = child_bones[i];

			// 각 관절별 계산
			init_rot[b] = BoneList[b].rotation;

			// Vector3 forward direction과 Vector3 upward direction 사이의 inversed rotation
			// BoneList[b].position - BoneList[cb].position : forward direction (정방향)
			// init_forward : upward direction (위쪽 방향)
			// quaternion : x, y, z, w(scalar)
			init_inv[b] = Quaternion.Inverse(Quaternion.LookRotation(BoneList[b].position - BoneList[cb].position, init_forward));
			Debug.Log($"{init_rot[b]},{init_inv[b]}"); // Rotation, inverse이 quaternion으로 계산됨
		}
	}
	void PointUpdate() // 매 txt 파일을 읽고 -> 각 point별 x, y, z 좌표 구하기 
	{
		if (NowFrame < Data_Size)
		{
			StreamReader fi = new StreamReader(Application.dataPath + Data_Path + File_Name + NowFrame.ToString() + ".txt");
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
                //List<Vector3> num = new List<Vector3>();
                //Vector3 position = new Vector3(NowFrame, NowFrame, NowFrame);
                for (int i = 1; i < 38; i++) // 38 -> 18 (왼팔만)
                {
                    // txt 파일 읽으면서 points 좌표 추가
                    points[i] = new Vector3(x[i], y[i], z[i]);

                }
                //num.Add(points[4]); // LeftHandThumb1 이라는데 ...?
                //num.Add(position);
                //Debug.Log(num[0]);
                //Debug.Log(num[1]);
                //Debug.Log(num.Count);

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
		Vector3 t = (a1 - b1) / 2;
		return t;
	}

	static Vector3 SpineCalc(Vector3 a2, Vector3 b2, Vector3 c2)
	{
		Vector3 s2 = (b2 - c2) / 2;
		Vector3 s = (a2 - s2) / 2;
		return s;
	}

	void SetBoneRot()
	{
		Vector3[] now_pos = points;
		// 7,4,1 points[0],points[18],points[36]
		Vector3 pos_forward = TriangleNormal(now_pos[1], now_pos[19], now_pos[37]);
		BoneList[0].position = now_pos[0] * scale_ratio + new Vector3(init_position.x, heal_position, init_position.z);
		BoneList[0].rotation = Quaternion.LookRotation(pos_forward) * init_inv[0] * init_rot[0];
		Debug.Log("확인 BoneList[0]의 position과 rotation");
		Debug.Log(BoneList[0].position);
		Debug.Log(BoneList[0].rotation);
		for (int i = 0; i < bones.Length; i++)
		{
			int b = bones[i];
			int cb = child_bones[i];
			Debug.Log($"{i},{b},{cb}");
			Debug.Log($"{BoneList[b].rotation = Quaternion.LookRotation(now_pos[b] - now_pos[cb], pos_forward) * init_inv[b] * init_rot[b]}");
		}

		BoneList[0].rotation = Quaternion.AngleAxis(head_angle, BoneList[19].position - BoneList[37].position) * BoneList[0].rotation;

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
		//Debug.DrawLine(s, e, c);
	}
	void DrawRay(Vector3 s, Vector3 d, Color c)
	{
		//Debug.DrawRay(s, d, c);
	}
}
enum PointsNum_hand
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