// Copyright (c) 2016 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

//#define _ENABLE_LIMBIK_FORCEFIX

using UnityEngine;

namespace SA
{
	public partial class FullBodyIK
	{
		public class LimbIK
		{
			struct RollBone
			{
				public Bone bone;
				public float rate;
			}

			Settings _settings;
			InternalValues _internalValues;

			public LimbIKLocation _limbIKLocation;
			LimbIKType _limbIKType;
			Side _limbIKSide;

			Bone _beginBone;
			Bone _bendingBone;
			Bone _endBone;
			Effector _bendingEffector;
			Effector _endEffector;

			RollBone[] _armRollBones;
			RollBone[] _elbowRollBones;

			public float _beginToBendingLength;
			public float _beginToBendingLengthSq;
			public float _bendingToEndLength;
			public float _bendingToEndLengthSq;

			Matrix3x3 _beginToBendingBoneBasis = Matrix3x3.identity;
			Quaternion _endEffectorToWorldRotation = Quaternion.identity;

			Matrix3x3 _effectorToBeginBoneBasis = Matrix3x3.identity;
			float _defaultSinTheta = 0.0f;
			float _defaultCosTheta = 1.0f;

			float _beginToEndMaxLength = 0.0f;
			CachedScaledValue _effectorMaxLength = CachedScaledValue.zero;
			CachedScaledValue _effectorMinLength = CachedScaledValue.zero;

			float _leg_upperLimitNearCircleZ = 0.0f;
			float _leg_upperLimitNearCircleY = 0.0f;

			CachedScaledValue _arm_elbowBasisForcefixEffectorLengthBegin = CachedScaledValue.zero;
			CachedScaledValue _arm_elbowBasisForcefixEffectorLengthEnd = CachedScaledValue.zero;

			// for Arm roll.
			Matrix3x3 _arm_bendingToBeginBoneBasis = Matrix3x3.identity;
			Quaternion _arm_bendingWorldToBeginBoneRotation = Quaternion.identity;
			// for Hand roll.
			Quaternion _arm_endWorldToBendingBoneRotation = Quaternion.identity;
			// for Arm/Hand roll.(Temporary)
			bool _arm_isSolvedLimbIK;
			Matrix3x3 _arm_solvedBeginBoneBasis = Matrix3x3.identity;
			Matrix3x3 _arm_solvedBendingBoneBasis = Matrix3x3.identity;

			public LimbIK( FullBodyIK fullBodyIK, LimbIKLocation limbIKLocation )
			{
				Assert( fullBodyIK != null );
				if( fullBodyIK == null ) {
					return;
				}

				_settings = fullBodyIK.settings;
				_internalValues = fullBodyIK.internalValues;

				_limbIKLocation = limbIKLocation;
				_limbIKType = FullBodyIK.ToLimbIKType( limbIKLocation );
				_limbIKSide = FullBodyIK.ToLimbIKSide( limbIKLocation );

				if( _limbIKType == LimbIKType.Leg ) {
					var legBones = (_limbIKSide == Side.Left) ? fullBodyIK.leftLegBones : fullBodyIK.rightLegBones;
					var legEffectors = (_limbIKSide == Side.Left) ? fullBodyIK.leftLegEffectors : fullBodyIK.rightLegEffectors;
					_beginBone = legBones.leg;
					_bendingBone = legBones.knee;
					_endBone = legBones.foot;
					_bendingEffector = legEffectors.knee;
					_endEffector = legEffectors.foot;
				} else if( _limbIKType == LimbIKType.Arm ) {
					var armBones = (_limbIKSide == Side.Left) ? fullBodyIK.leftArmBones : fullBodyIK.rightArmBones;
					var armEffectors = (_limbIKSide == Side.Left) ? fullBodyIK.leftArmEffectors : fullBodyIK.rightArmEffectors;
					_beginBone = armBones.arm;
					_bendingBone = armBones.elbow;
					_endBone = armBones.wrist;
					_bendingEffector = armEffectors.elbow;
					_endEffector = armEffectors.wrist;
					_PrepareRollBones( ref _armRollBones, armBones.armRoll );
					_PrepareRollBones( ref _elbowRollBones, armBones.elbowRoll );
				}

				_Prepare( fullBodyIK );
			}

			void _Prepare( FullBodyIK fullBodyIK )
			{
				SAFBIKQuatMultInv0( out _endEffectorToWorldRotation, ref _endEffector._defaultRotation, ref _endBone._defaultRotation );

				// for _defaultCosTheta, _defaultSinTheta
				_beginToBendingLength = _bendingBone._defaultLocalLength.length;
				_beginToBendingLengthSq = _bendingBone._defaultLocalLength.lengthSq;
				_bendingToEndLength = _endBone._defaultLocalLength.length;
				_bendingToEndLengthSq = _endBone._defaultLocalLength.lengthSq;

				float beginToEndLength, beginToEndLengthSq;
				beginToEndLength = SAFBIKVecLengthAndLengthSq2( out beginToEndLengthSq,
					ref _endBone._defaultPosition, ref _beginBone._defaultPosition );

				_defaultCosTheta = ComputeCosTheta(
					_bendingToEndLengthSq,          // lenASq
					beginToEndLengthSq,             // lenBSq
					_beginToBendingLengthSq,        // lenCSq
					beginToEndLength,               // lenB
					_beginToBendingLength );        // lenC

				_defaultSinTheta = SAFBIKSqrtClamp01( 1.0f - _defaultCosTheta * _defaultCosTheta );
				CheckNaN( _defaultSinTheta );
			}

			bool _isSyncDisplacementAtLeastOnce;

			void _SyncDisplacement()
			{
				// Require to call before _UpdateArgs()

				// Measure bone length.(Using worldPosition)
				// Force execution on 1st time. (Ignore case _settings.syncDisplacement == SyncDisplacement.Disable)
				if( _settings.syncDisplacement == SyncDisplacement.Everyframe || !_isSyncDisplacementAtLeastOnce ) {
					_isSyncDisplacementAtLeastOnce = true;

					SAFBIKMatMult( out _beginToBendingBoneBasis, ref _beginBone._localAxisBasisInv, ref _bendingBone._localAxisBasis );

					if( _armRollBones != null ) {
						if( _beginBone != null && _bendingBone != null ) {
							SAFBIKMatMult( out _arm_bendingToBeginBoneBasis, ref _bendingBone._boneToBaseBasis, ref _beginBone._baseToBoneBasis );
							SAFBIKMatMultGetRot( out _arm_bendingWorldToBeginBoneRotation, ref _bendingBone._worldToBaseBasis, ref _beginBone._baseToBoneBasis );
						}
					}

					if( _elbowRollBones != null ) {
						if( _endBone != null && _bendingBone != null ) {
							SAFBIKMatMultGetRot( out _arm_endWorldToBendingBoneRotation, ref _endBone._worldToBaseBasis, ref _bendingBone._baseToBoneBasis );
						}
					}

					_beginToBendingLength	= _bendingBone._defaultLocalLength.length;
					_beginToBendingLengthSq	= _bendingBone._defaultLocalLength.lengthSq;
					_bendingToEndLength		= _endBone._defaultLocalLength.length;
					_bendingToEndLengthSq	= _endBone._defaultLocalLength.lengthSq;
					_beginToEndMaxLength	= _beginToBendingLength + _bendingToEndLength;

					Vector3 beginToEndDir = _endBone._defaultPosition - _beginBone._defaultPosition;
					if( SAFBIKVecNormalize( ref beginToEndDir ) ) {
						if( _limbIKType == LimbIKType.Arm ) {
							if( _limbIKSide == Side.Left ) {
								beginToEndDir = -beginToEndDir;
							}
							Vector3 dirY = _internalValues.defaultRootBasis.column1;
							Vector3 dirZ = _internalValues.defaultRootBasis.column2;
							if( SAFBIKComputeBasisLockX( out _effectorToBeginBoneBasis, ref beginToEndDir, ref dirY, ref dirZ ) ) {
								_effectorToBeginBoneBasis = _effectorToBeginBoneBasis.transpose;
							}
						} else {
							beginToEndDir = -beginToEndDir;
							Vector3 dirX = _internalValues.defaultRootBasis.column0;
							Vector3 dirZ = _internalValues.defaultRootBasis.column2;
							// beginToEffectorBasis( identity to effectorDir(y) )
							if( SAFBIKComputeBasisLockY( out _effectorToBeginBoneBasis, ref dirX, ref beginToEndDir, ref dirZ ) ) {
								// effectorToBeginBasis( effectorDir(y) to identity )
								_effectorToBeginBoneBasis = _effectorToBeginBoneBasis.transpose;
							}
						}

						// effectorToBeginBasis( effectorDir(y) to _beginBone._localAxisBasis )
						SAFBIKMatMultRet0( ref _effectorToBeginBoneBasis, ref _beginBone._localAxisBasis );
					}

					if( _limbIKType == LimbIKType.Leg ) {
						_leg_upperLimitNearCircleZ = 0.0f;
						_leg_upperLimitNearCircleY = _beginToEndMaxLength;
					}

					// Forcereset args.
					_SyncDisplacement_UpdateArgs();
                }
			}

			float _cache_legUpperLimitAngle = 0.0f;
			float _cache_kneeUpperLimitAngle = 0.0f;

			void _UpdateArgs()
			{
				if( _limbIKType == LimbIKType.Leg ) {
					float effectorMinLengthRate = _settings.limbIK.legEffectorMinLengthRate;
                    if( _effectorMinLength._b != effectorMinLengthRate ) {
						_effectorMinLength._Reset( _beginToEndMaxLength, effectorMinLengthRate );
					}

					if( _cache_kneeUpperLimitAngle != _settings.limbIK.prefixKneeUpperLimitAngle ||
						_cache_legUpperLimitAngle != _settings.limbIK.prefixLegUpperLimitAngle ) {
						_cache_kneeUpperLimitAngle = _settings.limbIK.prefixKneeUpperLimitAngle;
						_cache_legUpperLimitAngle = _settings.limbIK.prefixLegUpperLimitAngle;

						// Memo: Their CachedDegreesToCosSin aren't required caching. (Use instantly.)
						CachedDegreesToCosSin kneeUpperLimitTheta = new CachedDegreesToCosSin( _settings.limbIK.prefixKneeUpperLimitAngle );
						CachedDegreesToCosSin legUpperLimitTheta = new CachedDegreesToCosSin( _settings.limbIK.prefixLegUpperLimitAngle );

						_leg_upperLimitNearCircleZ = _beginToBendingLength * legUpperLimitTheta.cos
													+ _bendingToEndLength * kneeUpperLimitTheta.cos;

						_leg_upperLimitNearCircleY = _beginToBendingLength * legUpperLimitTheta.sin
													+ _bendingToEndLength * kneeUpperLimitTheta.sin;
					}
				}

				if( _limbIKType == LimbIKType.Arm ) {
					float beginRate = _settings.limbIK.armBasisForcefixEffectorLengthRate - _settings.limbIK.armBasisForcefixEffectorLengthLerpRate;
					float endRate = _settings.limbIK.armBasisForcefixEffectorLengthRate;
					if( _arm_elbowBasisForcefixEffectorLengthBegin._b != beginRate ) {
						_arm_elbowBasisForcefixEffectorLengthBegin._Reset( _beginToEndMaxLength, beginRate );
                    }
					if( _arm_elbowBasisForcefixEffectorLengthEnd._b != endRate ) {
						_arm_elbowBasisForcefixEffectorLengthEnd._Reset( _beginToEndMaxLength, endRate );
					}
				}

				float effectorMaxLengthRate = (_limbIKType == LimbIKType.Leg) ? _settings.limbIK.legEffectorMaxLengthRate : _settings.limbIK.armEffectorMaxLengthRate;
				if( _effectorMaxLength._b != effectorMaxLengthRate ) {
					_effectorMaxLength._Reset( _beginToEndMaxLength, effectorMaxLengthRate );
				}
			}

			void _SyncDisplacement_UpdateArgs()
			{
				if( _limbIKType == LimbIKType.Leg ) {
					float effectorMinLengthRate = _settings.limbIK.legEffectorMinLengthRate;
					_effectorMinLength._Reset( _beginToEndMaxLength, effectorMinLengthRate );

					// Memo: Their CachedDegreesToCosSin aren't required caching. (Use instantly.)
					CachedDegreesToCosSin kneeUpperLimitTheta = new CachedDegreesToCosSin( _settings.limbIK.prefixKneeUpperLimitAngle );
					CachedDegreesToCosSin legUpperLimitTheta = new CachedDegreesToCosSin( _settings.limbIK.prefixLegUpperLimitAngle );

					_leg_upperLimitNearCircleZ = _beginToBendingLength * legUpperLimitTheta.cos
												+ _bendingToEndLength * kneeUpperLimitTheta.cos;

					_leg_upperLimitNearCircleY = _beginToBendingLength * legUpperLimitTheta.sin
												+ _bendingToEndLength * kneeUpperLimitTheta.sin;
				}

				float effectorMaxLengthRate = (_limbIKType == LimbIKType.Leg) ? _settings.limbIK.legEffectorMaxLengthRate : _settings.limbIK.armEffectorMaxLengthRate;
				_effectorMaxLength._Reset( _beginToEndMaxLength, effectorMaxLengthRate );
			}

			// for animatorEnabled
			bool _isPresolvedBending = false;
			Matrix3x3 _presolvedBendingBasis = Matrix3x3.identity;
			Vector3 _presolvedEffectorDir = Vector3.zero;
			float _presolvedEffectorLength = 0.0f;

			// effectorDir to beginBoneBasis
			void _SolveBaseBasis( out Matrix3x3 baseBasis, ref Matrix3x3 parentBaseBasis, ref Vector3 effectorDir )
			{
				if( _limbIKType == LimbIKType.Arm ) {
					Vector3 dirX = (_limbIKSide == Side.Left) ? -effectorDir : effectorDir;
					Vector3 basisY = parentBaseBasis.column1;
					Vector3 basisZ = parentBaseBasis.column2;
					if( SAFBIKComputeBasisLockX( out baseBasis, ref dirX, ref basisY, ref basisZ ) ) {
						SAFBIKMatMultRet0( ref baseBasis, ref _effectorToBeginBoneBasis );
					} else { // Failsafe.(Counts as default effectorDir.)
						SAFBIKMatMult( out baseBasis, ref parentBaseBasis, ref _beginBone._localAxisBasis );
                    }
				} else {
					Vector3 dirY = -effectorDir;
					Vector3 basisX = parentBaseBasis.column0;
					Vector3 basisZ = parentBaseBasis.column2;
					if( SAFBIKComputeBasisLockY( out baseBasis, ref basisX, ref dirY, ref basisZ ) ) {
						SAFBIKMatMultRet0( ref baseBasis, ref _effectorToBeginBoneBasis );
                    } else { // Failsafe.(Counts as default effectorDir.)
						SAFBIKMatMult( out baseBasis, ref parentBaseBasis, ref _beginBone._localAxisBasis );
                    }
				}
			}

			static void _PrepareRollBones( ref RollBone[] rollBones, Bone[] bones )
			{
				if( bones != null && bones.Length > 0 ) {
					int length = bones.Length;
					float t = 1.0f / (float)(length + 1);
					float r = t;
					rollBones = new RollBone[length];
					for( int i = 0; i < length; ++i, r += t ) {
						rollBones[i].bone = bones[i];
						rollBones[i].rate = r;
					}
				} else {
					rollBones = null;
                }
			}

			public void PresolveBeinding()
			{
				_SyncDisplacement();

				bool presolvedEnabled = (_limbIKType == LimbIKType.Leg) ? _settings.limbIK.presolveKneeEnabled : _settings.limbIK.presolveElbowEnabled;
				if( !presolvedEnabled ) {
					return;
				}

				_isPresolvedBending = false;

				if( _beginBone == null ||
					!_beginBone.transformIsAlive ||
					_beginBone.parentBone == null ||
					!_beginBone.parentBone.transformIsAlive ||
					_bendingEffector == null ||
					_bendingEffector.bone == null ||
					!_bendingEffector.bone.transformIsAlive ||
					_endEffector == null ||
					_endEffector.bone == null ||
					!_endEffector.bone.transformIsAlive ) {
					return ; // Failsafe.
				}

				if( !_internalValues.animatorEnabled ) {
					return; // No require.
				}

				if( _bendingEffector.positionEnabled ) {
					return; // No require.
				}

				if( _limbIKType == LimbIKType.Leg ) {
					if( _settings.limbIK.presolveKneeRate < IKEpsilon ) {
						return; // No effect.
					}
				} else {
					if( _settings.limbIK.presolveElbowRate < IKEpsilon ) {
						return; // No effect.
					}
				}

				Vector3 beginPos = _beginBone.worldPosition;
				Vector3 bendingPos = _bendingEffector.bone.worldPosition;
				Vector3 effectorPos = _endEffector.bone.worldPosition;
				Vector3 effectorTrans = effectorPos - beginPos;
				Vector3 bendingTrans = bendingPos - beginPos;

				float effectorLen = effectorTrans.magnitude;
				float bendingLen = bendingTrans.magnitude;
				if( effectorLen <= IKEpsilon || bendingLen <= IKEpsilon ) {
					return;
				}

				Vector3 effectorDir = effectorTrans * (1.0f / effectorLen);
				Vector3 bendingDir = bendingTrans * (1.0f / bendingLen);

				Matrix3x3 parentBaseBasis;
				Quaternion parentBoneWorldRotation = _beginBone.parentBone.worldRotation;
                SAFBIKMatSetRotMult( out parentBaseBasis, ref parentBoneWorldRotation, ref _beginBone.parentBone._worldToBaseRotation );

				// Solve EffectorDir Based Basis.
				Matrix3x3 baseBasis;
				_SolveBaseBasis( out baseBasis, ref parentBaseBasis, ref effectorDir );

				_presolvedEffectorDir = effectorDir;
				_presolvedEffectorLength = effectorLen;

				Matrix3x3 toBasis;
				if( _limbIKType == LimbIKType.Arm ) {
					Vector3 dirX = (_limbIKSide == Side.Left) ? -bendingDir : bendingDir;
					Vector3 basisY = parentBaseBasis.column1;
					Vector3 basisZ = parentBaseBasis.column2;
					if( SAFBIKComputeBasisLockX( out toBasis, ref dirX, ref basisY, ref basisZ ) ) {
						SAFBIKMatMultInv1( out _presolvedBendingBasis, ref toBasis, ref baseBasis );
						_isPresolvedBending = true;
					}
				} else {
					Vector3 dirY = -bendingDir;
					Vector3 basisX = parentBaseBasis.column0;
					Vector3 basisZ = parentBaseBasis.column2;
					if( SAFBIKComputeBasisLockY( out toBasis, ref basisX, ref dirY, ref basisZ ) ) {
						SAFBIKMatMultInv1( out _presolvedBendingBasis, ref toBasis, ref baseBasis );
						_isPresolvedBending = true;
					}
				}
			}

			//------------------------------------------------------------------------------------------------------------

			bool _PrefixLegEffectorPos_UpperNear( ref Vector3 localEffectorTrans )
			{
				float y = localEffectorTrans.y - _leg_upperLimitNearCircleY;
				float z = localEffectorTrans.z;

				float rZ = _leg_upperLimitNearCircleZ;
                float rY = _leg_upperLimitNearCircleY + _effectorMinLength.value;

				if( rZ > IKEpsilon && rY > IKEpsilon ) {
					bool isLimited = false;

					z /= rZ;
					if( y > _leg_upperLimitNearCircleY ) {
						isLimited = true;
					} else {
						y /= rY;
						float len = SAFBIKSqrt( y * y + z * z );
						if( len < 1.0f ) {
							isLimited = true;
						}
					}

					if( isLimited ) {
						float n = SAFBIKSqrt( 1.0f - z * z );
						if( n > IKEpsilon ) { // Memo: Upper only.
							localEffectorTrans.y = -n * rY + _leg_upperLimitNearCircleY;
						} else { // Failsafe.
							localEffectorTrans.z = 0.0f;
							localEffectorTrans.y = -_effectorMinLength.value;
						}
						return true;
					}
				}

				return false;
			}

			static bool _PrefixLegEffectorPos_Circular_Far( ref Vector3 localEffectorTrans, float effectorLength )
			{
				return _PrefixLegEffectorPos_Circular( ref localEffectorTrans, effectorLength, true );
            }

			static bool _PrefixLegEffectorPos_Circular( ref Vector3 localEffectorTrans, float effectorLength, bool isFar )
			{
				float y = localEffectorTrans.y;
				float z = localEffectorTrans.z;
				float len = SAFBIKSqrt( y * y + z * z );
				if( (isFar && len > effectorLength) || (!isFar && len < effectorLength) ) {
					float n = SAFBIKSqrt( effectorLength * effectorLength - localEffectorTrans.z * localEffectorTrans.z );
					if( n > IKEpsilon ) { // Memo: Lower only.
						localEffectorTrans.y = -n;
					} else { // Failsafe.
						localEffectorTrans.z = 0.0f;
						localEffectorTrans.y = -effectorLength;
					}

					return true;
				}

				return false;
			}

			static bool _PrefixLegEffectorPos_Upper_Circular_Far( ref Vector3 localEffectorTrans,
				float centerPositionZ,
				float effectorLengthZ, float effectorLengthY )
			{
				if( effectorLengthY > IKEpsilon && effectorLengthZ > IKEpsilon ) {
					float y = localEffectorTrans.y;
					float z = localEffectorTrans.z - centerPositionZ;

					y /= effectorLengthY;
					z /= effectorLengthZ;

					float len = SAFBIKSqrt( y * y + z * z );
					if( len > 1.0f ) {
						float n = SAFBIKSqrt( 1.0f - z * z );
						if( n > IKEpsilon ) { // Memo: Upper only.
							localEffectorTrans.y = n * effectorLengthY;
						} else { // Failsafe.
							localEffectorTrans.z = centerPositionZ;
							localEffectorTrans.y = effectorLengthY;
						}

						return true;
					}
				}

				return false;
			}

			//------------------------------------------------------------------------------------------------------------

			// for Arms.

			const float _LocalDirMaxTheta = 0.99f;
			const float _LocalDirLerpTheta = 0.01f;

			// Lefthand based.
			static void _ComputeLocalDirXZ( ref Vector3 localDir, out Vector3 localDirXZ )
			{
				if( localDir.y >= _LocalDirMaxTheta - IKEpsilon ) {
					localDirXZ = new Vector3( 1.0f, 0.0f, 0.0f );
				} else if( localDir.y > _LocalDirMaxTheta - _LocalDirLerpTheta - IKEpsilon ) {
					float r = (localDir.y - (_LocalDirMaxTheta - _LocalDirLerpTheta)) * (1.0f / _LocalDirLerpTheta);
					localDirXZ = new Vector3( localDir.x + (1.0f - localDir.x) * r, 0.0f, localDir.z - localDir.z * r );
					if( !SAFBIKVecNormalizeXZ( ref localDirXZ ) ) {
						localDirXZ = new Vector3( 1.0f, 0.0f, 0.0f );
					}
				} else if( localDir.y <= -_LocalDirMaxTheta + IKEpsilon ) {
					localDirXZ = new Vector3( -1.0f, 0.0f, 0.0f );
				} else if( localDir.y < -(_LocalDirMaxTheta - _LocalDirLerpTheta - IKEpsilon) ) {
					float r = (-(_LocalDirMaxTheta - _LocalDirLerpTheta) - localDir.y) * (1.0f / _LocalDirLerpTheta);
					localDirXZ = new Vector3( localDir.x + (-1.0f - localDir.x) * r, 0.0f, localDir.z - localDir.z * r );
					if( !SAFBIKVecNormalizeXZ( ref localDirXZ ) ) {
						localDirXZ = new Vector3( -1.0f, 0.0f, 0.0f );
					}
				} else {
					localDirXZ = new Vector3( localDir.x, 0.0f, localDir.z );
					if( !SAFBIKVecNormalizeXZ( ref localDirXZ ) ) {
						localDirXZ = new Vector3( 1.0f, 0.0f, 0.0f );
					}
				}
			}

			// Lefthand based.
			static void _ComputeLocalDirYZ( ref Vector3 localDir, out Vector3 localDirYZ )
			{
				if( localDir.x >= _LocalDirMaxTheta - IKEpsilon ) {
					localDirYZ = new Vector3( 0.0f, 0.0f, -1.0f );
				} else if( localDir.x > _LocalDirMaxTheta - _LocalDirLerpTheta - IKEpsilon ) {
					float r = (localDir.x - (_LocalDirMaxTheta - _LocalDirLerpTheta)) * (1.0f / _LocalDirLerpTheta);
					localDirYZ = new Vector3( 0.0f, localDir.y - localDir.y * r, localDir.z + (-1.0f - localDir.z) * r );
					if( !SAFBIKVecNormalizeYZ( ref localDirYZ ) ) {
						localDirYZ = new Vector3( 0.0f, 0.0f, -1.0f );
					}
				} else if( localDir.x <= -_LocalDirMaxTheta + IKEpsilon ) {
					localDirYZ = new Vector3( 0.0f, 0.0f, 1.0f );
				} else if( localDir.x < -(_LocalDirMaxTheta - _LocalDirLerpTheta - IKEpsilon) ) {
					float r = (-(_LocalDirMaxTheta - _LocalDirLerpTheta) - localDir.x) * (1.0f / _LocalDirLerpTheta);
					localDirYZ = new Vector3( 0.0f, localDir.y - localDir.y * r, localDir.z + (1.0f - localDir.z) * r );
					if( !SAFBIKVecNormalizeYZ( ref localDirYZ ) ) {
						localDirYZ = new Vector3( 0.0f, 0.0f, 1.0f );
					}
				} else {
					localDirYZ = new Vector3( 0.0f, localDir.y, localDir.z );
					if( !SAFBIKVecNormalizeYZ( ref localDirYZ ) ) {
						localDirYZ = new Vector3( 0.0f, 0.0f, (localDir.x >= 0.0f) ? -1.0f : 1.0f );
					}
				}
			}

			//------------------------------------------------------------------------------------------------------------

			CachedDegreesToCos _presolvedLerpTheta = CachedDegreesToCos.zero;
			CachedDegreesToCos _automaticKneeBaseTheta = CachedDegreesToCos.zero;
			CachedDegreesToCosSin _automaticArmElbowTheta = CachedDegreesToCosSin.zero;

			//------------------------------------------------------------------------------------------------------------

			public bool IsSolverEnabled()
			{
				if( !_endEffector.positionEnabled && !(_bendingEffector.positionEnabled && _bendingEffector.pull > IKEpsilon) ) {
					if( _limbIKType == LimbIKType.Arm ) {
						if( !_settings.limbIK.armAlwaysSolveEnabled ) {
							return false;
						}
					} else if( _limbIKType == LimbIKType.Leg ) {
						if( !_settings.limbIK.legAlwaysSolveEnabled ) {
							return false;
						}
					}
				}

				return true;
			}

			public bool Presolve(
				ref Matrix3x3 parentBaseBasis,
				ref Vector3 beginPos,
				out Vector3 solvedBeginToBendingDir,
				out Vector3 solvedBendingToEndDir )
			{
				float effectorLen;
				Matrix3x3 baseBasis;
				return PresolveInternal( ref parentBaseBasis, ref beginPos, out effectorLen, out baseBasis, out solvedBeginToBendingDir, out solvedBendingToEndDir );
            }

			public bool PresolveInternal(
				ref Matrix3x3 parentBaseBasis,
				ref Vector3 beginPos,
				out float effectorLen,
				out Matrix3x3 baseBasis,
				out Vector3 solvedBeginToBendingDir,
				out Vector3 solvedBendingToEndDir )
			{
				solvedBeginToBendingDir = Vector3.zero;
				solvedBendingToEndDir = Vector3.zero;

				Vector3 bendingPos = _bendingEffector._hidden_worldPosition;
				Vector3 effectorPos = _endEffector._hidden_worldPosition;

				if( _bendingEffector.positionEnabled && _bendingEffector.pull > IKEpsilon ) {
					Vector3 beginToBending = bendingPos - beginPos;
					float beginToBendingLenSq = beginToBending.sqrMagnitude;
					if( beginToBendingLenSq > _bendingBone._defaultLocalLength.length ) {
						float beginToBendingLen = SAFBIKSqrt( beginToBendingLenSq );
						float tempLen = beginToBendingLen - _bendingBone._defaultLocalLength.length;
						if( tempLen < -IKEpsilon && beginToBendingLen > IKEpsilon ) {
							bendingPos += beginToBending * (tempLen / beginToBendingLen);
                        }
                    }
                }

				if( _bendingEffector.positionEnabled && _bendingEffector.pull > IKEpsilon ) {
					Vector3 bendingToEffector = effectorPos - bendingPos;
					float bendingToEffectorLen = bendingToEffector.magnitude;
					if( bendingToEffectorLen > IKEpsilon ) {
						float tempLen = _endBone._defaultLocalLength.length - bendingToEffectorLen;
						if( tempLen > IKEpsilon || tempLen < -IKEpsilon ) {
							float pull;
							if( _endEffector.positionEnabled && _endEffector.pull > IKEpsilon ) {
								pull = _bendingEffector.pull / (_bendingEffector.pull + _endEffector.pull);
							} else {
								pull = _bendingEffector.pull;
                            }
							effectorPos += bendingToEffector * ((tempLen * pull) / bendingToEffectorLen);
						}
					}
                }

				Matrix3x3 parentBaseBasisInv = parentBaseBasis.transpose;

				Vector3 effectorTrans = effectorPos - beginPos;

				effectorLen = effectorTrans.magnitude;
				if( effectorLen <= IKEpsilon ) {
					baseBasis = Matrix3x3.identity;
					return false;
				}
				if( _effectorMaxLength.value <= IKEpsilon ) {
					baseBasis = Matrix3x3.identity;
					return false;
				}

				Vector3 effectorDir = effectorTrans * (1.0f / effectorLen);

				if( effectorLen > _effectorMaxLength.value ) {
					effectorTrans = effectorDir * _effectorMaxLength.value;
					effectorPos = beginPos + effectorTrans;
					effectorLen = _effectorMaxLength.value;
				}

				Vector3 localEffectorDir = new Vector3( 0.0f, 0.0f, 1.0f );
				if( _limbIKType == LimbIKType.Arm ) {
					SAFBIKMatMultVec( out localEffectorDir, ref parentBaseBasisInv, ref effectorDir );
				}

				// pending: Detail processing for Arm too.
				if( _limbIKType == LimbIKType.Leg && _settings.limbIK.prefixLegEffectorEnabled ) { // Override Effector Pos.
					Vector3 localEffectorTrans;
					SAFBIKMatMultVec( out localEffectorTrans, ref parentBaseBasisInv, ref effectorTrans );

					bool isProcessed = false;
					bool isLimited = false;
					if( localEffectorTrans.z >= 0.0f ) { // Front
						if( localEffectorTrans.z >= _beginToBendingLength + _bendingToEndLength ) { // So far.
							isProcessed = true;
							localEffectorTrans.z = _beginToBendingLength + _bendingToEndLength;
							localEffectorTrans.y = 0.0f;
						}

						if( !isProcessed &&
							localEffectorTrans.y >= -_effectorMinLength.value &&
							localEffectorTrans.z <= _leg_upperLimitNearCircleZ ) { // Upper(Near)
							isProcessed = true;
							isLimited = _PrefixLegEffectorPos_UpperNear( ref localEffectorTrans );
						}

						if( !isProcessed &&
							localEffectorTrans.y >= 0.0f &&
							localEffectorTrans.z > _leg_upperLimitNearCircleZ ) { // Upper(Far)
							isProcessed = true;
							_PrefixLegEffectorPos_Upper_Circular_Far( ref localEffectorTrans,
								_leg_upperLimitNearCircleZ,
								_beginToBendingLength + _bendingToEndLength - _leg_upperLimitNearCircleZ,
								_leg_upperLimitNearCircleY );
						}

						if( !isProcessed ) { // Lower
							isProcessed = true;
							isLimited = _PrefixLegEffectorPos_Circular_Far( ref localEffectorTrans, _beginToBendingLength + _bendingToEndLength );
						}

					} else { // Back
							 // Pending: Detail Processing.
						if( localEffectorTrans.y >= -_effectorMinLength.value ) {
							isLimited = true;
							localEffectorTrans.y = -_effectorMinLength.value;
						} else {
							isLimited = _PrefixLegEffectorPos_Circular_Far( ref localEffectorTrans, _beginToBendingLength + _bendingToEndLength );
						}
					}

					if( isLimited ) {
						_internalValues.AddDebugPoint( effectorPos, Color.black, 0.05f );

						SAFBIKMatMultVec( out effectorTrans, ref parentBaseBasis, ref localEffectorTrans );
						effectorLen = effectorTrans.magnitude;
						effectorPos = beginPos + effectorTrans;
						if( effectorLen > IKEpsilon ) {
							effectorDir = effectorTrans * (1.0f / effectorLen);
						}

						_internalValues.AddDebugPoint( effectorPos, Color.white, 0.05f );
					}
				}

				//Matrix3x3 baseBasis;
				_SolveBaseBasis( out baseBasis, ref parentBaseBasis, ref effectorDir );

				// Automatical bendingPos
				if( !_bendingEffector.positionEnabled ) {
					bool presolvedEnabled = (_limbIKType == LimbIKType.Leg) ? _settings.limbIK.presolveKneeEnabled : _settings.limbIK.presolveElbowEnabled;
					float presolvedBendingRate = (_limbIKType == LimbIKType.Leg) ? _settings.limbIK.presolveKneeRate : _settings.limbIK.presolveElbowRate;
					float presolvedLerpAngle = (_limbIKType == LimbIKType.Leg) ? _settings.limbIK.presolveKneeLerpAngle : _settings.limbIK.presolveElbowLerpAngle;
					float presolvedLerpLengthRate = (_limbIKType == LimbIKType.Leg) ? _settings.limbIK.presolveKneeLerpLengthRate : _settings.limbIK.presolveElbowLerpLengthRate;

					Vector3 presolvedBendingPos = Vector3.zero;

					if( presolvedEnabled && _isPresolvedBending ) {
						if( _presolvedEffectorLength > IKEpsilon ) {
							float lerpLength = _presolvedEffectorLength * presolvedLerpLengthRate;
							if( lerpLength > IKEpsilon ) {
								float tempLength = Mathf.Abs( _presolvedEffectorLength - effectorLen );
								if( tempLength < lerpLength ) {
									presolvedBendingRate *= 1.0f - (tempLength / lerpLength);
								} else {
									presolvedBendingRate = 0.0f;
								}
							} else { // Failsafe.
								presolvedBendingRate = 0.0f;
							}
						} else { // Failsafe.
							presolvedBendingRate = 0.0f;
						}

						if( presolvedBendingRate > IKEpsilon ) {
							if( _presolvedLerpTheta._degrees != presolvedLerpAngle ) {
								_presolvedLerpTheta._Reset( presolvedLerpAngle );
							}
							if( _presolvedLerpTheta.cos < 1.0f - IKEpsilon ) { // Lerp
								float presolvedFeedbackTheta = Vector3.Dot( effectorDir, _presolvedEffectorDir );
								if( presolvedFeedbackTheta > _presolvedLerpTheta.cos + IKEpsilon ) {
									float presolvedFeedbackRate = (presolvedFeedbackTheta - _presolvedLerpTheta.cos) / (1.0f - _presolvedLerpTheta.cos);
									presolvedBendingRate *= presolvedFeedbackRate;
								} else {
									presolvedBendingRate = 0.0f;
								}
							} else {
								presolvedBendingRate = 0.0f;
							}
						}

						if( presolvedBendingRate > IKEpsilon ) {
							Vector3 bendingDir;
							Matrix3x3 presolvedBendingBasis;
							SAFBIKMatMult( out presolvedBendingBasis, ref baseBasis, ref _presolvedBendingBasis );

							if( _limbIKType == LimbIKType.Arm ) {
								bendingDir = (_limbIKSide == Side.Left) ? -presolvedBendingBasis.column0 : presolvedBendingBasis.column0;
							} else {
								bendingDir = -presolvedBendingBasis.column1;
							}

							presolvedBendingPos = beginPos + bendingDir * _beginToBendingLength;
							bendingPos = presolvedBendingPos; // Failsafe.
						}
					} else {
						presolvedBendingRate = 0.0f;
					}

					if( presolvedBendingRate < 1.0f - IKEpsilon ) {
						float cosTheta = ComputeCosTheta(
							_bendingToEndLengthSq,          // lenASq
							effectorLen * effectorLen,      // lenBSq
							_beginToBendingLengthSq,        // lenCSq
							effectorLen,                    // lenB
							_beginToBendingLength );        // lenC

						float sinTheta = SAFBIKSqrtClamp01( 1.0f - cosTheta * cosTheta );

						float moveC = _beginToBendingLength * (1.0f - Mathf.Max( _defaultCosTheta - cosTheta, 0.0f ));
						float moveS = _beginToBendingLength * Mathf.Max( sinTheta - _defaultSinTheta, 0.0f );

						if( _limbIKType == LimbIKType.Arm ) {
							Vector3 dirX = (_limbIKSide == Side.Left) ? -baseBasis.column0 : baseBasis.column0;
							{
								float elbowBaseAngle = _settings.limbIK.automaticElbowBaseAngle;
								float elbowLowerAngle = _settings.limbIK.automaticElbowLowerAngle;
								float elbowUpperAngle = _settings.limbIK.automaticElbowUpperAngle;

								float elbowAngle = elbowBaseAngle;

								Vector3 localDir = (_limbIKSide == Side.Left) ? localEffectorDir : new Vector3( -localEffectorDir.x, localEffectorDir.y, localEffectorDir.z );

								if( localDir.y < 0.0f ) {
									elbowAngle = Mathf.Lerp( elbowAngle, elbowLowerAngle, -localDir.y );
								} else {
									elbowAngle = Mathf.Lerp( elbowAngle, elbowUpperAngle, localDir.y );
								}

								if( _settings.limbIK.armEffectorBackfixEnabled ) {
									float elbowBackUpperAngle = _settings.limbIK.automaticElbowBackUpperAngle;
									float elbowBackLowerAngle = _settings.limbIK.automaticElbowBackLowerAngle;

									// Based on localXZ
									float armEffectorBackBeginSinTheta = _internalValues.limbIK.armEffectorBackBeginTheta.sin;
									float armEffectorBackCoreBeginSinTheta = _internalValues.limbIK.armEffectorBackCoreBeginTheta.sin;
									float armEffectorBackCoreEndCosTheta = _internalValues.limbIK.armEffectorBackCoreEndTheta.cos;
									float armEffectorBackEndCosTheta = _internalValues.limbIK.armEffectorBackEndTheta.cos;

									// Based on localYZ
									float armEffectorBackCoreUpperSinTheta = _internalValues.limbIK.armEffectorBackCoreUpperTheta.sin;
									float armEffectorBackCoreLowerSinTheta = _internalValues.limbIK.armEffectorBackCoreLowerTheta.sin;

									Vector3 localXZ; // X is reversed in RightSide.
									Vector3 localYZ;
									_ComputeLocalDirXZ( ref localDir, out localXZ ); // Lefthand Based.
									_ComputeLocalDirYZ( ref localDir, out localYZ ); // Lefthand Based.

									if( localXZ.z < armEffectorBackBeginSinTheta &&
										localXZ.x > armEffectorBackEndCosTheta ) {

										float targetAngle;
										if( localYZ.y >= armEffectorBackCoreUpperSinTheta ) {
											targetAngle = elbowBackUpperAngle;
										} else if( localYZ.y <= armEffectorBackCoreLowerSinTheta ) {
											targetAngle = elbowBackLowerAngle;
										} else {
											float t = armEffectorBackCoreUpperSinTheta - armEffectorBackCoreLowerSinTheta;
											if( t > IKEpsilon ) {
												float r = (localYZ.y - armEffectorBackCoreLowerSinTheta) / t;
												targetAngle = Mathf.Lerp( elbowBackLowerAngle, elbowBackUpperAngle, r );
											} else {
												targetAngle = elbowBackLowerAngle;
											}
										}

										if( localXZ.x < armEffectorBackCoreEndCosTheta ) {
											float t = armEffectorBackCoreEndCosTheta - armEffectorBackEndCosTheta;
											if( t > IKEpsilon ) {
												float r = (localXZ.x - armEffectorBackEndCosTheta) / t;

												if( localYZ.y <= armEffectorBackCoreLowerSinTheta ) {
													elbowAngle = Mathf.Lerp( elbowAngle, targetAngle, r );
												} else if( localYZ.y >= armEffectorBackCoreUpperSinTheta ) {
													elbowAngle = Mathf.Lerp( elbowAngle, targetAngle - 360.0f, r );
												} else {
													float angle0 = Mathf.Lerp( elbowAngle, targetAngle, r ); // Lower
													float angle1 = Mathf.Lerp( elbowAngle, targetAngle - 360.0f, r ); // Upper
													float t2 = armEffectorBackCoreUpperSinTheta - armEffectorBackCoreLowerSinTheta;
													if( t2 > IKEpsilon ) {
														float r2 = (localYZ.y - armEffectorBackCoreLowerSinTheta) / t2;
														if( angle0 - angle1 > 180.0f ) {
															angle1 += 360.0f;
														}

														elbowAngle = Mathf.Lerp( angle0, angle1, r2 );
													} else { // Failsafe.
														elbowAngle = angle0;
													}
												}
											}
										} else if( localXZ.z > armEffectorBackCoreBeginSinTheta ) {
											float t = (armEffectorBackBeginSinTheta - armEffectorBackCoreBeginSinTheta);
											if( t > IKEpsilon ) {
												float r = (armEffectorBackBeginSinTheta - localXZ.z) / t;
												if( localDir.y >= 0.0f ) {
													elbowAngle = Mathf.Lerp( elbowAngle, targetAngle, r );
												} else {
													elbowAngle = Mathf.Lerp( elbowAngle, targetAngle - 360.0f, r );
												}
											} else { // Failsafe.
												elbowAngle = targetAngle;
											}
										} else {
											elbowAngle = targetAngle;
										}
									}
								}

								Vector3 dirY = parentBaseBasis.column1;
								Vector3 dirZ = Vector3.Cross( baseBasis.column0, dirY );
								dirY = Vector3.Cross( dirZ, baseBasis.column0 );
								if( !SAFBIKVecNormalize2( ref dirY, ref dirZ ) ) { // Failsafe.
									dirY = parentBaseBasis.column1;
									dirZ = parentBaseBasis.column2;
								}

								if( _automaticArmElbowTheta._degrees != elbowAngle ) {
									_automaticArmElbowTheta._Reset( elbowAngle );
								}

								bendingPos = beginPos + dirX * moveC
									+ -dirY * moveS * _automaticArmElbowTheta.cos
									+ -dirZ * moveS * _automaticArmElbowTheta.sin;
							}
						} else { // Leg
							float automaticKneeBaseAngle = _settings.limbIK.automaticKneeBaseAngle;
							if( automaticKneeBaseAngle >= -IKEpsilon && automaticKneeBaseAngle <= IKEpsilon ) { // Fuzzy 0
								bendingPos = beginPos + -baseBasis.column1 * moveC + baseBasis.column2 * moveS;
							} else {
								if( _automaticKneeBaseTheta._degrees != automaticKneeBaseAngle ) {
									_automaticKneeBaseTheta._Reset( automaticKneeBaseAngle );
								}

								float kneeSin = _automaticKneeBaseTheta.cos;
								float kneeCos = SAFBIKSqrt( 1.0f - kneeSin * kneeSin );
								if( _limbIKSide == Side.Right ) {
									if( automaticKneeBaseAngle >= 0.0f ) {
										kneeCos = -kneeCos;
									}
								} else {
									if( automaticKneeBaseAngle < 0.0f ) {
										kneeCos = -kneeCos;
									}
								}

								bendingPos = beginPos + -baseBasis.column1 * moveC
									+ baseBasis.column0 * moveS * kneeCos
									+ baseBasis.column2 * moveS * kneeSin;
							}
						}
					}

					if( presolvedBendingRate > IKEpsilon ) {
						bendingPos = Vector3.Lerp( bendingPos, presolvedBendingPos, presolvedBendingRate );
					}
				}

				bool isSolved = false;

				{
					Vector3 beginToBendingTrans = bendingPos - beginPos;
					Vector3 intersectBendingTrans = beginToBendingTrans - effectorDir * Vector3.Dot( effectorDir, beginToBendingTrans );
					float intersectBendingLen = intersectBendingTrans.magnitude;

					if( intersectBendingLen > IKEpsilon ) {
						Vector3 intersectBendingDir = intersectBendingTrans * (1.0f / intersectBendingLen);

						float bc2 = 2.0f * _beginToBendingLength * effectorLen;
						if( bc2 > IKEpsilon ) {
							float effectorCosTheta = (_beginToBendingLengthSq + effectorLen * effectorLen - _bendingToEndLengthSq) / bc2;
							float effectorSinTheta = SAFBIKSqrtClamp01( 1.0f - effectorCosTheta * effectorCosTheta );

							Vector3 beginToInterTranslate = effectorDir * effectorCosTheta * _beginToBendingLength
															+ intersectBendingDir * effectorSinTheta * _beginToBendingLength;
							Vector3 interToEndTranslate = effectorPos - (beginPos + beginToInterTranslate);

							if( SAFBIKVecNormalize2( ref beginToInterTranslate, ref interToEndTranslate ) ) {
								isSolved = true;
								solvedBeginToBendingDir = beginToInterTranslate;
								solvedBendingToEndDir = interToEndTranslate;
							}
						}
					}
				}

				if( isSolved && _limbIKType == LimbIKType.Arm && _settings.limbIK.armEffectorInnerfixEnabled ) {
					float elbowFrontInnerLimitSinTheta = _internalValues.limbIK.elbowFrontInnerLimitTheta.sin;
					float elbowBackInnerLimitSinTheta = _internalValues.limbIK.elbowBackInnerLimitTheta.sin;

					Vector3 localBendingDir;
					SAFBIKMatMultVec( out localBendingDir, ref parentBaseBasisInv, ref solvedBeginToBendingDir );

					bool isBack = localBendingDir.z < 0.0f;
					float limitTheta = isBack ? elbowBackInnerLimitSinTheta : elbowFrontInnerLimitSinTheta;

					float localX = (_limbIKSide == Side.Left) ? localBendingDir.x : (-localBendingDir.x);
					if( localX > limitTheta ) {
						localBendingDir.x = (_limbIKSide == Side.Left) ? limitTheta : -limitTheta;
						localBendingDir.z = SAFBIKSqrt( 1.0f - (localBendingDir.x * localBendingDir.x + localBendingDir.y * localBendingDir.y) );
						if( isBack ) {
							localBendingDir.z = -localBendingDir.z;
						}
						Vector3 bendingDir;
						SAFBIKMatMultVec( out bendingDir, ref parentBaseBasis, ref localBendingDir );
						Vector3 interPos = beginPos + bendingDir * _beginToBendingLength;
						Vector3 endDir = effectorPos - interPos;
						if( SAFBIKVecNormalize( ref endDir ) ) {
							solvedBeginToBendingDir = bendingDir;
							solvedBendingToEndDir = endDir;

							if( _settings.limbIK.armBasisForcefixEnabled ) { // Invalidate effectorLen.
								effectorLen = (effectorPos - beginPos).magnitude;
							}
						}
					}
				}

				if( !isSolved ) { // Failsafe.
					Vector3 bendingDir = bendingPos - beginPos;
					if( SAFBIKVecNormalize( ref bendingDir ) ) {
						Vector3 interPos = beginPos + bendingDir * _beginToBendingLength;
						Vector3 endDir = effectorPos - interPos;
						if( SAFBIKVecNormalize( ref endDir ) ) {
							isSolved = true;
							solvedBeginToBendingDir = bendingDir;
							solvedBendingToEndDir = endDir;
						}
					}
				}

				if( !isSolved ) {
					return false;
				}

				return true;
			}

			//------------------------------------------------------------------------------------------------------------

			public bool Solve()
			{
				_UpdateArgs();

				_arm_isSolvedLimbIK = false;

				Quaternion bendingBonePrevRotation = Quaternion.identity;
				Quaternion endBonePrevRotation = Quaternion.identity;
				if( !_internalValues.resetTransforms ) {
					float endRotationWeight = _endEffector.rotationEnabled ? _endEffector.rotationWeight : 0.0f;
					if( endRotationWeight > IKEpsilon ) {
						if( endRotationWeight < 1.0f - IKEpsilon ) {
							bendingBonePrevRotation = _bendingBone.worldRotation;
							endBonePrevRotation = _endBone.worldRotation;
						}
					}
				}

				bool r = _SolveInternal();
				r |= _SolveEndRotation( r, ref bendingBonePrevRotation, ref endBonePrevRotation );
				r |= _RollInternal();

				return r;
			}

			public bool _SolveInternal()
			{
				if( !IsSolverEnabled() ) {
					return false;
				}

				if( _beginBone.parentBone == null || !_beginBone.parentBone.transformIsAlive ) {
					return false; // Failsafe.
				}

				Quaternion parentBoneWorldRotation = _beginBone.parentBone.worldRotation;
				Matrix3x3 parentBaseBasis;
				SAFBIKMatSetRotMult( out parentBaseBasis, ref parentBoneWorldRotation, ref _beginBone.parentBone._worldToBaseRotation );

				Vector3 beginPos = _beginBone.worldPosition;

				float effectorLen;
				Matrix3x3 baseBasis;
				Vector3 solvedBeginToBendingDir;
				Vector3 solvedBendingToEndDir;

				if( !PresolveInternal( ref parentBaseBasis, ref beginPos, out effectorLen, out baseBasis, out solvedBeginToBendingDir, out solvedBendingToEndDir ) ) {
					return false;
				}

				Matrix3x3 beginBasis = Matrix3x3.identity;
				Matrix3x3 bendingBasis = Matrix3x3.identity;

				if( _limbIKType == LimbIKType.Arm ) {
					// Memo: Arm Bone Based Y Axis.
					if( _limbIKSide == Side.Left ) {
						solvedBeginToBendingDir = -solvedBeginToBendingDir;
						solvedBendingToEndDir = -solvedBendingToEndDir;
					}

					Vector3 basisY = parentBaseBasis.column1;
					Vector3 basisZ = parentBaseBasis.column2;
					if( !SAFBIKComputeBasisLockX( out beginBasis, ref solvedBeginToBendingDir, ref basisY, ref basisZ ) ) {
						return false;
					}

					{
						bool forcefixEnabled = _settings.limbIK.armBasisForcefixEnabled;

						if( forcefixEnabled && effectorLen > _arm_elbowBasisForcefixEffectorLengthEnd.value ) {
							SAFBIKMatMultCol1( out basisY, ref beginBasis, ref _beginToBendingBoneBasis );
						} else {
							basisY = Vector3.Cross( -solvedBeginToBendingDir, solvedBendingToEndDir ); // Memo: Require to MaxEffectorLengthRate is less than 1.0
							if( _limbIKSide == Side.Left ) {
								basisY = -basisY;
							}

							if( forcefixEnabled && effectorLen > _arm_elbowBasisForcefixEffectorLengthBegin.value ) {
								float t = _arm_elbowBasisForcefixEffectorLengthEnd.value - _arm_elbowBasisForcefixEffectorLengthBegin.value;
								if( t > IKEpsilon ) {
									float r = (effectorLen - _arm_elbowBasisForcefixEffectorLengthBegin.value) / t;
									Vector3 tempY;
									SAFBIKMatMultCol1( out tempY, ref beginBasis, ref _beginToBendingBoneBasis );
									basisY = Vector3.Lerp( basisY, tempY, r );
                                }
                            }
						}

						if( !SAFBIKComputeBasisFromXYLockX( out bendingBasis, ref solvedBendingToEndDir, ref basisY ) ) {
							return false;
						}
					}
				} else {
					// Memo: Leg Bone Based X Axis.
					solvedBeginToBendingDir = -solvedBeginToBendingDir;
					solvedBendingToEndDir = -solvedBendingToEndDir;

					Vector3 basisX = baseBasis.column0;
					Vector3 basisZ = baseBasis.column2;
					if( !SAFBIKComputeBasisLockY( out beginBasis, ref basisX, ref solvedBeginToBendingDir, ref basisZ ) ) {
						return false;
					}

					SAFBIKMatMultCol0( out basisX, ref beginBasis, ref _beginToBendingBoneBasis );

					if( !SAFBIKComputeBasisFromXYLockY( out bendingBasis, ref basisX, ref solvedBendingToEndDir ) ) {
						return false;
					}
				}

				if( _limbIKType == LimbIKType.Arm ) {
					_arm_isSolvedLimbIK = true;
					_arm_solvedBeginBoneBasis = beginBasis;
					_arm_solvedBendingBoneBasis = bendingBasis;
				}

				Quaternion worldRotation;
				SAFBIKMatMultGetRot( out worldRotation, ref beginBasis, ref _beginBone._boneToWorldBasis );
				_beginBone.worldRotation = worldRotation;
				SAFBIKMatMultGetRot( out worldRotation, ref bendingBasis, ref _bendingBone._boneToWorldBasis );
                _bendingBone.worldRotation = worldRotation;
				return true;
			}

			bool _SolveEndRotation( bool isSolved, ref Quaternion bendingBonePrevRotation, ref Quaternion endBonePrevRotation )
			{
				float endRotationWeight = _endEffector.rotationEnabled ? _endEffector.rotationWeight : 0.0f;
				if( endRotationWeight > IKEpsilon ) {
					Quaternion endEffectorWorldRotation = _endEffector.worldRotation;
					Quaternion toRotation;
					SAFBIKQuatMult( out toRotation, ref endEffectorWorldRotation, ref _endEffectorToWorldRotation );

					if( endRotationWeight < 1.0f - IKEpsilon ) {
						Quaternion fromRotation;
						if( _internalValues.resetTransforms ) {
							Quaternion bendingBoneWorldRotation = _bendingBone.worldRotation;
							SAFBIKQuatMult3( out fromRotation, ref bendingBoneWorldRotation, ref _bendingBone._worldToBaseRotation, ref _endBone._baseToWorldRotation );
						} else {
							if( isSolved ) {
								Quaternion bendingBoneWorldRotation = _bendingBone.worldRotation;
								SAFBIKQuatMultNorm3Inv1( out fromRotation, ref bendingBoneWorldRotation, ref bendingBonePrevRotation, ref endBonePrevRotation );
							} else {
								fromRotation = endBonePrevRotation; // This is able to use endBonePrevRotation directly.
							}
						}
						_endBone.worldRotation = Quaternion.Lerp( fromRotation, toRotation, endRotationWeight );
					} else {
						_endBone.worldRotation = toRotation;
					}

					_EndRotationLimit();
                    return true;
				} else {
					if( _internalValues.resetTransforms ) {
						Quaternion fromRotation, bendingBoneWorldRotation = _bendingBone.worldRotation;
						SAFBIKQuatMult3( out fromRotation, ref bendingBoneWorldRotation, ref _bendingBone._worldToBaseRotation, ref _endBone._baseToWorldRotation );
						_endBone.worldRotation = fromRotation;
						return true;
					}
				}

				return false;
			}

			void _EndRotationLimit()
			{
				if( _limbIKType == LimbIKType.Arm ) {
					if( !_settings.limbIK.wristLimitEnabled ) {
						return;
					}
				} else if( _limbIKType == LimbIKType.Leg ) {
					if( !_settings.limbIK.footLimitEnabled ) {
						return;
					}
				}

				// Rotation Limit.
				Quaternion tempRotation, endRotation, bendingRotation, localRotation;
				tempRotation = _endBone.worldRotation;
				SAFBIKQuatMult( out endRotation, ref tempRotation, ref _endBone._worldToBaseRotation );
				tempRotation = _bendingBone.worldRotation;
				SAFBIKQuatMult( out bendingRotation, ref tempRotation, ref _bendingBone._worldToBaseRotation );
				SAFBIKQuatMultInv0( out localRotation, ref bendingRotation, ref endRotation );

				if( _limbIKType == LimbIKType.Arm ) {
					bool isLimited = false;
					float limitAngle = _settings.limbIK.wristLimitAngle;

					float angle;
					Vector3 axis;
					localRotation.ToAngleAxis( out angle, out axis );
					if( angle < -limitAngle ) {
						angle = -limitAngle;
						isLimited = true;
					} else if( angle > limitAngle ) {
						angle = limitAngle;
						isLimited = true;
					}

					if( isLimited ) {
						localRotation = Quaternion.AngleAxis( angle, axis );
						SAFBIKQuatMultNorm3( out endRotation, ref bendingRotation, ref localRotation, ref _endBone._baseToWorldRotation );
						_endBone.worldRotation = endRotation;
					}
				} else if( _limbIKType == LimbIKType.Leg ) {
					Matrix3x3 localBasis;
					SAFBIKMatSetRot( out localBasis, ref localRotation );

					Vector3 localDirY = localBasis.column1;
					Vector3 localDirZ = localBasis.column2;

					bool isLimited = false;
					isLimited |= _LimitXZ_Square( ref localDirY,
						_internalValues.limbIK.footLimitRollTheta.sin,
						_internalValues.limbIK.footLimitRollTheta.sin,
						_internalValues.limbIK.footLimitPitchUpTheta.sin,
						_internalValues.limbIK.footLimitPitchDownTheta.sin );
					isLimited |= _LimitXY_Square( ref localDirZ,
						_internalValues.limbIK.footLimitYawTheta.sin,
						_internalValues.limbIK.footLimitYawTheta.sin,
						_internalValues.limbIK.footLimitPitchDownTheta.sin,
						_internalValues.limbIK.footLimitPitchUpTheta.sin );

					if( isLimited ) {
						if( SAFBIKComputeBasisFromYZLockZ( out localBasis, ref localDirY, ref localDirZ ) ) {
							SAFBIKMatGetRot( out localRotation, ref localBasis );
							SAFBIKQuatMultNorm3( out endRotation, ref bendingRotation, ref localRotation, ref _endBone._baseToWorldRotation );
							_endBone.worldRotation = endRotation;
						}
					}
				}
			}

			bool _RollInternal()
			{
				if( _limbIKType != LimbIKType.Arm || !_settings.rollBonesEnabled ) {
					return false;
				}

				bool isSolved = false;

				if( _armRollBones != null && _armRollBones.Length > 0 ) {
					int boneLength = _armRollBones.Length;

					Matrix3x3 beginBoneBasis;
					Matrix3x3 bendingBoneBasis; // Attension: bendingBoneBasis is based on beginBoneBasis
					if( _arm_isSolvedLimbIK ) {
						beginBoneBasis = _arm_solvedBeginBoneBasis;
						SAFBIKMatMult( out bendingBoneBasis, ref _arm_solvedBendingBoneBasis, ref _arm_bendingToBeginBoneBasis );
					} else {
						beginBoneBasis = new Matrix3x3( _beginBone.worldRotation * _beginBone._worldToBoneRotation );
						bendingBoneBasis = new Matrix3x3( _bendingBone.worldRotation * _arm_bendingWorldToBeginBoneRotation );
					}

					Vector3 dirX = beginBoneBasis.column0;
					Vector3 dirY = bendingBoneBasis.column1;
					Vector3 dirZ = bendingBoneBasis.column2;

					Matrix3x3 bendingBasisTo;

					if( SAFBIKComputeBasisLockX( out bendingBasisTo, ref dirX, ref dirY, ref dirZ ) ) {
						Matrix3x3 baseBasis, baseBasisTo;
						SAFBIKMatMult( out baseBasis, ref beginBoneBasis, ref _beginBone._boneToBaseBasis );
						SAFBIKMatMult( out baseBasisTo, ref bendingBasisTo, ref _beginBone._boneToBaseBasis );

						for( int i = 0; i < boneLength; ++i ) {
							if( _armRollBones[i].bone != null && _armRollBones[i].bone.transformIsAlive ) {
								Matrix3x3 tempBasis;
								float rate = _armRollBones[i].rate;
								SAFBIKMatFastLerp( out tempBasis, ref baseBasis, ref baseBasisTo, rate );
								Quaternion worldRotation;
								SAFBIKMatMultGetRot( out worldRotation, ref tempBasis, ref _elbowRollBones[i].bone._baseToWorldBasis );
								_armRollBones[i].bone.worldRotation = worldRotation;
                                isSolved = true;
							}
						}
					}
				}

				if( _elbowRollBones != null && _elbowRollBones.Length > 0 ) {
					int boneLength = _elbowRollBones.Length;

					Matrix3x3 bendingBoneBasis;
					if( _arm_isSolvedLimbIK ) {
						bendingBoneBasis = _arm_solvedBendingBoneBasis;
                    } else {
						bendingBoneBasis = new Matrix3x3( _bendingBone.worldRotation * _bendingBone._worldToBoneRotation );
					}

					// Attension: endBoneBasis is based on bendingBoneBasis
					Matrix3x3 endBoneBasis = new Matrix3x3( _endBone.worldRotation * _arm_endWorldToBendingBoneRotation );
					
					Vector3 dirZ = endBoneBasis.column2;
					Vector3 dirX = bendingBoneBasis.column0;
					Vector3 dirY = Vector3.Cross( dirZ, dirX );
					dirZ = Vector3.Cross( dirX, dirY );
					if( SAFBIKVecNormalize2( ref dirY, ref dirZ ) ) { // Lock dirX(bendingBoneBasis.column0)
						Matrix3x3 baseBasis;
						Matrix3x3 baseBasisTo = Matrix3x3.FromColumn( ref dirX, ref dirY, ref dirZ );
						SAFBIKMatMult( out baseBasis, ref bendingBoneBasis, ref _bendingBone._boneToBaseBasis );
						SAFBIKMatMultRet0( ref baseBasisTo, ref _bendingBone._boneToBaseBasis );

						for( int i = 0; i < boneLength; ++i ) {
							if( _elbowRollBones[i].bone != null && _elbowRollBones[i].bone.transformIsAlive ) {
								Matrix3x3 tempBasis;
								float rate = _elbowRollBones[i].rate;
								SAFBIKMatFastLerp( out tempBasis, ref baseBasis, ref baseBasisTo, rate );
								Quaternion worldRotation;
								SAFBIKMatMultGetRot( out worldRotation, ref tempBasis, ref _elbowRollBones[i].bone._baseToWorldBasis );
								_elbowRollBones[i].bone.worldRotation = worldRotation;
								isSolved = true;
                            }
						}
					}
				}

				return isSolved;
			}
		}
	}
}