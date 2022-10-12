// Copyright (c) 2016 Nora
// Released under the "Unity-Chan" license
// http://unity-chan.com/contents/license_en/
// http://unity-chan.com/contents/license_jp/

using UnityEngine;

namespace SA
{
	[System.Serializable]
	public class FullBodyIKUnityChan : FullBodyIK
	{
		Vector3 _headBoneLossyScale = Vector3.one;
		bool _isHeadBoneLossyScaleFuzzyIdentity = true;

		Quaternion _headToLeftEyeRotation = Quaternion.identity;
		Quaternion _headToRightEyeRotation = Quaternion.identity;

		Vector3 _unityChan_leftEyeDefaultPosition = Vector3.zero;
		Vector3 _unityChan_rightEyeDefaultPosition = Vector3.zero;

		static Vector3 _unityChan_leftEyeDefaultLocalPosition = new Vector3( -0.042531f + 0.024f, 0.048524f, 0.047682f - 0.02f );
		static Vector3 _unityChan_rightEyeDefaultLocalPosition = new Vector3( 0.042531f - 0.024f, 0.048524f, 0.047682f - 0.02f );

		static readonly float _unityChan_eyesHorzLimitTheta = Mathf.Sin( 40.0f * Mathf.Deg2Rad );
		static readonly float _unityChan_eyesVertLimitTheta = Mathf.Sin( 4.5f * Mathf.Deg2Rad );
		const float _unityChan_eyesYawRate = 0.796f;
		const float _unityChan_eyesPitchRate = 0.28f;
		const float _unityChan_eyesYawOuterRate = 0.096f;
		const float _unityChan_eyesYawInnerRate = 0.065f;
		const float _unityChan_eyesMoveXInnerRate = 0.063f * 0.1f;
		const float _unityChan_eyesMoveXOuterRate = 0.063f * 0.1f;

		public override bool _IsHiddenCustomEyes()
		{
			return true;
		}

		public override bool _PrepareCustomEyes( ref Quaternion headToLeftEyeRotation, ref Quaternion headToRightEyeRotation )
		{
			Bone headBone = (headBones != null) ? headBones.head : null;
			Bone leftEyeBone = (headBones != null) ? headBones.leftEye : null;
			Bone rightEyeBone = (headBones != null) ? headBones.rightEye : null;

			if( headBone != null && headBone.transformIsAlive &&
				leftEyeBone != null && leftEyeBone.transformIsAlive &&
				rightEyeBone != null && rightEyeBone.transformIsAlive ) {
				_headToLeftEyeRotation = headToLeftEyeRotation;
				_headToRightEyeRotation = headToRightEyeRotation;

				Vector3 leftPos, rightPos;
				SAFBIKMatMultVec( out leftPos, ref internalValues.defaultRootBasis, ref _unityChan_leftEyeDefaultLocalPosition );
				SAFBIKMatMultVec( out rightPos, ref internalValues.defaultRootBasis, ref _unityChan_rightEyeDefaultLocalPosition );

				_headBoneLossyScale = headBone.transform.lossyScale;
				_isHeadBoneLossyScaleFuzzyIdentity = IsFuzzy( _headBoneLossyScale, Vector3.one );

				if( !_isHeadBoneLossyScaleFuzzyIdentity ) {
					leftPos = Scale( ref leftPos, ref _headBoneLossyScale );
					rightPos = Scale( ref rightPos, ref _headBoneLossyScale );
				}

				_unityChan_leftEyeDefaultPosition = headBone._defaultPosition + leftPos;
				_unityChan_rightEyeDefaultPosition = headBone._defaultPosition + rightPos;
			}

			return true;
		}

		public override void _ResetCustomEyes()
		{
			Bone neckBone = (headBones != null) ? headBones.neck : null;
			Bone headBone = (headBones != null) ? headBones.head : null;
			Bone leftEyeBone = (headBones != null) ? headBones.leftEye : null;
			Bone rightEyeBone = (headBones != null) ? headBones.rightEye : null;

			if( neckBone != null && neckBone.transformIsAlive &&
				headBone != null && headBone.transformIsAlive &&
				leftEyeBone != null && leftEyeBone.transformIsAlive &&
				rightEyeBone != null && rightEyeBone.transformIsAlive ) {

				Quaternion neckWorldRotation = neckBone.worldRotation;
				Vector3 headWorldPosition, neckWorldPosition = neckBone.worldPosition;
				Matrix3x3 neckBasis;
				SAFBIKMatSetRotMultInv1( out neckBasis, ref neckWorldRotation, ref neckBone._defaultRotation );
				SAFBIKMatMultVecPreSubAdd( out headWorldPosition, ref neckBasis, ref headBone._defaultPosition, ref neckBone._defaultPosition, ref neckWorldPosition );

				Quaternion headWorldRotation = headBone.worldRotation;
				Matrix3x3 headBasis;
				SAFBIKMatSetRotMultInv1( out headBasis, ref headWorldRotation, ref headBone._defaultRotation );

				Vector3 worldPotision;
				Quaternion worldRotation;

				SAFBIKMatMultVecPreSubAdd( out worldPotision, ref headBasis, ref leftEyeBone._defaultPosition, ref headBone._defaultPosition, ref headWorldPosition );
				leftEyeBone.worldPosition = worldPotision;
				SAFBIKQuatMult( out worldRotation, ref headWorldRotation, ref _headToLeftEyeRotation );
				leftEyeBone.worldRotation = worldRotation;

				SAFBIKMatMultVecPreSubAdd( out worldPotision, ref headBasis, ref rightEyeBone._defaultPosition, ref headBone._defaultPosition, ref headWorldPosition );
				rightEyeBone.worldPosition = worldPotision;
				SAFBIKQuatMult( out worldRotation, ref headWorldRotation, ref _headToRightEyeRotation );
				rightEyeBone.worldRotation = worldRotation;
			}
		}

		public override void _SolveCustomEyes( ref Matrix3x3 neckBasis, ref Matrix3x3 headBasis, ref Matrix3x3 headBaseBasis )
		{
			Bone neckBone = (headBones != null) ? headBones.neck : null;
			Bone headBone = (headBones != null) ? headBones.head : null;
			Bone leftEyeBone = (headBones != null) ? headBones.leftEye : null;
			Bone rightEyeBone = (headBones != null) ? headBones.rightEye : null;
			Effector eyesEffector = (headEffectors != null) ? headEffectors.eyes : null;

			if( neckBone != null && neckBone.transformIsAlive && 
				headBone != null && headBone.transformIsAlive &&
				leftEyeBone != null && leftEyeBone.transformIsAlive &&
				rightEyeBone != null && rightEyeBone.transformIsAlive &&
				eyesEffector != null ) {

				Vector3 headWorldPosition, neckBoneWorldPosition = neckBone.worldPosition;
				SAFBIKMatMultVecPreSubAdd( out headWorldPosition, ref neckBasis, ref headBone._defaultPosition, ref neckBone._defaultPosition, ref neckBoneWorldPosition );
				Vector3 eyesPosition;
				SAFBIKMatMultVecPreSubAdd( out eyesPosition, ref headBasis, ref eyesEffector._defaultPosition, ref headBone._defaultPosition, ref headWorldPosition );

				Vector3 eyesDir = eyesEffector.worldPosition - eyesPosition;

				Matrix3x3 leftEyeBaseBasis = headBaseBasis;
				Matrix3x3 rightEyeBaseBasis = headBaseBasis;

				SAFBIKMatMultVecInv( out eyesDir, ref headBaseBasis, ref eyesDir );

				if( !SAFBIKVecNormalize( ref eyesDir ) ) {
					eyesDir = new Vector3( 0.0f, 0.0f, 1.0f );
				}

				if( eyesEffector.positionWeight < 1.0f - IKEpsilon ) {
					Vector3 tempDir = Vector3.Lerp( new Vector3( 0.0f, 0.0f, 1.0f ), eyesDir, eyesEffector.positionWeight );
					if( SAFBIKVecNormalize( ref tempDir ) ) {
						eyesDir = tempDir;
					}
				}

				_LimitXY_Square( ref eyesDir,
					_unityChan_eyesHorzLimitTheta,
					_unityChan_eyesHorzLimitTheta,
					_unityChan_eyesVertLimitTheta,
					_unityChan_eyesVertLimitTheta );

				float moveX = eyesDir.x * _unityChan_eyesYawRate;
				if( moveX < -_unityChan_eyesHorzLimitTheta ) {
					moveX = -_unityChan_eyesHorzLimitTheta;
                } else if( moveX > _unityChan_eyesHorzLimitTheta ) {
					moveX = _unityChan_eyesHorzLimitTheta;
				}

				eyesDir.x *= _unityChan_eyesYawRate;
				eyesDir.y *= _unityChan_eyesPitchRate;
				Vector3 leftEyeDir = eyesDir;
				Vector3 rightEyeDir = eyesDir;

				if( eyesDir.x >= 0.0f ) {
					leftEyeDir.x *= _unityChan_eyesYawInnerRate;
					rightEyeDir.x *= _unityChan_eyesYawOuterRate;
				} else {
					leftEyeDir.x *= _unityChan_eyesYawOuterRate;
					rightEyeDir.x *= _unityChan_eyesYawInnerRate;
				}

				SAFBIKVecNormalize2( ref leftEyeDir, ref rightEyeDir );

				SAFBIKMatMultVec( out leftEyeDir, ref headBaseBasis, ref leftEyeDir );
				SAFBIKMatMultVec( out rightEyeDir, ref headBaseBasis, ref rightEyeDir );

				float leftXRate = (moveX >= 0.0f) ? _unityChan_eyesMoveXInnerRate : _unityChan_eyesMoveXOuterRate;
				float rightXRate = (moveX >= 0.0f) ? _unityChan_eyesMoveXOuterRate : _unityChan_eyesMoveXInnerRate;

				SAFBIKComputeBasisLockZ( out leftEyeBaseBasis, ref headBasis.column0, ref headBasis.column1, ref leftEyeDir );
				SAFBIKComputeBasisLockZ( out rightEyeBaseBasis, ref headBasis.column0, ref headBasis.column1, ref rightEyeDir );

				Vector3 leftEyeWorldPosition = headBaseBasis.column0 * (leftXRate * moveX);
				Vector3 rightEyeWorldPosition = headBaseBasis.column0 * (rightXRate * moveX);

				if( !_isHeadBoneLossyScaleFuzzyIdentity ) {
					leftEyeWorldPosition = Scale( ref leftEyeWorldPosition, ref _headBoneLossyScale );
					rightEyeWorldPosition = Scale( ref rightEyeWorldPosition, ref _headBoneLossyScale );
				}

				Vector3 tempVec;
				SAFBIKMatMultVecPreSubAdd( out tempVec, ref headBasis, ref _unityChan_leftEyeDefaultPosition, ref headBone._defaultPosition, ref headWorldPosition );
				leftEyeWorldPosition += tempVec;
				SAFBIKMatMultVecPreSubAdd( out tempVec, ref headBasis, ref _unityChan_rightEyeDefaultPosition, ref headBone._defaultPosition, ref headWorldPosition );
				rightEyeWorldPosition += tempVec;

				Matrix3x3 leftEyeBasis, rightEyeBasis;
				SAFBIKMatMult( out leftEyeBasis, ref leftEyeBaseBasis, ref internalValues.defaultRootBasisInv );
				SAFBIKMatMult( out rightEyeBasis, ref rightEyeBaseBasis, ref internalValues.defaultRootBasisInv );

				Vector3 worldPosition;
				Quaternion worldRotation;

				SAFBIKMatMultVecPreSubAdd( out worldPosition, ref leftEyeBasis, ref leftEyeBone._defaultPosition, ref _unityChan_leftEyeDefaultPosition, ref leftEyeWorldPosition );
				leftEyeBone.worldPosition = worldPosition;
				SAFBIKMatMultGetRot( out worldRotation, ref leftEyeBaseBasis, ref leftEyeBone._baseToWorldBasis );
				leftEyeBone.worldRotation = worldRotation;

				SAFBIKMatMultVecPreSubAdd( out worldPosition, ref rightEyeBasis, ref rightEyeBone._defaultPosition, ref _unityChan_rightEyeDefaultPosition, ref rightEyeWorldPosition );
				rightEyeBone.worldPosition = worldPosition;
				SAFBIKMatMultGetRot( out worldRotation, ref rightEyeBaseBasis, ref rightEyeBone._baseToWorldBasis );
				rightEyeBone.worldRotation = worldRotation;
			}
		}
	}
}
