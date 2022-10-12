// Copyright (c) 2016 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php
using UnityEngine;

namespace SA
{
	public partial class FullBodyIK
	{
		public class FingerIK
		{
			const float _positionLerpRate = 1.15f;

			public class _FingerLink
			{
				public Bone bone = null;
				public Matrix3x3 boneToSolvedBasis = Matrix3x3.identity;
				public Matrix3x3 solvedToBoneBasis = Matrix3x3.identity;
				public Matrix3x4 boneTransform = Matrix3x4.identity;
				public float childToLength = 0.0f;
				public float childToLengthSq = 0.0f;
			}

			public struct _FingerIKParams
			{
				public float lengthD0;
				public float lengthABCDInv;
				public float beginLink_endCosTheta;
			}

			public class _FingerBranch
			{
				public Effector effector = null;
				public _FingerLink[] fingerLinks = null;
				public Matrix3x3 boneToSolvedBasis = Matrix3x3.identity;
				public Matrix3x3 solvedToBoneBasis = Matrix3x3.identity;
				public FastAngle notThumb1BaseAngle = new FastAngle();
				public FastAngle notThumb2BaseAngle = new FastAngle();

				public float link0ToEffectorLength = 0.0f;
				public float link0ToEffectorLengthSq = 0.0f;

				public _FingerIKParams fingerIKParams = new _FingerIKParams();
			}

			class _ThumbLink
			{
				public Matrix3x3 thumb_boneToSolvedBasis = Matrix3x3.identity; // link to effector.
				public Matrix3x3 thumb_solvedToBoneBasis = Matrix3x3.identity; // link to effector.
			}

			class _ThumbBranch
			{
				public _ThumbLink[] thumbLinks = null;
				public Vector3 thumbSolveY = Vector3.zero;
				public Vector3 thumbSolveZ = Vector3.zero;

				public bool thumb0_isLimited = false;
				public float thumb0_lowerLimit = 0.0f;
				public float thumb0_upperLimit = 0.0f;
				public float thumb0_innerLimit = 0.0f;
				public float thumb0_outerLimit = 0.0f;

				public float linkLength0to1Sq = 0.0f;
				public float linkLength0to1 = 0.0f;
				public float linkLength1to3Sq = 0.0f;
				public float linkLength1to3 = 0.0f;

				public float linkLength1to2Sq = 0.0f;
				public float linkLength1to2 = 0.0f;
				public float linkLength2to3Sq = 0.0f;
				public float linkLength2to3 = 0.0f;

				public float thumb1_baseThetaAtoB = 1.0f;
				public float thumb1_Acos_baseThetaAtoB = 0.0f;
			}

			FingerIKType _fingerIKType;
			Settings _settings;
			InternalValues _internalValues;

			Bone _parentBone; // wrist/leg
			_FingerBranch[] _fingerBranches = new _FingerBranch[(int)FingerType.Max];
			_ThumbBranch _thumbBranch = null;

			FastAngle _notThumbYawThetaLimit = new FastAngle( 10.0f * Mathf.Deg2Rad );
			FastAngle _notThumbPitchUThetaLimit = new FastAngle( 60.0f * Mathf.Deg2Rad );
			FastAngle _notThumbPitchLThetaLimit = new FastAngle( 160.0f * Mathf.Deg2Rad );

			FastAngle _notThumb0FingerIKLimit = new FastAngle( 60.0f * Mathf.Deg2Rad );

			FastAngle _notThumb1PitchUTrace = new FastAngle( 5.0f * Mathf.Deg2Rad );
			FastAngle _notThumb1PitchUSmooth = new FastAngle( 5.0f * Mathf.Deg2Rad );
			FastAngle _notThumb1PitchUTraceSmooth = new FastAngle( 10.0f * Mathf.Deg2Rad ); // _notThumb1PitchUTrace + _notThumb1PitchUSmooth
			FastAngle _notThumb1PitchLTrace = new FastAngle( 10.0f * Mathf.Deg2Rad );
			FastAngle _notThumb1PitchLLimit = new FastAngle( 80.0f * Mathf.Deg2Rad );

			public FingerIK( FullBodyIK fullBodyIK, FingerIKType fingerIKType )
			{
				_fingerIKType = fingerIKType;
				_settings = fullBodyIK.settings;
				_internalValues = fullBodyIK.internalValues;

				FingersBones fingerBones = null;
                FingersEffectors fingerEffectors = null;
				switch( fingerIKType ) {
				case FingerIKType.LeftWrist:
					_parentBone		= fullBodyIK.leftArmBones.wrist;
					fingerBones		= fullBodyIK.leftHandFingersBones;
					fingerEffectors	= fullBodyIK.leftHandFingersEffectors;
                    break;
				case FingerIKType.RightWrist:
					_parentBone		= fullBodyIK.rightArmBones.wrist;
					fingerBones		= fullBodyIK.rightHandFingersBones;
					fingerEffectors	= fullBodyIK.rightHandFingersEffectors;
					break;
				}

				_notThumb1PitchUTraceSmooth = new FastAngle( _notThumb1PitchUTrace.angle + _notThumb1PitchUSmooth.angle );

				if( fingerBones != null && fingerEffectors != null ) {
					for( int fingerType = 0; fingerType < (int)FingerType.Max; ++fingerType ) {
						Bone[] bones = null;
						Effector effector = null;
						switch( fingerType ) {
						case (int)FingerType.Thumb:
							bones = fingerBones.thumb;
							effector = fingerEffectors.thumb;
							break;
						case (int)FingerType.Index:
							bones = fingerBones.index;
							effector = fingerEffectors.index;
							break;
						case (int)FingerType.Middle:
							bones = fingerBones.middle;
							effector = fingerEffectors.middle;
							break;
						case (int)FingerType.Ring:
							bones = fingerBones.ring;
							effector = fingerEffectors.ring;
							break;
						case (int)FingerType.Little:
							bones = fingerBones.little;
							effector = fingerEffectors.little;
							break;
						}

						if( bones != null && effector != null ) {
							_PrepareBranch( fingerType, bones, effector );
						}
					}
				}
			}

			// Allocation only.
			void _PrepareBranch( int fingerType, Bone[] bones, Effector effector )
			{
				if( _parentBone == null || bones == null || effector == null ) {
					return;
				}

				int boneLength = bones.Length;
				if( boneLength == 0 ) {
					return;
				}

				if( effector.bone != null && bones[boneLength - 1] == effector.bone ) {
					boneLength -= 1;
					if( boneLength == 0 ) {
						return;
					}
				}

				if( boneLength != 0 ) {
					if( bones[boneLength - 1] == null || bones[boneLength - 1].transform == null ) {
						boneLength -= 1;
						if( boneLength == 0 ) {
							return;
						}
					}
				}

				_FingerBranch fingerBranch = new _FingerBranch();
				fingerBranch.effector = effector;

				fingerBranch.fingerLinks = new _FingerLink[boneLength];
				for( int linkID = 0; linkID < boneLength; ++linkID ) {
					if( bones[linkID] == null || bones[linkID].transform == null ) {
						return;
					}

					_FingerLink fingerLink = new _FingerLink();
					fingerLink.bone = bones[linkID];
					fingerBranch.fingerLinks[linkID] = fingerLink;
				}

				_fingerBranches[fingerType] = fingerBranch;

				if( fingerType == (int)FingerType.Thumb ) {
					_thumbBranch = new _ThumbBranch();
					_thumbBranch.thumbLinks = new _ThumbLink[boneLength];
					for( int i = 0; i != boneLength; ++i ) {
						_thumbBranch.thumbLinks[i] = new _ThumbLink();
					}
				}
			}

			static bool _SolveThumbYZ(
				ref Matrix3x3 middleBoneToSolvedBasis,
				ref Vector3 thumbSolveY,
				ref Vector3 thumbSolveZ )
			{
				if( SAFBIKVecNormalize2( ref thumbSolveY, ref thumbSolveZ ) ) {
					if( Mathf.Abs( thumbSolveY.z ) > Mathf.Abs( thumbSolveZ.z ) ) {
						Vector3 t = thumbSolveY;
						thumbSolveY = thumbSolveZ;
						thumbSolveZ = t;
					}

					if( thumbSolveY.y < 0.0f ) {
						thumbSolveY = -thumbSolveY;
					}
					if( thumbSolveZ.z < 0.0f ) {
						thumbSolveZ = -thumbSolveZ;
					}

					SAFBIKMatMultVec( out thumbSolveY, ref middleBoneToSolvedBasis, ref thumbSolveY );
					SAFBIKMatMultVec( out thumbSolveZ, ref middleBoneToSolvedBasis, ref thumbSolveZ );
					return true;
				}

				thumbSolveY = Vector3.zero;
				thumbSolveZ = Vector3.zero;
				return false;
			}

			// for Prepare, SyncDisplacement.
			void _PrepareBranch2( int fingerType )
			{
				_FingerBranch fingerBranch = _fingerBranches[fingerType];
				if( _parentBone == null || fingerBranch == null ) {
					return;
				}

				Effector fingerEffector = fingerBranch.effector;
				int fingerLinkLength = fingerBranch.fingerLinks.Length;

				bool isRight = (_fingerIKType == FingerIKType.RightWrist);

				if( fingerBranch.fingerLinks != null && fingerBranch.fingerLinks.Length > 0 && fingerBranch.fingerLinks[0].bone != null ) {
					Vector3 dirX = fingerEffector.defaultPosition - fingerBranch.fingerLinks[0].bone._defaultPosition;
					dirX = isRight ? dirX : -dirX;
					if( SAFBIKVecNormalize( ref dirX ) && SAFBIKComputeBasisFromXZLockX( out fingerBranch.boneToSolvedBasis, dirX, _internalValues.defaultRootBasis.column2 ) ) {
						fingerBranch.solvedToBoneBasis = fingerBranch.boneToSolvedBasis.transpose;
					}

					fingerBranch.link0ToEffectorLength = SAFBIKVecLengthAndLengthSq2(
						out fingerBranch.link0ToEffectorLengthSq,
						ref fingerEffector._defaultPosition, ref fingerBranch.fingerLinks[0].bone._defaultPosition );
				}

				if( fingerType == (int)FingerType.Thumb ) {
					_FingerBranch middleFingerBranch = _fingerBranches[(int)FingerType.Middle];
					if( middleFingerBranch == null ) {
						return;
					}

					if( middleFingerBranch.fingerLinks.Length >= 1 ) {
						_FingerLink middleFingerLink0 = middleFingerBranch.fingerLinks[0];
						Matrix3x3 middleBoneToSolvedBasis = Matrix3x3.identity;
						Matrix3x3 middleSolvedToBoneBasis = Matrix3x3.identity;
						Vector3 middleDirX = middleFingerLink0.bone._defaultPosition - _parentBone._defaultPosition;
						if( SAFBIKVecNormalize( ref middleDirX ) ) {
							middleDirX = isRight ? middleDirX : -middleDirX;
							if( SAFBIKComputeBasisFromXZLockX( out middleBoneToSolvedBasis, middleDirX, _internalValues.defaultRootBasis.column2 ) ) {
								middleSolvedToBoneBasis = middleBoneToSolvedBasis.transpose;
							}
						}

						// Solve thumb's basis Y / Z vectors.

						bool isSolved = false;
						if( fingerLinkLength >= 2 && fingerEffector._isSimulateFingerTips == false ) {
							// Memo: Skip if fingerEffector._isSimulateFingerTips = true.(Because always thumbSolveZ = 0, 0, 1)
							_FingerLink thumbFingerLink0 = fingerBranch.fingerLinks[fingerLinkLength - 2];
							_FingerLink thumbFingerLink1 = fingerBranch.fingerLinks[fingerLinkLength - 1];
							Vector3 thumbPosition0 = thumbFingerLink0.bone._defaultPosition;
							Vector3 thumbPosition1 = thumbFingerLink1.bone._defaultPosition;
							Vector3 thumbPosition2 = fingerEffector._defaultPosition;
							Vector3 thumb0to1 = thumbPosition1 - thumbPosition0;
							Vector3 thumb1to2 = thumbPosition2 - thumbPosition1;

							// World to Local Basis.(Reference Middle Finger.)
							SAFBIKMatMultVec( out thumb0to1, ref middleSolvedToBoneBasis, ref thumb0to1 );
							SAFBIKMatMultVec( out thumb1to2, ref middleSolvedToBoneBasis, ref thumb1to2 );

							Vector3 tempY = Vector3.Cross( thumb0to1, thumb1to2 );

							_thumbBranch.thumbSolveY = tempY;
							_thumbBranch.thumbSolveZ = Vector3.Cross( thumb1to2, tempY );

							isSolved = _SolveThumbYZ( ref middleBoneToSolvedBasis,
								ref _thumbBranch.thumbSolveY,
								ref _thumbBranch.thumbSolveZ );
						}

						if( !isSolved && fingerLinkLength >= 3 ) {
							_FingerLink thumbFingerLink0 = fingerBranch.fingerLinks[fingerLinkLength - 3];
							_FingerLink thumbFingerLink1 = fingerBranch.fingerLinks[fingerLinkLength - 2];
							_FingerLink thumbFingerLink2 = fingerBranch.fingerLinks[fingerLinkLength - 1];
							Vector3 thumbPosition0 = thumbFingerLink0.bone._defaultPosition;
							Vector3 thumbPosition1 = thumbFingerLink1.bone._defaultPosition;
							Vector3 thumbPosition2 = thumbFingerLink2.bone._defaultPosition;
							Vector3 thumb0to1 = thumbPosition1 - thumbPosition0;
							Vector3 thumb1to2 = thumbPosition2 - thumbPosition1;

							// World to Local Basis.(Reference Middle Finger.)
							SAFBIKMatMultVec( out thumb0to1, ref middleSolvedToBoneBasis, ref thumb0to1 );
							SAFBIKMatMultVec( out thumb1to2, ref middleSolvedToBoneBasis, ref thumb1to2 );

							Vector3 tempY = Vector3.Cross( thumb0to1, thumb1to2 );

							_thumbBranch.thumbSolveY = tempY;
							_thumbBranch.thumbSolveZ = Vector3.Cross( thumb1to2, tempY );

							isSolved = _SolveThumbYZ( ref middleBoneToSolvedBasis,
								ref _thumbBranch.thumbSolveY,
								ref _thumbBranch.thumbSolveZ );
						}

						if( !isSolved ) {
							_thumbBranch.thumbSolveZ = new Vector3( 0.0f, 1.0f, 2.0f );
							_thumbBranch.thumbSolveY = new Vector3( 0.0f, 2.0f, -1.0f );
							SAFBIKVecNormalize2( ref _thumbBranch.thumbSolveZ, ref _thumbBranch.thumbSolveY );
						}
					}
				}

				for( int n = 0; n != fingerBranch.fingerLinks.Length; ++n ) {
					_FingerLink fingerLink = fingerBranch.fingerLinks[n];

					Vector3 sourcePosition = fingerLink.bone._defaultPosition;
					Vector3 destPosition;
					FastLength sourceToDestLength;
					Vector3 sourceToDestDirection;
					if( n + 1 != fingerBranch.fingerLinks.Length ) {
						destPosition = fingerBranch.fingerLinks[n + 1].bone._defaultPosition;
						sourceToDestLength = fingerBranch.fingerLinks[n + 1].bone._defaultLocalLength;
						sourceToDestDirection = fingerBranch.fingerLinks[n + 1].bone._defaultLocalDirection;
					} else {
						destPosition = fingerBranch.effector._defaultPosition;
						if( !fingerBranch.effector._isSimulateFingerTips ) {
							sourceToDestLength = fingerBranch.effector.bone._defaultLocalLength;
							sourceToDestDirection = fingerBranch.effector.bone._defaultLocalDirection;
						} else {
							Vector3 tempTranslate = destPosition - sourcePosition;
                            sourceToDestLength = FastLength.FromVector3( ref tempTranslate );
							if( sourceToDestLength.length > FLOAT_EPSILON ) {
								sourceToDestDirection = tempTranslate * (1.0f / sourceToDestLength.length);
                            } else {
								sourceToDestDirection = Vector3.zero;
                            }
                        }
					}

					if( fingerType != (int)FingerType.Thumb ) {
						fingerLink.childToLength = sourceToDestLength.length;
						fingerLink.childToLengthSq = sourceToDestLength.lengthSq;
					}

					{
						Vector3 dirX = sourceToDestDirection;
						if( dirX.x != 0.0f || dirX.y != 0.0f || dirX.z != 0.0f ) {
							dirX = isRight ? dirX : -dirX;
							if( SAFBIKComputeBasisFromXZLockX( out fingerLink.boneToSolvedBasis, dirX, _internalValues.defaultRootBasis.column2 ) ) {
								fingerLink.solvedToBoneBasis = fingerLink.boneToSolvedBasis.transpose;
							}
						}
					}

					if( fingerType == (int)FingerType.Thumb ) {
						_ThumbLink thumbLink = _thumbBranch.thumbLinks[n];

						Vector3 dirX = fingerBranch.effector._defaultPosition - sourcePosition;
						if( SAFBIKVecNormalize( ref dirX ) ) {
							dirX = isRight ? dirX : -dirX;
							if( SAFBIKComputeBasisFromXYLockX( out thumbLink.thumb_boneToSolvedBasis, ref dirX, ref _thumbBranch.thumbSolveY ) ) {
								thumbLink.thumb_solvedToBoneBasis = thumbLink.thumb_boneToSolvedBasis.transpose;
							}
						}
					}
				}

				if( fingerType != (int)FingerType.Thumb ) {
					if( fingerBranch.fingerLinks.Length == 3 ) {
						// Compute rotate angle. Based X/Y coordinate.
						// !isRight ... Plus value as warp, minus value as bending.
						// isRight ... Minus value as warp, plus value as bending.
						fingerBranch.notThumb1BaseAngle = new FastAngle( _ComputeJointBaseAngle(
							ref _internalValues.defaultRootBasis,
							ref fingerBranch.fingerLinks[0].bone._defaultPosition,
							ref fingerBranch.fingerLinks[1].bone._defaultPosition,
							ref fingerBranch.effector._defaultPosition, isRight ) );

						fingerBranch.notThumb2BaseAngle = new FastAngle( _ComputeJointBaseAngle(
							ref _internalValues.defaultRootBasis,
							ref fingerBranch.fingerLinks[1].bone._defaultPosition,
							ref fingerBranch.fingerLinks[2].bone._defaultPosition,
							ref fingerBranch.effector._defaultPosition, isRight ) );

						float linkLength0 = fingerBranch.fingerLinks[0].childToLength;
						float linkLength1 = fingerBranch.fingerLinks[1].childToLength;
						float linkLength2 = fingerBranch.fingerLinks[2].childToLength;

						float lengthH0 = Mathf.Abs( linkLength0 - linkLength2 );
						float lengthD0 = SAFBIKSqrt( linkLength1 * linkLength1 - lengthH0 * lengthH0 );

						float beginLink_endCosTheta = 0.0f; // 90'

						if( linkLength0 > linkLength2 ) {
							float min_cosTheta = _notThumb0FingerIKLimit.cos; // 60
							float min_sinTheta = _notThumb0FingerIKLimit.sin; // 60
							float norm_lengthD0 = lengthD0 * (1.0f / linkLength1); // = cosTheta
							if( norm_lengthD0 < min_cosTheta ) {
								lengthD0 = min_cosTheta * linkLength1;
								lengthH0 = min_sinTheta * linkLength1;

								float beginLink_endSinTheta = Mathf.Clamp01( (linkLength2 + lengthH0) * (1.0f / linkLength0) );
								beginLink_endCosTheta = SAFBIKSqrtClamp01( 1.0f - beginLink_endSinTheta * beginLink_endSinTheta );
							}
						}

						float lengthCtoD = linkLength1 - lengthD0;
						float lengthAplusB = linkLength0 + linkLength2;
						float lengthABCDInv = lengthAplusB + lengthCtoD;
						lengthABCDInv = (lengthABCDInv > IKEpsilon) ? (1.0f / lengthABCDInv) : 0.0f;

						fingerBranch.fingerIKParams.lengthD0 = lengthD0;
						fingerBranch.fingerIKParams.lengthABCDInv = lengthABCDInv;
						fingerBranch.fingerIKParams.beginLink_endCosTheta = beginLink_endCosTheta;
					}
				}
			}

			// for Prepare, SyncDisplacement.
			void _PrepareThumb()
			{
				_FingerBranch fingerBranch = _fingerBranches[(int)FingerType.Thumb];
				_FingerBranch indexFingerBranch = _fingerBranches[(int)FingerType.Index];
				if( fingerBranch == null || fingerBranch.fingerLinks.Length != 3 ||
					indexFingerBranch == null || indexFingerBranch.fingerLinks.Length == 0 ) {
					return;
				}

				_FingerLink fingerLink0 = fingerBranch.fingerLinks[0];
				_FingerLink fingerLink1 = fingerBranch.fingerLinks[1];
				_FingerLink fingerLink2 = fingerBranch.fingerLinks[2];

				{
					_FingerLink indexBeginLink = indexFingerBranch.fingerLinks[0];
					// Direction thumb0 to index0.
					Vector3 thumbToIndex = indexBeginLink.bone._defaultPosition - fingerLink0.bone._defaultPosition;
					Vector3 localThumbToIndex;
					SAFBIKMatMultVec( out localThumbToIndex, ref fingerBranch.solvedToBoneBasis, ref thumbToIndex );
					if( SAFBIKVecNormalize( ref localThumbToIndex ) ) {
						_thumbBranch.thumb0_isLimited = true;
						_thumbBranch.thumb0_innerLimit = Mathf.Max( -localThumbToIndex.z, 0.0f ); // innerLimit = under index 0
						_thumbBranch.thumb0_outerLimit = (float)System.Math.Sin( Mathf.Max( -(SAFBIKAsin( _thumbBranch.thumb0_innerLimit ) - 40.0f * Mathf.Deg2Rad), 0.0f ) );
						_thumbBranch.thumb0_upperLimit = Mathf.Max( localThumbToIndex.y, 0.0f ); // upperLimit = height index 0
						_thumbBranch.thumb0_lowerLimit = (float)System.Math.Sin( Mathf.Max( -(SAFBIKAsin( _thumbBranch.thumb0_upperLimit ) - 45.0f * Mathf.Deg2Rad), 0.0f ) );
					}
				}

				_thumbBranch.linkLength0to1 = SAFBIKVecLengthAndLengthSq2( out _thumbBranch.linkLength0to1Sq,
					ref fingerLink1.bone._defaultPosition, ref fingerLink0.bone._defaultPosition );
				_thumbBranch.linkLength1to2 = SAFBIKVecLengthAndLengthSq2( out _thumbBranch.linkLength1to2Sq,
					ref fingerLink2.bone._defaultPosition, ref fingerLink1.bone._defaultPosition );
				_thumbBranch.linkLength2to3 = SAFBIKVecLengthAndLengthSq2( out _thumbBranch.linkLength2to3Sq,
					ref fingerBranch.effector._defaultPosition, ref fingerLink2.bone._defaultPosition );

				// Memo: Straight length.
				_thumbBranch.linkLength1to3 = SAFBIKVecLengthAndLengthSq2( out _thumbBranch.linkLength1to3Sq,
					ref fingerBranch.effector._defaultPosition, ref fingerLink1.bone._defaultPosition );

				_thumbBranch.thumb1_baseThetaAtoB = _ComputeTriangleTheta(
					_thumbBranch.linkLength1to2,
					_thumbBranch.linkLength1to3,
					_thumbBranch.linkLength2to3,
					_thumbBranch.linkLength1to2Sq,
					_thumbBranch.linkLength1to3Sq,
					_thumbBranch.linkLength2to3Sq );

				_thumbBranch.thumb1_Acos_baseThetaAtoB = SAFBIKAcos( _thumbBranch.thumb1_baseThetaAtoB );
			}

			bool _isSyncDisplacementAtLeastOnce;

			void _SyncDisplacement()
			{
				// Measure bone length.(Using worldPosition)
				// Force execution on 1st time. (Ignore case _settings.syncDisplacement == SyncDisplacement.Disable)
				if( _settings.syncDisplacement == SyncDisplacement.Everyframe || !_isSyncDisplacementAtLeastOnce ) {
					_isSyncDisplacementAtLeastOnce = true;

					for( int fingerType = 0; fingerType != (int)FingerType.Max; ++fingerType ) {
						_PrepareBranch2( fingerType );
					}

					_PrepareThumb();
				}
			}

			//------------------------------------------------------------------------------------------------------------------------------------------------

			// Helpers.

			static float _ComputeJointBaseAngle(
				ref Matrix3x3 rootBaseBasis,
				ref Vector3 beginPosition,
				ref Vector3 nextPosition,
				ref Vector3 endPosition,
				bool isRight )
			{
				Vector3 linkToEnd = endPosition - beginPosition;
				Vector3 linkToNext = nextPosition - beginPosition;
				if( SAFBIKVecNormalize2( ref linkToEnd, ref linkToNext ) ) {
					Matrix3x3 linkToEndBasis;
					Vector3 dirX = isRight ? linkToEnd : -linkToEnd;
					SAFBIKComputeBasisFromXZLockX( out linkToEndBasis, dirX, rootBaseBasis.column2 );
					dirX = isRight ? linkToNext : -linkToNext;
					Vector3 dirY = linkToEndBasis.column2;
					Matrix3x3 linkToNextBasis;
					SAFBIKComputeBasisFromXZLockZ( out linkToNextBasis, ref dirX, ref dirY );

					float dotX = Vector3.Dot( linkToEndBasis.column0, linkToNextBasis.column0 );
					float dotY = Vector3.Dot( linkToEndBasis.column1, linkToNextBasis.column0 );

					float r = SAFBIKAcos( dotX );
					if( dotY < 0.0f ) {
						r = -r;
					}

					return r;
				}

				return 0.0f;
			}

			static bool _SolveInDirect(
				bool isRight,
				ref Vector3 solvedDirY,
				ref Vector3 solvedDirZ,
				ref Matrix3x3 rootBasis,
				ref Matrix3x3 linkBoneToSolvedBasis,
				ref Vector3 effectorDirection )
			{
				Vector3 dirX = isRight ? effectorDirection : -effectorDirection;
				Vector3 dirZ;
				SAFBIKMatMultVec( out dirZ, ref rootBasis, ref linkBoneToSolvedBasis.column2 );
				Matrix3x3 linkSolvedBasis;
				if( !SAFBIKComputeBasisFromXZLockX( out linkSolvedBasis, ref dirX, ref dirZ ) ) {
					return false;
				}

				solvedDirY = linkSolvedBasis.column1;
				solvedDirZ = linkSolvedBasis.column2;
				return true;
			}

			static float _ComputeTriangleTheta( float lenA, float lenB, float lenC, float lenASq, float lenBSq, float lenCSq )
			{
				float tempAB = lenA * lenB;
				if( tempAB >= IKEpsilon ) {
					return (lenASq + lenBSq - lenCSq) / (2.0f * tempAB);
				}

				return 1.0f;
			}

			static void _LerpEffectorLength(
				ref float effectorLength, // out
				ref Vector3 effectorDirection,
				ref Vector3 effectorTranslate,
				ref Vector3 effectorPosition,
				ref Vector3 effectorOrigin,
				float minLength,
				float maxLength,
				float lerpLength )
			{
				if( lerpLength > IKEpsilon ) {
					float subLength = effectorLength - minLength;
					float r = subLength / lerpLength;
					effectorLength = minLength + r * (maxLength - minLength);
				} else {
					effectorLength = minLength;
				}

				effectorTranslate = effectorLength * effectorDirection;
				effectorPosition = effectorOrigin + effectorTranslate;
			}

			static Vector3 SolveFingerIK(
				ref Vector3 beginPosition,
				ref Vector3 endPosition,
				ref Vector3 bendingDirection,
				float linkLength0,
				float linkLength1,
				float linkLength2,
				ref _FingerIKParams fingerIKParams )
			{
				float beginToEndBaseLength = linkLength0 + linkLength1 + linkLength2;
				float beginToEndLength = (endPosition - beginPosition).magnitude;
				if( beginToEndLength <= IKEpsilon ) {
					return Vector3.zero;
				}

				Vector3 beginToEndDirection = endPosition - beginPosition;
				beginToEndDirection *= 1.0f / beginToEndLength;

				if( beginToEndLength >= beginToEndBaseLength - IKEpsilon ) {
					return beginToEndDirection;
				}

				if( linkLength0 <= IKEpsilon || linkLength1 <= IKEpsilon || linkLength2 <= IKEpsilon ) {
					return Vector3.zero;
				}

				Vector3 centerToBendingDirection = Vector3.Cross( beginToEndDirection, bendingDirection );
				centerToBendingDirection = Vector3.Cross( centerToBendingDirection, beginToEndDirection );

				float centerToBendingDirectionLengthTemp = centerToBendingDirection.magnitude;
				if( centerToBendingDirectionLengthTemp <= IKEpsilon ) {
					return Vector3.zero;
				}

				centerToBendingDirection *= 1.0f / centerToBendingDirectionLengthTemp;

				float solveCosTheta = Mathf.Lerp( fingerIKParams.beginLink_endCosTheta, 1.0f, Mathf.Clamp01( (beginToEndLength - fingerIKParams.lengthD0) * fingerIKParams.lengthABCDInv ) );
				float solveSinTheta = SAFBIKSqrtClamp01( 1.0f - solveCosTheta * solveCosTheta );

				Vector3 solvedDirection = beginToEndDirection * solveCosTheta + centerToBendingDirection * solveSinTheta;
				if( !SAFBIKVecNormalize( ref solvedDirection ) ) {
					return Vector3.zero;
				}

				return solvedDirection;
			}

			static Vector3 SolveLimbIK(
				ref Vector3 beginPosition,
				ref Vector3 endPosition,
				float beginToInterBaseLength,
				float beginToInterBaseLengthSq,
				float interToEndBaseLength,
				float interToEndBaseLengthSq,
				ref Vector3 bendingDirection )
			{
				float beginToEndBaseLength = beginToInterBaseLength + interToEndBaseLength;
				if( beginToEndBaseLength <= IKEpsilon ) {
					return Vector3.zero;
				}

				float beginToEndLengthSq = (endPosition - beginPosition).sqrMagnitude;
				float beginToEndLength = SAFBIKSqrt( beginToEndLengthSq );
				if( beginToEndLength <= IKEpsilon ) {
					return Vector3.zero;
				}

				Vector3 beginToEndDirection = (endPosition - beginPosition) * (1.0f / beginToEndLength);
				if( beginToEndLength >= beginToEndBaseLength - IKEpsilon ) {
					return beginToEndDirection;
				}

				Vector3 centerToBendingDirection = Vector3.Cross( beginToEndDirection, bendingDirection );
				centerToBendingDirection = Vector3.Cross( centerToBendingDirection, beginToEndDirection );

				float centerToBendingDirectionLengthTemp = centerToBendingDirection.magnitude;
				if( centerToBendingDirectionLengthTemp <= IKEpsilon ) {
					return Vector3.zero;
				}

				centerToBendingDirection *= 1.0f / centerToBendingDirectionLengthTemp;

				float beginToInterTheta = 1.0f;
				float triASq = interToEndBaseLengthSq;
				float triB = beginToInterBaseLength;
				float triBSq = beginToInterBaseLengthSq;
				float triC = beginToEndLength;
				float triCSq = beginToEndLengthSq;
				if( beginToEndLength < beginToEndBaseLength ) {
					float bc2 = 2.0f * triB * triC;
					if( bc2 > IKEpsilon ) {
						beginToInterTheta = (triASq - triBSq - triCSq) / -bc2;
					}
				}

				float sinTheta = SAFBIKSqrtClamp01( 1.0f - beginToInterTheta * beginToInterTheta );

				Vector3 beginToInterDirection = beginToEndDirection * beginToInterTheta * beginToInterBaseLength
												+ centerToBendingDirection * sinTheta * beginToInterBaseLength;
				float beginToInterDirectionLengthTemp = beginToInterDirection.magnitude;
				if( beginToInterDirectionLengthTemp <= IKEpsilon ) {
					return Vector3.zero;
				}

				beginToInterDirection *= 1.0f / beginToInterDirectionLengthTemp;
				return beginToInterDirection;
			}

			//------------------------------------------------------------------------------------------------------------------------------------------------

			public bool Solve()
			{
				if( _parentBone == null ) {
					return false;
				}

				_SyncDisplacement();

				bool isSolved = false;

				Matrix3x4 parentTransform = Matrix3x4.identity;
				parentTransform.origin = _parentBone.worldPosition;
				Quaternion parentBoneWorldRotation = _parentBone.worldRotation;
				SAFBIKMatSetRotMultInv1( out parentTransform.basis, ref parentBoneWorldRotation, ref _parentBone._defaultRotation );

				for( int i = 0; i != (int)FingerType.Max; ++i ) {
					_FingerBranch fingerBranch = _fingerBranches[i];
					if( fingerBranch == null || fingerBranch.effector == null || !fingerBranch.effector.positionEnabled ) {
						continue;
					}

					if( i == (int)FingerType.Thumb ) {
						isSolved |= _SolveThumb( ref parentTransform );
					} else {
						isSolved |= _SolveNotThumb( i, ref parentTransform );
					}
				}

				return isSolved;
			}

			static Vector3 _GetEffectorPosition(
				InternalValues internalValues,
				Bone rootBone,
				Bone beginLinkBone,
				Effector effector,
				float link0ToEffectorLength,
				ref Matrix3x4 parentTransform )
			{
				if( rootBone != null && beginLinkBone != null && effector != null ) {
					var effectorPosition = effector.worldPosition;
					if( effector.positionWeight < 1.0f - IKEpsilon ) {
						Vector3 endLinkPosition;
						if( internalValues.continuousSolverEnabled || internalValues.resetTransforms ) {
							endLinkPosition = parentTransform * (effector._defaultPosition - rootBone._defaultPosition);
						} else {
							endLinkPosition = effector.bone_worldPosition;
						}

						Vector3 beginLinkPosition = parentTransform * (beginLinkBone._defaultPosition - rootBone._defaultPosition);

						Vector3 moveFrom = endLinkPosition - beginLinkPosition;
						Vector3 moveTo = effectorPosition - beginLinkPosition;

						float lengthFrom = link0ToEffectorLength; // Optimized.
						float lengthTo = moveTo.magnitude;

						if( lengthFrom > IKEpsilon && lengthTo > IKEpsilon ) {
							Vector3 dirFrom = moveFrom * (1.0f / lengthFrom);
							Vector3 dirTo = moveTo * (1.0f / lengthTo);
							Vector3 dir = _LerpDir( ref dirFrom, ref dirTo, effector.positionWeight );
							float len = Mathf.Lerp( lengthFrom, lengthTo, Mathf.Clamp01( 1.0f - (1.0f - effector.positionWeight) * _positionLerpRate ) );
							return dir * len + beginLinkPosition;
						}
					}

					return effectorPosition;
                }

				return Vector3.zero;
			}

			bool _SolveNotThumb( int fingerType, ref Matrix3x4 parentTransform )
			{
				_FingerBranch fingerBranch = _fingerBranches[fingerType];
				if( fingerBranch == null || fingerBranch.fingerLinks.Length != 3 ) {
					return false;
				}

				bool isRight = (_fingerIKType == FingerIKType.RightWrist);

				_FingerLink beginLink = fingerBranch.fingerLinks[0];
				_FingerLink bendingLink0 = fingerBranch.fingerLinks[1];
				_FingerLink bendingLink1 = fingerBranch.fingerLinks[2];
				Effector endEffector = fingerBranch.effector;

				float linkLength0 = beginLink.childToLength;
				float linkLength1 = bendingLink0.childToLength;
				float linkLength1Sq = bendingLink0.childToLengthSq;
				float linkLength2 = bendingLink1.childToLength;
				float linkLength2Sq = bendingLink1.childToLengthSq;
				float baseLength = fingerBranch.link0ToEffectorLength;

				Vector3 beginLinkPosition = parentTransform * (beginLink.bone._defaultPosition - _parentBone._defaultPosition);
				Vector3 effectorPosition = _GetEffectorPosition( _internalValues, _parentBone, beginLink.bone, endEffector, fingerBranch.link0ToEffectorLength, ref parentTransform );
				Vector3 effectorTranslate = effectorPosition - beginLinkPosition;

				float effectorLength = effectorTranslate.magnitude;
				if( effectorLength <= IKEpsilon || baseLength <= IKEpsilon ) {
					return false;
				}

				Vector3 effectorDirection = effectorTranslate * (1.0f / effectorLength);

				bool isWarp = isRight ? (fingerBranch.notThumb1BaseAngle.angle <= IKEpsilon) : (fingerBranch.notThumb1BaseAngle.angle >= -IKEpsilon);

				{
					float maxLength = isWarp ? baseLength : (linkLength0 + linkLength1 + linkLength2);
					if( effectorLength > maxLength ) {
						effectorLength = maxLength;
						effectorTranslate = effectorDirection * effectorLength;
						effectorPosition = beginLinkPosition + effectorTranslate;
					} else if( effectorLength < linkLength1 ) {
						effectorLength = linkLength1;
						effectorTranslate = effectorDirection * effectorLength;
						effectorPosition = beginLinkPosition + effectorTranslate;
					}
				}

				bool isUpper = false;

				{
					Matrix3x3 beginToEndBasis;
					SAFBIKMatMult( out beginToEndBasis, ref parentTransform.basis, ref fingerBranch.boneToSolvedBasis );
					Vector3 localEffectorDirection;
					SAFBIKMatMultVecInv( out localEffectorDirection, ref beginToEndBasis, ref effectorDirection );

					isUpper = (localEffectorDirection.y >= 0.0f);

					if( _LimitFingerNotThumb(
						isRight,
						ref localEffectorDirection,
						ref _notThumbPitchUThetaLimit,
						ref _notThumbPitchLThetaLimit,
						ref _notThumbYawThetaLimit ) ) {
						SAFBIKMatMultVec( out effectorDirection, ref beginToEndBasis, ref localEffectorDirection );
						effectorTranslate = effectorDirection * effectorLength;
						effectorPosition = beginLinkPosition + effectorTranslate;
					}
				}

				Vector3 solveDirY = Vector3.zero;
				Vector3 solveDirZ = Vector3.zero;
				if( !_SolveInDirect(
					isRight,
					ref solveDirY,
					ref solveDirZ,
					ref parentTransform.basis,
					ref beginLink.boneToSolvedBasis,
					ref effectorDirection ) ) {
					return false;
				}

				bool solveFingerIK = !isWarp;

				if( isWarp ) {
					bool imm_isUpper = false;
					float imm_traceRate = 0.0f;

					Vector3 bendingLink0Position = parentTransform * (bendingLink0.bone._defaultPosition - _parentBone._defaultPosition);
					Vector3 bendingLink1Position = parentTransform * (bendingLink1.bone._defaultPosition - _parentBone._defaultPosition);
					Vector3 endPosition = parentTransform * (endEffector._defaultPosition - _parentBone._defaultPosition);

					Vector3 beginLinkDirX = Vector3.zero;

					{
						Vector3 beginLinkToBendingLink0Direction = bendingLink0Position - beginLinkPosition;
						Vector3 beginLinkToEndDirection = endPosition - beginLinkPosition;
						if( SAFBIKVecNormalize2( ref beginLinkToBendingLink0Direction, ref beginLinkToEndDirection ) ) {

							Matrix3x3 effBasis;
							if( SAFBIKComputeBasisFromXZLockX( out effBasis, isRight ? effectorDirection : -effectorDirection, solveDirZ ) ) {
								Matrix3x3 bendBasis;
								Matrix3x3 endBasis;
								if( SAFBIKComputeBasisFromXZLockZ( out bendBasis, isRight ? beginLinkToBendingLink0Direction : -beginLinkToBendingLink0Direction, effBasis.column2 ) &&
									SAFBIKComputeBasisFromXZLockZ( out endBasis, isRight ? beginLinkToEndDirection : -beginLinkToEndDirection, effBasis.column2 ) ) {
									// effBasis  ... beginLink to current effector basis.
									// bendBasis ... beginLink to default bendLink0 basis.
									// endBasis  ... beginLink to default effector basis.

									Vector3 effX = isRight ? effBasis.column0 : -effBasis.column0;
									Vector3 effY = effBasis.column1;
									Vector3 effZ = effBasis.column2;
									Vector3 bendX = isRight ? bendBasis.column0 : -bendBasis.column0;
									Vector3 bendY = bendBasis.column1;
									Vector3 endX = isRight ? endBasis.column0 : -endBasis.column0;
									Vector3 endY = endBasis.column1;

									// rotBendX ... begin to current bendLink0 basis.			
									float endBendDotX = Vector3.Dot( bendX, endX ); // Cosine
									float endBendDotY = Vector3.Dot( bendX, endY ); // Sine
									Vector3 rotBendX = _Rotate( ref effX, ref effY, endBendDotX, endBendDotY );

									imm_isUpper = (Vector3.Dot( endY, effX ) >= 0.0f);

									bool imm_isLimitL = false;

									float endEffDotX = Vector3.Dot( endX, effX );
									if( imm_isUpper ) {
										if( isWarp ) {
											float traceLimitUAngle = _notThumb1PitchUTraceSmooth.angle;
											float cosTraceLimitUAngle = _notThumb1PitchUTraceSmooth.cos;
											if( traceLimitUAngle <= IKEpsilon || endEffDotX < cosTraceLimitUAngle ) {
												Vector3 rotBendY = Vector3.Cross( effZ, rotBendX );
												if( SAFBIKVecNormalize( ref rotBendY ) ) {
													float cosTraceAngle = _notThumb1PitchUTrace.cos;
													float sinTraceAngle = _notThumb1PitchUTrace.sin;
													beginLinkDirX = _Rotate( ref rotBendX, ref rotBendY, cosTraceAngle, isRight ? -sinTraceAngle : sinTraceAngle );
												}
											} else {
												float r = SAFBIKAcos( endEffDotX );
												r = r / traceLimitUAngle;
												r = _notThumb1PitchUTrace.angle * r;
												beginLinkDirX = _Rotate( ref bendX, ref bendY, r );
											}
										} else {
											solveFingerIK = true;
										}
									} else {
										if( isWarp ) {
											float baseAngle = Mathf.Abs( fingerBranch.notThumb1BaseAngle.angle );
											float traceAngle = Mathf.Max( baseAngle, _notThumb1PitchLTrace.angle );
											float cosTraceAngle = Mathf.Min( fingerBranch.notThumb1BaseAngle.cos, _notThumb1PitchLTrace.cos );

											if( endEffDotX < cosTraceAngle ) {
												solveFingerIK = true;
												float smoothLen = linkLength2 * 0.25f;
												if( effectorLength >= baseLength - (smoothLen) ) {
													_LerpEffectorLength(
														ref effectorLength, ref effectorDirection, ref effectorTranslate, ref effectorPosition, ref beginLinkPosition,
														baseLength - smoothLen, linkLength0 + linkLength1 + linkLength2, smoothLen );
												} else {
													// Nothing.
												}
											} else {
												if( traceAngle <= IKEpsilon || traceAngle == baseAngle ) {
													beginLinkDirX = bendX;
													if( traceAngle <= IKEpsilon ) {
														imm_traceRate = 1.0f;
													} else {
														float r = SAFBIKAcos( endEffDotX );
														imm_traceRate = r / traceAngle;
													}
												} else {
													float r = SAFBIKAcos( endEffDotX );
													r = r / traceAngle;
													imm_traceRate = r;
													r = (_notThumb1PitchLTrace.angle - baseAngle) * r;
													beginLinkDirX = _Rotate( ref bendX, ref bendY, -r );
												}
											}
										} else {
											solveFingerIK = true;
										}
									}

									if( isWarp ) {
										if( !solveFingerIK ) {
											if( effectorLength < baseLength - IKEpsilon ) {
												float extendLen = 0.0f;
												if( !imm_isLimitL ) {
													extendLen = Vector3.Dot( beginLinkDirX, effX );
													extendLen = SAFBIKSqrt( 1.0f - extendLen * extendLen ); // Cosine to Sine
													extendLen *= linkLength0; // Sine Length
												}
												float smoothLen = linkLength2 * 0.25f;
												if( extendLen > IKEpsilon && effectorLength >= baseLength - extendLen ) {
													float r = 1.0f - (effectorLength - (baseLength - extendLen)) / extendLen;
													beginLinkDirX = _FastLerpDir( ref beginLinkDirX, ref effX, r );
													imm_traceRate += (1.0f - imm_traceRate) * r;
												} else {
													solveFingerIK = true;
													if( effectorLength >= baseLength - (extendLen + smoothLen) ) {
														_LerpEffectorLength(
															ref effectorLength, ref effectorDirection, ref effectorTranslate, ref effectorPosition, ref beginLinkPosition,
															baseLength - (extendLen + smoothLen), linkLength0 + linkLength1 + linkLength2, smoothLen );
													} else {
														// Nothing.
													}
												}
											}
										}
									}
								}
							}
						}
					}

					if( !solveFingerIK ) {
						if( beginLinkDirX == Vector3.zero ) {
							return false;
						}

						if( !SAFBIKComputeBasisFromXZLockX( out beginLink.boneTransform.basis, isRight ? beginLinkDirX : -beginLinkDirX, solveDirZ ) ) {
							return false;
						}

						beginLink.boneTransform.origin = beginLinkPosition;
						SAFBIKMatMultRet0( ref beginLink.boneTransform.basis, ref beginLink.solvedToBoneBasis );

						bendingLink0Position = beginLink.boneTransform * (bendingLink0.bone._defaultPosition - beginLink.bone._defaultPosition);
						bendingLink1Position = beginLink.boneTransform * (bendingLink1.bone._defaultPosition - beginLink.bone._defaultPosition);
						endPosition = beginLink.boneTransform * (endEffector._defaultPosition - beginLink.bone._defaultPosition);

						Vector3 basedEffectorPosition = beginLinkPosition + effectorDirection * baseLength;

						Vector3 bendingLink0ToEffectorDirection = basedEffectorPosition - bendingLink0Position;
						Vector3 bendingLink0ToBendingLink0Direction = bendingLink1Position - bendingLink0Position;
						Vector3 bendingLink0ToEndDirection = endPosition - bendingLink0Position;
						if( !SAFBIKVecNormalize3( ref bendingLink0ToEffectorDirection, ref bendingLink0ToBendingLink0Direction, ref bendingLink0ToEndDirection ) ) {
							return false;
						}

						Vector3 bendingLink0DirX = Vector3.zero;

						{
							Matrix3x3 effBasis;
							if( !SAFBIKComputeBasisFromXZLockX( out effBasis, isRight ? bendingLink0ToEffectorDirection : -bendingLink0ToEffectorDirection, solveDirZ ) ) {
								return false;
							}
							Matrix3x3 bendBasis;
							Matrix3x3 endBasis;
							// Effector direction stamp X/Y Plane.(Feedback Y Axis.)
							if( !SAFBIKComputeBasisFromXZLockZ( out bendBasis, isRight ? bendingLink0ToBendingLink0Direction : -bendingLink0ToBendingLink0Direction, effBasis.column2 ) ||
								!SAFBIKComputeBasisFromXZLockZ( out endBasis, isRight ? bendingLink0ToEndDirection : -bendingLink0ToEndDirection, effBasis.column2 ) ) {
								return false;
							}

							Vector3 effX = isRight ? effBasis.column0 : -effBasis.column0;
							Vector3 effY = effBasis.column1;
							Vector3 bendX = isRight ? bendBasis.column0 : -bendBasis.column0;
							Vector3 endX = isRight ? endBasis.column0 : -endBasis.column0;
							Vector3 endY = endBasis.column1;

							float endBendDotX = Vector3.Dot( bendX, endX ); // Cosine
							float endBendDotY = Vector3.Dot( bendX, endY ); // Sine
							Vector3 rotBendX = _Rotate( ref effX, ref effY, endBendDotX, endBendDotY );

							if( imm_isUpper ) {
								bendingLink0DirX = _FastLerpDir( ref rotBendX, ref effX, imm_traceRate );
							} else {
								bendingLink0DirX = _FastLerpDir( ref bendX, ref effX, imm_traceRate );
							}
						}

						if( !SAFBIKComputeBasisFromXZLockX( out bendingLink0.boneTransform.basis, isRight ? bendingLink0DirX : -bendingLink0DirX, solveDirZ ) ) {
							return false;
						}

						bendingLink0.boneTransform.origin = bendingLink0Position;
						SAFBIKMatMultRet0( ref bendingLink0.boneTransform.basis, ref bendingLink0.solvedToBoneBasis );

						bendingLink1Position = bendingLink0.boneTransform * (bendingLink1.bone._defaultPosition - bendingLink0.bone._defaultPosition);

						{
							Vector3 dirX = basedEffectorPosition - bendingLink1Position;
							if( !SAFBIKVecNormalize( ref dirX ) ) {
								return false;
							}

							Vector3 dirZ;
							SAFBIKMatMultVec( out dirZ, ref bendingLink0.boneTransform.basis, ref bendingLink1.boneToSolvedBasis.column2 );

							if( !SAFBIKComputeBasisFromXZLockX( out bendingLink1.boneTransform.basis, isRight ? dirX : -dirX, dirZ ) ) {
								return false;
							}

							bendingLink1.boneTransform.origin = bendingLink1Position;
							SAFBIKMatMultRet0( ref bendingLink1.boneTransform.basis, ref bendingLink1.solvedToBoneBasis );
                        }
					}
				}

				if( solveFingerIK ) {
					{
						Vector3 linkSolved = SolveFingerIK(
							ref beginLinkPosition,
							ref effectorPosition,
							ref solveDirY,
							linkLength0,
							linkLength1,
							linkLength2,
							ref fingerBranch.fingerIKParams );

						if( linkSolved == Vector3.zero ) {
							return false;
						}

						// Limit angle for finger0.
						if( !isUpper ) {
							Matrix3x3 baseBasis;
							Vector3 dirX;
							SAFBIKMatMultVec( out dirX, ref parentTransform.basis, ref beginLink.boneToSolvedBasis.column0 );

							if( SAFBIKComputeBasisFromXZLockZ( out baseBasis, dirX, solveDirZ ) ) {
								Vector3 localFingerSolve;
								SAFBIKMatMultVecInv( out localFingerSolve, ref baseBasis, ref linkSolved );

								float finX = localFingerSolve.x;
								float finY = localFingerSolve.y;
								float finZ = localFingerSolve.z;

								float cosNotThumb1PitchLLimit = _notThumb1PitchLLimit.cos;
								if( (isRight && finX < cosNotThumb1PitchLLimit) || (!isRight && finX > -cosNotThumb1PitchLLimit) ) {
									float lenY = SAFBIKSqrt( 1.0f - (cosNotThumb1PitchLLimit * cosNotThumb1PitchLLimit + finZ * finZ) );
									localFingerSolve.x = isRight ? cosNotThumb1PitchLLimit : -cosNotThumb1PitchLLimit;
									localFingerSolve.y = (finY >= 0.0f) ? lenY : -lenY;
									SAFBIKMatMultVec( out linkSolved, ref baseBasis, ref localFingerSolve );
								}
							}
						}

						if( !SAFBIKComputeBasisFromXZLockX( out beginLink.boneTransform.basis, isRight ? linkSolved : -linkSolved, solveDirZ ) ) {
							return false;
						}

						beginLink.boneTransform.origin = beginLinkPosition;
						SAFBIKMatMultRet0( ref beginLink.boneTransform.basis, ref beginLink.solvedToBoneBasis );
					}

					{
						Vector3 bendingLink0Position = beginLink.boneTransform * (bendingLink0.bone._defaultPosition - beginLink.bone._defaultPosition);

						// Forcefix:
						Vector3 bendingLink0ToEffector = effectorPosition - bendingLink0Position;

						Vector3 dirZ;
						SAFBIKMatMultVec( out dirZ, ref beginLink.boneTransform.basis, ref _internalValues.defaultRootBasis.column2 );

						solveDirY = Vector3.Cross( dirZ, bendingLink0ToEffector );
						if( !SAFBIKVecNormalize( ref solveDirY ) ) {
							return false;
						}

						solveDirY = isRight ? solveDirY : -solveDirY;

						Vector3 linkSolved = SolveLimbIK(
							ref bendingLink0Position,
							ref effectorPosition,
							linkLength1,
							linkLength1Sq,
							linkLength2,
							linkLength2Sq,
							ref solveDirY );

						if( linkSolved == Vector3.zero ) {
							return false;
						}

						SAFBIKMatMultVec( out dirZ, ref beginLink.boneTransform.basis, ref bendingLink0.boneToSolvedBasis.column2 );

						if( !SAFBIKComputeBasisFromXZLockX( out bendingLink0.boneTransform.basis, isRight ? linkSolved : -linkSolved, dirZ ) ) {
							return false;
						}

						bendingLink0.boneTransform.origin = bendingLink0Position;
						SAFBIKMatMultRet0( ref bendingLink0.boneTransform.basis, ref bendingLink0.solvedToBoneBasis );
                    }

					{
						Vector3 bendingLink1Position = bendingLink0.boneTransform * (bendingLink1.bone._defaultPosition - bendingLink0.bone._defaultPosition);

						Vector3 dirX = effectorPosition - bendingLink1Position;
						if( !SAFBIKVecNormalize( ref dirX ) ) {
							return false;
						}

						Vector3 dirZ;
						SAFBIKMatMultVec( out dirZ, ref bendingLink0.boneTransform.basis, ref bendingLink1.boneToSolvedBasis.column2 );

						if( !SAFBIKComputeBasisFromXZLockX( out bendingLink1.boneTransform.basis, isRight ? dirX : -dirX, dirZ ) ) {
							return false;
						}

						bendingLink1.boneTransform.origin = bendingLink1Position;
						SAFBIKMatMultRet0( ref bendingLink1.boneTransform.basis, ref bendingLink1.solvedToBoneBasis );
                    }
				}

				Quaternion worldRotation;
				SAFBIKMatMultGetRot( out worldRotation, ref beginLink.boneTransform.basis, ref beginLink.bone._defaultBasis );
				beginLink.bone.worldRotation = worldRotation;
				SAFBIKMatMultGetRot( out worldRotation, ref bendingLink0.boneTransform.basis, ref bendingLink0.bone._defaultBasis );
				bendingLink0.bone.worldRotation = worldRotation;
				SAFBIKMatMultGetRot( out worldRotation, ref bendingLink1.boneTransform.basis, ref bendingLink1.bone._defaultBasis );
				bendingLink1.bone.worldRotation = worldRotation;
				return true;
			}

			bool _SolveThumb( ref Matrix3x4 parentTransform )
			{
				_FingerBranch fingerBranch = _fingerBranches[(int)FingerType.Thumb];
				if( fingerBranch == null || fingerBranch.fingerLinks.Length != 3 ) {
					return false;
				}

				_FingerLink fingerLink0 = fingerBranch.fingerLinks[0];
				_FingerLink fingerLink1 = fingerBranch.fingerLinks[1];
				_FingerLink fingerLink2 = fingerBranch.fingerLinks[2];

				_ThumbLink thumbLink0 = _thumbBranch.thumbLinks[0];
				_ThumbLink thumbLink1 = _thumbBranch.thumbLinks[1];
				_ThumbLink thumbLink2 = _thumbBranch.thumbLinks[2];

				bool isRight = (_fingerIKType == FingerIKType.RightWrist);

				{
					Vector3 fingerLinkPosition0 = parentTransform * (fingerLink0.bone._defaultPosition - _parentBone._defaultPosition);
					var endEffector = fingerBranch.effector;
                    Vector3 effectorPosition = _GetEffectorPosition( _internalValues, _parentBone, fingerLink0.bone, endEffector, fingerBranch.link0ToEffectorLength, ref parentTransform );
					Vector3 effectorTranslate = effectorPosition - fingerLinkPosition0;
					float effectorLength = effectorTranslate.magnitude;
					if( effectorLength < IKEpsilon || fingerBranch.link0ToEffectorLength < IKEpsilon ) {
						return false;
					}

					Vector3 effectorDirection = effectorTranslate * (1.0f / effectorLength);
					if( effectorLength > fingerBranch.link0ToEffectorLength ) {
						effectorLength = fingerBranch.link0ToEffectorLength;
						effectorTranslate = effectorDirection * fingerBranch.link0ToEffectorLength;
						effectorPosition = fingerLinkPosition0 + effectorTranslate;
					}

					{
						// thumb0 (1st pass.)
						// Simply, compute direction thumb0 to effector.
						Vector3 dirX = effectorDirection;

						// Limit yaw pitch for thumb0 to effector.
						if( _thumbBranch.thumb0_isLimited ) {
							Matrix3x3 beginToEndBasis;
							SAFBIKMatMult( out beginToEndBasis, ref parentTransform.basis, ref fingerBranch.boneToSolvedBasis );
							Vector3 localEffectorDirection;
							SAFBIKMatMultVecInv( out localEffectorDirection, ref beginToEndBasis, ref dirX );
							if( _LimitYZ(
								isRight,
								ref localEffectorDirection,
								_thumbBranch.thumb0_lowerLimit,
								_thumbBranch.thumb0_upperLimit,
								_thumbBranch.thumb0_innerLimit,
								_thumbBranch.thumb0_outerLimit ) ) {
								SAFBIKMatMultVec( out dirX, ref beginToEndBasis, ref localEffectorDirection ); // Local to world.
							}
						}

						Vector3 dirY;
						SAFBIKMatMultVec( out dirY, ref parentTransform.basis, ref thumbLink0.thumb_boneToSolvedBasis.column1 );

						if( !SAFBIKComputeBasisFromXYLockX( out fingerLink0.boneTransform.basis, isRight ? dirX : -dirX, dirY ) ) {
							return false;
						}

						fingerLink0.boneTransform.origin = fingerLinkPosition0;
						SAFBIKMatMultRet0( ref fingerLink0.boneTransform.basis, ref thumbLink0.thumb_solvedToBoneBasis );
					}

					// thumb0 / Limit length based thumb1/2 (Type3)
					{
						Vector3 fingerLinkPosition1 = fingerLink0.boneTransform * (fingerLink1.bone._defaultPosition - fingerLink0.bone._defaultPosition);
						Vector3 effectorTranslate1to3 = effectorPosition - fingerLinkPosition1;
						float effectorLength1to3 = effectorTranslate1to3.magnitude;

						if( effectorLength1to3 < _thumbBranch.linkLength1to3 - IKEpsilon ) {
							Vector3 effectorTranslate0to3 = effectorPosition - fingerLink0.boneTransform.origin;
							float effectorLength0to3Sq;
							float effectorLength0to3 = SAFBIKVecLengthAndLengthSq( out effectorLength0to3Sq, ref effectorTranslate0to3 );

							float baseTheta = 1.0f;
							if( effectorLength0to3 > IKEpsilon ) {
								Vector3 baseDirection0to1 = fingerLinkPosition1 - fingerLink0.boneTransform.origin;
								if( SAFBIKVecNormalize( ref baseDirection0to1 ) ) {
									Vector3 effectorDirection0to3 = effectorTranslate0to3 * (1.0f / effectorLength0to3);
									baseTheta = Vector3.Dot( effectorDirection0to3, baseDirection0to1 );
								}
							}

							float moveLenA = _thumbBranch.linkLength0to1;
							float moveLenASq = _thumbBranch.linkLength0to1Sq;
							float moveLenB = effectorLength0to3;
							float moveLenBSq = effectorLength0to3Sq;
							float moveLenC = effectorLength1to3 + (_thumbBranch.linkLength1to3 - effectorLength1to3) * 0.5f; // 0.5f = Magic number.(Balancer)
							float moveLenCSq = moveLenC * moveLenC;

							float moveTheta = _ComputeTriangleTheta( moveLenA, moveLenB, moveLenC, moveLenASq, moveLenBSq, moveLenCSq );
							if( moveTheta < baseTheta ) {
								float newAngle = SAFBIKAcos( moveTheta ) - SAFBIKAcos( baseTheta );
								if( newAngle > 0.01f * Mathf.Deg2Rad ) {
									// moveLenAtoAD = Move length thumb1 origin with bending thumb0.
									float moveLenASq2 = moveLenASq * 2.0f;
									float moveLenAtoAD = SAFBIKSqrt( moveLenASq2 * (1.0f - SAFBIKCos( newAngle )) );
									if( moveLenAtoAD > IKEpsilon ) {
										Vector3 solveDirection;
										SAFBIKMatMultVec( out solveDirection, ref fingerLink0.boneTransform.basis, ref _thumbBranch.thumbSolveZ );

										fingerLinkPosition1 += solveDirection * moveLenAtoAD;

										Vector3 newX = fingerLinkPosition1 - fingerLink0.boneTransform.origin;
										if( SAFBIKVecNormalize( ref newX ) ) {
											Vector3 dirY;
											SAFBIKMatMultVec( out dirY, ref fingerLink0.boneTransform.basis, ref fingerLink0.boneToSolvedBasis.column1 );

											Matrix3x3 solveBasis0;
											if( SAFBIKComputeBasisFromXYLockX( out solveBasis0, isRight ? newX : -newX, dirY ) ) {
												SAFBIKMatMult( out fingerLink0.boneTransform.basis, ref solveBasis0, ref fingerLink0.solvedToBoneBasis );
                                            }
										}
									}
								}
							}
						}
					}

					{
						// thumb1
						{
							Vector3 fingerLinkPosition1 = fingerLink0.boneTransform * (fingerLink1.bone._defaultPosition - fingerLink0.bone._defaultPosition);
							// Simply, compute direction thumb1 to effector.
							// (Compute push direction for thumb1.)
							Vector3 dirX = effectorPosition - fingerLinkPosition1;
							if( !SAFBIKVecNormalize( ref dirX ) ) {
								return false;
							}

							Vector3 dirY;
							SAFBIKMatMultVec( out dirY, ref fingerLink0.boneTransform.basis, ref thumbLink1.thumb_boneToSolvedBasis.column1 );

							if( !SAFBIKComputeBasisFromXYLockX( out fingerLink1.boneTransform.basis, isRight ? dirX : -dirX, dirY ) ) {
								return false;
							}

							fingerLink1.boneTransform.origin = fingerLinkPosition1;
							SAFBIKMatMultRet0( ref fingerLink1.boneTransform.basis, ref thumbLink1.thumb_solvedToBoneBasis );
						}

						Vector3 effectorTranslate1to3 = effectorPosition - fingerLink1.boneTransform.origin;
						float effectorLength1to3Sq = effectorTranslate1to3.sqrMagnitude;
						float effectorLength1to3 = SAFBIKSqrt( effectorLength1to3Sq );

						float moveLenA = _thumbBranch.linkLength1to2;
						float moveLenASq = _thumbBranch.linkLength1to2Sq;
						float moveLenB = effectorLength1to3;
						float moveLenBSq = effectorLength1to3Sq;
						float moveLenC = _thumbBranch.linkLength2to3;
						float moveLenCSq = _thumbBranch.linkLength2to3Sq;

						// Compute angle moved A/B origin.
						float moveThetaAtoB = _ComputeTriangleTheta( moveLenA, moveLenB, moveLenC, moveLenASq, moveLenBSq, moveLenCSq );
						if( moveThetaAtoB < _thumbBranch.thumb1_baseThetaAtoB ) {
							float newAngle = SAFBIKAcos( moveThetaAtoB ) - _thumbBranch.thumb1_Acos_baseThetaAtoB;
							if( newAngle > 0.01f * Mathf.Deg2Rad ) {
								float moveLenASq2 = moveLenASq * 2.0f;
								float moveLenAtoAD = SAFBIKSqrt( moveLenASq2 - moveLenASq2 * SAFBIKCos( newAngle ) );
								{
									Vector3 solveDirection;
									SAFBIKMatMultVec( out solveDirection, ref fingerLink1.boneTransform.basis, ref _thumbBranch.thumbSolveZ );
									Vector3 fingerLinkPosition2 = fingerLink1.boneTransform * (fingerLink2.bone._defaultPosition - fingerLink1.bone._defaultPosition);
									fingerLinkPosition2 += solveDirection * moveLenAtoAD;

									Vector3 newX = fingerLinkPosition2 - fingerLink1.boneTransform.origin;
									if( SAFBIKVecNormalize( ref newX ) ) {
										Vector3 dirY;
										SAFBIKMatMultVec( out dirY, ref fingerLink1.boneTransform.basis, ref fingerLink1.boneToSolvedBasis.column1 );
										Matrix3x3 solveBasis1;
										if( SAFBIKComputeBasisFromXYLockX( out solveBasis1, isRight ? newX : -newX, dirY ) ) {
											SAFBIKMatMult( out fingerLink1.boneTransform.basis, ref solveBasis1, ref fingerLink1.solvedToBoneBasis );
                                        }
									}
								}
							}
						}
					}

					{
						// thumb2
						// Simply, compute direction thumb2 to effector.
						Vector3 fingerLinkPosition2 = fingerLink1.boneTransform * (fingerLink2.bone._defaultPosition - fingerLink1.bone._defaultPosition);
						Vector3 dirX = effectorPosition - fingerLinkPosition2;
						if( !SAFBIKVecNormalize( ref dirX ) ) {
							return false;
						}

						Vector3 dirY;
						SAFBIKMatMultVec( out dirY, ref fingerLink1.boneTransform.basis, ref thumbLink2.thumb_boneToSolvedBasis.column1 );
						if( !SAFBIKComputeBasisFromXYLockX( out fingerLink2.boneTransform.basis, isRight ? dirX : -dirX, dirY ) ) {
							return false;
						}

						fingerLink2.boneTransform.origin = fingerLinkPosition2;
						SAFBIKMatMultRet0( ref fingerLink2.boneTransform.basis, ref thumbLink2.thumb_solvedToBoneBasis );
					}
				}

				Quaternion worldRotation;
				SAFBIKMatMultGetRot( out worldRotation, ref fingerLink0.boneTransform.basis, ref fingerLink0.bone._defaultBasis );
				fingerLink0.bone.worldRotation = worldRotation;
				SAFBIKMatMultGetRot( out worldRotation, ref fingerLink1.boneTransform.basis, ref fingerLink1.bone._defaultBasis );
				fingerLink1.bone.worldRotation = worldRotation;
				SAFBIKMatMultGetRot( out worldRotation, ref fingerLink2.boneTransform.basis, ref fingerLink2.bone._defaultBasis );
				fingerLink2.bone.worldRotation = worldRotation;
				return true;
			}


		}
	}
}