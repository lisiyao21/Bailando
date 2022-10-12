// Copyright (c) 2016 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

#if SAFULLBODYIK_DEBUG
//#define SAFULLBODYIK_DEBUG_DETAIL_SOLVETORSO
//#define SAFULLBODYIK_DEBUG_DETAIL_SOLVELEGS
#endif

using UnityEngine;

namespace SA
{
	public partial class FullBodyIK
	{
		public class BodyIK
		{
			LimbIK[] _limbIK; // for UpperSolve. (Presolve Shoulder / Elbow)

			Bone _hipsBone; // Null accepted.
			Bone[] _spineBones; // Null accepted.
			bool[] _spineEnabled; // Null accepted.
			Matrix3x3[] _spinePrevCenterArmToChildBasis; // Null accepted.
			Matrix3x3[] _spineCenterArmToChildBasis; // Null accepted.

			Bone _spineBone; // Null accepted.
			Bone _spineUBone; // Null accepted.
			Bone _neckBone; // Null accepted.
			Bone _headBone; // Null accepted.
			Bone[] _kneeBones;
			Bone[] _elbowBones;
			Bone[] _legBones; // Null accepted.
			Bone[] _shoulderBones; // Null accepted.
			Bone[] _armBones; // Null accepted.
			Bone[] _nearArmBones; // _shouderBones or _armBones
			float[] _spineDirXRate;
			
			Effector _hipsEffector;
			Effector _neckEffector;
			Effector _headEffector;
			Effector _eyesEffector;

			Effector[] _armEffectors = new Effector[2];
			Effector[] _elbowEffectors = new Effector[2];
			Effector[] _wristEffectors = new Effector[2];
			Effector[] _kneeEffectors = new Effector[2];
			Effector[] _footEffectors = new Effector[2];

			Vector3 _defaultCenterLegPos = Vector3.zero;

			Matrix3x3 _centerLegBoneBasis = Matrix3x3.identity;
			Matrix3x3 _centerLegBoneBasisInv = Matrix3x3.identity;

			Matrix3x3 _centerLegToArmBasis = Matrix3x3.identity;        // dirX = armPos[1] - armPos[0] or shoulderPos[1] - shoulderPos[0], dirY = centerArmPos - centerLegPos
			Matrix3x3 _centerLegToArmBasisInv = Matrix3x3.identity;     // _centerLegToArmBasis.transpose
			Matrix3x3 _centerLegToArmBoneToBaseBasis = Matrix3x3.identity;
			Matrix3x3 _centerLegToArmBaseToBoneBasis = Matrix3x3.identity;

			float[] _shoulderToArmLength = new float[2];
			bool[] _shouderLocalAxisYInv = new bool[2];
			FastLength[] _elbowEffectorMaxLength = new FastLength[2];
			FastLength[] _wristEffectorMaxLength = new FastLength[2];
			FastLength[] _kneeEffectorMaxLength = new FastLength[2];
			FastLength[] _footEffectorMaxLength = new FastLength[2];

			public class SolverCaches
			{
				public Bone[] armBones;
				public Bone[] shoulderBones;
				public Bone[] nearArmBones;

				public float armToArmLen;
				public float nearArmToNearArmLen;
				public float[] shoulderToArmLength;
				public float[] nearArmToNeckLength = new float[2];
				public float neckToHeadLength;

				public float neckPull = 0.0f;
				public float headPull = 0.0f;
				public float eyesRate = 0.0f;
				public float neckHeadPull = 0.0f;
				public float[] armPull = new float[2];
				public float[] elbowPull = new float[2];
				public float[] wristPull = new float[2];
				public float[] kneePull = new float[2];
				public float[] footPull = new float[2];

				public float[] fullArmPull = new float[2]; // arm + elbow + wrist, max 1.0
				public float[] limbLegPull = new float[2]; // knee + foot, max 1.0

				public float[] armToElbowPull = new float[2]; // pull : arm / elbow
				public float[] armToWristPull = new float[2]; // pull : arm / wrist
				public float[] neckHeadToFullArmPull = new float[2]; // pull : (neck + head) / (arm + elbow + wrist)

				public float limbArmRate = 0.0f;
				public float limbLegRate = 0.0f;

				public float armToLegRate = 0.0f;

				public Matrix3x3 centerLegToNearArmBasis = Matrix3x3.identity; // dirX = armPos[1] - armPos[0] or shoulderPos[1] - shoulderPos[0], dirY = centerArmPos - centerLegPos
				public Matrix3x3 centerLegToNearArmBasisInv = Matrix3x3.identity; // centerLegToNearArmBasis.transpose
				public Matrix3x3 centerLegToNaerArmBoneToBaseBasis = Matrix3x3.identity;
				public Matrix3x3 centerLegToNaerArmBaseToBoneBasis = Matrix3x3.identity;

				public Vector3 defaultCenterLegPos = Vector3.zero;
			}

			SolverCaches _solverCaches = new SolverCaches();

			Vector3 _defaultCenterArmPos = Vector3.zero;
			float _defaultCenterLegLen; // LeftLeg to RightLeg Length.
			float _defaultCenterLegHalfLen; // LeftLeg to RightLeg Length / 2.
			float _defaultNearArmToNearArmLen = 0.0f;
			float _defaultCenterLegToCeterArmLen = 0.0f;

			Vector3 _defaultCenterEyePos = Vector3.zero;

			SolverInternal _solverInternal;

			Settings _settings;
			InternalValues _internalValues;

			public BodyIK( FullBodyIK fullBodyIK, LimbIK[] limbIK )
			{
				Assert( fullBodyIK != null );

				_limbIK = limbIK;

				_settings = fullBodyIK.settings;
				_internalValues = fullBodyIK.internalValues;

				_hipsBone = _PrepareBone( fullBodyIK.bodyBones.hips );
				_neckBone = _PrepareBone( fullBodyIK.headBones.neck );
				_headBone = _PrepareBone( fullBodyIK.headBones.head );

				_hipsEffector = fullBodyIK.bodyEffectors.hips;
				_neckEffector = fullBodyIK.headEffectors.neck;
				_headEffector = fullBodyIK.headEffectors.head;
				_eyesEffector = fullBodyIK.headEffectors.eyes;

				_armEffectors[0] = fullBodyIK.leftArmEffectors.arm;
				_armEffectors[1] = fullBodyIK.rightArmEffectors.arm;
				_elbowEffectors[0] = fullBodyIK.leftArmEffectors.elbow;
				_elbowEffectors[1] = fullBodyIK.rightArmEffectors.elbow;
				_wristEffectors[0] = fullBodyIK.leftArmEffectors.wrist;
				_wristEffectors[1] = fullBodyIK.rightArmEffectors.wrist;
				_kneeEffectors[0] = fullBodyIK.leftLegEffectors.knee;
				_kneeEffectors[1] = fullBodyIK.rightLegEffectors.knee;
				_footEffectors[0] = fullBodyIK.leftLegEffectors.foot;
				_footEffectors[1] = fullBodyIK.rightLegEffectors.foot;

				_spineBones = _PrepareSpineBones( fullBodyIK.bones );
				if( _spineBones != null && _spineBones.Length > 0 ) {
					int spineLength = _spineBones.Length;
                    _spineBone = _spineBones[0];
					_spineUBone = _spineBones[spineLength - 1];
					_spineEnabled = new bool[spineLength];
                }

				// Memo: These should be pair bones.(Necessary each side bones.)

				_kneeBones = _PrepareBones( fullBodyIK.leftLegBones.knee, fullBodyIK.rightLegBones.knee );
				_elbowBones = _PrepareBones( fullBodyIK.leftArmBones.elbow, fullBodyIK.rightArmBones.elbow );
				_legBones = _PrepareBones( fullBodyIK.leftLegBones.leg, fullBodyIK.rightLegBones.leg );
				_armBones = _PrepareBones( fullBodyIK.leftArmBones.arm, fullBodyIK.rightArmBones.arm );
				_shoulderBones = _PrepareBones( fullBodyIK.leftArmBones.shoulder, fullBodyIK.rightArmBones.shoulder );
				_nearArmBones = (_shoulderBones != null) ? _shoulderBones : _nearArmBones;

				_Prepare( fullBodyIK );
			}

			static Bone[] _PrepareSpineBones( Bone[] bones )
			{
				if( bones == null || bones.Length != (int)BoneLocation.Max ) {
					Assert( false );
					return null;
				}

				int spineLength = 0;
				for( int i = (int)BoneLocation.Spine; i <= (int)BoneLocation.SpineU; ++i ) {
					if( bones[i] != null && bones[i].transformIsAlive ) {
						++spineLength;
					}
				}

				if( spineLength == 0 ) {
					return null;
				}

				Bone[] spineBones = new Bone[spineLength];
				int index = 0;
				for( int i = (int)BoneLocation.Spine; i <= (int)BoneLocation.SpineU; ++i ) {
					if( bones[i] != null && bones[i].transformIsAlive ) {
						spineBones[index] = bones[i];
						++index;
					}
				}

				return spineBones;
			}

			void _Prepare( FullBodyIK fullBodyIK )
			{
				if( _spineBones != null ) {
					int spineLength = _spineBones.Length;
					_spineDirXRate = new float[spineLength];

					if( spineLength > 1 ) {
						_spinePrevCenterArmToChildBasis = new Matrix3x3[spineLength - 1];
						_spineCenterArmToChildBasis = new Matrix3x3[spineLength - 1];
						for( int i = 0; i != spineLength - 1; ++i ) {
							_spinePrevCenterArmToChildBasis[i] = Matrix3x3.identity;
							_spineCenterArmToChildBasis[i] = Matrix3x3.identity;
						}
					}
				}
			}

			bool _isSyncDisplacementAtLeastOnce;

			void _SyncDisplacement()
			{
				// Measure bone length.(Using worldPosition)
				// Force execution on 1st time. (Ignore case _settings.syncDisplacement == SyncDisplacement.Disable)
				if( _settings.syncDisplacement == SyncDisplacement.Everyframe || !_isSyncDisplacementAtLeastOnce ) {
					_isSyncDisplacementAtLeastOnce = true;

					// Limit for Shoulder.
					if( _shoulderBones != null ) {
						for( int i = 0; i != 2; ++i ) {
							Assert( _shoulderBones[i] != null );
							Vector3 dirY = _shoulderBones[i]._localAxisBasis.column1;
							_shouderLocalAxisYInv[i] = Vector3.Dot( dirY, _internalValues.defaultRootBasis.column1 ) < 0.0f;
						}
					}

					// _defaultCenterEyePos
					if( _eyesEffector != null ) {
						_defaultCenterEyePos = _eyesEffector.defaultPosition;
					}

					// _defaultCenterLegPos
					if( _legBones != null ) {
						_defaultCenterLegPos = (_legBones[0]._defaultPosition + _legBones[1]._defaultPosition) * 0.5f;
					}

					// _defaultCenterArmPos, _centerLegToArmBasis, _centerLegToArmBasisInv, _centerLegToArmBoneToBaseBasis, _centerLegToArmBaseToBoneBasis

					if( _nearArmBones != null ) {
						_defaultCenterArmPos = (_nearArmBones[1]._defaultPosition + _nearArmBones[0]._defaultPosition) * 0.5f;

						Vector3 dirX = _nearArmBones[1]._defaultPosition - _nearArmBones[0]._defaultPosition;
						Vector3 dirY = _defaultCenterArmPos - _defaultCenterLegPos;
						if( SAFBIKVecNormalize( ref dirY ) && SAFBIKComputeBasisFromXYLockY( out _centerLegToArmBasis, ref dirX, ref dirY ) ) {
                            _centerLegToArmBasisInv = _centerLegToArmBasis.transpose;
							SAFBIKMatMult( out _centerLegToArmBoneToBaseBasis, ref _centerLegToArmBasisInv, ref _internalValues.defaultRootBasis );
							_centerLegToArmBaseToBoneBasis = _centerLegToArmBoneToBaseBasis.transpose;
						}
					}

					_solverCaches.armBones = _armBones;
					_solverCaches.shoulderBones = _shoulderBones;
					_solverCaches.nearArmBones = _nearArmBones;
					_solverCaches.centerLegToNearArmBasis = _centerLegToArmBasis;
					_solverCaches.centerLegToNearArmBasisInv = _centerLegToArmBasisInv;
					_solverCaches.centerLegToNaerArmBoneToBaseBasis = _centerLegToArmBoneToBaseBasis;
					_solverCaches.centerLegToNaerArmBaseToBoneBasis = _centerLegToArmBaseToBoneBasis;
					_solverCaches.defaultCenterLegPos = _defaultCenterLegPos;

					_defaultCenterLegToCeterArmLen = SAFBIKVecLength2( ref _defaultCenterLegPos, ref _defaultCenterArmPos );

					if( _footEffectors != null ) {
						if( _footEffectors[0].bone != null && _footEffectors[1].bone != null ) {
							_defaultCenterLegLen = SAFBIKVecLength2( ref _footEffectors[0].bone._defaultPosition, ref _footEffectors[1].bone._defaultPosition );
							_defaultCenterLegHalfLen = _defaultCenterLegLen * 0.5f;
						}
					}

					if( _spineBone != null && _legBones != null ) {
						if( _ComputeCenterLegBasis( out _centerLegBoneBasis,
							ref _spineBone._defaultPosition,
							ref _legBones[0]._defaultPosition,
							ref _legBones[1]._defaultPosition ) ) {
							_centerLegBoneBasisInv = _centerLegBoneBasis.transpose;
						}
					}

					// for UpperSolve.
					if( _armBones != null ) {
						if( _shoulderBones != null ) {
							for( int i = 0; i != 2; ++i ) {
								_shoulderToArmLength[i] = _armBones[i]._defaultLocalLength.length;
							}
                        }

						_solverCaches.armToArmLen = SAFBIKVecLength2( ref _armBones[0]._defaultPosition, ref _armBones[1]._defaultPosition );
					}

					if( _nearArmBones != null ) {
						_defaultNearArmToNearArmLen = SAFBIKVecLength2( ref _nearArmBones[0]._defaultPosition, ref _nearArmBones[1]._defaultPosition );
						if( _neckBone != null && _neckBone.transformIsAlive ) {
							_solverCaches.nearArmToNeckLength[0] = SAFBIKVecLength2( ref _neckBone._defaultPosition, ref _nearArmBones[0]._defaultPosition );
							_solverCaches.nearArmToNeckLength[1] = SAFBIKVecLength2( ref _neckBone._defaultPosition, ref _nearArmBones[1]._defaultPosition );
						}
					}
					if( _neckBone != null && _headBone != null ) {
						_solverCaches.neckToHeadLength = SAFBIKVecLength2( ref _neckBone._defaultPosition, ref _headBone._defaultPosition );
                    }

					_solverCaches.shoulderToArmLength = _shoulderToArmLength;
					_solverCaches.nearArmToNearArmLen = _defaultNearArmToNearArmLen;

					if( _kneeBones != null && _footEffectors != null ) {
						for( int i = 0; i != 2; ++i ) {
							Bone bendingBone = _kneeBones[i];
							Bone endBone = _footEffectors[i].bone;
							_kneeEffectorMaxLength[i] = bendingBone._defaultLocalLength;
                            _footEffectorMaxLength[i] = FastLength.FromLength( bendingBone._defaultLocalLength.length + endBone._defaultLocalLength.length );
						}
					}

					if( _elbowBones != null && _wristEffectors != null ) {
						for( int i = 0; i != 2; ++i ) {
							Bone bendingBone = _elbowBones[i];
							Bone endBone = _wristEffectors[i].bone;
							_elbowEffectorMaxLength[i] = bendingBone._defaultLocalLength;
							_wristEffectorMaxLength[i] = FastLength.FromLength( bendingBone._defaultLocalLength.length + endBone._defaultLocalLength.length );
						}
					}

					if( _spineBones != null ) {
						if( (_nearArmBones != null || _legBones != null) && _neckBone != null && _neckBone.transformIsAlive ) {
							//Vector3 armDirX = (_nearArmBones != null)
							//	? (_nearArmBones[1]._defaultPosition - _nearArmBones[0]._defaultPosition)
							//	: (_legBones[1]._defaultPosition - _legBones[0]._defaultPosition);

							Vector3 armDirX = _internalValues.defaultRootBasis.column0;

							int spineLength = _spineBones.Length;

							for( int i = 0; i < spineLength - 1; ++i ) {
								Matrix3x3 prevToCenterArmBasis;
								Matrix3x3 currToNeckBasis;

								Vector3 prevPos = (i != 0) ? _spineBones[i - 1]._defaultPosition : _defaultCenterLegPos;
								Vector3 dirY0 = _defaultCenterArmPos - prevPos;
								Vector3 dirY1 = _defaultCenterArmPos - _spineBones[i]._defaultPosition;

								if( !SAFBIKVecNormalize2( ref dirY0, ref dirY1 ) ) {
									continue;
								}

								if( !SAFBIKComputeBasisFromXYLockY( out prevToCenterArmBasis, ref armDirX, ref dirY0 ) ||
									!SAFBIKComputeBasisFromXYLockY( out currToNeckBasis, ref armDirX, ref dirY1 ) ) {
									continue;
								}

								SAFBIKMatMultInv0( out _spinePrevCenterArmToChildBasis[i], ref prevToCenterArmBasis, ref _spineBones[i]._localAxisBasis );
								SAFBIKMatMultInv0( out _spineCenterArmToChildBasis[i], ref currToNeckBasis, ref _spineBones[i]._localAxisBasis );
							}
						}
					}
				}
			}

			public bool Solve()
			{
				bool isEffectorEnabled = _IsEffectorEnabled();
				if( !isEffectorEnabled && !_settings.bodyIK.forceSolveEnabled ) {
					return false;
				}

				_SyncDisplacement();

				if( !_PrepareSolverInternal() ) {
					return false;
				}

				var temp = _solverInternal;

				if( !_internalValues.resetTransforms ) {
					if( temp.spinePos != null ) {
						for( int i = 0; i != _spineBones.Length; ++i ) {
							if( _spineBones[i] != null ) {
								temp.spinePos[i] = _spineBones[i].worldPosition;
							}
						}
					}
					if( _neckBone != null ) {
						temp.neckPos = _neckBone.worldPosition;
					}
					if( _headBone != null ) {
						temp.headPos = _headBone.worldPosition;
					}
					if( temp.shoulderPos != null ) {
						for( int i = 0; i < 2; ++i ) {
							temp.shoulderPos[i] = _shoulderBones[i].worldPosition;
						}
					}
					if( temp.armPos != null ) {
						for( int i = 0; i != 2; ++i ) {
							temp.armPos[i] = _armBones[i].worldPosition;
						}
					}
					if( temp.legPos != null ) {
						for( int i = 0; i != 2; ++i ) {
							temp.legPos[i] = _legBones[i].worldPosition;
						}
					}

					temp.SetDirtyVariables();
				}

				if( _internalValues.resetTransforms ) {
					_ResetTransforms();
				} else if( _internalValues.animatorEnabled ) {
					_PresolveHips();
				}

				if( !_internalValues.resetTransforms ) {
					if( _settings.bodyIK.shoulderSolveEnabled ) {
						_ResetShoulderTransform();
					}
				}

				// Arms, Legs (Need calls after _ResetTransform() / _PresolveHips() / _ResetShoulderTransform().)
				// (Using temp.armPos[] / temp.legPos[])
				_solverInternal.arms.Prepare( _elbowEffectors, _wristEffectors );
				_solverInternal.legs.Prepare( _kneeEffectors, _footEffectors );

#if SAFULLBODYIK_DEBUG
				bool _isVisibleWorldTransform = true;
				_internalValues.UpdateDebugValue( "_isVisibleWorldTransform", ref _isVisibleWorldTransform );
#endif

				if( _settings.bodyIK.lowerSolveEnabled ) {
					_LowerSolve( true );
				}

				if( _settings.bodyIK.upperSolveEnabled ) {
					_UpperSolve();
				}

				if( _settings.bodyIK.lowerSolveEnabled ) {
					_LowerSolve( false );
				}

				if( _settings.bodyIK.shoulderSolveEnabled ) {
					_ShoulderResolve();
				}

				if( _settings.bodyIK.computeWorldTransform ) {
					_ComputeWorldTransform();
				}

#if SAFULLBODYIK_DEBUG
				if( _isVisibleWorldTransform ) {
					_internalValues.AddDebugPoint( temp.centerLegPos );
					if( temp.spinePos != null ) {
						for( int i = 0; i < temp.spinePos.Length; ++i ) {
							_internalValues.AddDebugPoint( temp.spinePos[i] );
						}
					}
					_internalValues.AddDebugPoint( temp.neckPos );
					for( int i = 0; i < 2; ++i ) {
						if( temp.shoulderPos != null ) {
							_internalValues.AddDebugPoint( temp.shoulderPos[i] );
						}
						_internalValues.AddDebugPoint( temp.armPos[i] );
						_internalValues.AddDebugPoint( temp.legPos[i] );
					}
				}
#endif

				return true;
			}

			bool _UpperSolve()
			{
				var temp = _solverInternal;
				Assert( temp != null );

				float hipsPull = _hipsEffector.positionEnabled ? _hipsEffector.pull : 0.0f;
				float neckPull = _solverCaches.neckPull;
				float headPull = _solverCaches.headPull;
				float eyesRate = _solverCaches.eyesRate; // pull * positionWeight
                float[] armPull = _solverCaches.armPull;
				float[] elbowPull = _solverCaches.elbowPull;
				float[] wristPull = _solverCaches.wristPull;

				if( _settings.bodyIK.forceSolveEnabled ) {
					// Nothing.
				} else {
					if( hipsPull <= IKEpsilon && neckPull <= IKEpsilon && headPull <= IKEpsilon && eyesRate <= IKEpsilon &&
						armPull[0] <= IKEpsilon && armPull[1] <= IKEpsilon &&
						elbowPull[0] <= IKEpsilon && elbowPull[1] <= IKEpsilon &&
						wristPull[0] <= IKEpsilon && wristPull[1] <= IKEpsilon ) {
						return false; // No moved.
					}
				}

				Vector3 baseCenterLegPos = Vector3.zero; // for continuousSolver

				bool continuousSolverEnabled = _internalValues.continuousSolverEnabled;

				// Preprocess for armPos / armPos2
				if( continuousSolverEnabled ) {
					Matrix3x3 centerLegBasis;
					_UpperSolve_PresolveBaseCenterLegTransform( out baseCenterLegPos, out centerLegBasis );

					temp.Backup(); // for Testsolver.

					if( _spineBones != null ) {
						for( int i = 0; i < _spineBones.Length; ++i ) {
							SAFBIKMatMultVecPreSubAdd( out temp.spinePos[i], ref centerLegBasis, ref _spineBones[i]._defaultPosition, ref _defaultCenterLegPos, ref baseCenterLegPos );
						}
					}
					if( _neckBone != null ) {
						SAFBIKMatMultVecPreSubAdd( out temp.neckPos, ref centerLegBasis, ref _neckBone._defaultPosition, ref _defaultCenterLegPos, ref baseCenterLegPos );
					}
					if( _headBone != null && temp.headEnabled ) {
						SAFBIKMatMultVecPreSubAdd( out temp.headPos, ref centerLegBasis, ref _headBone._defaultPosition, ref _defaultCenterLegPos, ref baseCenterLegPos );
					}
					for( int n = 0; n < 2; ++n ) {
						if( _shoulderBones != null ) {
							SAFBIKMatMultVecPreSubAdd( out temp.shoulderPos[n], ref centerLegBasis, ref _shoulderBones[n]._defaultPosition, ref _defaultCenterLegPos, ref baseCenterLegPos );
						}
						if( _armBones != null ) {
							SAFBIKMatMultVecPreSubAdd( out temp.armPos[n], ref centerLegBasis, ref _armBones[n]._defaultPosition, ref _defaultCenterLegPos, ref baseCenterLegPos );
						}
						if( _legBones != null ) {
							SAFBIKMatMultVecPreSubAdd( out temp.legPos[n], ref centerLegBasis, ref _legBones[n]._defaultPosition, ref _defaultCenterLegPos, ref baseCenterLegPos );
						}
					}
					temp.SetDirtyVariables();
				}

				temp.UpperSolve();

				float upperLerpDir1Rate = _internalValues.bodyIK.upperCenterLegRotateRate.value;
				float upperLerpDir2Rate = _internalValues.bodyIK.upperSpineRotateRate.value;
				float upperLerpPos1Rate = _internalValues.bodyIK.upperCenterLegTranslateRate.value;
				float upperLerpPos2Rate = _internalValues.bodyIK.upperSpineTranslateRate.value;

				Vector3 targetCenterArmPos = temp.targetCenterArmPos;
				Vector3 targetCenterArmDir = temp.targetCenterArmDir;
				Vector3 currentCenterArmPos = temp.currentCenterArmPos;
				Vector3 currentCenterArmDir = temp.currentCenterArmDir;

				Vector3 centerArmDirX = _LerpDir( ref currentCenterArmDir, ref targetCenterArmDir, upperLerpDir1Rate );
				Vector3 centerArmDirX2 = _LerpDir( ref currentCenterArmDir, ref targetCenterArmDir, upperLerpDir2Rate );

				Vector3 centerArmPos, centerArmPos2;
				Vector3 centerArmDirY, centerArmDirY2;

				centerArmPos = Vector3.Lerp( currentCenterArmPos, targetCenterArmPos, upperLerpPos1Rate );
				centerArmPos2 = Vector3.Lerp( currentCenterArmPos, targetCenterArmPos, upperLerpPos2Rate );

				centerArmDirY = centerArmPos - temp.centerLegPos;
				centerArmDirY2 = centerArmPos2 - temp.centerLegPos;
				if( !SAFBIKVecNormalize2( ref centerArmDirY, ref centerArmDirY2 ) ) {
					return false;
				}

				if( _settings.bodyIK.upperDirXLimitEnabled ) {
					if( _internalValues.bodyIK.upperDirXLimitThetaY.sin <= IKEpsilon ) {
						if( !_FitToPlaneDir( ref centerArmDirX, centerArmDirY ) ||
							!_FitToPlaneDir( ref centerArmDirX2, centerArmDirY2 ) ) {
							return false;
						}
					} else {
						if( !_LimitToPlaneDirY( ref centerArmDirX, centerArmDirY, _internalValues.bodyIK.upperDirXLimitThetaY.sin ) ||
							!_LimitToPlaneDirY( ref centerArmDirX2, centerArmDirY2, _internalValues.bodyIK.upperDirXLimitThetaY.sin ) ) {
							return false;
						}
					}
				}

				// Limit for spine.
				if( _settings.bodyIK.spineLimitEnabled ) {
					float spineLimitAngleX = _internalValues.bodyIK.spineLimitAngleX.value;
					float spineLimitAngleY = _internalValues.bodyIK.spineLimitAngleY.value;

					if( _settings.bodyIK.spineAccurateLimitEnabled ) { // Quaternion lerp.
						float fromToX = Vector3.Dot( centerArmDirX, centerArmDirX2 );
						float fromToXAng = SAFBIKAcos( fromToX );
						if( fromToXAng > spineLimitAngleX ) {
							Vector3 axisDir = Vector3.Cross( centerArmDirX, centerArmDirX2 );
							if( SAFBIKVecNormalize( ref axisDir ) ) {
								Quaternion q = Quaternion.AngleAxis( _settings.bodyIK.spineLimitAngleX, axisDir );
								Matrix3x3 rotateBasis;
								SAFBIKMatSetRot( out rotateBasis, ref q );
								SAFBIKMatMultVec( out centerArmDirX2, ref rotateBasis, ref centerArmDirX );
							}
						}

						float fromToY = Vector3.Dot( centerArmDirY, centerArmDirY2 );
						float fromToYAng = SAFBIKAcos( fromToY );
						if( fromToYAng > spineLimitAngleY ) {
							Vector3 axisDir = Vector3.Cross( centerArmDirY, centerArmDirY2 );
							if( SAFBIKVecNormalize( ref axisDir ) ) {
								Quaternion q = Quaternion.AngleAxis( _settings.bodyIK.spineLimitAngleY, axisDir );
								Matrix3x3 rotateBasis;
								SAFBIKMatSetRot( out rotateBasis, ref q );
								SAFBIKMatMultVec( out centerArmDirY2, ref rotateBasis, ref centerArmDirY );
							}
						}
					} else { // Lienar lerp.
						// Recompute centerLegToArmBoneBasisTo2( for Spine )
						float fromToX = Vector3.Dot( centerArmDirX, centerArmDirX2 );
						float fromToXAng = SAFBIKAcos( fromToX );
						if( fromToXAng > spineLimitAngleX ) {
							if( fromToXAng > IKEpsilon ) {
								float balancedRate = spineLimitAngleX / fromToXAng;
								Vector3 dirX2Balanced = Vector3.Lerp( centerArmDirX, centerArmDirX2, balancedRate );
								if( SAFBIKVecNormalize( ref dirX2Balanced ) ) {
									centerArmDirX2 = dirX2Balanced;
								}
							}
						}

						// Pending: spine stiffness.(Sin scale to balanced rate.)
						float fromToY = Vector3.Dot( centerArmDirY, centerArmDirY2 );
						float fromToYAng = SAFBIKAcos( fromToY );
						if( fromToYAng > spineLimitAngleY ) {
							if( fromToYAng > IKEpsilon ) {
								float balancedRate = spineLimitAngleY / fromToYAng;
								Vector3 dirY2Balanced = Vector3.Lerp( centerArmDirY, centerArmDirY2, balancedRate );
								if( SAFBIKVecNormalize( ref dirY2Balanced ) ) {
									centerArmDirY2 = dirY2Balanced;
								}
							}
						}
					}
				}

				// This is missing. todo: Fix
				Vector3 presolveCenterLegPos = temp.centerLegPos; // for continuousSolverEnabled

				// for eyes.
				// presolvedCenterLegPos2 = presolveCenterLegPos + presolved postTranslate.
				Vector3 presolvedCenterLegPos2 = temp.centerLegPos;
				if( eyesRate > IKEpsilon ) {
					Vector3 source = temp.centerLegPos + centerArmDirY2 * _defaultCenterLegToCeterArmLen;
					presolvedCenterLegPos2 += temp.targetCenterArmPos - source;
                }

				// Eyes
				if( eyesRate > IKEpsilon ) {
					// Based on centerArmDirX2 / centerArmDirY2
					Matrix3x3 toBasis;
					if( SAFBIKComputeBasisFromXYLockY( out toBasis, ref centerArmDirX2, ref centerArmDirY2 ) ) {
						Matrix3x3 toBasisGlobal;
						SAFBIKMatMult( out toBasisGlobal, ref toBasis, ref _centerLegToArmBasisInv );

						Matrix3x3 fromBasis = toBasis;
						SAFBIKMatMultRet0( ref toBasis, ref _centerLegToArmBoneToBaseBasis );

						Vector3 eyePos;
						SAFBIKMatMultVecPreSubAdd( out eyePos, ref toBasisGlobal, ref _defaultCenterEyePos, ref _defaultCenterLegPos, ref presolvedCenterLegPos2 );

						Vector3 eyeDir = _eyesEffector.worldPosition - eyePos; // Memo: Not use _eyesEffector._hidden_worldPosition

						{
							float upperEyesXLimit = _internalValues.bodyIK.upperEyesLimitYaw.sin;
							float upperEyesYUpLimit = _internalValues.bodyIK.upperEyesLimitPitchUp.sin;
							float upperEyesYDownLimit = _internalValues.bodyIK.upperEyesLimitPitchDown.sin;

							SAFBIKMatMultVecInv( out eyeDir, ref toBasis, ref eyeDir ); // to Local

							eyeDir.x *= _settings.bodyIK.upperEyesYawRate;
                            if( eyeDir.y >= 0.0f ) {
								eyeDir.y *= _settings.bodyIK.upperEyesPitchUpRate;
							} else {
								eyeDir.y *= _settings.bodyIK.upperEyesPitchDownRate;
							}

							SAFBIKVecNormalize( ref eyeDir );

							if( _ComputeEyesRange( ref eyeDir, _internalValues.bodyIK.upperEyesTraceTheta.cos ) ) {
								_LimitXY( ref eyeDir, upperEyesXLimit, upperEyesXLimit, upperEyesYDownLimit, upperEyesYUpLimit );
							}

							SAFBIKMatMultVec( out eyeDir, ref toBasis, ref eyeDir ); // to Global

							{
								Vector3 xDir = toBasis.column0;
								Vector3 yDir = toBasis.column1;
								Vector3 zDir = eyeDir;

								if( SAFBIKComputeBasisLockZ( out toBasis, ref xDir, ref yDir, ref zDir ) ) {
									// Nothing.
								}
							}
						}

						SAFBIKMatMultRet0( ref toBasis, ref _centerLegToArmBaseToBoneBasis );

						float upperEyesRate1 = _settings.bodyIK.upperEyesToCenterLegRate * eyesRate;
						float upperEyesRate2 = _settings.bodyIK.upperEyesToSpineRate * eyesRate;

						Matrix3x3 solveBasis;
						if( upperEyesRate2 > IKEpsilon ) {
							SAFBIKMatFastLerp( out solveBasis, ref fromBasis, ref toBasis, upperEyesRate2 );
							centerArmDirX2 = solveBasis.column0;
							centerArmDirY2 = solveBasis.column1;
						}

						if( upperEyesRate1 > IKEpsilon ) {
							if( SAFBIKComputeBasisFromXYLockY( out fromBasis, ref centerArmDirX, ref centerArmDirY ) ) {
								SAFBIKMatFastLerp( out solveBasis, ref fromBasis, ref toBasis, upperEyesRate1 );
								centerArmDirX = solveBasis.column0;
								centerArmDirY = solveBasis.column1;
							}
						}
					}
				}

				if( continuousSolverEnabled ) {
					// Salvage bone positions at end of testsolver.
					temp.Restore();

					temp.UpperSolve();
				}

				int spineLength = (_spineBones != null) ? (_spineBones.Length) : 0;

				float stableCenterLegRate = _settings.bodyIK.upperContinuousCenterLegRotationStableRate;

				// centerLeg(Hips)
				if( _settings.bodyIK.upperSolveHipsEnabled ) {
					Matrix3x3 toBasis;
					if( SAFBIKComputeBasisFromXYLockY( out toBasis, ref centerArmDirX, ref centerArmDirY ) ) {
						Matrix3x3 rotateBasis = Matrix3x3.identity;

						if( _internalValues.animatorEnabled || _internalValues.resetTransforms ) {
							// for animatorEnabled or resetTransform(Base on armPos)
							if( continuousSolverEnabled && stableCenterLegRate > IKEpsilon ) {
								Matrix3x3 presolveCenterLegBasis = Matrix3x3.identity;
								Vector3 solveDirY = centerArmPos - presolveCenterLegPos;
								Vector3 solveDirX = centerArmDirX;
								if( SAFBIKVecNormalize( ref solveDirY ) && SAFBIKComputeBasisFromXYLockY( out presolveCenterLegBasis, ref solveDirX, ref solveDirY ) ) {
									Matrix3x3 tempBasis;
									SAFBIKMatFastLerp( out tempBasis, ref toBasis, ref presolveCenterLegBasis, stableCenterLegRate );
									toBasis = tempBasis;
								}
							}

							Matrix3x3 fromBasis;
							Vector3 currentDirX = temp.nearArmPos[1] - temp.nearArmPos[0];
							Vector3 currentDirY = (temp.nearArmPos[1] + temp.nearArmPos[0]) * 0.5f - temp.centerLegPos;
							if( SAFBIKVecNormalize( ref currentDirY ) && SAFBIKComputeBasisFromXYLockY( out fromBasis, ref currentDirX, ref currentDirY ) ) {
								SAFBIKMatMultInv1( out rotateBasis, ref toBasis, ref fromBasis );
							}
						} else { // for continuousSolverEnabled.(Base on centerLegBasis)
							SAFBIKMatMultRet0( ref toBasis, ref _centerLegToArmBasisInv );

							if( continuousSolverEnabled && stableCenterLegRate > IKEpsilon ) {
								Matrix3x3 presolveCenterLegBasis = Matrix3x3.identity;
								Vector3 solveDirY = centerArmPos - presolveCenterLegPos;
								Vector3 solveDirX = centerArmDirX;
								if( SAFBIKVecNormalize( ref solveDirY ) && SAFBIKComputeBasisFromXYLockY( out presolveCenterLegBasis, ref solveDirX, ref solveDirY ) ) {
									SAFBIKMatMultRet0( ref presolveCenterLegBasis, ref _centerLegToArmBasisInv );
									Matrix3x3 tempBasis;
									SAFBIKMatFastLerp( out tempBasis, ref toBasis, ref presolveCenterLegBasis, stableCenterLegRate );
									toBasis = tempBasis;
								}
							}

							Matrix3x3 centerLegBasis = temp.centerLegBasis;
							SAFBIKMatMultInv1( out rotateBasis, ref toBasis, ref centerLegBasis );
						}

						if( _settings.bodyIK.upperCenterLegLerpRate < 1.0f - IKEpsilon ) {
							SAFBIKMatFastLerpToIdentity( ref rotateBasis, 1.0f - _settings.bodyIK.upperCenterLegLerpRate );
						}

						temp.UpperRotation( -1, ref rotateBasis );
					}
				}

				{
					float centerLegToArmLength = _defaultCenterLegToCeterArmLen;

					Vector3 centerLegBasisX = temp.centerLegBasis.column0;
					Vector3 centerArmPosY2 = centerArmDirY2 * centerLegToArmLength + temp.centerLegPos;
					float upperSpineLerpRate = _settings.bodyIK.upperSpineLerpRate;
					
					for( int i = 0; i != spineLength; ++i ) {
						if( !_spineEnabled[i] ) {
							continue;
						}

						Vector3 origPos = temp.spinePos[i];

						if( i + 1 == spineLength ) {
							Vector3 currentDirX = temp.nearArmPos[1] - temp.nearArmPos[0];
							Vector3 currentDirY = temp.centerArmPos - origPos;

							Vector3 targetDirX = centerArmDirX2;
							Vector3 targetDirY = centerArmPosY2 - origPos;

							if( !SAFBIKVecNormalize2( ref currentDirY, ref targetDirY ) ) {
								continue; // Skip.
							}

							Matrix3x3 toBasis;
							SAFBIKComputeBasisFromXYLockY( out toBasis, ref targetDirX, ref targetDirY );
							Matrix3x3 fromBasis;
							SAFBIKComputeBasisFromXYLockY( out fromBasis, ref currentDirX, ref currentDirY );

							Matrix3x3 rotateBasis;
							SAFBIKMatMultInv1( out rotateBasis, ref toBasis, ref fromBasis );

							if( upperSpineLerpRate < 1.0f - IKEpsilon ) {
								SAFBIKMatFastLerpToIdentity( ref rotateBasis, 1.0f - upperSpineLerpRate );
							}

							temp.UpperRotation( i, ref rotateBasis );
						} else {
							Vector3 childPos = (i + 1 == spineLength) ? temp.neckPos : temp.spinePos[i + 1];
							Vector3 prevPos = (i != 0) ? temp.spinePos[i - 1] : temp.centerLegPos;

							Vector3 currentDirX = temp.nearArmPos[1] - temp.nearArmPos[0];
							Vector3 currentDirY = childPos - origPos;

							Vector3 targetDirX = centerArmDirX2;
							Vector3 targetDirY = centerArmPosY2 - prevPos;
							Vector3 targetDirY2 = centerArmPosY2 - origPos;

							if( !SAFBIKVecNormalize4( ref currentDirX, ref currentDirY, ref targetDirY, ref targetDirY2 ) ) {
								continue; // Skip.
							}

							Matrix3x3 prevToNearArmBasis;
							Matrix3x3 origToNearArmBasis;

							if( !SAFBIKComputeBasisFromXYLockY( out prevToNearArmBasis, ref currentDirX, ref targetDirY ) ||
								!SAFBIKComputeBasisFromXYLockY( out origToNearArmBasis, ref currentDirX, ref targetDirY2 ) ) {
								continue;
							}

							// Get prevPos to child dir.
							SAFBIKMatMultCol1( out targetDirY, ref prevToNearArmBasis, ref _spinePrevCenterArmToChildBasis[i] );
							SAFBIKMatMultCol1( out targetDirY2, ref origToNearArmBasis, ref _spineCenterArmToChildBasis[i] );

							float spineDirXLegToArmRate = _spineDirXRate[i];

							// Simply Lerp.(dirX)
							currentDirX = Vector3.Lerp( centerLegBasisX, currentDirX, spineDirXLegToArmRate );
							targetDirX = Vector3.Lerp( centerLegBasisX, targetDirX, spineDirXLegToArmRate );

							// Simply Lerp.(dirY)
							if( i + 1 != spineLength ) { // Exclude spineU
								targetDirY = Vector3.Lerp( targetDirY, targetDirY2, _settings.bodyIK.spineDirYLerpRate );
								if( !SAFBIKVecNormalize( ref targetDirY ) ) { // Failsafe.
									targetDirY = currentDirY;
								}
							}

							Matrix3x3 toBasis;
							SAFBIKComputeBasisFromXYLockY( out toBasis, ref targetDirX, ref targetDirY );
							Matrix3x3 fromBasis;
							SAFBIKComputeBasisFromXYLockY( out fromBasis, ref currentDirX, ref currentDirY );

							Matrix3x3 rotateBasis;
							SAFBIKMatMultInv1( out rotateBasis, ref toBasis, ref fromBasis );

							if( upperSpineLerpRate < 1.0f - IKEpsilon ) {
								SAFBIKMatFastLerpToIdentity( ref rotateBasis, 1.0f - upperSpineLerpRate );
							}

							temp.UpperRotation( i, ref rotateBasis );
						}
					}
				}

				_UpperSolve_Translate2(
					ref _internalValues.bodyIK.upperPostTranslateRate,
					ref _internalValues.bodyIK.upperContinuousPostTranslateStableRate,
					ref baseCenterLegPos );

				return true;
			}

			void _ShoulderResolve()
			{
				var temp = _solverInternal;
				Assert( temp != null );
				temp.ShoulderResolve();
            }

			void _UpperSolve_PresolveBaseCenterLegTransform( out Vector3 centerLegPos, out Matrix3x3 centerLegBasis )
			{
				Assert( _internalValues != null && _internalValues.continuousSolverEnabled );
				var temp = _solverInternal;
				var cache = _solverCaches;
				Assert( temp != null );
				Assert( cache != null );

				_GetBaseCenterLegTransform( out centerLegPos, out centerLegBasis );

				if( _legBones == null || !_legBones[0].transformIsAlive || !_legBones[1].transformIsAlive ) {
					return;
				}

				if( cache.limbLegPull[0] <= IKEpsilon && cache.limbLegPull[1] <= IKEpsilon ) {
					return; // Pull nothing.
				}

				Vector3 legPos0, legPos1;
				SAFBIKMatMultVecPreSubAdd( out legPos0, ref centerLegBasis, ref _legBones[0]._defaultPosition, ref _defaultCenterLegPos, ref centerLegPos );
				SAFBIKMatMultVecPreSubAdd( out legPos1, ref centerLegBasis, ref _legBones[1]._defaultPosition, ref _defaultCenterLegPos, ref centerLegPos );

				bool isLimited = false;
				isLimited |= temp.legs.SolveTargetBeginPos( 0, ref legPos0 );
				isLimited |= temp.legs.SolveTargetBeginPos( 1, ref legPos1 );

				if( isLimited ) {
					Vector3 vecX = centerLegBasis.column0 * _defaultCenterLegHalfLen;
					centerLegPos = Vector3.Lerp( legPos0 + vecX, legPos1 - vecX, cache.limbLegRate );
				}
			}

			void _UpperSolve_Transform( int origIndex, ref Matrix3x3 transformBasis )
			{
				var temp = _solverInternal;

				Vector3 origPos = (origIndex == -1) ? temp.centerLegPos : temp.spinePos[origIndex];

				for( int i = 0; i != 2; ++i ) {
					SAFBIKMatMultVecPreSubAdd( out temp.armPos[i], ref transformBasis, ref temp.armPos[i], ref origPos, ref origPos );
					if( _shoulderBones != null ) {
						SAFBIKMatMultVecPreSubAdd( out temp.shoulderPos[i] , ref transformBasis , ref temp.shoulderPos[i] , ref origPos, ref origPos );
					}
				}

				int spineLength = (_spineBones != null) ? (_spineBones.Length) : 0;
				for( int spineIndex = origIndex + 1; spineIndex < spineLength; ++spineIndex ) {
					SAFBIKMatMultVecPreSubAdd( out temp.spinePos[spineIndex], ref transformBasis, ref temp.spinePos[spineIndex], ref origPos, ref origPos );
				}

				if( _neckBone != null ) {
					SAFBIKMatMultVecPreSubAdd( out temp.neckPos, ref transformBasis, ref temp.neckPos, ref origPos, ref origPos );
				}

				if( origIndex == -1 ) {
					if( temp.legPos != null ) {
						for( int i = 0; i < 2; ++i ) {
							SAFBIKMatMultVecPreSubAdd( out temp.legPos[i], ref transformBasis, ref temp.legPos[i], ref origPos, ref origPos );
						}
					}
				}

				temp.SetDirtyVariables();
			}

			bool _UpperSolve_PreTranslate2( out Vector3 translate, ref CachedRate01 translateRate, ref CachedRate01 stableRate, ref Vector3 stableCenterLegPos )
			{
				// If resetTransform = false, contain targetBeginPos to default transform or modify _UpperSolve_Translate()

				// Memo: Prepare SolveTargetBeginPosRated().

				translate = Vector3.zero;

				Assert( _hipsEffector != null );
				if( _hipsEffector.positionEnabled &&
					_hipsEffector.positionWeight <= IKEpsilon &&
					_hipsEffector.pull >= 1.0f - IKEpsilon ) {
					return false; // Always locked.
				}

				var temp = _solverInternal;
				Assert( temp != null );

				bool continuousSolverEnabled = _internalValues.continuousSolverEnabled;

				bool translateEnabled = (continuousSolverEnabled && stableRate.isGreater0);
				if( temp.targetCenterArmEnabled ) {
					translate = temp.targetCenterArmPos - temp.currentCenterArmPos;
					translateEnabled = true;
				}

				if( translateEnabled ) {
					if( translateRate.isLess1 ) {
						translate *= translateRate.value;
					}

					if( continuousSolverEnabled && stableRate.isGreater0 ) {
						Vector3 extraTranslate = stableCenterLegPos - temp.centerLegPos;
						translate = Vector3.Lerp( translate, extraTranslate, stableRate.value );
					}

					if( _hipsEffector.positionEnabled && _hipsEffector.pull > IKEpsilon ) {
						Vector3 extraTranslate = _hipsEffector._hidden_worldPosition - temp.centerLegPos;
						translate = Vector3.Lerp( translate, extraTranslate, _hipsEffector.pull );
					}

					return true;
				}

				return false;
			}

			void _UpperSolve_Translate2( ref CachedRate01 translateRate, ref CachedRate01 stableRate, ref Vector3 stableCenterLegPos )
			{
				Vector3 translate;
				if( _UpperSolve_PreTranslate2( out translate, ref translateRate, ref stableRate, ref stableCenterLegPos ) ) {
					var temp = _solverInternal;
					Assert( temp != null );
					temp.Translate( ref translate );
				}
			}

			void _LowerSolve( bool firstPass )
			{
				var temp = _solverInternal;
				var cache = _solverCaches;
				Assert( temp != null );
				Assert( cache != null );
				if( temp.spinePos == null || temp.spinePos.Length == 0 ) {
					return;
				}

				float limbRate = firstPass ? 1.0f : cache.armToLegRate;

				if( temp.PrepareLowerRotation( 0 ) ) {
					Vector3 centerLegBoneY = temp.centerLegBasis.column1;
					for( int i = 0; i < 2; ++i ) {
						if( temp.legs.endPosEnabled[i] && temp.legs.targetBeginPosEnabled[i] ) {
							Vector3 legDir = temp.legs.targetBeginPos[i] - temp.legs.beginPos[i];
							if( SAFBIKVecNormalize( ref legDir ) ) {
								float legDirFeedbackRate = Vector3.Dot( centerLegBoneY, -legDir );
#if false
								legDirFeedbackRate = Mathf.Asin( Mathf.Clamp( legDirFeedbackRate, -1.0f, 1.0f ) );
								legDirFeedbackRate *= 1.0f / (90.0f * Mathf.Deg2Rad);
								legDirFeedbackRate = Mathf.Clamp01( legDirFeedbackRate );
#else // Faster
								legDirFeedbackRate = Mathf.Clamp01( legDirFeedbackRate );
#endif
								legDirFeedbackRate = 1.0f - legDirFeedbackRate;
								temp.SetSolveFeedbackRate( i, legDirFeedbackRate * limbRate );
							}
						}
					}
					
					Quaternion origLowerRotation;
					if( temp.SolveLowerRotation( 0, out origLowerRotation ) ) {
						temp.LowerRotation( 0, ref origLowerRotation, false );
					}
				}

				if( _hipsEffector.positionEnabled &&
					_hipsEffector.positionWeight <= IKEpsilon &&
					_hipsEffector.pull >= 1.0f - IKEpsilon ) {
					// Nothing.(Always locked.)
				} else {
					if( temp.PrepareLowerTranslate() ) {
						Vector3 origLowerTranslate;
						if( temp.SolveLowerTranslate( out origLowerTranslate ) ) {
							if( limbRate < 1.0f - IKEpsilon ) {
								origLowerTranslate *= limbRate;
							}

							if( _hipsEffector.positionEnabled && _hipsEffector.pull > IKEpsilon ) {
								Vector3 extraTranslate = _hipsEffector._hidden_worldPosition - temp.centerLegPos;
								origLowerTranslate = Vector3.Lerp( origLowerTranslate, extraTranslate, _hipsEffector.pull );
							}

							temp.Translate( ref origLowerTranslate );
						}
					}
				}
			}

			void _ComputeWorldTransform()
			{
				var temp = _solverInternal;
				if( temp == null || temp.spinePos == null || temp.spinePos.Length == 0 ) {
					return;
				}

				// Compute worldPosition / worldRotation.
				if( _hipsBone != null && _hipsBone.transformIsAlive && temp.spinePos != null && temp.spinePos.Length > 0 && _neckBone != null && _neckBone.transformIsAlive ) {
					Vector3 hipsToSpineDirX = new Vector3( 1.0f, 0.0f, 0.0f );

					Vector3 dirX = temp.legs.beginPos[1] - temp.legs.beginPos[0];
					Vector3 dirY = temp.spinePos[0] - (temp.legs.beginPos[1] + temp.legs.beginPos[0]) * 0.5f;

					Matrix3x3 boneBasis = new Matrix3x3();

					if( SAFBIKVecNormalize( ref dirY ) && SAFBIKComputeBasisFromXYLockY( out boneBasis, ref dirX, ref dirY ) ) {
						Matrix3x3 tempBasis;
						SAFBIKMatMult( out tempBasis, ref boneBasis, ref _centerLegBoneBasisInv );

						hipsToSpineDirX = boneBasis.column0; // Counts as baseBasis.

						Quaternion worldRotation;
						SAFBIKMatMultGetRot( out worldRotation, ref tempBasis, ref _hipsBone._defaultBasis );
						_hipsBone.worldRotation = worldRotation;

						if( _hipsBone.isWritebackWorldPosition ) {
							Vector3 worldPosition;
							Vector3 inv_defaultLocalTranslate = -_spineBone._defaultLocalTranslate;
							SAFBIKMatMultVecAdd( out worldPosition, ref tempBasis, ref inv_defaultLocalTranslate, ref temp.spinePos[0] );
							_hipsBone.worldPosition = worldPosition;
						}
					} else { // Failsafe.
						if( SAFBIKVecNormalize( ref dirX ) ) {
							hipsToSpineDirX = dirX;
                        }
					}

					int spineLength = temp.spinePos.Length;
                    for( int i = 0; i != spineLength; ++i ) {
						if( !_spineEnabled[i] ) {
							continue;
						}

						if( i + 1 == spineLength ) {
							dirY = temp.neckPos - temp.spinePos[i];
							if( temp.nearArmPos != null ) {
								dirX = temp.nearArmPos[1] - temp.nearArmPos[0];
							} else { // Failsafe.
								dirX = hipsToSpineDirX;
							}
						} else {
							dirY = temp.spinePos[i + 1] - temp.spinePos[i];
							dirX = hipsToSpineDirX;
							if( temp.nearArmPos != null ) {
								Vector3 dirX0 = temp.nearArmPos[1] - temp.nearArmPos[0];
								if( SAFBIKVecNormalize( ref dirX0 ) ) {
									dirX = Vector3.Lerp( dirX, dirX0, _settings.bodyIK.spineDirXLegToArmRate );
								}
							}
						}

						if( SAFBIKVecNormalize( ref dirY ) && SAFBIKComputeBasisFromXYLockY( out boneBasis, ref dirX, ref dirY ) ) {
							hipsToSpineDirX = boneBasis.column0;
							Quaternion worldRotation;
							SAFBIKMatMultGetRot( out worldRotation, ref boneBasis, ref _spineBones[i]._boneToWorldBasis );
							_spineBones[i].worldRotation = worldRotation;
							if( _spineBones[i].isWritebackWorldPosition ) {
								_spineBones[i].worldPosition = temp.spinePos[i];
							}
						}
					}

					if( _shoulderBones != null ) {
						for( int i = 0; i != 2; ++i ) {
							Vector3 xDir, yDir, zDir;
							xDir = temp.armPos[i] - temp.shoulderPos[i];
							if( _internalValues.shoulderDirYAsNeck != 0 ) {
								yDir = temp.neckPos - temp.shoulderPos[i];
							} else {
								yDir = temp.shoulderPos[i] - temp.spineUPos;
							}
							xDir = (i == 0) ? -xDir : xDir;
							zDir = Vector3.Cross( xDir, yDir );
							yDir = Vector3.Cross( zDir, xDir );

							if( SAFBIKVecNormalize3( ref xDir, ref yDir, ref zDir ) ) {
								boneBasis.SetColumn( ref xDir, ref yDir, ref zDir );
								Quaternion worldRotation;
								SAFBIKMatMultGetRot( out worldRotation, ref boneBasis, ref _shoulderBones[i]._boneToWorldBasis );
								_shoulderBones[i].worldRotation = worldRotation;
							}
						}
					}
				}
			}

			bool _IsEffectorEnabled()
			{
				if( (_hipsEffector.positionEnabled && _hipsEffector.pull > IKEpsilon) ||
					(_hipsEffector.rotationEnabled && _hipsEffector.rotationWeight > IKEpsilon) ||
					(_neckEffector.positionEnabled && _neckEffector.pull > IKEpsilon) ||
					(_eyesEffector.positionEnabled && (_eyesEffector.positionWeight > IKEpsilon && _eyesEffector.pull > IKEpsilon)) ||
					(_armEffectors[0].positionEnabled && _armEffectors[0].pull > IKEpsilon) ||
					(_armEffectors[1].positionEnabled && _armEffectors[1].pull > IKEpsilon) ) {
					return true;
				}

				if( (_elbowEffectors[0].positionEnabled && _elbowEffectors[0].pull > IKEpsilon) ||
					(_elbowEffectors[1].positionEnabled && _elbowEffectors[1].pull > IKEpsilon) ||
					(_kneeEffectors[0].positionEnabled && _kneeEffectors[0].pull > IKEpsilon) ||
					(_kneeEffectors[1].positionEnabled && _kneeEffectors[1].pull > IKEpsilon) ||
					(_wristEffectors[0].positionEnabled && _wristEffectors[0].pull > IKEpsilon) ||
					(_wristEffectors[1].positionEnabled && _wristEffectors[1].pull > IKEpsilon) ||
					(_footEffectors[0].positionEnabled && _footEffectors[0].pull > IKEpsilon) ||
					(_footEffectors[1].positionEnabled && _footEffectors[1].pull > IKEpsilon) ) {
					return true;
				}

				return false;
			}

			bool _PrepareSolverInternal()
			{
				if( _armBones == null || _legBones == null ) {
					_solverInternal = null;
					return false;
				}

				// Get pull values at SolverCaches.
				if( _neckEffector != null ) {
					_solverCaches.neckPull = _neckEffector.positionEnabled ? _neckEffector.pull : 0.0f;
				}
				if( _headEffector != null ) {
					_solverCaches.headPull = _headEffector.positionEnabled ? _headEffector.pull : 0.0f;
				}
				if( _eyesEffector != null ) {
					_solverCaches.eyesRate = _eyesEffector.positionEnabled ? (_eyesEffector.pull * _eyesEffector.positionWeight) : 0.0f;
				}
				for( int i = 0; i != 2; ++i ) {
					if( _armEffectors[i] != null ) {
						_solverCaches.armPull[i] = _armEffectors[i].positionEnabled ? _armEffectors[i].pull : 0.0f;
					}
					if( _elbowEffectors[i] != null ) {
						_solverCaches.elbowPull[i] = _elbowEffectors[i].positionEnabled ? _elbowEffectors[i].pull : 0.0f;
					}
					if( _wristEffectors[i] != null ) {
						_solverCaches.wristPull[i] = _wristEffectors[i].positionEnabled ? _wristEffectors[i].pull : 0.0f;
					}
					if( _kneeEffectors[i] != null ) {
						_solverCaches.kneePull[i] = _kneeEffectors[i].positionEnabled ? _kneeEffectors[i].pull : 0.0f;
					}
					if( _footEffectors[i] != null ) {
						_solverCaches.footPull[i] = _footEffectors[i].positionEnabled ? _footEffectors[i].pull : 0.0f;
					}
				}

				_solverCaches.neckHeadPull = _ConcatPull( _solverCaches.neckPull, _solverCaches.headPull );

				// Update pull values at SolverInternal.
				float upperPull = _solverCaches.neckHeadPull;
				float lowerPull = 0.0f;
				for( int i = 0; i != 2; ++i ) {
					float fullArmPull = _solverCaches.armPull[i];
					if( fullArmPull == 0.0f ) { // Optimized for function calls.
						fullArmPull = _solverCaches.elbowPull[i];
					} else {
						fullArmPull = _ConcatPull( fullArmPull, _solverCaches.elbowPull[i] );
					}
					if( fullArmPull == 0.0f ) { // Optimized for function calls.
						fullArmPull = _solverCaches.wristPull[i];
					} else {
						fullArmPull = _ConcatPull( fullArmPull, _solverCaches.wristPull[i] );
					}

					float limbArmPull = _solverCaches.kneePull[i];
					if( limbArmPull == 0.0f ) { // Optimized for function calls.
						limbArmPull = _solverCaches.wristPull[i];
					} else {
						limbArmPull = _ConcatPull( limbArmPull, _solverCaches.wristPull[i] );
					}

					float legPull = _solverCaches.kneePull[i];
					if( legPull == 0.0f ) { // Optimized for function calls.
						legPull = _solverCaches.footPull[i];
					} else {
						legPull = _ConcatPull( legPull, _solverCaches.footPull[i] );
					}

					_solverCaches.fullArmPull[i] = fullArmPull;
                    _solverCaches.limbLegPull[i] = legPull;

					_solverCaches.armToElbowPull[i] = _GetBalancedPullLockFrom( _solverCaches.armPull[i], _solverCaches.elbowPull[i] );
					_solverCaches.armToWristPull[i] = _GetBalancedPullLockFrom( _solverCaches.armPull[i], _solverCaches.wristPull[i] );
					_solverCaches.neckHeadToFullArmPull[i] = _GetBalancedPullLockTo( _solverCaches.neckHeadPull, fullArmPull );

					upperPull += fullArmPull;
					lowerPull += legPull;
                }

				_solverCaches.limbArmRate = _GetLerpRateFromPull2( _solverCaches.fullArmPull[0], _solverCaches.fullArmPull[1] );
				_solverCaches.limbLegRate = _GetLerpRateFromPull2( _solverCaches.limbLegPull[0], _solverCaches.limbLegPull[1] );
				_solverCaches.armToLegRate = _GetLerpRateFromPull2( upperPull, lowerPull );

				// _spineDirXRate, _spineEnabled
				if( _spineBones != null ) {
					int spineLength = _spineBones.Length;
					Assert( _spineDirXRate != null && _spineDirXRate.Length == spineLength );

					float spineDirXRate = Mathf.Clamp01( _settings.bodyIK.spineDirXLegToArmRate );
					float spineDirXToRate = Mathf.Max( _settings.bodyIK.spineDirXLegToArmToRate, spineDirXRate );

					for( int i = 0; i != spineLength; ++i ) {
						if( i == 0 ) {
							_spineDirXRate[i] = spineDirXRate;
						} else if( i + 1 == spineLength ) {
							_spineDirXRate[i] = spineDirXToRate;
						} else {
							_spineDirXRate[i] = spineDirXRate + (spineDirXToRate - spineDirXRate) * ((float)i / (float)(spineLength - 1));
                        }
                    }

					if( spineLength > 0 ) {
						_spineEnabled[0] = _settings.bodyIK.upperSolveSpineEnabled;
					}
					if( spineLength > 1 ) {
						_spineEnabled[1] = _settings.bodyIK.upperSolveSpine2Enabled;
					}
					if( spineLength > 2 ) {
						_spineEnabled[2] = _settings.bodyIK.upperSolveSpine3Enabled;
					}
					if( spineLength > 3 ) {
						_spineEnabled[3] = _settings.bodyIK.upperSolveSpine4Enabled;
					}
				}

				//------------------------------------------------------------------------------------------------------------------------

				// Allocate solverInternal.
				if( _solverInternal == null ) {
					_solverInternal								= new SolverInternal();
					_solverInternal.settings					= _settings;
					_solverInternal.internalValues				= _internalValues;
					_solverInternal._solverCaches				= _solverCaches;
					_solverInternal.arms._bendingPull			= _solverCaches.armToElbowPull;
					_solverInternal.arms._endPull				= _solverCaches.armToWristPull;
					_solverInternal.arms._beginToBendingLength	= _elbowEffectorMaxLength;
					_solverInternal.arms._beginToEndLength		= _wristEffectorMaxLength;
					_solverInternal.legs._bendingPull			= _solverCaches.kneePull;
					_solverInternal.legs._endPull				= _solverCaches.footPull;
					_solverInternal.legs._beginToBendingLength	= _kneeEffectorMaxLength;
					_solverInternal.legs._beginToEndLength		= _footEffectorMaxLength;

					_solverInternal._shouderLocalAxisYInv = _shouderLocalAxisYInv;
					_solverInternal._armEffectors = _armEffectors;
					_solverInternal._wristEffectors = _wristEffectors;
					_solverInternal._neckEffector = _neckEffector;
					_solverInternal._headEffector = _headEffector;
                    _solverInternal._spineBones = _spineBones;
					_solverInternal._shoulderBones = _shoulderBones;
					_solverInternal._armBones = _armBones;
					_solverInternal._limbIK = _limbIK;
					_solverInternal._centerLegBoneBasisInv = this._centerLegBoneBasisInv;
					PrepareArray( ref _solverInternal.shoulderPos, _shoulderBones );
					PrepareArray( ref _solverInternal.spinePos, _spineBones );
					_solverInternal.nearArmPos = (_shoulderBones != null) ? _solverInternal.shoulderPos : _solverInternal.armPos;
					if( _spineUBone != null ) {
						if( _shoulderBones != null || _armBones != null ) {
							var nearArmBones = (_shoulderBones != null) ? _shoulderBones : _armBones;
							Vector3 dirY = nearArmBones[1]._defaultPosition + nearArmBones[0]._defaultPosition;
							Vector3 dirX = nearArmBones[1]._defaultPosition - nearArmBones[0]._defaultPosition;
							dirY = dirY * 0.5f - _spineUBone._defaultPosition;
							Vector3 dirZ = Vector3.Cross( dirX, dirY );
							dirX = Vector3.Cross( dirY, dirZ );
							if( SAFBIKVecNormalize3( ref dirX, ref dirY, ref dirZ ) ) {
								Matrix3x3 localBasis = Matrix3x3.FromColumn( ref dirX, ref dirY, ref dirZ );
								_solverInternal._spineUBoneLocalAxisBasisInv = localBasis.transpose;
							}
						}
					}
				}

				_solverInternal.headEnabled = _headBone != null && _solverCaches.headPull > IKEpsilon;
				return true;
			}

			void _PresolveHips()
			{
				Assert( _internalValues != null && _internalValues.animatorEnabled );

				var temp = _solverInternal;
				Assert( temp != null );

				if( _hipsEffector == null ) {
					return;
				}

				bool rotationEnabled = _hipsEffector.rotationEnabled && _hipsEffector.rotationWeight > IKEpsilon;
				bool positionEnabled = _hipsEffector.positionEnabled && _hipsEffector.pull > IKEpsilon; // Note: Not positionWeight.

				if( !rotationEnabled && !positionEnabled ) {
					return;
				}

				Matrix3x3 centerLegBasis = temp.centerLegBasis;

				if( rotationEnabled ) {
					Quaternion centerLegRotationTo = _hipsEffector.worldRotation * Inverse( _hipsEffector._defaultRotation );
					Quaternion centerLegRotationFrom;
					SAFBIKMatGetRot( out centerLegRotationFrom, ref centerLegBasis );

					Quaternion centerLegRotation = centerLegRotationTo * Inverse( centerLegRotationFrom );

					if( _hipsEffector.rotationWeight < 1.0f - IKEpsilon ) {
						centerLegRotation = Quaternion.Lerp( Quaternion.identity, centerLegRotation, _hipsEffector.rotationWeight );
					}

					temp.LowerRotation( -1, ref centerLegRotation, true );
					centerLegBasis = temp.centerLegBasis;
				}

				if( positionEnabled ) {
					// Note: _hidden_worldPosition is contained positionWeight.
                    Vector3 hipsEffectorWorldPosition = _hipsEffector._hidden_worldPosition;
                    Vector3 centerLegPos;
					SAFBIKMatMultVecPreSubAdd( out centerLegPos, ref centerLegBasis, ref _defaultCenterLegPos, ref _hipsEffector._defaultPosition, ref hipsEffectorWorldPosition );

					Vector3 translate = centerLegPos - temp.centerLegPos;
					if( _hipsEffector.pull < 1.0f - IKEpsilon ) {
						translate *= _hipsEffector.pull;
                    }

					temp.Translate( ref translate );
				}
			}

			void _ResetTransforms()
			{
				Assert( _internalValues != null && _internalValues.resetTransforms );
				Matrix3x3 centerLegBasis = Matrix3x3.identity;
				Vector3 centerLegPos = Vector3.zero;
				_GetBaseCenterLegTransform( out centerLegPos, out centerLegBasis );
				_ResetCenterLegTransform( ref centerLegPos, ref centerLegBasis );
			}

			void _GetBaseCenterLegTransform( out Vector3 centerLegPos, out Matrix3x3 centerLegBasis )
			{
				// Use from resetTransforms & continuousSolverEnabled.
				Assert( _internalValues != null );

				centerLegBasis = _internalValues.baseHipsBasis;

				if( _hipsEffector != null ) {
					SAFBIKMatMultVecPreSubAdd(
						out centerLegPos,
						ref _internalValues.baseHipsBasis,
						ref _defaultCenterLegPos,
						ref _hipsEffector._defaultPosition,
						ref _internalValues.baseHipsPos );
				} else { // Failsafe.
					centerLegPos = new Vector3();
				}
			}

			void _ResetCenterLegTransform( ref Vector3 centerLegPos, ref Matrix3x3 centerLegBasis )
			{
				var temp = _solverInternal;
				Assert( temp != null );

				Vector3 defaultCenterLegPos = _defaultCenterLegPos;

				if( _legBones != null ) {
					for( int i = 0; i != 2; ++i ) {
						SAFBIKMatMultVecPreSubAdd( out temp.legPos[i], ref centerLegBasis, ref _legBones[i]._defaultPosition, ref defaultCenterLegPos, ref centerLegPos );
                    }
				}
				if( _spineBones != null ) {
					for( int i = 0; i != _spineBones.Length; ++i ) {
						SAFBIKMatMultVecPreSubAdd( out temp.spinePos[i], ref centerLegBasis, ref _spineBones[i]._defaultPosition, ref defaultCenterLegPos, ref centerLegPos );
					}
				}
				if( _shoulderBones != null ) {
					for( int i = 0; i != 2; ++i ) {
						SAFBIKMatMultVecPreSubAdd( out temp.shoulderPos[i], ref centerLegBasis, ref _shoulderBones[i]._defaultPosition, ref defaultCenterLegPos, ref centerLegPos );
					}
				}
				if( _armBones != null ) {
					for( int i = 0; i != 2; ++i ) {
						SAFBIKMatMultVecPreSubAdd( out temp.armPos[i], ref centerLegBasis, ref _armBones[i]._defaultPosition, ref defaultCenterLegPos, ref centerLegPos );
					}
				}
				if( _neckBone != null ) {
					SAFBIKMatMultVecPreSubAdd( out temp.neckPos, ref centerLegBasis, ref _neckBone._defaultPosition, ref defaultCenterLegPos, ref centerLegPos );
				}
				if( _headBone != null && temp.headEnabled ) {
					SAFBIKMatMultVecPreSubAdd( out temp.headPos, ref centerLegBasis, ref _headBone._defaultPosition, ref defaultCenterLegPos, ref centerLegPos );
				}

				temp.SetDirtyVariables();
				temp._SetCenterLegPos( ref centerLegPos ); // Optimized.
			}

			// for ShoulderResolve
			void _ResetShoulderTransform()
			{
				var temp = _solverInternal;
				Assert( temp != null );
				Assert( _limbIK != null );

				if( _armBones == null || _shoulderBones == null ) {
					return;
				}

				if( _spineUBone == null || !_spineUBone.transformIsAlive ||
					_neckBone == null || !_neckBone.transformIsAlive ) {
				}

				if( !_limbIK[(int)LimbIKLocation.LeftArm].IsSolverEnabled() &&
					!_limbIK[(int)LimbIKLocation.RightArm].IsSolverEnabled() ) {
					return;
				}

				Vector3 dirY = temp.neckPos - temp.spineUPos;
				Vector3 dirX = temp.nearArmPos[1] - temp.nearArmPos[0];

				Matrix3x3 boneBasis;
				if( SAFBIKVecNormalize( ref dirY ) && SAFBIKComputeBasisFromXYLockY( out boneBasis, ref dirX, ref dirY ) ) {
					Matrix3x3 tempBasis;
					SAFBIKMatMult( out tempBasis, ref boneBasis, ref _spineUBone._localAxisBasisInv );

					Vector3 tempPos = temp.spineUPos;
					for( int i = 0; i != 2; ++i ) {
						int limbIKIndex = (i == 0) ? (int)LimbIKLocation.LeftArm : (int)LimbIKLocation.RightArm;
						if( _limbIK[limbIKIndex].IsSolverEnabled() ) {
							SAFBIKMatMultVecPreSubAdd( out temp.armPos[i], ref tempBasis, ref _armBones[i]._defaultPosition, ref _spineUBone._defaultPosition, ref tempPos );
                        }
                    }
				}
			}

			//----------------------------------------------------------------------------------------------------------------------------------------

			class SolverInternal
			{
				public class Limb
				{
					public Vector3[] beginPos;

					public float[] _bendingPull;
					public float[] _endPull;
					public FastLength[] _beginToBendingLength;
					public FastLength[] _beginToEndLength;

					public bool[] targetBeginPosEnabled = new bool[2];
					public Vector3[] targetBeginPos = new Vector3[2];

					public bool[] bendingPosEnabled = new bool[2];
					public bool[] endPosEnabled = new bool[2];
					public Vector3[] bendingPos = new Vector3[2];
					public Vector3[] endPos = new Vector3[2];
					
					public void Prepare( Effector[] bendingEffectors, Effector[] endEffectors )
					{
						Assert( bendingEffectors != null );
						Assert( endEffectors != null );

						for( int i = 0; i < 2; ++i ) {
							targetBeginPos[i] = beginPos[i];
							targetBeginPosEnabled[i] = false;

							if( bendingEffectors[i] != null && bendingEffectors[i].bone != null && bendingEffectors[i].bone.transformIsAlive ) {
								bendingPosEnabled[i] = true;
								bendingPos[i] = bendingEffectors[i]._hidden_worldPosition;
							} else {
								bendingPosEnabled[i] = false;
								bendingPos[i] = new Vector3();
							}

							if( endEffectors[i] != null && endEffectors[i].bone != null && endEffectors[i].bone.transformIsAlive ) {
								endPosEnabled[i] = true;
								endPos[i] = endEffectors[i]._hidden_worldPosition;
							} else {
								endPosEnabled[i] = false;
								endPos[i] = new Vector3();
							}
						}
					}

					public void ClearEnvTargetBeginPos()
					{
						for( int i = 0; i < 2; ++i ) {
							_ClearEnvTargetBeginPos( i, ref beginPos[i] );
						}
					}

					public void ClearEnvTargetBeginPos( int i )
					{
						_ClearEnvTargetBeginPos( i, ref beginPos[i] );
					}

					public void _ClearEnvTargetBeginPos( int i, ref Vector3 beginPos )
					{
						targetBeginPos[i] = beginPos;
						targetBeginPosEnabled[i] = false;
					}

					public bool SolveTargetBeginPos()
					{
						bool r = false;
						for( int i = 0; i < 2; ++i ) {
							r |= SolveTargetBeginPos( i, ref this.beginPos[i] );
						}
						return r;
					}

					public bool SolveTargetBeginPos( int i )
					{
						return SolveTargetBeginPos( i, ref this.beginPos[i] );
					}

					public bool SolveTargetBeginPos( int i, ref Vector3 beginPos )
					{
						targetBeginPos[i] = beginPos;
						targetBeginPosEnabled[i] = false;

						if( endPosEnabled[i] && _endPull[i] > IKEpsilon ) {
							targetBeginPosEnabled[i] |= _SolveTargetBeginPos( ref targetBeginPos[i], ref endPos[i], ref _beginToEndLength[i], _endPull[i] );
						}
						if( bendingPosEnabled[i] && _bendingPull[i] > IKEpsilon ) {
							targetBeginPosEnabled[i] |= _SolveTargetBeginPos( ref targetBeginPos[i], ref bendingPos[i], ref _beginToBendingLength[i], _bendingPull[i] );
						}

						return targetBeginPosEnabled[i];
					}

					static bool _SolveTargetBeginPos( ref Vector3 targetBeginPos, ref Vector3 targetEndPos, ref FastLength targetBeginToEndLength, float endPull )
					{
						Vector3 beginToEnd = (targetEndPos - targetBeginPos);
						float beginToEndLengthSq = beginToEnd.sqrMagnitude;
						if( beginToEndLengthSq > targetBeginToEndLength.lengthSq + FLOAT_EPSILON ) {
							float beginToEndLength = SAFBIKSqrt( beginToEndLengthSq );
							if( beginToEndLength > IKEpsilon ) {
								float tempLength = beginToEndLength - targetBeginToEndLength.length;
								tempLength = tempLength / beginToEndLength;
								if( tempLength > IKEpsilon ) {
									if( endPull < 1.0f - IKEpsilon ) {
										tempLength *= endPull;
									}
									targetBeginPos += beginToEnd * tempLength;
									return true;
								}
							}
						}

						return false;
					}
				}

				public Settings settings;
				public InternalValues internalValues;
				public bool[] _shouderLocalAxisYInv;
				public Effector[] _armEffectors;
				public Effector _neckEffector;
				public Effector _headEffector;

				public Bone[] _spineBones;
				public Bone[] _shoulderBones;
				public Bone[] _armBones;

				public Limb arms = new Limb();
				public Limb legs = new Limb();

				public Vector3[] origToBeginDir = new Vector3[2];
				public Vector3[] origToTargetBeginDir = new Vector3[2];
				public float[] origTheta = new float[2];
				public Vector3[] origAxis = new Vector3[2];
				public Vector3[] origTranslate = new Vector3[2];
				public float[] origFeedbackRate = new float[2];

				public Vector3[] spinePos;

				public Vector3 neckPos;
				public Vector3 headPos;
				public bool headEnabled; // _headEffector != null && _headEffector.positionEnabled && _headEffector.pull > IKEpsilon

				public Vector3[] nearArmPos;
				public Vector3[] shoulderPos;
				public Vector3[] armPos = new Vector3[2]; // = arms.beginPos
				public Vector3[] legPos = new Vector3[2]; // = legs.beginPos

				public Matrix3x3 _centerLegBoneBasisInv = Matrix3x3.identity; // Require setting on initialize.
				public Matrix3x3 _spineUBoneLocalAxisBasisInv = Matrix3x3.identity; // Require setting on initialize.

				public Vector3 _centerArmPos = Vector3.zero;
				public Vector3 _centerLegPos = Vector3.zero;
				public Matrix3x3 _centerLegBasis = Matrix3x3.identity;
				public Matrix3x3 _spineUBasis = Matrix3x3.identity;

				bool _isDirtyCenterArmPos = true;
				bool _isDirtyCenterLegPos = true;
				bool _isDirtyCenterLegBasis = true;
				bool _isDirtySpineUBasis = true;

				public SolverInternal()
				{
					this.arms.beginPos = this.armPos;
					this.legs.beginPos = this.legPos;
				}

				public Vector3 centerArmPos
				{
					get
					{
						if( _isDirtyCenterArmPos ) {
							_UpdateCenterArmPos();
						}

						return _centerArmPos;
					}
				}

				public Vector3 centerLegPos
				{
					get
					{
						if( _isDirtyCenterLegPos ) {
							_UpdateCenterLegPos();
						}

						return _centerLegPos;
					}
				}

				public void _UpdateCenterArmPos()
				{
					if( _isDirtyCenterArmPos ) {
						_isDirtyCenterArmPos = false;
						var nearArmPos = this.shoulderPos;
						if( nearArmPos == null ) {
							nearArmPos = this.armPos;
						}
						if( nearArmPos != null ) {
							_centerArmPos = (nearArmPos[0] + nearArmPos[1]) * 0.5f;
						}
					}
				}

				public void _UpdateCenterLegPos()
				{
					if( _isDirtyCenterLegPos ) {
						_isDirtyCenterLegPos = false;
						var legPos = this.legPos;
						if( legPos != null ) {
							_centerLegPos = (legPos[0] + legPos[1]) * 0.5f;
						}
					}
				}

				public void _SetCenterArmPos( ref Vector3 centerArmPos )
				{
					_isDirtyCenterArmPos = false;
					_centerArmPos = centerArmPos;
				}

				public void _SetCenterLegPos( ref Vector3 centerLegPos )
				{
					_isDirtyCenterLegPos = false;
					_centerLegPos = centerLegPos;
				}

				public Matrix3x3 centerLegBasis
				{
					get
					{
						if( _isDirtyCenterLegBasis ) {
							_UpdateCenterLegBasis();
						}

						return _centerLegBasis;
					}
				}

				public Matrix3x3 spineUBasis
				{
					get
					{
						if( _isDirtySpineUBasis ) {
							_UpdateSpineUBasis();
						}

						return _spineUBasis;
					}
				}

				public void _UpdateCenterLegBasis()
				{
					if( _isDirtyCenterLegBasis ) {
						_isDirtyCenterLegBasis = false;
						var legPos = this.legPos;
						_centerLegBasis = Matrix3x3.identity;
						if( this.spinePos != null && this.spinePos.Length > 0 && legPos != null ) {
							Vector3 dirX = legPos[1] - legPos[0];
							Vector3 dirY = this.spinePos[0] - this.centerLegPos;
							Vector3 dirZ = Vector3.Cross( dirX, dirY );
							dirX = Vector3.Cross( dirY, dirZ );
							if( SAFBIKVecNormalize3( ref dirX, ref dirY, ref dirZ ) ) {
								_centerLegBasis.SetColumn( ref dirX, ref dirY, ref dirZ );
								SAFBIKMatMultRet0( ref _centerLegBasis, ref _centerLegBoneBasisInv );
							}
						}
					}
				}

				public void _UpdateSpineUBasis()
				{
					if( _isDirtySpineUBasis ) {
						_isDirtySpineUBasis = false;
						_spineUBasis = Matrix3x3.identity;
						Vector3 dirY = (this.shoulderPos != null) ? (this.shoulderPos[1] + this.shoulderPos[0]) : (this.armPos[1] + this.armPos[0]);
						dirY = dirY * 0.5f - this.spineUPos;
						Vector3 dirX = (this.shoulderPos != null) ? (this.shoulderPos[1] - this.shoulderPos[0]) : (this.armPos[1] - this.armPos[0]);
						Vector3 dirZ = Vector3.Cross( dirX, dirY );
						dirX = Vector3.Cross( dirY, dirZ );
						if( SAFBIKVecNormalize3( ref dirX, ref dirY, ref dirZ ) ) {
							_spineUBasis.SetColumn( ref dirX, ref dirY, ref dirZ );
							SAFBIKMatMultRet0( ref _spineUBasis, ref _spineUBoneLocalAxisBasisInv );
                        }
					}
				}

				public void SetDirtyVariables()
				{
					_isDirtyCenterArmPos = true;
					_isDirtyCenterLegPos = true;
					_isDirtyCenterLegBasis = true;
					_isDirtySpineUBasis = true;
				}

				public Vector3 spineUPos
				{
					get
					{
						if( this.spinePos != null && this.spinePos.Length != 0 ) {
							return this.spinePos[this.spinePos.Length - 1];
						}

						return Vector3.zero;
					}
				}

				public class BackupData
				{
					public Vector3 centerArmPos;
					public Vector3 centerLegPos;
					public Matrix3x3 centerLegBasis;
					public Matrix3x3 spineUBasis;

					public Vector3[] spinePos;
 					public Vector3 neckPos;
					public Vector3 headPos;
					public Vector3[] shoulderPos;

					public Vector3[] armPos = new Vector3[2];
					public Vector3[] legPos = new Vector3[2];
				}

				BackupData _backupData = new BackupData();

				public void Backup()
				{
					_backupData.centerArmPos = this.centerArmPos;
					_backupData.centerLegPos = this.centerLegPos;
					_backupData.centerLegBasis = this.centerLegBasis;
					_backupData.spineUBasis = this.spineUBasis;
					CloneArray( ref _backupData.spinePos, this.spinePos );
					_backupData.neckPos = this.neckPos;
					_backupData.headPos = this.headPos;
					CloneArray( ref _backupData.shoulderPos, this.shoulderPos );
					CloneArray( ref _backupData.armPos, this.arms.beginPos );
					CloneArray( ref _backupData.legPos, this.legs.beginPos );
				}

				public void Restore()
				{
					_isDirtyCenterArmPos = false;
					_isDirtyCenterLegPos = false;
					_isDirtyCenterLegBasis = false;
					_isDirtySpineUBasis = false;
					_centerArmPos = _backupData.centerArmPos;
					_centerLegPos = _backupData.centerLegPos;
					_centerLegBasis = _backupData.centerLegBasis;
					_spineUBasis = _backupData.spineUBasis;
					CloneArray( ref this.spinePos, _backupData.spinePos );
					this.neckPos = _backupData.neckPos;
					this.headPos = _backupData.headPos;
					CloneArray( ref this.shoulderPos, _backupData.shoulderPos );
					CloneArray( ref this.arms.beginPos, _backupData.armPos );
					CloneArray( ref this.legs.beginPos, _backupData.legPos );
				}

				struct _UpperSolverPreArmsTemp
				{
					public Vector3[] shoulderPos;
					public Vector3[] armPos;
					public Vector3[] nearArmPos; // shoulderPos / armPos
					public Vector3 neckPos;
					public bool shoulderEnabled;

					public static _UpperSolverPreArmsTemp Alloc()
					{
						_UpperSolverPreArmsTemp r = new _UpperSolverPreArmsTemp();
						r.shoulderPos = new Vector3[2];
						r.armPos = new Vector3[2];
						r.nearArmPos = null; // shoulderPos / armPos
						r.shoulderEnabled = false;
						return r;
					}
				}

				struct _UpperSolverArmsTemp
				{
					public Vector3[] shoulderPos;
					public Vector3[] armPos;
					public Vector3[] nearArmPos; // shoulderPos / armPos
					public bool shoulderEnabled;

					public Vector3 centerArmPos;
					public Vector3 centerArmDir;

					public static _UpperSolverArmsTemp Alloc()
					{
						_UpperSolverArmsTemp r = new _UpperSolverArmsTemp();
						r.shoulderPos = new Vector3[2];
						r.armPos = new Vector3[2];
						r.nearArmPos = null; // shoulderPos / armPos
						r.shoulderEnabled = false;
						r.centerArmPos = Vector3.zero;
						r.centerArmDir = Vector3.zero;
						return r;
                    }
				}

				struct _UpperSolverTemp
				{
					public Vector3[] targetArmPos;
					public Vector3 targetNeckPos;
					public Vector3 targetHeadPos;

					public float[] wristToArmRate; // wristPull or balanced to armEffector.pull / wristEffector.pull
					public float[] neckToWristRate; // neckPull or balanced to neckPull / neckEffector.pull

					public static _UpperSolverTemp Alloc()
					{
						_UpperSolverTemp r = new _UpperSolverTemp();
						r.targetArmPos = new Vector3[2];
						r.targetNeckPos = new Vector3();
						r.targetHeadPos = new Vector3();
						r.wristToArmRate = new float[2];
						r.neckToWristRate = new float[2];
						return r;
					}
				}

				public Effector[] _wristEffectors;
				public SolverCaches _solverCaches;
				_UpperSolverPreArmsTemp _upperSolverPreArmsTemp = _UpperSolverPreArmsTemp.Alloc();
                _UpperSolverArmsTemp[] _upperSolverArmsTemps = new _UpperSolverArmsTemp[2] { _UpperSolverArmsTemp.Alloc(), _UpperSolverArmsTemp.Alloc() };
				_UpperSolverTemp _upperSolverTemp = _UpperSolverTemp.Alloc();

				void _SolveArmsToArms( ref _UpperSolverArmsTemp armsTemp, float armPull, int idx0 )
				{
					Vector3 targetArmPos = _upperSolverTemp.targetArmPos[idx0];
					armsTemp.armPos[idx0] = Vector3.Lerp( armsTemp.armPos[idx0], targetArmPos, armPull );
				}

				void _SolveArmsToNeck( ref _UpperSolverArmsTemp armsTemp, float neckToFullArmPull, int idx0 )
				{
					Vector3 nearArmPos0 = armsTemp.nearArmPos[idx0];
					_KeepLength( ref nearArmPos0, ref _upperSolverTemp.targetNeckPos, _solverCaches.nearArmToNeckLength[idx0] );
					armsTemp.nearArmPos[idx0] = Vector3.Lerp( nearArmPos0, armsTemp.nearArmPos[idx0], neckToFullArmPull );
				}

				void _SolveArms( ref _UpperSolverArmsTemp armsTemp, int idx0 )
				{
					int idx1 = 1 - idx0;

					float neckHeadPull = _solverCaches.neckHeadPull;
					float[] armPull = _solverCaches.armPull;
					float[] elbowPull = _solverCaches.elbowPull;
					float[] wristPull = _solverCaches.wristPull;
					float[] neckHeadToFullArmPull = _solverCaches.neckHeadToFullArmPull;

					if( wristPull[idx0] > IKEpsilon || elbowPull[idx0] > IKEpsilon || armPull[idx0] > IKEpsilon || neckHeadPull > IKEpsilon ) {
						if( armPull[idx0] > IKEpsilon ) {
							_SolveArmsToArms( ref armsTemp, armPull[idx0], idx0 );
						}
						if( (wristPull[idx0] > IKEpsilon || elbowPull[idx0] > IKEpsilon) &&
							arms.SolveTargetBeginPos( idx0, ref armsTemp.armPos[idx0] ) ) {
							armsTemp.armPos[idx0] = arms.targetBeginPos[idx0]; // Update armPos
							if( armsTemp.shoulderEnabled ) {
								_KeepLength( ref armsTemp.shoulderPos[idx0], ref armsTemp.armPos[idx0], _solverCaches.shoulderToArmLength[idx0] );
								if( neckHeadPull > IKEpsilon ) {
									_SolveArmsToNeck( ref armsTemp, neckHeadToFullArmPull[idx0], idx0 ); // Contain wristPull/neckPull.
									_KeepLength( ref armsTemp.armPos[idx0], ref armsTemp.shoulderPos[idx0], _solverCaches.shoulderToArmLength[idx0] );
								}
								_KeepLength( ref armsTemp.shoulderPos[idx1], ref armsTemp.shoulderPos[idx0], _solverCaches.nearArmToNearArmLen );
								_KeepLength( ref armsTemp.armPos[idx1], ref armsTemp.shoulderPos[idx1], _solverCaches.shoulderToArmLength[idx1] );
							} else {
								if( neckHeadPull > IKEpsilon ) {
									_SolveArmsToNeck( ref armsTemp, neckHeadToFullArmPull[idx0], idx0 );
								}
								_KeepLength( ref armsTemp.armPos[idx1], ref armsTemp.armPos[idx0], _solverCaches.armToArmLen );
							}
						} else if( armPull[idx0] > IKEpsilon || neckHeadPull > IKEpsilon ) {
							if( armPull[idx0] > IKEpsilon ) {
								if( armsTemp.shoulderEnabled ) {
									_KeepLength( ref armsTemp.shoulderPos[idx0], ref armsTemp.armPos[idx0], _solverCaches.shoulderToArmLength[idx0] );
								}
							}
							if( neckHeadPull > IKEpsilon ) {
								_SolveArmsToNeck( ref armsTemp, neckHeadToFullArmPull[idx0], idx0 ); // Contain wristPull/neckPull.
								if( armsTemp.shoulderEnabled ) {
									_KeepLength( ref armsTemp.armPos[idx0], ref armsTemp.shoulderPos[idx0], _solverCaches.shoulderToArmLength[idx0] );
								}
							}
							if( armsTemp.shoulderEnabled ) {
								_KeepLength( ref armsTemp.shoulderPos[idx1], ref armsTemp.shoulderPos[idx0], _solverCaches.nearArmToNearArmLen );
								_KeepLength( ref armsTemp.armPos[idx1], ref armsTemp.shoulderPos[idx1], _solverCaches.shoulderToArmLength[idx1] );
							} else {
								_KeepLength( ref armsTemp.armPos[idx1], ref armsTemp.armPos[idx0], _solverCaches.armToArmLen );
							}
						}
					}
				}

				public bool targetCenterArmEnabled = false;
				public Vector3 targetCenterArmPos = Vector3.zero;
				public Vector3 targetCenterArmDir = Vector3.zero;

				public Vector3 currentCenterArmPos {
					get {
						if( this.shoulderPos != null ) {
							return (this.shoulderPos[0] + this.shoulderPos[1]) * 0.5f;
                        } else if( this.armPos != null ){
							return (this.armPos[0] + this.armPos[1]) * 0.5f;
						}
						return Vector3.zero;
					}
				}

				public Vector3 currentCenterArmDir {
					get {
						if( this.shoulderPos != null ) {
							Vector3 dir = (this.shoulderPos[1] - this.shoulderPos[0]);
							if( SAFBIKVecNormalize( ref dir ) ) {
								return dir;
							}
						} else if( this.armPos != null ) {
							Vector3 dir = (this.armPos[1] - this.armPos[0]);
							if( SAFBIKVecNormalize( ref dir ) ) {
								return dir;
							}
						}
						return Vector3.zero;
					}
				}

				public bool UpperSolve()
				{
					targetCenterArmEnabled = false;

					float neckPull = _solverCaches.neckPull;
					float headPull = _solverCaches.headPull;
					float[] armPull = _solverCaches.armPull;
					float[] elbowPull = _solverCaches.elbowPull;
					float[] wristPull = _solverCaches.wristPull;

					if( wristPull[0] <= IKEpsilon && wristPull[1] <= IKEpsilon &&
						elbowPull[0] <= IKEpsilon && elbowPull[1] <= IKEpsilon &&
						armPull[0] <= IKEpsilon && armPull[1] <= IKEpsilon &&
						neckPull <= IKEpsilon && headPull <= IKEpsilon ) {
						targetCenterArmPos = this.currentCenterArmPos;
						targetCenterArmDir = this.currentCenterArmDir;
						return false;
					}

					// Prepare _upperSolverTemp
					_upperSolverTemp.targetNeckPos = (_neckEffector != null) ? _neckEffector._hidden_worldPosition : neckPos;
					_upperSolverTemp.targetHeadPos = (_headEffector != null) ? _headEffector._hidden_worldPosition : headPos;
					_upperSolverTemp.targetArmPos[0] = (_armEffectors != null) ? _armEffectors[0]._hidden_worldPosition : armPos[0];
					_upperSolverTemp.targetArmPos[1] = (_armEffectors != null) ? _armEffectors[1]._hidden_worldPosition : armPos[1];

					// Prepare _upperSolverPreArmsTemp
					_upperSolverPreArmsTemp.neckPos = neckPos;
                    _upperSolverPreArmsTemp.armPos[0] = armPos[0];
					_upperSolverPreArmsTemp.armPos[1] = armPos[1];
					_upperSolverPreArmsTemp.shoulderEnabled = (shoulderPos != null);
                    if( _upperSolverPreArmsTemp.shoulderEnabled ) {
						_upperSolverPreArmsTemp.shoulderPos[0] = shoulderPos[0];
						_upperSolverPreArmsTemp.shoulderPos[1] = shoulderPos[1];
						_upperSolverPreArmsTemp.nearArmPos = _upperSolverPreArmsTemp.shoulderPos;
                    } else {
						_upperSolverPreArmsTemp.nearArmPos = _upperSolverPreArmsTemp.armPos;
					}

					// Moving fix.
					float bodyMovingfixRate = settings.bodyIK.upperBodyMovingfixRate;
					float headMovingfixRate = settings.bodyIK.upperHeadMovingfixRate;
					if( bodyMovingfixRate > IKEpsilon || headMovingfixRate > IKEpsilon ) {
						Vector3 headMove = Vector3.zero;
						Vector3 bodyMove = Vector3.zero;
						if( headPull > IKEpsilon ) {
							headMove = _upperSolverTemp.targetHeadPos - headPos;
							if( headMovingfixRate < 1.0f - IKEpsilon ) {
								headMove *= headPull * headMovingfixRate;
							} else {
								headMove *= headPull;
							}
						}

						float bodyPull = 0.0f;
						float bodyPullInv = 0.0f;
						if( neckPull > IKEpsilon || armPull[0] > IKEpsilon || armPull[1] > IKEpsilon ) {
							bodyPull = (neckPull + armPull[0] + armPull[1]);
							bodyPullInv = 1.0f / bodyPull;
							if( neckPull > IKEpsilon ) {
								bodyMove = (_upperSolverTemp.targetNeckPos - neckPos) * (neckPull * neckPull);
							}
							if( armPull[0] > IKEpsilon ) {
								bodyMove += (_upperSolverTemp.targetArmPos[0] - armPos[0]) * (armPull[0] * armPull[0]);
							}
							if( armPull[1] > IKEpsilon ) {
								bodyMove += (_upperSolverTemp.targetArmPos[1] - armPos[1]) * (armPull[1] * armPull[1]);
							}
                            if( bodyMovingfixRate < 1.0f - IKEpsilon ) {
								bodyMove *= bodyPullInv * bodyMovingfixRate;
							} else {
								bodyMove *= bodyPullInv;
							}
						}

						Vector3 totalMove = new Vector3();
						if( headPull > IKEpsilon && bodyPull > IKEpsilon ) {
							totalMove = (headMove * headPull) + (bodyMove * bodyPull);
							totalMove *= 1.0f / (headPull + bodyPull);
                        } else if( headPull > IKEpsilon ) {
							totalMove = headMove;
                        } else {
							totalMove = bodyMove;
						}

                        _upperSolverPreArmsTemp.neckPos += totalMove;
						_upperSolverPreArmsTemp.armPos[0] += totalMove;
						_upperSolverPreArmsTemp.armPos[1] += totalMove;
						if( _upperSolverPreArmsTemp.shoulderEnabled ) {
							_upperSolverPreArmsTemp.shoulderPos[0] += totalMove;
							_upperSolverPreArmsTemp.shoulderPos[1] += totalMove;
						}
					}

					// Preprocess neckSolver.
					if( headMovingfixRate < 1.0f - IKEpsilon || bodyMovingfixRate < 1.0f - IKEpsilon ) {
						if( headPull > IKEpsilon || neckPull > IKEpsilon ) {
							if( headMovingfixRate < 1.0f - IKEpsilon && headPull > IKEpsilon ) {
								Vector3 tempNeckPos = _upperSolverPreArmsTemp.neckPos;
								if( _KeepMaxLength( ref tempNeckPos, ref _upperSolverTemp.targetHeadPos, _solverCaches.neckToHeadLength ) ) { // Not KeepLength
									_upperSolverPreArmsTemp.neckPos = Vector3.Lerp( _upperSolverPreArmsTemp.neckPos, tempNeckPos, headPull );
								}
                            }
							for( int i = 0; i != 2; ++i ) {
								if( bodyMovingfixRate < 1.0f - IKEpsilon && neckPull > IKEpsilon ) {
									Vector3 tempNearArmPos = _upperSolverPreArmsTemp.nearArmPos[i];
									_KeepLength( ref tempNearArmPos, ref _upperSolverTemp.targetNeckPos, _solverCaches.nearArmToNeckLength[i] );
									_upperSolverPreArmsTemp.nearArmPos[i] = Vector3.Lerp( _upperSolverPreArmsTemp.nearArmPos[i], tempNearArmPos, neckPull ); // Not use neckToFullArmPull in Presolve.
								} else {
									_KeepLength(
										ref _upperSolverPreArmsTemp.nearArmPos[i],
										ref _upperSolverPreArmsTemp.neckPos,
										_solverCaches.nearArmToNeckLength[i] );
								}
								if( _upperSolverPreArmsTemp.shoulderEnabled ) {
									_KeepLength(
										ref _upperSolverPreArmsTemp.armPos[i],
										ref _upperSolverPreArmsTemp.shoulderPos[i],
										_solverCaches.shoulderToArmLength[i] );
								}
								//internalValues.AddDebugPoint( nearArmPos, Color.black, 0.1f );
							}
						}
					}

					// Update targetNeckPos using presolved. (Contain neckPull / headPull)
					_upperSolverTemp.targetNeckPos = _upperSolverPreArmsTemp.neckPos;

					// Prepare _upperSolverArmsTemps
					for( int i = 0; i != 2; ++i ) {
						_upperSolverArmsTemps[i].armPos[0] = _upperSolverPreArmsTemp.armPos[0];
						_upperSolverArmsTemps[i].armPos[1] = _upperSolverPreArmsTemp.armPos[1];
						_upperSolverArmsTemps[i].shoulderEnabled = _upperSolverPreArmsTemp.shoulderEnabled;
						if( _upperSolverArmsTemps[i].shoulderEnabled ) {
							_upperSolverArmsTemps[i].shoulderPos[0] = _upperSolverPreArmsTemp.shoulderPos[0];
							_upperSolverArmsTemps[i].shoulderPos[1] = _upperSolverPreArmsTemp.shoulderPos[1];
							_upperSolverArmsTemps[i].nearArmPos = _upperSolverArmsTemps[i].shoulderPos;
                        } else {
							_upperSolverArmsTemps[i].nearArmPos = _upperSolverArmsTemps[i].armPos;
						}
					}

					// Check enabled by side.
					bool enabled0 = (wristPull[0] > IKEpsilon || elbowPull[0] > IKEpsilon || armPull[0] > IKEpsilon);
					bool enabled1 = (wristPull[1] > IKEpsilon || elbowPull[1] > IKEpsilon || armPull[1] > IKEpsilon);

					float neckHeadPull = _solverCaches.neckHeadPull;
					if( (enabled0 && enabled1) || neckHeadPull > IKEpsilon ) {
						for( int i = 0; i != 2; ++i ) {
							int idx0 = i;
							int idx1 = 1 - i;

							_SolveArms( ref _upperSolverArmsTemps[idx0], idx0 );
							_SolveArms( ref _upperSolverArmsTemps[idx0], idx1 );
							_SolveArms( ref _upperSolverArmsTemps[idx0], idx0 );

							if( _upperSolverArmsTemps[idx0].shoulderEnabled ) {
								_upperSolverArmsTemps[idx0].centerArmPos = (_upperSolverArmsTemps[idx0].shoulderPos[0] + _upperSolverArmsTemps[idx0].shoulderPos[1]) * 0.5f;
								_upperSolverArmsTemps[idx0].centerArmDir = _upperSolverArmsTemps[idx0].shoulderPos[1] - _upperSolverArmsTemps[idx0].shoulderPos[0];
							} else {
								_upperSolverArmsTemps[idx0].centerArmPos = (_upperSolverArmsTemps[idx0].armPos[0] + _upperSolverArmsTemps[idx0].armPos[1]) * 0.5f;
								_upperSolverArmsTemps[idx0].centerArmDir = _upperSolverArmsTemps[idx0].armPos[1] - _upperSolverArmsTemps[idx0].armPos[0];
							}
						}

						if( !SAFBIKVecNormalize2( ref _upperSolverArmsTemps[0].centerArmDir, ref _upperSolverArmsTemps[1].centerArmDir ) ) {
							return false;
						}

						float limbArmRate = _solverCaches.limbArmRate;

						targetCenterArmEnabled = true;
						targetCenterArmPos = Vector3.Lerp( _upperSolverArmsTemps[0].centerArmPos, _upperSolverArmsTemps[1].centerArmPos, limbArmRate );
						targetCenterArmDir = _LerpDir( ref _upperSolverArmsTemps[0].centerArmDir, ref _upperSolverArmsTemps[1].centerArmDir, limbArmRate );
					} else {
						int idx0 = enabled0 ? 0 : 1;
						_SolveArms( ref _upperSolverArmsTemps[idx0], idx0 );

						if( _upperSolverArmsTemps[idx0].shoulderEnabled ) {
							_upperSolverArmsTemps[idx0].centerArmPos = (_upperSolverArmsTemps[idx0].shoulderPos[0] + _upperSolverArmsTemps[idx0].shoulderPos[1]) * 0.5f;
							_upperSolverArmsTemps[idx0].centerArmDir = _upperSolverArmsTemps[idx0].shoulderPos[1] - _upperSolverArmsTemps[idx0].shoulderPos[0];
							//internalValues.AddDebugPoint( _upperSolverArmsTemps[idx0].shoulderPos[0], Color.black, 0.1f );
							//internalValues.AddDebugPoint( _upperSolverArmsTemps[idx0].shoulderPos[1], Color.black, 0.1f );
						} else {
							_upperSolverArmsTemps[idx0].centerArmPos = (_upperSolverArmsTemps[idx0].armPos[0] + _upperSolverArmsTemps[idx0].armPos[1]) * 0.5f;
							_upperSolverArmsTemps[idx0].centerArmDir = _upperSolverArmsTemps[idx0].armPos[1] - _upperSolverArmsTemps[idx0].armPos[0];
							//internalValues.AddDebugPoint( _upperSolverArmsTemps[idx0].armPos[0], Color.black, 0.1f );
							//internalValues.AddDebugPoint( _upperSolverArmsTemps[idx0].armPos[1], Color.black, 0.1f );
						}

						if( !SAFBIKVecNormalize( ref _upperSolverArmsTemps[idx0].centerArmDir ) ) {
							return false;
						}

						targetCenterArmEnabled = true;
						targetCenterArmPos = _upperSolverArmsTemps[idx0].centerArmPos;
						targetCenterArmDir = _upperSolverArmsTemps[idx0].centerArmDir;
					}

					return true;
				}

				Vector3[] _tempArmPos = new Vector3[2];
				Vector3[] _tempArmToElbowDir = new Vector3[2];
				Vector3[] _tempElbowToWristDir = new Vector3[2];
				bool[] _tempElbowPosEnabled = new bool[2];
				public LimbIK[] _limbIK;

				Matrix3x3[] _tempParentBasis = new Matrix3x3[2] { Matrix3x3.identity, Matrix3x3.identity };
				Vector3[] _tempArmToElbowDefaultDir = new Vector3[2];

				public bool ShoulderResolve()
				{
					Bone[] armBones = _solverCaches.armBones;
					Bone[] shoulderBones = _solverCaches.shoulderBones;
					float[] shoulderToArmLength = _solverCaches.shoulderToArmLength;
					if( armBones == null || shoulderBones == null ) {
						return false;
					}

					Assert( shoulderToArmLength != null );
					Assert( _limbIK != null );

					if( !_limbIK[(int)LimbIKLocation.LeftArm].IsSolverEnabled() &&
						!_limbIK[(int)LimbIKLocation.RightArm].IsSolverEnabled() ) {
						return false; // Not required.
					}

					for( int i = 0; i != 2; ++i ) {
						int limbIKIndex = (i == 0) ? (int)LimbIKLocation.LeftArm : (int)LimbIKLocation.RightArm;

						if( _limbIK[limbIKIndex].IsSolverEnabled() ) {
							Vector3 xDir, yDir, zDir;
							xDir = this.armPos[i] - this.shoulderPos[i];
							if( internalValues.shoulderDirYAsNeck != 0 ) {
								yDir = this.neckPos - this.shoulderPos[i];
							} else {
								yDir = this.shoulderPos[i] - this.spineUPos;
							}
							xDir = (i == 0) ? -xDir : xDir;
							zDir = Vector3.Cross( xDir, yDir );
							yDir = Vector3.Cross( zDir, xDir );
							if( SAFBIKVecNormalize3( ref xDir, ref yDir, ref zDir ) ) {
								Matrix3x3 boneBasis = Matrix3x3.FromColumn( ref xDir, ref yDir, ref zDir );
								SAFBIKMatMult( out _tempParentBasis[i], ref boneBasis, ref _shoulderBones[i]._boneToBaseBasis );
							}

							_tempArmPos[i] = this.armPos[i];
							_tempElbowPosEnabled[i] = _limbIK[limbIKIndex].Presolve(
								ref _tempParentBasis[i],
								ref _tempArmPos[i],
								out _tempArmToElbowDir[i],
								out _tempElbowToWristDir[i] );

							if( _tempElbowPosEnabled[i] ) {
								SAFBIKMatMultCol0( out _tempArmToElbowDefaultDir[i], ref _tempParentBasis[i], ref _armBones[i]._baseToBoneBasis );
								if( i == 0 ) {
									_tempArmToElbowDefaultDir[i] = -_tempArmToElbowDefaultDir[i];
								}
							}
						}
					}

					if( !_tempElbowPosEnabled[0] && !_tempElbowPosEnabled[1] ) {
						return false; // Not required.
					}

					float feedbackRate = settings.bodyIK.shoulderSolveBendingRate;

					bool updateAnything = false;
					for( int i = 0; i != 2; ++i ) {
						if( _tempElbowPosEnabled[i] ) {
							float theta;
							Vector3 axis;
							_ComputeThetaAxis( ref _tempArmToElbowDefaultDir[i], ref _tempArmToElbowDir[i], out theta, out axis );
							if( theta >= -FLOAT_EPSILON && theta <= FLOAT_EPSILON ) {
								// Nothing.
							} else {
								updateAnything = true;
								theta = SAFBIKCos( SAFBIKAcos( theta ) * feedbackRate );
								Matrix3x3 m = new Matrix3x3();
								SAFBIKMatSetAxisAngle( out m, ref axis, theta );

								Vector3 tempShoulderPos = this.shoulderPos[i];
								Vector3 tempDir = _tempArmPos[i] - tempShoulderPos;
								SAFBIKVecNormalize( ref tempDir );
								Vector3 resultDir;
								SAFBIKMatMultVec( out resultDir, ref m, ref tempDir );
								Vector3 destArmPos = tempShoulderPos + resultDir * shoulderToArmLength[i];

								SolveShoulderToArmInternal( i, ref destArmPos );
							}
						}
					}

					return updateAnything;
				}

				public bool PrepareLowerRotation( int origIndex )
				{
					bool r = false;
					for( int i = 0; i < 2; ++i ) {
						this.legs.SolveTargetBeginPos( i );
						r |= _PrepareLimbRotation( this.legs, i, origIndex, ref this.legs.beginPos[i] );
					}
					return r;
				}

				public bool _PrepareLimbRotation( Limb limb, int i, int origIndex, ref Vector3 beginPos )
				{
					Assert( i < 2 );
					this.origTheta[i] = 0.0f;
					this.origAxis[i] = new Vector3( 0.0f, 0.0f, 1.0f );

					if( !limb.targetBeginPosEnabled[i] ) {
						return false;
					}

					// Memo: limb index = orig index.

					var targetBeginPos = limb.targetBeginPos;

					Vector3 origPos = (origIndex == -1) ? this.centerLegPos : this.spinePos[origIndex];

					return _ComputeThetaAxis(
						ref origPos,
						ref beginPos,
						ref targetBeginPos[i],
						out this.origTheta[i],
						out this.origAxis[i] );
				}

				public void SetSolveFeedbackRate( float feedbackRate )
				{
					for( int i = 0; i < this.origFeedbackRate.Length; ++i ) {
						this.origFeedbackRate[i] = feedbackRate;
					}
				}

				public void SetSolveFeedbackRate( int i, float feedbackRate )
				{
					this.origFeedbackRate[i] = feedbackRate;
				}
				
				public bool SolveLowerRotation( int origIndex, out Quaternion origRotation )
				{
					return _SolveLimbRotation( this.legs, origIndex, out origRotation );
				}

				bool _SolveLimbRotation( Limb limb, int origIndex, out Quaternion origRotation )
				{
					origRotation = Quaternion.identity;

					int pullIndex = -1;
					int pullLength = 0;
					for( int i = 0; i < 2; ++i ) {
						if( limb.targetBeginPosEnabled[i] ) {
							pullIndex = i;
							++pullLength;
						}
					}

					if( pullLength == 0 ) {
						return false; // Failsafe.
					}

					float lerpRate = (limb == arms) ? _solverCaches.limbArmRate : _solverCaches.limbLegRate;

					if( pullLength == 1 ) {
						int i0 = pullIndex;
						if( this.origTheta[i0] == 0.0f ) {
							return false;
						}

						if( i0 == 0 ) {
							lerpRate = 1.0f - lerpRate;
						}

						origRotation = _GetRotation( ref this.origAxis[i0], this.origTheta[i0], this.origFeedbackRate[i0] * lerpRate );
						return true;
					}

					// Fix for rotate 180 degrees or more.( half rotation in GetRotation & double rotation in origRotation * origRotation. )
					Quaternion origRotation0 = _GetRotation( ref this.origAxis[0], this.origTheta[0], this.origFeedbackRate[0] * 0.5f );
					Quaternion origRotation1 = _GetRotation( ref this.origAxis[1], this.origTheta[1], this.origFeedbackRate[1] * 0.5f );
					origRotation = Quaternion.Lerp( origRotation0, origRotation1, lerpRate );
					origRotation = origRotation * origRotation; // Optimized: Not normalize.
					return true;
				}

				public void UpperRotation( int origIndex, ref Matrix3x3 origBasis )
				{
					Vector3 origPos = (origIndex == -1) ? this.centerLegPos : this.spinePos[origIndex];

					{
						var armPos = this.armPos;
						if( armPos != null ) {
							for( int i = 0; i < armPos.Length; ++i ) {
								SAFBIKMatMultVecPreSubAdd( out armPos[i], ref origBasis, ref armPos[i], ref origPos, ref origPos );
							}
						}
					}

					if( shoulderPos != null ) {
						for( int i = 0; i < this.shoulderPos.Length; ++i ) {
							SAFBIKMatMultVecPreSubAdd( out shoulderPos[i], ref origBasis, ref shoulderPos[i], ref origPos, ref origPos );
						}
					}
					
					SAFBIKMatMultVecPreSubAdd( out neckPos, ref origBasis, ref neckPos, ref origPos, ref origPos );
					if( headEnabled ) {
						SAFBIKMatMultVecPreSubAdd( out headPos, ref origBasis, ref headPos, ref origPos, ref origPos );
					}

					// Legs					
					if( origIndex == -1 ) { // Rotation origin is centerLeg
						var legPos = this.legPos;
						if( legPos != null ) {
							for( int i = 0; i < legPos.Length; ++i ) {
								SAFBIKMatMultVecPreSubAdd( out legPos[i], ref origBasis, ref legPos[i], ref origPos, ref origPos );
                            }
						}
						
						_isDirtyCenterLegBasis = true;
					}

					// Spine
					for( int t = (origIndex == -1) ? 0 : origIndex; t < this.spinePos.Length; ++t ) {
						SAFBIKMatMultVecPreSubAdd( out this.spinePos[t], ref origBasis, ref this.spinePos[t], ref origPos, ref origPos );
					}

					_isDirtyCenterArmPos = true;
					_isDirtySpineUBasis = true;
				}

				public void LowerRotation( int origIndex, ref Quaternion origRotation, bool bodyRotation )
				{
					Matrix3x3 origBasis = new Matrix3x3( origRotation );
					LowerRotation( origIndex, ref origBasis, bodyRotation );
				}

				public void LowerRotation( int origIndex, ref Matrix3x3 origBasis, bool bodyRotation )
				{
					Vector3 origPos = (origIndex == -1) ? this.centerLegPos : this.spinePos[origIndex];

					var legPos = this.legPos;
					if( legPos != null ) {
						for( int i = 0; i < 2; ++i ) {
							SAFBIKMatMultVecPreSubAdd( out legPos[i], ref origBasis, ref legPos[i], ref origPos, ref origPos );
                        }
					}

					if( this.spinePos != null ) {
						int length = bodyRotation ? this.spinePos.Length : origIndex;
						for( int n = 0; n < length; ++n ) {
							SAFBIKMatMultVecPreSubAdd( out spinePos[n], ref origBasis, ref spinePos[n], ref origPos, ref origPos );
						}
					}

					_isDirtyCenterArmPos = true;
					_isDirtyCenterLegPos = true;
					_isDirtyCenterLegBasis = true;

					if( bodyRotation || this.spinePos == null || origIndex + 1 == this.spinePos.Length ) {
						SAFBIKMatMultVecPreSubAdd( out neckPos, ref origBasis, ref neckPos, ref origPos, ref origPos );
						if( headEnabled ) {
							SAFBIKMatMultVecPreSubAdd( out headPos, ref origBasis, ref headPos, ref origPos, ref origPos );
						}

						var armPos = this.armPos;
						if( armPos != null ) {
							for( int i = 0; i < 2; ++i ) {
								SAFBIKMatMultVecPreSubAdd( out armPos[i], ref origBasis, ref armPos[i], ref origPos, ref origPos );
							}
						}

						if( this.shoulderPos != null ) {
							for( int i = 0; i < 2; ++i ) {
								SAFBIKMatMultVecPreSubAdd( out shoulderPos[i], ref origBasis, ref shoulderPos[i], ref origPos, ref origPos );
							}
						}

						_isDirtySpineUBasis = true;
					}
				}

				public bool PrepareLowerTranslate()
				{
					bool r = false;
					for( int i = 0; i < 2; ++i ) {
						this.legs.SolveTargetBeginPos( i );
						r |= _PrepareLimbTranslate( this.legs, i, ref this.legs.beginPos[i] );
					}
					return r;
				}

				bool _PrepareLimbTranslate( Limb limb, int i, ref Vector3 beginPos )
				{
					this.origTranslate[i] = Vector3.zero;
					if( limb.targetBeginPosEnabled[i] ) {
						this.origTranslate[i] = (limb.targetBeginPos[i] - beginPos);
						return true;
					}

					return false;
				}

				public bool SolveLowerTranslate( out Vector3 translate )
				{
					return _SolveLimbTranslate( this.legs, out translate );
				}

				bool _SolveLimbTranslate( Limb limb, out Vector3 origTranslate )
				{
					origTranslate = Vector3.zero;

					float lerpRate = (limb == arms) ? _solverCaches.limbArmRate : _solverCaches.limbLegRate;

					if( limb.targetBeginPosEnabled[0] && limb.targetBeginPosEnabled[1] ) {
						origTranslate = Vector3.Lerp( this.origTranslate[0], this.origTranslate[1], lerpRate );
					} else if( limb.targetBeginPosEnabled[0] || limb.targetBeginPosEnabled[1] ) {
						int i0 = limb.targetBeginPosEnabled[0] ? 0 : 1;
						float lerpRate1to0 = limb.targetBeginPosEnabled[0] ? (1.0f - lerpRate) : lerpRate;
						origTranslate = this.origTranslate[i0] * lerpRate1to0;
					}

					return (origTranslate != Vector3.zero);
				}
				
				public void LowerTranslateBeginOnly( ref Vector3 origTranslate )
				{
					_LimbTranslateBeginOnly( this.legs, ref origTranslate );
					_centerLegPos += origTranslate; // Optimized.
				}

				void _LimbTranslateBeginOnly( Limb limb, ref Vector3 origTranslate )
				{
					for( int i = 0; i < 2; ++i ) {
						limb.beginPos[i] += origTranslate;
					}
				}

				public void Translate( ref Vector3 origTranslate )
				{
					_centerArmPos += origTranslate;
					_centerLegPos += origTranslate;

					if( spinePos != null ) {
						for( int i = 0; i != spinePos.Length; ++i ) {
							spinePos[i] += origTranslate;
						}
					}
					
					neckPos += origTranslate;
					if( headEnabled ) {
						headPos += origTranslate;
					}
					
					for( int i = 0; i != 2; ++i ) {
						if( legPos != null ) {
							legPos[i] += origTranslate;
						}

						if( shoulderPos != null ) {
							shoulderPos[i] += origTranslate;
						}

						if( armPos != null ) {
							armPos[i] += origTranslate;
						}
					}
				}

				public void SolveShoulderToArmInternal( int i, ref Vector3 destArmPos )
				{
					if( !settings.bodyIK.shoulderSolveEnabled ) {
						return;
					}

					Bone[] shoulderBones = _solverCaches.shoulderBones;
					float[] shoulderToArmLength = _solverCaches.shoulderToArmLength;
					float limitYPlus = internalValues.bodyIK.shoulderLimitThetaYPlus.sin;
					float limitYMinus = internalValues.bodyIK.shoulderLimitThetaYMinus.sin;
					float limitZ = internalValues.bodyIK.shoulderLimitThetaZ.sin;

					if( shoulderBones == null ) {
						return;
					}

					if( _shouderLocalAxisYInv[i] ) {
						float t = limitYPlus;
						limitYPlus = limitYMinus;
						limitYMinus = t;
					}

					if( !IsFuzzy( ref armPos[i], ref destArmPos ) ) {
						Vector3 dirX = destArmPos - this.shoulderPos[i];
						if( SAFBIKVecNormalize( ref dirX ) ) {
							if( settings.bodyIK.shoulderLimitEnabled ) {
								Matrix3x3 worldBasis = this.spineUBasis;
								SAFBIKMatMultRet0( ref worldBasis, ref shoulderBones[i]._localAxisBasis );
								SAFBIKMatMultVecInv( out dirX, ref worldBasis, ref dirX );
								_LimitYZ_Square( i != 0, ref dirX, limitYMinus, limitYPlus, limitZ, limitZ );
								SAFBIKMatMultVec( out dirX, ref worldBasis, ref dirX );
							}

							this.armPos[i] = this.shoulderPos[i] + dirX * shoulderToArmLength[i];
						}
					}
				}
			}

			//----------------------------------------------------------------------------------------------------------------------------------------

			static bool _ComputeCenterLegBasis(
				out Matrix3x3 centerLegBasis,
				ref Vector3 spinePos,
				ref Vector3 leftLegPos,
				ref Vector3 rightLegPos )
			{
				Vector3 dirX = rightLegPos - leftLegPos;
				Vector3 dirY = spinePos - (rightLegPos + leftLegPos) * 0.5f;
				if( SAFBIKVecNormalize( ref dirY ) ) {
					return SAFBIKComputeBasisFromXYLockY( out centerLegBasis, ref dirX, ref dirY );
				} else {
					centerLegBasis = Matrix3x3.identity;
					return false;
				}
			}

			//----------------------------------------------------------------------------------------------------------------------------------------

			static bool _KeepMaxLength( ref Vector3 posTo, ref Vector3 posFrom, float keepLength )
			{
				Vector3 v = posTo - posFrom;
				float len = SAFBIKVecLength( ref v );
				if( len > IKEpsilon && len > keepLength ) {
					v = v * (keepLength / len);
					posTo = posFrom + v;
					return true;
				}

				return false;
			}

			static bool _KeepMaxLength( ref Vector3 posTo, ref Vector3 posFrom, ref FastLength keepLength )
			{
				Vector3 v = posTo - posFrom;
				float len = SAFBIKVecLength( ref v );
				if( len > IKEpsilon && len > keepLength.length ) {
					v = v * (keepLength.length / len);
					posTo = posFrom + v;
					return true;
				}

				return false;
			}

			static bool _KeepLength( ref Vector3 posTo, ref Vector3 posFrom, float keepLength )
			{
				Vector3 v = posTo - posFrom;
				float len = SAFBIKVecLength( ref v );
				if( len > IKEpsilon ) {
					v = v * (keepLength / len);
					posTo = posFrom + v;
					return true;
				}

				return false;
			}

			static Quaternion _GetRotation( ref Vector3 axisDir, float theta, float rate )
			{
				if( (theta >= -IKEpsilon && theta <= IKEpsilon) || (rate >= -IKEpsilon && rate <= IKEpsilon) ) {
					return Quaternion.identity;
				} else {
					return Quaternion.AngleAxis( SAFBIKAcos( theta ) * rate * Mathf.Rad2Deg, axisDir );
				}
			}

			//--------------------------------------------------------------------------------------------------------------------------------

			static float _ConcatPull( float pull, float effectorPull )
			{
				if( pull >= 1.0f - IKEpsilon ) {
					return 1.0f;
				}
				if( pull <= IKEpsilon ) {
					return effectorPull;
				}
				if( effectorPull > IKEpsilon ) {
					if( effectorPull >= 1.0f - IKEpsilon ) {
						return 1.0f;
					} else {
						return pull + (1.0f - pull) * effectorPull;
					}
				} else {
					return pull;
				}
			}

			static float _GetBalancedPullLockTo( float pullFrom, float pullTo )
			{
				if( pullTo <= IKEpsilon ) {
					return 1.0f - pullFrom;
				}
				if( pullFrom <= IKEpsilon ) {
					return 1.0f; // Lock to.
				}

				return pullTo / (pullFrom + pullTo);
			}

			static float _GetBalancedPullLockFrom( float pullFrom, float pullTo )
			{
				if( pullFrom <= IKEpsilon ) {
					return pullTo;
				}
				if( pullTo <= IKEpsilon ) {
					return 0.0f; // Lock from.
				}

				return pullTo / (pullFrom + pullTo);
			}

			static float _GetLerpRateFromPull2( float pull0, float pull1 )
			{
				if( pull0 > FLOAT_EPSILON && pull1 > FLOAT_EPSILON ) {
					return Mathf.Clamp01( pull1 / (pull0 + pull1) );
				} else if( pull0 > FLOAT_EPSILON ) {
					return 0.0f;
				} else if( pull1 > FLOAT_EPSILON ) {
					return 1.0f;
				} else {
					return 0.5f;
				}
			}
		}
	}
}
