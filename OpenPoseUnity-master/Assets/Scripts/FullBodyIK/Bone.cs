// Copyright (c) 2016 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

#if SAFULLBODYIK_DEBUG
//#define _FORCE_NO_LOCAL_IDENTITY
//#define _FORCE_NO_WORLD_IDENTITY
//#define _FORCE_FEEDBACK_WORLDTRANSFORM
//#define _FORCE_CANCEL_FEEDBACK_WORLDTRANSFORM
#endif

using UnityEngine;

namespace SA
{
	public partial class FullBodyIK
	{
		public enum _LocalAxisFrom
		{
			None,
			Parent,
			Child,
			Max,
			Unknown = Max,
		}

		[System.Serializable]
		public class Bone
		{
			public Transform transform = null;

			[SerializeField]
			bool _isPresetted = false;
			[SerializeField]
			BoneLocation _boneLocation = BoneLocation.Unknown;
			[SerializeField]
			BoneType _boneType = BoneType.Unknown;
			[SerializeField]
			Side _boneSide = Side.None;
			[SerializeField]
			FingerType _fingerType = FingerType.Unknown;
			[SerializeField]
			int _fingerIndex = -1;
			[SerializeField]
			_LocalAxisFrom _localAxisFrom = _LocalAxisFrom.Unknown;
			[SerializeField]
			_DirectionAs _localDirectionAs = _DirectionAs.Uknown;

			public BoneLocation boneLocation { get { return _boneLocation; } }
			public BoneType boneType { get { return _boneType; } }
			public Side boneSide { get { return _boneSide; } }
			public FingerType fingerType { get { return _fingerType; } }
			public int fingerIndex { get { return _fingerIndex; } }
			public _LocalAxisFrom localAxisFrom { get { return _localAxisFrom; } }
			public _DirectionAs localDirectionAs { get { return _localDirectionAs; } }

			// These aren't serialize field.
			// Memo: If this instance is cloned, will be cloned these properties, too.
			// This value is modified in Prepare(). (Skip null transform.)
			Bone _parentBone = null;
			// This value is modified in Prefix(). (Don't skip null transform.)
			Bone _parentBoneLocationBased = null;

			public Bone parentBone { get { return _parentBone; } }
			public Bone parentBoneLocationBased { get { return _parentBoneLocationBased; } }

			// Internal values. Acepted public accessing. Because faster than property methods.
			// Memo: defaultPosition / defaultRotation is copied from transform.
			public Vector3 _defaultPosition = Vector3.zero;				// transform.position
			public Quaternion _defaultRotation = Quaternion.identity;   // transform.rotation
			public Matrix3x3 _defaultBasis = Matrix3x3.identity;
			public Vector3 _defaultLocalTranslate = Vector3.zero;       // transform.position - transform.parent.position
			public Vector3 _defaultLocalDirection = Vector3.zero;       // _defaultLocalTranslate.Normalize()
			public FastLength _defaultLocalLength = new FastLength();	// _defaultLocalTranslate.magnitude

			// Internal values. Acepted public accessing. Because faster than property methods.
			// Memo: These values are modified in Prepare().
			public Matrix3x3 _localAxisBasis = Matrix3x3.identity;
			public Matrix3x3 _localAxisBasisInv = Matrix3x3.identity;
			public Quaternion _localAxisRotation = Quaternion.identity;
			public Quaternion _localAxisRotationInv = Quaternion.identity;
			public Matrix3x3 _worldToBoneBasis = Matrix3x3.identity;
			public Matrix3x3 _boneToWorldBasis = Matrix3x3.identity;
			public Matrix3x3 _worldToBaseBasis = Matrix3x3.identity;
			public Matrix3x3 _baseToWorldBasis = Matrix3x3.identity;
			public Quaternion _worldToBoneRotation = Quaternion.identity; // Inverse( _defaultRotation ) * _localAxisRotation
			public Quaternion _boneToWorldRotation = Quaternion.identity; // Inverse( _worldToBoneRotation )
			public Quaternion _worldToBaseRotation = Quaternion.identity; // Inverse( _defaultRotation ) * baseRotation
			public Quaternion _baseToWorldRotation = Quaternion.identity; // Inverse( _worldToBaseRotation )
			public Matrix3x3 _baseToBoneBasis = Matrix3x3.identity;
			public Matrix3x3 _boneToBaseBasis = Matrix3x3.identity;

			// Internal Flags. These values are modified in Prepare().
			[SerializeField]
			bool _isWritebackWorldPosition = false; // for Hips / Spine only.

			public bool isWritebackWorldPosition { get { return _isWritebackWorldPosition; } }

			// Internal values. Acepted public accessing. Because these values are required for OnDrawGizmos.
			// (For debug only. You must use worldPosition / worldRotation in useful case.)
			[System.NonSerialized]
			public Vector3 _worldPosition = Vector3.zero;
			[System.NonSerialized]
			public Quaternion _worldRotation = Quaternion.identity;

			// Internal Flags.
			bool _isReadWorldPosition = false;
			bool _isReadWorldRotation = false;
			bool _isWrittenWorldPosition = false;
			bool _isWrittenWorldRotation = false;

			int _transformIsAlive = -1;

			public string name {
				get {
					return _boneType.ToString();
				}
			}

			public bool transformIsAlive {
				get {
					if( _transformIsAlive == -1 ) {
						_transformIsAlive = CheckAlive( ref this.transform ) ? 1 : 0;
					}

					return _transformIsAlive != 0;
				}
			}

			public Transform parentTransform {
				get {
					if( _parentBone != null ) {
						return _parentBone.transform;
					}

					return null;
				}
			}

			// Call from Serializer.
			public static Bone Preset( BoneLocation boneLocation )
			{
				Bone bone = new Bone();
				bone._PresetBoneLocation( boneLocation );
				return bone;
			}

			void _PresetBoneLocation( BoneLocation boneLocation )
			{
				_isPresetted = true;
				_boneLocation = boneLocation;
				_boneType = ToBoneType( boneLocation );
				_boneSide = ToBoneSide( boneLocation );
				if( _boneType == BoneType.HandFinger ) {
					_fingerType = ToFingerType( boneLocation );
					_fingerIndex = ToFingerIndex( boneLocation );
				} else {
					_fingerType = FingerType.Unknown;
					_fingerIndex = -1;
				}
				_PresetLocalAxis();
			}

			void _PresetLocalAxis()
			{
				switch( _boneType ) {
				case BoneType.Hips:			_PresetLocalAxis( _LocalAxisFrom.Child, _DirectionAs.YPlus ); return;
				case BoneType.Spine:		_PresetLocalAxis( _LocalAxisFrom.Child, _DirectionAs.YPlus ); return;
				case BoneType.Neck:			_PresetLocalAxis( _LocalAxisFrom.Child, _DirectionAs.YPlus ); return;
				case BoneType.Head:			_PresetLocalAxis( _LocalAxisFrom.None, _DirectionAs.None ); return;
				case BoneType.Eye:			_PresetLocalAxis( _LocalAxisFrom.None, _DirectionAs.None ); return;

				case BoneType.Leg:			_PresetLocalAxis( _LocalAxisFrom.Child, _DirectionAs.YMinus ); return;
				case BoneType.Knee:			_PresetLocalAxis( _LocalAxisFrom.Child, _DirectionAs.YMinus ); return;
				case BoneType.Foot:			_PresetLocalAxis( _LocalAxisFrom.Parent, _DirectionAs.YMinus ); return;

				case BoneType.Shoulder:		_PresetLocalAxis( _LocalAxisFrom.Child, (_boneSide == Side.Left) ? _DirectionAs.XMinus : _DirectionAs.XPlus ); return;
				case BoneType.Arm:			_PresetLocalAxis( _LocalAxisFrom.Child, (_boneSide == Side.Left) ? _DirectionAs.XMinus : _DirectionAs.XPlus ); return;
				case BoneType.ArmRoll:		_PresetLocalAxis( _LocalAxisFrom.Parent, (_boneSide == Side.Left) ? _DirectionAs.XMinus : _DirectionAs.XPlus ); return;
				case BoneType.Elbow:		_PresetLocalAxis( _LocalAxisFrom.Child, (_boneSide == Side.Left) ? _DirectionAs.XMinus : _DirectionAs.XPlus ); return;
				case BoneType.ElbowRoll:	_PresetLocalAxis( _LocalAxisFrom.Parent, (_boneSide == Side.Left) ? _DirectionAs.XMinus : _DirectionAs.XPlus ); return;
				case BoneType.Wrist:		_PresetLocalAxis( _LocalAxisFrom.Parent, (_boneSide == Side.Left) ? _DirectionAs.XMinus : _DirectionAs.XPlus ); return;
				}

				if( _boneType == BoneType.HandFinger ) {
					_LocalAxisFrom localAxisFrom = (_fingerIndex + 1 == MaxHandFingerLength) ? _LocalAxisFrom.Parent : _LocalAxisFrom.Child;
					_PresetLocalAxis( localAxisFrom, (_boneSide == Side.Left) ? _DirectionAs.XMinus : _DirectionAs.XPlus );
					return;
				}
			}

			void _PresetLocalAxis( _LocalAxisFrom localAxisFrom, _DirectionAs localDirectionAs )
			{
				_localAxisFrom = localAxisFrom;
				_localDirectionAs = localDirectionAs;
			}

			// Call from Awake() or Editor Scripts.
			// Memo: transform is null yet.
			public static void Prefix( Bone[] bones, ref Bone bone, BoneLocation boneLocation, Bone parentBoneLocationBased = null )
			{
				Assert( bones != null );
				if( bone == null ) {
					bone = new Bone();
				}

				if( !bone._isPresetted ||
					bone._boneLocation != boneLocation ||
					(int)bone._boneType < 0 ||
					(int)bone._boneType >= (int)BoneType.Max ||
					bone._localAxisFrom == _LocalAxisFrom.Unknown ||
					bone._localDirectionAs == _DirectionAs.Uknown ) {
					bone._PresetBoneLocation( boneLocation );
				}

				bone._parentBoneLocationBased = parentBoneLocationBased;
				
				if( bones != null ) {
					bones[(int)boneLocation] = bone;
				}
			}

			public void Prepare( FullBodyIK fullBodyIK )
			{
				Assert( fullBodyIK != null );

				_transformIsAlive = -1;
				_localAxisBasis = Matrix3x3.identity;
				_isWritebackWorldPosition = false;

				_parentBone = null;

				// Find transform alive parent bone.
				if( this.transformIsAlive ) {
					for( Bone temp = _parentBoneLocationBased; temp != null; temp = temp._parentBoneLocationBased ) {
						if( temp.transformIsAlive ) {
							_parentBone = temp;
							break;
						}
					}
				}
				
#if SAFULLBODYIK_DEBUG
				if( _boneType != BoneType.Hips && _boneType != BoneType.Eye ) {
					if( this.transformIsAlive && _parentBone == null ) {
						DebugLogError( "parentBone is not found. " + _boneLocation + " (" + _boneType + ") parentBoneLocationBased: " + ((_parentBoneLocationBased != null) ? _parentBoneLocationBased.name : "") );
					}
				}
#endif
				
				if( _boneLocation == BoneLocation.Hips ) {
					if( this.transformIsAlive ) {
						_isWritebackWorldPosition = true;
					}
				} else if( _boneLocation == BoneLocation.Spine ) {
					if( this.transformIsAlive ) {
						if( _parentBone != null && _parentBone.transformIsAlive ) {
							if( IsParentOfRecusively( _parentBone.transform, this.transform ) ) {
								_isWritebackWorldPosition = true;
							}
						}
					}
				}

				if( _boneType == BoneType.Eye ) {
					if( fullBodyIK._IsHiddenCustomEyes() ) {
						_isWritebackWorldPosition = true;
					}
				}

				// Get defaultPosition / defaultRotation
				if( this.transformIsAlive ) {
					_defaultPosition = this.transform.position;
					_defaultRotation = this.transform.rotation;
					SAFBIKMatSetRot( out _defaultBasis, ref _defaultRotation );
					if( _parentBone != null ) { // Always _parentBone.transformIsAlive == true
						_defaultLocalTranslate = _defaultPosition - _parentBone._defaultPosition;
						_defaultLocalLength = FastLength.FromVector3( ref _defaultLocalTranslate );
						if( _defaultLocalLength.length > FLOAT_EPSILON ) {
							float lengthInv = (1.0f / _defaultLocalLength.length);
							_defaultLocalDirection.x = _defaultLocalTranslate.x * lengthInv;
							_defaultLocalDirection.y = _defaultLocalTranslate.y * lengthInv;
							_defaultLocalDirection.z = _defaultLocalTranslate.z * lengthInv;
						}
					}

					SAFBIKMatMultInv0( out _worldToBaseBasis, ref _defaultBasis, ref fullBodyIK.internalValues.defaultRootBasis );
					_baseToWorldBasis = _worldToBaseBasis.transpose;
					SAFBIKMatGetRot( out _worldToBaseRotation, ref _worldToBaseBasis );
                    _baseToWorldRotation = Inverse( _worldToBaseRotation );
				} else {
					_defaultPosition = Vector3.zero;
					_defaultRotation = Quaternion.identity;
					_defaultBasis = Matrix3x3.identity;
					_defaultLocalTranslate = Vector3.zero;
					_defaultLocalLength = new FastLength();
					_defaultLocalDirection = Vector3.zero;

					_worldToBaseBasis = Matrix3x3.identity;
					_baseToWorldBasis = Matrix3x3.identity;
					_worldToBaseRotation = Quaternion.identity;
					_baseToWorldRotation = Quaternion.identity;
				}

				_ComputeLocalAxis( fullBodyIK ); // Require PostPrepare()
			}

			void _ComputeLocalAxis( FullBodyIK fullBodyIK )
			{
				// Compute _localAxisBasis for each bones.
				if( this.transformIsAlive && (_parentBone != null && _parentBone.transformIsAlive) ) {
					if( _localAxisFrom == _LocalAxisFrom.Parent ||
						_parentBone._localAxisFrom == _LocalAxisFrom.Child ) {
						Vector3 dir = _defaultLocalDirection;
						if( dir.x != 0.0f || dir.y != 0.0f || dir.z != 0.0f ) {
							if( _localAxisFrom == _LocalAxisFrom.Parent ) {
								SAFBIKComputeBasisFrom( out _localAxisBasis, ref fullBodyIK.internalValues.defaultRootBasis, ref dir, _localDirectionAs );
							}

							if( _parentBone._localAxisFrom == _LocalAxisFrom.Child ) {
								if( _parentBone._boneType == BoneType.Shoulder ) {
									Bone shoulderBone = _parentBone;
									Bone spineUBone = _parentBone._parentBone;
									Bone neckBone = (fullBodyIK.headBones != null) ? fullBodyIK.headBones.neck : null;
									if( neckBone != null && !neckBone.transformIsAlive ) {
										neckBone = null;
									}

									if( fullBodyIK.internalValues.shoulderDirYAsNeck == -1 ) {
										if( fullBodyIK.settings.shoulderDirYAsNeck == AutomaticBool.Auto ) {
											if( spineUBone != null && neckBone != null ) {
												Vector3 shoulderToSpineU = shoulderBone._defaultLocalDirection;
												Vector3 shoulderToNeck = neckBone._defaultPosition - shoulderBone._defaultPosition;
												if( SAFBIKVecNormalize( ref shoulderToNeck ) ) {
													float shoulderToSpineUTheta = Mathf.Abs( Vector3.Dot( dir, shoulderToSpineU ) );
													float shoulderToNeckTheta = Mathf.Abs( Vector3.Dot( dir, shoulderToNeck ) );
													if( shoulderToSpineUTheta < shoulderToNeckTheta ) {
														fullBodyIK.internalValues.shoulderDirYAsNeck = 0;
													} else {
														fullBodyIK.internalValues.shoulderDirYAsNeck = 1;
													}
												} else {
													fullBodyIK.internalValues.shoulderDirYAsNeck = 0;
												}
											} else {
												fullBodyIK.internalValues.shoulderDirYAsNeck = 0;
											}
										} else {
											fullBodyIK.internalValues.shoulderDirYAsNeck = (fullBodyIK.settings.shoulderDirYAsNeck != AutomaticBool.Disable) ? 1 : 0;
                                        }
									}

									Vector3 xDir, yDir, zDir;
									xDir = (_parentBone._localDirectionAs == _DirectionAs.XMinus) ? -dir : dir;
									if( fullBodyIK.internalValues.shoulderDirYAsNeck != 0 && neckBone != null ) {
										yDir = neckBone._defaultPosition - shoulderBone._defaultPosition;
									} else {
										yDir = shoulderBone._defaultLocalDirection;
									}
									zDir = Vector3.Cross( xDir, yDir );
									yDir = Vector3.Cross( zDir, xDir );
									if( SAFBIKVecNormalize2( ref yDir, ref zDir ) ) {
										_parentBone._localAxisBasis.SetColumn( ref xDir, ref yDir, ref zDir );
									}
								} else if( _parentBone._boneType == BoneType.Spine && _boneType != BoneType.Spine && _boneType != BoneType.Neck ) {
									// Compute spine/neck only( Exclude shouder / arm ).
								} else if( _parentBone._boneType == BoneType.Hips && _boneType != BoneType.Spine ) {
									// Compute spine only( Exclude leg ).
								} else {
									if( _parentBone._boneType == BoneType.Hips ) {
										Vector3 baseX = fullBodyIK.internalValues.defaultRootBasis.column0;
										SAFBIKComputeBasisFromXYLockY( out _parentBone._localAxisBasis, ref baseX, ref dir );
									} else if( _parentBone._boneType == BoneType.Spine || _parentBone._boneType == BoneType.Neck ) {
										// Using parent axis for spine or neck. Preprocess for BodyIK.
										if( _parentBone._parentBone != null ) {
											Vector3 dirX = _parentBone._parentBone._localAxisBasis.column0;
											SAFBIKComputeBasisFromXYLockY( out _parentBone._localAxisBasis, ref dirX, ref dir );
										}
									} else {
										if( _localAxisFrom == _LocalAxisFrom.Parent && _localDirectionAs == _parentBone._localDirectionAs ) {
											_parentBone._localAxisBasis = _localAxisBasis;
										} else {
											SAFBIKComputeBasisFrom( out _parentBone._localAxisBasis,
												ref fullBodyIK.internalValues.defaultRootBasis, ref dir, _parentBone._localDirectionAs );
										}
									}
								}
							}

						}
					}
				}
			}

			public void PostPrepare()
			{
				if( _localAxisFrom != _LocalAxisFrom.None ) {
					_localAxisBasisInv = _localAxisBasis.transpose;
					SAFBIKMatGetRot( out _localAxisRotation, ref _localAxisBasis );
					_localAxisRotationInv = Inverse( _localAxisRotation );
					SAFBIKMatMultInv0( out _worldToBoneBasis, ref _defaultBasis, ref _localAxisBasis );
					_boneToWorldBasis = _worldToBoneBasis.transpose;
					SAFBIKMatGetRot( out _worldToBoneRotation, ref _worldToBoneBasis );
					_boneToWorldRotation = Inverse( _worldToBoneRotation );
				} else {
					_localAxisBasis = Matrix3x3.identity;
					_localAxisBasisInv = Matrix3x3.identity;
					_localAxisRotation = Quaternion.identity;
					_localAxisRotationInv = Quaternion.identity;

					_worldToBoneBasis = _defaultBasis.transpose;
					_boneToWorldBasis = _defaultBasis;
					_worldToBoneRotation = Inverse( _defaultRotation );
					_boneToWorldRotation = _defaultRotation;
				}

				SAFBIKMatMultInv0( out _baseToBoneBasis, ref _worldToBaseBasis, ref _worldToBoneBasis );
				_boneToBaseBasis = _baseToBoneBasis.transpose;
			}

			public void PrepareUpdate()
			{
				_transformIsAlive = -1;
				_isReadWorldPosition = false;
				_isReadWorldRotation = false;
				_isWrittenWorldPosition = false;
				_isWrittenWorldRotation = false;
			}

			public void SyncDisplacement()
			{
				if( _parentBone != null && _parentBone.transformIsAlive && transformIsAlive ) {
					Vector3 translate = this.worldPosition - _parentBone.worldPosition;
					_defaultLocalLength = FastLength.FromVector3( ref translate );
					if( _parentBone.transform == this.transform.parent ) {
						Vector3 localPosition = this.transform.localPosition;
						if( SAFBIKVecNormalize( ref localPosition ) ) {
							Vector3 tempDirection;
							SAFBIKMatMultVec( out tempDirection, ref _parentBone._defaultBasis, ref localPosition );
							_defaultLocalDirection = tempDirection;
							_defaultLocalTranslate = tempDirection * _defaultLocalLength.length;
						} else {
							_defaultLocalDirection = Vector3.zero;
							_defaultLocalTranslate = Vector3.zero;
                        }
					} else {
						_defaultLocalTranslate = _defaultLocalDirection * _defaultLocalLength.length;
					}
				}
			}

			public void PostSyncDisplacement( FullBodyIK fullBodyIK )
			{
				if( _boneLocation == BoneLocation.Hips ) {
					_defaultPosition = fullBodyIK.boneCaches.defaultHipsPosition + fullBodyIK.boneCaches.hipsOffset;
				} else if( _parentBone != null ) {
					_defaultPosition = _parentBone._defaultPosition + _defaultLocalTranslate;
				}

				_ComputeLocalAxis( fullBodyIK ); // Require PostPrepare()
			}

			public Vector3 worldPosition {
				get {
					if( !_isReadWorldPosition && !_isWrittenWorldPosition ) {
						_isReadWorldPosition = true;
						if( this.transformIsAlive ) {
							_worldPosition = this.transform.position;
						}
					}
					return _worldPosition;
				}
				set {
					_isWrittenWorldPosition = true;
					_worldPosition = value;
				}
			}

			public Quaternion worldRotation {
				get {
					if( !_isReadWorldRotation && !_isWrittenWorldRotation ) {
						_isReadWorldRotation = true;
						if( this.transformIsAlive ) {
							_worldRotation = this.transform.rotation;
						}
					}
					return _worldRotation;
				}
				set {
					_isWrittenWorldRotation = true;
					_worldRotation = value;
				}
			}
			
			public void forcefix_worldRotation()
			{
				if( this.transformIsAlive ) {
					if( !_isReadWorldRotation ) {
						_isReadWorldRotation = true;
						_worldRotation = this.transform.rotation;
					}
					_isWrittenWorldRotation = true;

					// Fix worldPosition
					if( _parentBone != null && _parentBone.transformIsAlive ) {
						Quaternion parentWorldRotation = _parentBone.worldRotation;

						Matrix3x3 parentRotationBasis;
						SAFBIKMatSetRotMultInv1( out parentRotationBasis, ref parentWorldRotation, ref this.parentBone._defaultRotation );

						Vector3 parentWorldPosition = parentBone.worldPosition;

						Vector3 tempPos;
						SAFBIKMatMultVecPreSubAdd( out tempPos, ref parentRotationBasis, ref _defaultPosition, ref parentBone._defaultPosition, ref parentWorldPosition );

						_isWrittenWorldPosition = true;
						_isWritebackWorldPosition = true;
						_worldPosition = tempPos;
					}
				}
			}

			public void WriteToTransform()
			{
#if _FORCE_CANCEL_FEEDBACK_WORLDTRANSFORM
				// Nothing.
#else
				if( _isWrittenWorldPosition ) {
					_isWrittenWorldPosition = false; // Turn off _isWrittenWorldPosition
					if( _isWritebackWorldPosition && this.transformIsAlive ) {
						this.transform.position = _worldPosition;
					}
				}
				if( _isWrittenWorldRotation ) {
					_isWrittenWorldRotation = false; // Turn off _isWrittenWorldRotation
					if( this.transformIsAlive ) {
						this.transform.rotation = _worldRotation;
					}
				}
#endif
			}
		}

		//----------------------------------------------------------------------------------------------------------------------------------------------------
	}

}