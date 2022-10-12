// Copyright (c) 2016 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

#if SAFULLBODYIK_DEBUG
//#define _FORCE_CANCEL_FEEDBACK_WORLDTRANSFORM
#endif

using UnityEngine;

namespace SA
{

	public partial class FullBodyIK
	{
		[System.Serializable]
		public class Effector
		{
			[System.Flags]
			enum _EffectorFlags
			{
				None = 0x00,
				RotationContained = 0x01, // Hips/Wrist/Foot
				PullContained = 0x02, // Foot/Wrist
			}
			
			// Memo: If transform is created & cloned this instance, will be cloned effector transform, too.
			public Transform transform = null;

			public bool positionEnabled = false;
			public bool rotationEnabled = false;
			public float positionWeight = 1.0f;
			public float rotationWeight = 1.0f;
			public float pull = 0.0f;

			[System.NonSerialized]
			public Vector3 _hidden_worldPosition = Vector3.zero;

			public bool effectorEnabled {
				get {
					return this.positionEnabled || (this.rotationContained && this.rotationContained);
				}
			}

			[SerializeField]
			bool _isPresetted = false;
			[SerializeField]
			EffectorLocation _effectorLocation = EffectorLocation.Unknown;
			[SerializeField]
			EffectorType _effectorType = EffectorType.Unknown;
			[SerializeField]
			_EffectorFlags _effectorFlags = _EffectorFlags.None;

			// These aren't serialize field.
			// Memo: If this instance is cloned, will be copyed these properties, too.
			Effector _parentEffector = null;
			Bone _bone = null; // Hips : Hips Eyes : Head
			Bone _leftBone = null; // Hips : LeftLeg Eyes : LeftEye Others : null
			Bone _rightBone = null; // Hips : RightLeg Eyes : RightEye Others : null

			// Memo: If transform is created & cloned this instance, will be cloned effector transform, too.
			[SerializeField]
			Transform _createdTransform = null; // Hidden, for destroy check.

			// Memo: defaultPosition / defaultRotation is copied from bone.
			[SerializeField]
			public Vector3 _defaultPosition = Vector3.zero;
			[SerializeField]
			public Quaternion _defaultRotation = Quaternion.identity;

			public bool _isSimulateFingerTips = false; // Bind effector fingerTips2

			// Basiclly flags.
			public bool rotationContained { get { return (this._effectorFlags & _EffectorFlags.RotationContained) != _EffectorFlags.None; } }
			public bool pullContained { get { return (this._effectorFlags & _EffectorFlags.PullContained) != _EffectorFlags.None; } }

			// These are read only properties.
			public EffectorLocation effectorLocation { get { return _effectorLocation; } }
			public EffectorType effectorType { get { return _effectorType; } }
			public Effector parentEffector { get { return _parentEffector; } }
			public Bone bone { get { return _bone; } }
			public Bone leftBone { get { return _leftBone; } }
			public Bone rightBone { get { return _rightBone; } }
			public Vector3 defaultPosition { get { return _defaultPosition; } }
			public Quaternion defaultRotation { get { return _defaultRotation; } }

			// Internal values. Acepted public accessing. Because these values are required for OnDrawGizmos.
			// (For debug only. You must use worldPosition / worldRotation in useful case.)
			[System.NonSerialized]
			public Vector3 _worldPosition = Vector3.zero;
			[System.NonSerialized]
			public Quaternion _worldRotation = Quaternion.identity;

			// Internal flags.
			bool _isReadWorldPosition = false;
			bool _isReadWorldRotation = false;
			bool _isWrittenWorldPosition = false;
			bool _isWrittenWorldRotation = false;

			bool _isHiddenEyes = false;

			int _transformIsAlive = -1;

			public string name {
				get {
					return GetEffectorName( _effectorLocation );
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

			bool _defaultLocalBasisIsIdentity {
				get {
					if( (_effectorFlags & _EffectorFlags.RotationContained) != _EffectorFlags.None ) { // Hips, Wrist, Foot
						Assert( _bone != null );
						if( _bone != null && _bone.localAxisFrom != _LocalAxisFrom.None && _bone.boneType != BoneType.Hips ) { // Exclude Hips.
							// Hips is identity transform.
							return false;
						}
					}

					return true;
				}
			}
			
			public void Prefix()
			{
				positionEnabled = _GetPresetPositionEnabled( _effectorType );
				positionWeight = _GetPresetPositionWeight( _effectorType );
				pull = _GetPresetPull( _effectorType );
			}

			void _PresetEffectorLocation( EffectorLocation effectorLocation )
			{
				_isPresetted = true;
				_effectorLocation = effectorLocation;
				_effectorType = ToEffectorType( effectorLocation );
				_effectorFlags = _GetEffectorFlags( _effectorType );
			}

			// Call from Awake() or Editor Scripts.
			// Memo: bone.transform is null yet.
			public static void Prefix(
				Effector[] effectors,
				ref Effector effector,
				EffectorLocation effectorLocation,
				bool createEffectorTransform,
				Transform parentTransform,
				Effector parentEffector = null,
				Bone bone = null,
				Bone leftBone = null,
				Bone rightBone = null )
			{
				if( effector == null ) {
					effector = new Effector();
				}

				if( !effector._isPresetted ||
					effector._effectorLocation != effectorLocation ||
					(int)effector._effectorType < 0 ||
					(int)effector._effectorType >= (int)EffectorType.Max ) {
					effector._PresetEffectorLocation( effectorLocation );
				}
				
				effector._parentEffector = parentEffector;
				effector._bone = bone;
				effector._leftBone = leftBone;
				effector._rightBone = rightBone;

				// Create or destroy effectorTransform.
				effector._PrefixTransform( createEffectorTransform, parentTransform );

				if( effectors != null ) {
					effectors[(int)effectorLocation] = effector;
				}
			}
			
			static bool _GetPresetPositionEnabled( EffectorType effectorType )
			{
				switch( effectorType ) {
				case EffectorType.Wrist:	return true;
				case EffectorType.Foot:		return true;
				}

				return false;
			}

			static float _GetPresetPositionWeight( EffectorType effectorType )
			{
				switch( effectorType ) {
				case EffectorType.Arm:		return 0.0f;
				}

				return 1.0f;
			}

			static float _GetPresetPull( EffectorType effectorType )
			{
				switch( effectorType ) {
				case EffectorType.Hips:		return 1.0f;
				case EffectorType.Eyes:		return 1.0f;
				case EffectorType.Arm:		return 1.0f;
				case EffectorType.Wrist:	return 1.0f;
				case EffectorType.Foot:		return 1.0f;
				}

				return 0.0f;
			}
			
			static _EffectorFlags _GetEffectorFlags( EffectorType effectorType )
			{
				switch( effectorType ) {
				case EffectorType.Hips:		return _EffectorFlags.RotationContained | _EffectorFlags.PullContained;
				case EffectorType.Neck:		return _EffectorFlags.PullContained;
				case EffectorType.Head:		return _EffectorFlags.RotationContained | _EffectorFlags.PullContained;
				case EffectorType.Eyes:		return _EffectorFlags.PullContained;
				case EffectorType.Arm:		return _EffectorFlags.PullContained;
				case EffectorType.Wrist:	return _EffectorFlags.RotationContained | _EffectorFlags.PullContained;
				case EffectorType.Foot:		return _EffectorFlags.RotationContained | _EffectorFlags.PullContained;
				case EffectorType.Elbow:	return _EffectorFlags.PullContained;
				case EffectorType.Knee:		return _EffectorFlags.PullContained;
				}
				
				return _EffectorFlags.None;
			}
			
			void _PrefixTransform( bool createEffectorTransform, Transform parentTransform )
			{
				if( createEffectorTransform ) {
					if( this.transform == null || this.transform != _createdTransform ) {
						if( this.transform == null ) {
							var go = new GameObject( GetEffectorName( _effectorLocation ) );
							if( parentTransform != null ) {
								go.transform.SetParent( parentTransform, false );
							} else if( _parentEffector != null && _parentEffector.transformIsAlive ) {
								go.transform.SetParent( _parentEffector.transform, false );
							}
							this.transform = go.transform;
							this._createdTransform = go.transform;
						} else { // Cleanup created transform.
							DestroyImmediate( ref _createdTransform, true );
						}
					} else {
						CheckAlive( ref _createdTransform ); // Overwrite weak reference.
					}
				} else { // Cleanup created transform.
					if( _createdTransform != null ) {
						if( this.transform == _createdTransform ) {
							this.transform = null;
						}
						Object.DestroyImmediate( _createdTransform.gameObject, true );
					}
					_createdTransform = null; // Overwrite weak reference.
				}

				_transformIsAlive = CheckAlive( ref this.transform ) ? 1 : 0;
			}

			public void Prepare( FullBodyIK fullBodyIK )
			{
				Assert( fullBodyIK != null );

				_ClearInternal();

				_ComputeDefaultTransform( fullBodyIK );
				
				// Reset transform.
				if( this.transformIsAlive ) {
					if( _effectorType == EffectorType.Eyes ) {
						this.transform.position = _defaultPosition + fullBodyIK.internalValues.defaultRootBasis.column2 * Eyes_DefaultDistance;
					} else {
						this.transform.position = _defaultPosition;
					}

					if( !_defaultLocalBasisIsIdentity ) {
						this.transform.rotation = _defaultRotation;
					} else {
						this.transform.localRotation = Quaternion.identity;
					}

					this.transform.localScale = Vector3.one;
				}

				_worldPosition = _defaultPosition;
				_worldRotation = _defaultRotation;
				if( _effectorType == EffectorType.Eyes ) {
					_worldPosition += fullBodyIK.internalValues.defaultRootBasis.column2 * Eyes_DefaultDistance;
				}
			}

			public void _ComputeDefaultTransform( FullBodyIK fullBodyIK )
			{
				if( _parentEffector != null ) {
					_defaultRotation = _parentEffector._defaultRotation;
				}

				if( _effectorType == EffectorType.Root ) {
					_defaultPosition = fullBodyIK.internalValues.defaultRootPosition;
					_defaultRotation = fullBodyIK.internalValues.defaultRootRotation;
				} else if( _effectorType == EffectorType.HandFinger ) {
					Assert( _bone != null );
					if( _bone != null ) {
						if( _bone.transformIsAlive ) {
							_defaultPosition = bone._defaultPosition;
						} else { // Failsafe. Simulate finger tips.
								 // Memo: If transformIsAlive == false, _parentBone is null.
							Assert( _bone.parentBoneLocationBased != null && _bone.parentBoneLocationBased.parentBoneLocationBased != null );
							if( _bone.parentBoneLocationBased != null && _bone.parentBoneLocationBased.parentBoneLocationBased != null ) {
								Vector3 tipTranslate = (bone.parentBoneLocationBased._defaultPosition - bone.parentBoneLocationBased.parentBoneLocationBased._defaultPosition);
								_defaultPosition = bone.parentBoneLocationBased._defaultPosition + tipTranslate;
								_isSimulateFingerTips = true;
							}
						}
					}
				} else if( _effectorType == EffectorType.Eyes ) {
					Assert( _bone != null );
					_isHiddenEyes = fullBodyIK._IsHiddenCustomEyes();
					if( !_isHiddenEyes && _bone != null && _bone.transformIsAlive &&
						_leftBone != null && _leftBone.transformIsAlive &&
						_rightBone != null && _rightBone.transformIsAlive ) {
						// _bone ... Head / _leftBone ... LeftEye / _rightBone ... RightEye
						_defaultPosition = (_leftBone._defaultPosition + _rightBone._defaultPosition) * 0.5f;
					} else if( _bone != null && _bone.transformIsAlive ) {
						_defaultPosition = _bone._defaultPosition;
						// _bone ... Head / _bone.parentBone ... Neck
						if( _bone.parentBone != null && _bone.parentBone.transformIsAlive && _bone.parentBone.boneType == BoneType.Neck ) {
							Vector3 neckToHead = _bone._defaultPosition - _bone.parentBone._defaultPosition;
							float neckToHeadY = (neckToHead.y > 0.0f) ? neckToHead.y : 0.0f;
							_defaultPosition += fullBodyIK.internalValues.defaultRootBasis.column1 * neckToHeadY;
							_defaultPosition += fullBodyIK.internalValues.defaultRootBasis.column2 * neckToHeadY;
						}
					}
				} else if( _effectorType == EffectorType.Hips ) {
					Assert( _bone != null && _leftBone != null && _rightBone != null );
					if( _bone != null && _leftBone != null && _rightBone != null ) {
						// _bone ... Hips / _leftBone ... LeftLeg / _rightBone ... RightLeg
						_defaultPosition = (_leftBone._defaultPosition + _rightBone._defaultPosition) * 0.5f;
					}
				} else { // Normally case.
					Assert( _bone != null );
					if( _bone != null ) {
						_defaultPosition = bone._defaultPosition;
						if( !_defaultLocalBasisIsIdentity ) { // For wrist & foot.
							_defaultRotation = bone._localAxisRotation;
						}
					}
				}
			}

			void _ClearInternal()
			{
				_transformIsAlive = -1;
				_defaultPosition = Vector3.zero;
				_defaultRotation = Quaternion.identity;
			}

			public void PrepareUpdate()
			{
				_transformIsAlive = -1;
				_isReadWorldPosition = false;
				_isReadWorldRotation = false;
				_isWrittenWorldPosition = false;
				_isWrittenWorldRotation = false;
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

			public Vector3 bone_worldPosition {
				get {
					if( _effectorType == EffectorType.Eyes ) {
						if( !_isHiddenEyes && _bone != null && _bone.transformIsAlive &&
							_leftBone != null && _leftBone.transformIsAlive &&
							_rightBone != null && _rightBone.transformIsAlive ) {
							// _bone ... Head / _leftBone ... LeftEye / _rightBone ... RightEye
							return (_leftBone.worldPosition + _rightBone.worldPosition) * 0.5f;
						} else if( _bone != null && _bone.transformIsAlive ) {
							Vector3 currentPosition = _bone.worldPosition;
							// _bone ... Head / _bone.parentBone ... Neck
							if( _bone.parentBone != null && _bone.parentBone.transformIsAlive && _bone.parentBone.boneType == BoneType.Neck ) {
								Vector3 neckToHead = _bone._defaultPosition - _bone.parentBone._defaultPosition;
								float neckToHeadY = (neckToHead.y > 0.0f) ? neckToHead.y : 0.0f;
								Quaternion parentBaseRotation = (_bone.parentBone.worldRotation * _bone.parentBone._worldToBaseRotation);
								Matrix3x3 parentBaseBasis;
								SAFBIKMatSetRot( out parentBaseBasis, ref parentBaseRotation );
								currentPosition += parentBaseBasis.column1 * neckToHeadY;
								currentPosition += parentBaseBasis.column2 * neckToHeadY;
							}
							return currentPosition;
						}
					} else if( _isSimulateFingerTips ) {
						if( _bone != null &&
							_bone.parentBoneLocationBased != null &&
							_bone.parentBoneLocationBased.transformIsAlive &&
							_bone.parentBoneLocationBased.parentBoneLocationBased != null &&
							_bone.parentBoneLocationBased.parentBoneLocationBased.transformIsAlive ) {
							Vector3 parentPosition = _bone.parentBoneLocationBased.worldPosition;
							Vector3 parentParentPosition = _bone.parentBoneLocationBased.parentBoneLocationBased.worldPosition;
							return parentPosition + (parentPosition - parentParentPosition);
						}
					} else {
						if( _bone != null && _bone.transformIsAlive ) {
							return _bone.worldPosition;
						}
					}

					return this.worldPosition; // Failsafe.
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

			public void WriteToTransform()
			{
#if _FORCE_CANCEL_FEEDBACK_WORLDTRANSFORM
				// Nothing.
#else
				if( _isWrittenWorldPosition ) {
					_isWrittenWorldPosition = false; // Turn off _isWrittenWorldPosition
					if( this.transformIsAlive ) {
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
	}

}