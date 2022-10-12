// Copyright (c) 2016 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using UnityEngine;

namespace SA
{

	public partial class FullBodyIK
	{
		public enum EyesType
		{
			Normal,
			LegacyMove,
		}

		public enum Side
		{
			Left,
			Right,
			Max,
			None = Max,
		}

		public enum LimbIKType
		{
			Leg,
			Arm,
			Max,
			Unknown = Max,
		}

		public enum LimbIKLocation
		{
			LeftLeg,
			RightLeg,
			LeftArm,
			RightArm,
			Max,
			Unknown = Max,
		}

		public static LimbIKType ToLimbIKType( LimbIKLocation limbIKLocation )
		{
			switch( limbIKLocation ) {
			case LimbIKLocation.LeftLeg:	return LimbIKType.Leg;
			case LimbIKLocation.RightLeg:	return LimbIKType.Leg;
			case LimbIKLocation.LeftArm:	return LimbIKType.Arm;
			case LimbIKLocation.RightArm:	return LimbIKType.Arm;
			}

			return LimbIKType.Unknown;
		}

		public static Side ToLimbIKSide( LimbIKLocation limbIKLocation )
		{
			switch( limbIKLocation ) {
			case LimbIKLocation.LeftLeg:	return Side.Left;
			case LimbIKLocation.RightLeg:	return Side.Right;
			case LimbIKLocation.LeftArm:	return Side.Left;
			case LimbIKLocation.RightArm:	return Side.Right;
			}

			return Side.None;
		}

		public enum FingerIKType
		{
			LeftWrist,
			RightWrist,
			Max,
			None = Max,
		}

		public enum BoneType
		{
			Hips,
			Spine,
			Neck,
			Head,
			Eye,

			Leg,
			Knee,
			Foot,

			Shoulder,
			Arm,
			ArmRoll,
			Elbow,
			ElbowRoll,
			Wrist,

			HandFinger,

			Max,
			Unknown = Max,
		}

		public enum BoneLocation
		{
			Hips,
			Spine,
			Spine2,
			Spine3,
			Spine4,
			Neck,
			Head,
			LeftEye,
			RightEye,

			LeftLeg,
			RightLeg,
			LeftKnee,
			RightKnee,
			LeftFoot,
			RightFoot,

			LeftShoulder,
			RightShoulder,
			LeftArm,
			RightArm,
			LeftArmRoll0,
			LeftArmRoll1,
			LeftArmRoll2,
			LeftArmRoll3,
			RightArmRoll0,
			RightArmRoll1,
			RightArmRoll2,
			RightArmRoll3,
			LeftElbow,
			RightElbow,
			LeftElbowRoll0,
			LeftElbowRoll1,
			LeftElbowRoll2,
			LeftElbowRoll3,
			RightElbowRoll0,
			RightElbowRoll1,
			RightElbowRoll2,
			RightElbowRoll3,
			LeftWrist,
			RightWrist,

			LeftHandThumb0,
			LeftHandThumb1,
			LeftHandThumb2,
			LeftHandThumbTip,
			LeftHandIndex0,
			LeftHandIndex1,
			LeftHandIndex2,
			LeftHandIndexTip,
			LeftHandMiddle0,
			LeftHandMiddle1,
			LeftHandMiddle2,
			LeftHandMiddleTip,
			LeftHandRing0,
			LeftHandRing1,
			LeftHandRing2,
			LeftHandRingTip,
			LeftHandLittle0,
			LeftHandLittle1,
			LeftHandLittle2,
			LeftHandLittleTip,

			RightHandThumb0,
			RightHandThumb1,
			RightHandThumb2,
			RightHandThumbTip,
			RightHandIndex0,
			RightHandIndex1,
			RightHandIndex2,
			RightHandIndexTip,
			RightHandMiddle0,
			RightHandMiddle1,
			RightHandMiddle2,
			RightHandMiddleTip,
			RightHandRing0,
			RightHandRing1,
			RightHandRing2,
			RightHandRingTip,
			RightHandLittle0,
			RightHandLittle1,
			RightHandLittle2,
			RightHandLittleTip,

			Max,
			Unknown = Max,
			SpineU = Spine4,
		}

		public const int MaxArmRollLength = 4;
		public const int MaxElbowRollLength = 4;
		public const int MaxHandFingerLength = 4;

		public static BoneType ToBoneType( BoneLocation boneLocation )
		{
			switch( boneLocation ) {
			case BoneLocation.Hips:				return BoneType.Hips;
			case BoneLocation.Neck:				return BoneType.Neck;
			case BoneLocation.Head:				return BoneType.Head;
			case BoneLocation.LeftEye:			return BoneType.Eye;
			case BoneLocation.RightEye:			return BoneType.Eye;

			case BoneLocation.LeftLeg:			return BoneType.Leg;
			case BoneLocation.RightLeg:			return BoneType.Leg;
			case BoneLocation.LeftKnee:			return BoneType.Knee;
			case BoneLocation.RightKnee:		return BoneType.Knee;
			case BoneLocation.LeftFoot:			return BoneType.Foot;
			case BoneLocation.RightFoot:		return BoneType.Foot;

			case BoneLocation.LeftShoulder:		return BoneType.Shoulder;
			case BoneLocation.RightShoulder:	return BoneType.Shoulder;
			case BoneLocation.LeftArm:			return BoneType.Arm;
			case BoneLocation.RightArm:			return BoneType.Arm;
			case BoneLocation.LeftElbow:		return BoneType.Elbow;
			case BoneLocation.RightElbow:		return BoneType.Elbow;
			case BoneLocation.LeftWrist:		return BoneType.Wrist;
			case BoneLocation.RightWrist:		return BoneType.Wrist;
			}

			if( (int)boneLocation >= (int)BoneLocation.Spine &&
				(int)boneLocation <= (int)BoneLocation.SpineU ) {
				return BoneType.Spine;
			}

			if( (int)boneLocation >= (int)BoneLocation.LeftArmRoll0 &&
				(int)boneLocation <= (int)BoneLocation.RightArmRoll0 + MaxArmRollLength - 1 ) {
				return BoneType.ArmRoll;
			}

			if( (int)boneLocation >= (int)BoneLocation.LeftElbowRoll0 &&
				(int)boneLocation <= (int)BoneLocation.RightElbowRoll0 + MaxElbowRollLength - 1 ) {
				return BoneType.ElbowRoll;
			}

			if( (int)boneLocation >= (int)BoneLocation.LeftHandThumb0 &&
				(int)boneLocation <= (int)BoneLocation.RightHandLittleTip ) {
				return BoneType.HandFinger;
			}

			return BoneType.Unknown;
		}

		public static Side ToBoneSide( BoneLocation boneLocation )
		{
			switch( boneLocation ) {
			case BoneLocation.LeftEye:			return Side.Left;
			case BoneLocation.RightEye:			return Side.Right;

			case BoneLocation.LeftLeg:			return Side.Left;
			case BoneLocation.RightLeg:			return Side.Right;
			case BoneLocation.LeftKnee:			return Side.Left;
			case BoneLocation.RightKnee:		return Side.Right;
			case BoneLocation.LeftFoot:			return Side.Left;
			case BoneLocation.RightFoot:		return Side.Right;

			case BoneLocation.LeftShoulder:		return Side.Left;
			case BoneLocation.RightShoulder:	return Side.Right;
			case BoneLocation.LeftArm:			return Side.Left;
			case BoneLocation.RightArm:			return Side.Right;
			case BoneLocation.LeftElbow:		return Side.Left;
			case BoneLocation.RightElbow:		return Side.Right;
			case BoneLocation.LeftWrist:		return Side.Left;
			case BoneLocation.RightWrist:		return Side.Right;
			}

			if( (int)boneLocation >= (int)BoneLocation.LeftHandThumb0 &&
				(int)boneLocation <= (int)BoneLocation.LeftHandLittleTip ) {
				return Side.Left;
			}

			if( (int)boneLocation >= (int)BoneLocation.LeftArmRoll0 &&
				(int)boneLocation <= (int)BoneLocation.LeftArmRoll0 + MaxArmRollLength - 1 ) {
				return Side.Left;
			}

			if( (int)boneLocation >= (int)BoneLocation.RightArmRoll0 &&
				(int)boneLocation <= (int)BoneLocation.RightArmRoll0 + MaxArmRollLength - 1 ) {
				return Side.Right;
			}

			if( (int)boneLocation >= (int)BoneLocation.LeftElbowRoll0 &&
				(int)boneLocation <= (int)BoneLocation.LeftElbowRoll0 + MaxElbowRollLength - 1 ) {
				return Side.Left;
			}

			if( (int)boneLocation >= (int)BoneLocation.RightElbowRoll0 &&
				(int)boneLocation <= (int)BoneLocation.RightElbowRoll0 + MaxElbowRollLength - 1 ) {
				return Side.Right;
			}

			if( (int)boneLocation >= (int)BoneLocation.RightHandThumb0 &&
				(int)boneLocation <= (int)BoneLocation.RightHandLittleTip ) {
				return Side.Right;
			}

			return Side.None;
		}

		public static FingerType ToFingerType( BoneLocation boneLocation )
		{
			if( (int)boneLocation >= (int)BoneLocation.LeftHandThumb0 &&
				(int)boneLocation <= (int)BoneLocation.LeftHandLittleTip ) {
				return (FingerType)(((int)boneLocation - (int)BoneLocation.LeftHandThumb0) / MaxHandFingerLength);
			}

			if( (int)boneLocation >= (int)BoneLocation.RightHandThumb0 &&
				(int)boneLocation <= (int)BoneLocation.RightHandLittleTip ) {
				return (FingerType)(((int)boneLocation - (int)BoneLocation.RightHandThumb0) / MaxHandFingerLength);
			}

			return FingerType.Unknown;
		}

		public static int ToFingerIndex( BoneLocation boneLocation )
		{
			if( (int)boneLocation >= (int)BoneLocation.LeftHandThumb0 &&
				(int)boneLocation <= (int)BoneLocation.LeftHandLittleTip ) {
				return ((int)boneLocation - (int)BoneLocation.LeftHandThumb0) % MaxHandFingerLength;
			}

			if( (int)boneLocation >= (int)BoneLocation.RightHandThumb0 &&
				(int)boneLocation <= (int)BoneLocation.RightHandLittleTip ) {
				return ((int)boneLocation - (int)BoneLocation.RightHandThumb0) % MaxHandFingerLength;
			}

			return -1;
		}

		public enum EffectorType
		{
			Root,
			Hips,
			Neck,
			Head,
			Eyes,
			
			Knee,
			Foot,
			
			Arm,
			Elbow,
			Wrist,

			HandFinger,
			
			Max,
			Unknown = Max,
		}

		public enum EffectorLocation
		{
			Root,
			Hips,
			Neck,
			Head,
			Eyes,
			
			LeftKnee,
			RightKnee,
			LeftFoot,
			RightFoot,
			
			LeftArm,
			RightArm,
			LeftElbow,
			RightElbow,
			LeftWrist,
			RightWrist,

			LeftHandThumb,
			LeftHandIndex,
			LeftHandMiddle,
			LeftHandRing,
			LeftHandLittle,
			RightHandThumb,
			RightHandIndex,
			RightHandMiddle,
			RightHandRing,
			RightHandLittle,
			
			Max,
			Unknown = Max,
		}
		
		public static EffectorType ToEffectorType( EffectorLocation effectorLocation )
		{
			switch( effectorLocation ) {
			case EffectorLocation.Root:			return EffectorType.Root;
			case EffectorLocation.Hips:			return EffectorType.Hips;
			case EffectorLocation.Neck:			return EffectorType.Neck;
			case EffectorLocation.Head:			return EffectorType.Head;
			case EffectorLocation.Eyes:			return EffectorType.Eyes;

			case EffectorLocation.LeftKnee:		return EffectorType.Knee;
			case EffectorLocation.RightKnee:	return EffectorType.Knee;
			case EffectorLocation.LeftFoot:		return EffectorType.Foot;
			case EffectorLocation.RightFoot:	return EffectorType.Foot;

			case EffectorLocation.LeftArm:		return EffectorType.Arm;
			case EffectorLocation.RightArm:		return EffectorType.Arm;
			case EffectorLocation.LeftElbow:	return EffectorType.Elbow;
			case EffectorLocation.RightElbow:	return EffectorType.Elbow;
			case EffectorLocation.LeftWrist:	return EffectorType.Wrist;
			case EffectorLocation.RightWrist:	return EffectorType.Wrist;
			}

			if( (int)effectorLocation >= (int)EffectorLocation.LeftHandThumb &&
				(int)effectorLocation <= (int)EffectorLocation.RightHandLittle ) {
				return EffectorType.HandFinger;
			}

			return EffectorType.Unknown;
		}

		public static Side ToEffectorSide( EffectorLocation effectorLocation )
		{
			switch( effectorLocation ) {
			case EffectorLocation.LeftKnee:		return Side.Left;
			case EffectorLocation.RightKnee:	return Side.Right;
			case EffectorLocation.LeftFoot:		return Side.Left;
			case EffectorLocation.RightFoot:	return Side.Right;

			case EffectorLocation.LeftArm:		return Side.Left;
			case EffectorLocation.RightArm:		return Side.Right;
			case EffectorLocation.LeftElbow:	return Side.Left;
			case EffectorLocation.RightElbow:	return Side.Right;
			case EffectorLocation.LeftWrist:	return Side.Left;
			case EffectorLocation.RightWrist:	return Side.Right;
			}

			if( (int)effectorLocation >= (int)EffectorLocation.LeftHandThumb &&
				(int)effectorLocation <= (int)EffectorLocation.LeftHandLittle ) {
				return Side.Left;
			}

			if( (int)effectorLocation >= (int)EffectorLocation.RightHandThumb &&
				(int)effectorLocation <= (int)EffectorLocation.RightHandLittle ) {
				return Side.Right;
			}

			return Side.None;
		}

		public static string GetEffectorName( EffectorLocation effectorLocation )
		{
			if( effectorLocation == EffectorLocation.Root ) {
				return "FullBodyIK";
			} else if( IsHandFingerEffectors( effectorLocation ) ) {
				return ToFingerType( effectorLocation ).ToString();
			} else {
				return effectorLocation.ToString();
			}
		}
		
		public static bool IsHandFingerEffectors( EffectorLocation effectorLocation )
		{
			int v = (int)effectorLocation;
			return v >= (int)EffectorLocation.LeftHandThumb && v <= (int)EffectorLocation.RightHandLittle;
		}
		
		public static FingerType ToFingerType( EffectorLocation effectorLocation )
		{
			if( IsHandFingerEffectors( effectorLocation ) ) {
				int value = (int)effectorLocation - (int)EffectorLocation.LeftHandThumb;
				return (FingerType)(value % 5);
			}

			return FingerType.Unknown;
		}
		
		public enum FingerType
		{
			Thumb,
			Index,
			Middle,
			Ring,
			Little,
			Max,
			Unknown = Max,
		}

		public const float Eyes_DefaultDistance = 1.0f;
		public const float Eyes_MinDistance = 0.5f;

		public const float SimualteEys_NeckHeadDistanceScale = 1.0f;

		//----------------------------------------------------------------------------------------------------------------

		public enum _DirectionAs
		{
			None,
			XPlus,
			XMinus,
			YPlus,
			YMinus,
			Max,
			Uknown = Max,
		}

		//----------------------------------------------------------------------------------------------------------------
	}

}