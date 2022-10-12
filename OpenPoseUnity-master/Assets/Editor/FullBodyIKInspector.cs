// Copyright (c) 2016 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using EditorUtil = SA.FullBodyIKEditorUtility;

namespace SA
{
	public class FullBodyIKInspectorBase : Editor
	{
		bool _initializedGUIStyle;
		GUIStyle _guiStyle_header;
		GUIStyle _guiStyle_section;
		GUIStyle _guiStyle_boneName_Unselected;
		GUIStyle _guiStyle_boneName_Unselected_Optional;

		Vector2 _scrollViewPos_Bones;
		Vector2 _scrollViewPos_Effectors;

		const float boneNameFieldSize = 100.0f;
		const float effectorNameFieldSize = 80.0f;
		static readonly string[] toolbarContents = new string[] { "Basic", "Bone", "Effector" };

		void _Initialize()
		{
			if( !_initializedGUIStyle || _guiStyle_header == null ) {
				_guiStyle_header = new GUIStyle( EditorStyles.label );
				var styleState = new GUIStyleState();
				styleState.textColor = new Color( 0.7f, 0.7f, 0.0f );
				_guiStyle_header.normal = styleState;
				_guiStyle_header.wordWrap = false;
				_guiStyle_header.fontStyle = FontStyle.Bold;
			}

			if( !_initializedGUIStyle || _guiStyle_section == null ) {
				_guiStyle_section = new GUIStyle( EditorStyles.label );
				var styleState = new GUIStyleState();
				styleState.textColor = new Color( 0.2f, 0.7f, 1.0f );
				_guiStyle_section.normal = styleState;
				_guiStyle_section.wordWrap = false;
				_guiStyle_section.fontStyle = FontStyle.Bold;
			}

			if( !_initializedGUIStyle || _guiStyle_boneName_Unselected == null ) {
				_guiStyle_boneName_Unselected = new GUIStyle( EditorStyles.label );
				var styleState = new GUIStyleState();
				styleState.textColor = new Color( 1.0f, 0.4f, 0.4f );
				_guiStyle_boneName_Unselected.normal = styleState;
				_guiStyle_boneName_Unselected.wordWrap = false;
				_guiStyle_boneName_Unselected.alignment = TextAnchor.MiddleLeft;
				_guiStyle_boneName_Unselected.fontStyle = FontStyle.Bold;
			}

			if( !_initializedGUIStyle || _guiStyle_boneName_Unselected_Optional == null ) {
				_guiStyle_boneName_Unselected_Optional = new GUIStyle( EditorStyles.label );
				_guiStyle_boneName_Unselected_Optional.wordWrap = false;
				_guiStyle_boneName_Unselected_Optional.alignment = TextAnchor.MiddleLeft;
				_guiStyle_boneName_Unselected_Optional.fontStyle = FontStyle.Bold;
			}

			_initializedGUIStyle = true;
		}

		void _Header( string name )
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label( name, _guiStyle_header );
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		void _BoneField( string boneName, ref FullBodyIK.Bone bone, bool isOptional )
		{
			FullBodyIK.SafeNew( ref bone );
			var fbik = this.target as FullBodyIKBehaviourBase;
			EditorGUILayout.BeginHorizontal();
			if( bone.transform == null ) {
				if( isOptional ) {
					GUILayout.Label( boneName + " *", _guiStyle_boneName_Unselected_Optional, GUILayout.Width( boneNameFieldSize ) );
				} else {
					GUILayout.Label( boneName, _guiStyle_boneName_Unselected, GUILayout.Width( boneNameFieldSize ) );
				}
			} else {
				GUILayout.Label( boneName, GUILayout.Width( boneNameFieldSize ) );
			}
			EditorUtil.GUI.PushEnabled( !Application.isPlaying );
			EditorUtil.GUI.TransformField( fbik, "", ref bone.transform, true );
			EditorUtil.GUI.PopEnabled();
			EditorGUILayout.EndHorizontal();
		}

		void _FingerBoneField( string boneName, ref FullBodyIK.Bone[] bones, bool isOptional )
		{
			if( bones == null || bones.Length != 4 ) {
				bones = new FullBodyIK.Bone[4];
			}

			for( int i = 0; i < bones.Length; ++i ) {
				string name = null;
				if( i + 1 == bones.Length ) {
					name = boneName + " Tip";
				} else {
					name = boneName + " " + (i + 1).ToString();
				}
				_BoneField( name, ref bones[i], isOptional );
			}
		}

		void _EffectorField( string effectorName, ref FullBodyIK.Effector effector )
		{
			var fbik = this.target as FullBodyIKBehaviourBase;
			var editorSettings = fbik.fullBodyIK.editorSettings;

			if( effector == null ) {
				return;
			}

			GUILayout.BeginHorizontal();
			GUILayout.Label( effectorName, GUILayout.Width( effectorNameFieldSize ) );
			GUILayout.FlexibleSpace();

			if( editorSettings.isShowEffectorTransform ) {
				EditorUtil.GUI.ObjectField( fbik, "", ref effector.transform, true, GUILayout.Height( 18.0f ) );
			} else {
				float labelSpace = 24.0f;

				EditorGUILayout.LabelField( "Pos", GUILayout.Width( labelSpace ) );
				EditorUtil.GUI.ToggleLegacy( fbik, "", ref effector.positionEnabled );
				EditorUtil.GUI.PushEnabled( effector.positionEnabled );
				EditorUtil.GUI.HorizonalSlider( fbik, ref effector.positionWeight, 0.0f, 1.0f, GUILayout.ExpandWidth( false ), GUILayout.Width( 30.0f ) );
				EditorUtil.GUI.PushEnabled( effector.pullContained );
				EditorGUILayout.LabelField( "Pull", GUILayout.Width( labelSpace ) );
				EditorUtil.GUI.HorizonalSlider( fbik, ref effector.pull, 0.0f, 1.0f, GUILayout.ExpandWidth( false ), GUILayout.MinWidth( 30.0f ) );
				EditorUtil.GUI.PopEnabled();
				EditorUtil.GUI.PopEnabled();

				EditorUtil.GUI.PushEnabled( effector.rotationContained );
				EditorGUILayout.LabelField( "Rot", GUILayout.Width( labelSpace ) );
				EditorUtil.GUI.ToggleLegacy( fbik, "", ref effector.rotationEnabled );
				EditorUtil.GUI.PushEnabled( effector.rotationEnabled );
				EditorUtil.GUI.HorizonalSlider( fbik, ref effector.rotationWeight, 0.0f, 1.0f, GUILayout.ExpandWidth( false ), GUILayout.Width( 30.0f ) );
				EditorUtil.GUI.PopEnabled();
				EditorUtil.GUI.PopEnabled();
			}
			GUILayout.EndHorizontal();
		}

		public override void OnInspectorGUI()
		{
			_Initialize();

			var fbik = this.target as FullBodyIKBehaviourBase;
			fbik.Prefix();

			var editorSettings = fbik.fullBodyIK.editorSettings;

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			EditorUtil.GUI.ToggleLegacy( fbik, "Advanced", ref editorSettings.isAdvanced );
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			EditorUtil.GUI.Toolbar( fbik, ref editorSettings.toolbarSelected, toolbarContents );
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();



			switch( editorSettings.toolbarSelected ) {
			case 0:
				_OnInspectorGUI_Basic();
				break;
			case 1:
				_OnInspectorGUI_Bones();
				break;
			case 2:
				_OnInspectorGUI_Effectors();
				break;
			}
		}

		void _OnInspectorGUI_Basic()
		{
			var fbik = this.target as FullBodyIKBehaviourBase;
			var settings = fbik.fullBodyIK.settings;
			var editorSettings = fbik.fullBodyIK.editorSettings;
			var internalValues = fbik.fullBodyIK.internalValues;

			if( editorSettings.isAdvanced ) {
				settings.animatorEnabled = (FullBodyIK.AutomaticBool)EditorGUILayout.EnumPopup( "Animator Enabled", settings.animatorEnabled );
				settings.resetTransforms = (FullBodyIK.AutomaticBool)EditorGUILayout.EnumPopup( "Reset Transforms", settings.resetTransforms );
				settings.syncDisplacement = (FullBodyIK.SyncDisplacement)EditorGUILayout.EnumPopup( "Sync Displcement", settings.syncDisplacement );

				settings.shoulderDirYAsNeck = (FullBodyIK.AutomaticBool)EditorGUILayout.EnumPopup( "Shoulder DirY As Neck", settings.shoulderDirYAsNeck );

				EditorUtil.GUI.Field( "Automatic Prepare Humanoid", ref settings.automaticPrepareHumanoid );
				EditorUtil.GUI.Field( "Automatic Configure Spine Enabled", ref settings.automaticConfigureSpineEnabled );
				EditorUtil.GUI.Field( "Automatic Configure Roll Bones Enabled", ref settings.automaticConfigureRollBonesEnabled );
				EditorUtil.GUI.Field( "Roll Bones Enabled", ref settings.rollBonesEnabled );

				_Header( "BodyIK" );

				EditorUtil.GUI.Field( "Force Solve Enabled", ref settings.bodyIK.forceSolveEnabled );
				EditorUtil.GUI.Field( "Upper Solve Enabled", ref settings.bodyIK.upperSolveEnabled );
				EditorUtil.GUI.Field( "Lower Solve Enabled", ref settings.bodyIK.lowerSolveEnabled );
				EditorUtil.GUI.Field( "Compute World Transform", ref settings.bodyIK.computeWorldTransform );

				EditorUtil.GUI.Field( "Shoulder Solve Enabled", ref settings.bodyIK.shoulderSolveEnabled );
				EditorUtil.GUI.Slider( "Shoulder Solve Bending Rate", ref settings.bodyIK.shoulderSolveBendingRate, 0.0f, 1.0f );
				EditorUtil.GUI.Field( "Shoulder Limit Enabled", ref settings.bodyIK.shoulderLimitEnabled );
				EditorUtil.GUI.Field( "Shoulder Limit Angle YPlus", ref settings.bodyIK.shoulderLimitAngleYPlus );
				EditorUtil.GUI.Field( "Shoulder Limit Angle YMinus", ref settings.bodyIK.shoulderLimitAngleYMinus );
				EditorUtil.GUI.Field( "Shoulder Limit Angle Z", ref settings.bodyIK.shoulderLimitAngleZ );

				EditorUtil.GUI.Field( "Upper Solve Hips Enabled", ref settings.bodyIK.upperSolveHipsEnabled );
				EditorUtil.GUI.Field( "Upper Solve Spine Enabled", ref settings.bodyIK.upperSolveSpineEnabled );
				EditorUtil.GUI.Field( "Upper Solve Spine 2 Enabled", ref settings.bodyIK.upperSolveSpine2Enabled );
				EditorUtil.GUI.Field( "Upper Solve Spine 3 Enabled", ref settings.bodyIK.upperSolveSpine3Enabled );
				EditorUtil.GUI.Field( "Upper Solve Spine 4 Enabled", ref settings.bodyIK.upperSolveSpine4Enabled );

				EditorUtil.GUI.Slider01( "Spine DirX Leg To Arm Rate", ref settings.bodyIK.spineDirXLegToArmRate );
				EditorUtil.GUI.Slider01( "Spine DirX Leg To Arm To Rate", ref settings.bodyIK.spineDirXLegToArmToRate );
				EditorUtil.GUI.Slider01( "Spine DirY Lerp Rate", ref settings.bodyIK.spineDirYLerpRate );

				EditorUtil.GUI.Slider01( "Upper Body Movingfix Rate", ref settings.bodyIK.upperBodyMovingfixRate );
				EditorUtil.GUI.Slider01( "Upper Head Movingfix Rate", ref settings.bodyIK.upperHeadMovingfixRate );
				EditorUtil.GUI.Slider01( "Upper CenterLeg Translate Rate", ref settings.bodyIK.upperCenterLegTranslateRate );
				EditorUtil.GUI.Slider01( "Upper Spine Translate Rate", ref settings.bodyIK.upperSpineTranslateRate );
				EditorUtil.GUI.Slider01( "Upper CenterLeg Rotate Rate", ref settings.bodyIK.upperCenterLegRotateRate );
				EditorUtil.GUI.Slider01( "Upper Spine Rotate Rate", ref settings.bodyIK.upperSpineRotateRate );
				EditorUtil.GUI.Slider01( "Upper PostTranslate Rate", ref settings.bodyIK.upperPostTranslateRate );
				EditorUtil.GUI.Slider01( "Upper CenterLeg Lerp Rate", ref settings.bodyIK.upperCenterLegLerpRate );
				EditorUtil.GUI.Slider01( "Upper Spine Lerp Rate", ref settings.bodyIK.upperSpineLerpRate );

				GUILayout.Label( "Upper DirX", _guiStyle_section );
				EditorUtil.GUI.Field( "Upper DirX Limit Enabled", ref settings.bodyIK.upperDirXLimitEnabled );
				EditorUtil.GUI.Slider( "Upper DirX Limit Angle Y", ref settings.bodyIK.upperDirXLimitAngleY, 0.0f, 89.99f );

				GUILayout.Label( "Spine", _guiStyle_section );
				EditorUtil.GUI.Field( "Spine Limit Enabled", ref settings.bodyIK.spineLimitEnabled );
				EditorUtil.GUI.Field( "Spine Accurate Limit Enabled", ref settings.bodyIK.spineAccurateLimitEnabled );
				EditorUtil.GUI.Slider( "Spine Limit Angle X", ref settings.bodyIK.spineLimitAngleX, 0.0f, 89.99f );
				EditorUtil.GUI.Slider( "Spine Limit Angle Y", ref settings.bodyIK.spineLimitAngleY, 0.0f, 89.99f );

				GUILayout.Label( "Continuous", _guiStyle_section );
				EditorUtil.GUI.Slider01( "Upper Continuous PreTranslate Rate", ref settings.bodyIK.upperContinuousPreTranslateRate );
				EditorUtil.GUI.Slider01( "Upper Continuous PreTranslate Stable Rate", ref settings.bodyIK.upperContinuousPreTranslateStableRate );
				EditorUtil.GUI.Slider01( "Upper Continuous CenterLeg Rotation Stable Rate", ref settings.bodyIK.upperContinuousCenterLegRotationStableRate );
				EditorUtil.GUI.Slider01( "Upper Continuous PostTranslate Stable Rate", ref settings.bodyIK.upperContinuousPostTranslateStableRate );
				EditorUtil.GUI.Slider01( "Upper Continuous Spine DirY Rate", ref settings.bodyIK.upperContinuousSpineDirYLerpRate );

				GUILayout.Label( "Neck", _guiStyle_section );
				EditorUtil.GUI.Slider01( "Upper Neck To CenterLeg Rate", ref settings.bodyIK.upperNeckToCenterLegRate );
				EditorUtil.GUI.Slider01( "Upper Neck To Spine Rate", ref settings.bodyIK.upperNeckToSpineRate );

				GUILayout.Label( "Eyes", _guiStyle_section );
				EditorUtil.GUI.Slider01( "Upper Eyes To CenterLeg Rate", ref settings.bodyIK.upperEyesToCenterLegRate );
				EditorUtil.GUI.Slider01( "Upper Eyes To Spine Rate", ref settings.bodyIK.upperEyesToSpineRate );
				EditorUtil.GUI.Slider01( "Upper Eyes Yaw Rate", ref settings.bodyIK.upperEyesYawRate );
				EditorUtil.GUI.Slider01( "Upper Eyes Pitch Up Rate", ref settings.bodyIK.upperEyesPitchUpRate );
				EditorUtil.GUI.Slider01( "Upper Eyes Pitch Down Rate", ref settings.bodyIK.upperEyesPitchDownRate );
				EditorUtil.GUI.Slider( "Upper Eyes Limit Yaw", ref settings.bodyIK.upperEyesLimitYaw, 0.0f, 89.99f );
				EditorUtil.GUI.Slider( "Upper Eyes Limit Pitch Up", ref settings.bodyIK.upperEyesLimitPitchUp, 0.0f, 89.99f );
				EditorUtil.GUI.Slider( "Upper Eyes Limit Pitch Down", ref settings.bodyIK.upperEyesLimitPitchDown, 0.0f, 89.99f );
				EditorUtil.GUI.Slider( "Upper Eyes Trace Angle", ref settings.bodyIK.upperEyesTraceAngle, 90.0f, 180.0f );

				_Header( "LimbIK" );

				EditorUtil.GUI.Field( "Leg Always Solve Enabled", ref settings.limbIK.legAlwaysSolveEnabled );
				EditorUtil.GUI.Field( "Arm Always Solve Enabled", ref settings.limbIK.armAlwaysSolveEnabled );

				EditorUtil.GUI.Field( "Automatic Knee Base Angle", ref settings.limbIK.automaticKneeBaseAngle );

				GUILayout.Label( "Presolve", _guiStyle_section );
				EditorUtil.GUI.Field( "Presolve Knee Enabled", ref settings.limbIK.presolveKneeEnabled );
				EditorUtil.GUI.Field( "Presolve Elbow Enabled", ref settings.limbIK.presolveElbowEnabled );
				EditorUtil.GUI.Slider01( "Presolve Knee Rate", ref settings.limbIK.presolveKneeRate );
				EditorUtil.GUI.Slider( "Presolve Knee Lerp Angle", ref settings.limbIK.presolveKneeLerpAngle, 0.0f, 89.99f );
				EditorUtil.GUI.Slider01( "Presolve Knee Lerp Length Rate", ref settings.limbIK.presolveKneeLerpLengthRate );
				EditorUtil.GUI.Slider01( "Presolve Elbow Rate", ref settings.limbIK.presolveElbowRate );
				EditorUtil.GUI.Slider( "Presolve Elbow Lerp Angle", ref settings.limbIK.presolveElbowLerpAngle, 0.0f, 89.99f );
				EditorUtil.GUI.Slider01( "Presolve Elbow Lerp Length Rate", ref settings.limbIK.presolveElbowLerpLengthRate );

				GUILayout.Label( "Prefix", _guiStyle_section );
				EditorUtil.GUI.Field( "Prefix Leg Effector Enabled", ref settings.limbIK.prefixLegEffectorEnabled );
				EditorUtil.GUI.Slider( "Prefix Leg Upper Limit Angle", ref settings.limbIK.prefixLegUpperLimitAngle, 0.0f, 90.0f );
				EditorUtil.GUI.Slider( "Prefix Knee Upper Limit Angle", ref settings.limbIK.prefixKneeUpperLimitAngle, 0.0f, 90.0f );

				GUILayout.Label( "Effector Length", _guiStyle_section );
				EditorUtil.GUI.Slider01( "Leg Effector Min Length Rate", ref settings.limbIK.legEffectorMinLengthRate );
				EditorUtil.GUI.Slider01( "Leg Effector Max Length Rate", ref settings.limbIK.legEffectorMaxLengthRate );
				EditorUtil.GUI.Slider01( "Arm Effector Max Length Rate", ref settings.limbIK.armEffectorMaxLengthRate );

				GUILayout.Label( "Arm basis forcefix", _guiStyle_section );
				EditorUtil.GUI.Field( "Arm Basis Forcefix Enabled", ref settings.limbIK.armBasisForcefixEnabled );
				EditorUtil.GUI.Slider01( "Arm Basis Forcefix Effector Length Rate", ref settings.limbIK.armBasisForcefixEffectorLengthRate );
				EditorUtil.GUI.Slider01( "Arm Basis Forcefix Effector Length Lerp Rate", ref settings.limbIK.armBasisForcefixEffectorLengthLerpRate );

				GUILayout.Label( "Arm effector fixes(Automatic)", _guiStyle_section );
				EditorUtil.GUI.Field( "Arm Effector Backfix Enabled", ref settings.limbIK.armEffectorBackfixEnabled );
				EditorUtil.GUI.Field( "Arm Effector Innerfix Enabled", ref settings.limbIK.armEffectorInnerfixEnabled );

				GUILayout.Label( "Arm back area(Automatic, XZ)", _guiStyle_section );
				EditorUtil.GUI.Slider( "Arm Effector Back Begin Angle", ref settings.limbIK.armEffectorBackBeginAngle, -90.00f, 45.0f );
				EditorUtil.GUI.Slider( "Arm Effector Back Core Begin Angle", ref settings.limbIK.armEffectorBackCoreBeginAngle, -90.00f, 45.0f );
				EditorUtil.GUI.Slider( "Arm Effector Back Core End Angle", ref settings.limbIK.armEffectorBackCoreEndAngle, -180.00f, 45.0f );
				EditorUtil.GUI.Slider( "Arm Effector Back End Angle", ref settings.limbIK.armEffectorBackEndAngle, -180.00f, 45.0f );

				GUILayout.Label( "Arm back area(Automatic, YZ)", _guiStyle_section );
				EditorUtil.GUI.Slider( "Arm Effector Back Core Upper Angle", ref settings.limbIK.armEffectorBackCoreUpperAngle, -90.0f, 90.0f );
				EditorUtil.GUI.Slider( "Arm Effector Back Core Lower Angle", ref settings.limbIK.armEffectorBackCoreLowerAngle, -90.0f, 90.0f );

				GUILayout.Label( "Arm elbow angle(Automatic)", _guiStyle_section );
				EditorUtil.GUI.Slider( "Automatic Elbow Base Angle", ref settings.limbIK.automaticElbowBaseAngle, -360.0f, 360.0f );
				EditorUtil.GUI.Slider( "Automatic Elbow Lower Angle", ref settings.limbIK.automaticElbowLowerAngle, -360.0f, 360.0f );
				EditorUtil.GUI.Slider( "Automatic Elbow Upper Angle", ref settings.limbIK.automaticElbowUpperAngle, -360.0f, 360.0f );
				EditorUtil.GUI.Slider( "Automatic Elbow Back Upper Angle", ref settings.limbIK.automaticElbowBackUpperAngle, -360.0f, 360.0f );
				EditorUtil.GUI.Slider( "Automatic Elbow Back Lower Angle", ref settings.limbIK.automaticElbowBackLowerAngle, -360.0f, 360.0f );

				GUILayout.Label( "Arm elbow limit angle(Automatic, Manual)", _guiStyle_section );
				EditorUtil.GUI.Slider( "Elbow Front Inner Limit Angle", ref settings.limbIK.elbowFrontInnerLimitAngle, 0.0f, 90.0f );
				EditorUtil.GUI.Slider( "Elbow Back Inner Limit Angle", ref settings.limbIK.elbowBackInnerLimitAngle, 0.0f, 90.0f );

				GUILayout.Label( "Wrist limit", _guiStyle_section );
				EditorUtil.GUI.Field( "Wrist Limit Enabled", ref settings.limbIK.wristLimitEnabled );
				EditorUtil.GUI.Slider( "Wrist Limit Angle", ref settings.limbIK.wristLimitAngle, 0.0f, 180.0f );

				GUILayout.Label( "Foot limit", _guiStyle_section );
				EditorUtil.GUI.Slider( "Foot Limit Yaw", ref settings.limbIK.footLimitYaw, 0.0f, 89.99f );
				EditorUtil.GUI.Slider( "Foot Limit Pitch Up", ref settings.limbIK.footLimitPitchUp, 0.0f, 89.99f );
				EditorUtil.GUI.Slider( "Foot Limit Pitch Down", ref settings.limbIK.footLimitPitchDown, 0.0f, 89.99f );
				EditorUtil.GUI.Slider( "Foot Limit Roll", ref settings.limbIK.footLimitRoll, 0.0f, 89.99f );

				_Header( "HeadIK" );
				EditorUtil.GUI.Slider( "Neck Limit Pitch Up", ref settings.headIK.neckLimitPitchUp, 0.0f, 89.99f );
				EditorUtil.GUI.Slider( "Neck Limit Pitch Down", ref settings.headIK.neckLimitPitchDown, 0.0f, 89.99f );
				EditorUtil.GUI.Slider( "Neck Limit Roll", ref settings.headIK.neckLimitRoll, 0.0f, 89.99f );
				EditorUtil.GUI.Slider( "Eyes To Neck Pitch Rate", ref settings.headIK.eyesToNeckPitchRate, 0.0f, 1.0f );
				EditorUtil.GUI.Slider( "Head Limit Yaw", ref settings.headIK.headLimitYaw, 0.0f, 89.99f );
				EditorUtil.GUI.Slider( "Head Limit Pitch Up", ref settings.headIK.headLimitPitchUp, 0.0f, 89.99f );
				EditorUtil.GUI.Slider( "Head Limit Pitch Down", ref settings.headIK.headLimitPitchDown, 0.0f, 89.99f );
				EditorUtil.GUI.Slider( "Head Limit Roll", ref settings.headIK.headLimitRoll, 0.0f, 89.99f );
				EditorUtil.GUI.Slider( "Eyes To Head Yaw Rate", ref settings.headIK.eyesToHeadYawRate, 0.0f, 1.0f );
				EditorUtil.GUI.Slider( "Eyes To Head Pitch Rate", ref settings.headIK.eyesToHeadPitchRate, 0.0f, 1.0f );
				EditorUtil.GUI.Slider( "Eyes Trace Angle", ref settings.headIK.eyesTraceAngle, 90.0f, 180.0f );
			}

#if SAFULLBODYIK_DEBUG
			EditorGUILayout.Separator();
			if( internalValues == null || internalValues.debugData.debugValues.Count == 0 ) {
				EditorGUILayout.LabelField( "No debug properties." );
			} else {
				foreach( var debugValue in internalValues.debugData.debugValues ) {
					var v = debugValue.Value;
					switch( debugValue.Value.valueType ) {
					case FullBodyIK.DebugValueType.Int:
						v.intValue = EditorGUILayout.IntField( debugValue.Key, debugValue.Value.intValue );
						break;
					case FullBodyIK.DebugValueType.Float:
						if( debugValue.Key.Contains( "Rate" ) ) {
							v.floatValue = EditorGUILayout.Slider( debugValue.Key, debugValue.Value.floatValue, 0.0f, 1.0f );
						} else {
							v.floatValue = EditorGUILayout.FloatField( debugValue.Key, debugValue.Value.floatValue );
						}
						break;
					case FullBodyIK.DebugValueType.Bool:
						v.boolValue = EditorGUILayout.Toggle( debugValue.Key, debugValue.Value.boolValue );
						break;
					}
				}
			}
#endif
		}

		void _OnInspectorGUI_Bones()
		{
			var fbik = this.target as FullBodyIKBehaviourBase;
			var bodyBones = fbik.fullBodyIK.bodyBones;
			var headBones = fbik.fullBodyIK.headBones;
			var leftLegBones = fbik.fullBodyIK.leftLegBones;
			var rightLegBones = fbik.fullBodyIK.rightLegBones;
			var leftArmBones = fbik.fullBodyIK.leftArmBones;
			var rightArmBones = fbik.fullBodyIK.rightArmBones;
			var leftHandFingersBones = fbik.fullBodyIK.leftHandFingersBones;
			var rightHandFingersBones = fbik.fullBodyIK.rightHandFingersBones;

			_scrollViewPos_Bones = EditorGUILayout.BeginScrollView( _scrollViewPos_Bones );

			Animator animator = fbik.gameObject.GetComponent<Animator>();
			bool isAnimatorHumanoid = (animator != null) ? animator.isHuman : false;

			_Header( "Tool" );
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			EditorUtil.GUI.PushEnabled( !Application.isPlaying && isAnimatorHumanoid );
			if( GUILayout.Button( "Configure from Humanoid" ) ) {
				_ConfigureHumanoidBones();
			}
			EditorUtil.GUI.PopEnabled();
			EditorUtil.GUI.PushEnabled( !Application.isPlaying );
			if( GUILayout.Button( "Reset" ) ) {
				_ResetBones();
			}
			EditorUtil.GUI.PopEnabled();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Separator();
			EditorGUILayout.LabelField( "* is Optional." );

			EditorGUILayout.Separator();
			_Header( "Body" );

			_BoneField( "Hips", ref bodyBones.hips, false );
			_BoneField( "Spine", ref bodyBones.spine, false );
			_BoneField( "Spine 2", ref bodyBones.spine2, true );
			_BoneField( "Spine 3", ref bodyBones.spine3, true );
			_BoneField( "Spine 4", ref bodyBones.spine4, true );

			EditorGUILayout.Separator();
			_Header( "Head" );

			_BoneField( "Neck", ref headBones.neck, false );
			_BoneField( "Head", ref headBones.head, true );
			_BoneField( "Left Eye", ref headBones.leftEye, true );
			_BoneField( "Right Eye", ref headBones.rightEye, true );

			for( int i = 0; i < 2; ++i ) {
				FullBodyIK.LegBones legBones = (i == 0) ? leftLegBones : rightLegBones;

				EditorGUILayout.Separator();
				_Header( (i == 0) ? "Left Leg" : "Right Leg" );
				string prefix = (i == 0) ? "L " : "R ";

				_BoneField( prefix + "Leg", ref legBones.leg, false );
				_BoneField( prefix + "Knee", ref legBones.knee, false );
				_BoneField( prefix + "Foot", ref legBones.foot, false );
			}

			for( int i = 0; i < 2; ++i ) {
				FullBodyIK.ArmBones armBones = (i == 0) ? leftArmBones : rightArmBones;

				EditorGUILayout.Separator();
				_Header( (i == 0) ? "Left Arm" : "Right Arm" );
				string prefix = (i == 0) ? "L " : "R ";

				_BoneField( prefix + "Shoulder", ref armBones.shoulder, true );
				_BoneField( prefix + "Arm", ref armBones.arm, false );
				if( armBones.armRoll != null ) {
					for( int n = 0; n < armBones.armRoll.Length; ++n ) {
						_BoneField( prefix + "ArmRoll", ref armBones.armRoll[n], true );
					}
				}
				_BoneField( prefix + "Elbow", ref armBones.elbow, false );
				if( armBones.elbowRoll != null ) {
					for( int n = 0; n < armBones.elbowRoll.Length; ++n ) {
						_BoneField( prefix + "ElbowRoll", ref armBones.elbowRoll[n], true );
					}
				}
				_BoneField( prefix + "Wrist", ref armBones.wrist, false );
			}

			for( int i = 0; i < 2; ++i ) {
				FullBodyIK.FingersBones fingerBones = (i == 0) ? leftHandFingersBones : rightHandFingersBones;

				EditorGUILayout.Separator();
				_Header( (i == 0) ? "Left Fingers" : "Right Fingers" );
				string prefix = (i == 0) ? "L " : "R ";

				_FingerBoneField( prefix + "Thumb", ref fingerBones.thumb, true );
				EditorGUILayout.Separator();
				_FingerBoneField( prefix + "Index", ref fingerBones.index, true );
				EditorGUILayout.Separator();
				_FingerBoneField( prefix + "Middle", ref fingerBones.middle, true );
				EditorGUILayout.Separator();
				_FingerBoneField( prefix + "Ring", ref fingerBones.ring, true );
				EditorGUILayout.Separator();
				_FingerBoneField( prefix + "Little", ref fingerBones.little, true );
				EditorGUILayout.Separator();
			}

			EditorGUILayout.EndScrollView();
		}

		void _OnInspectorGUI_Effectors()
		{
			var fbik = this.target as FullBodyIKBehaviourBase;
			var editorSettings = fbik.fullBodyIK.editorSettings;
			var bodyEffectors = fbik.fullBodyIK.bodyEffectors;
			var headEffectors = fbik.fullBodyIK.headEffectors;
			var leftLegEffectors = fbik.fullBodyIK.leftLegEffectors;
			var rightLegEffectors = fbik.fullBodyIK.rightLegEffectors;
			var leftArmEffectors = fbik.fullBodyIK.leftArmEffectors;
			var rightArmEffectors = fbik.fullBodyIK.rightArmEffectors;
			var leftHandFingersEffectors = fbik.fullBodyIK.leftHandFingersEffectors;
			var rightHandFingersEffectors = fbik.fullBodyIK.rightHandFingersEffectors;

			_Header( "Tool" );
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			EditorUtil.GUI.PushEnabled( !Application.isPlaying );
			if( GUILayout.Button( "Prepare Transforms" ) ) {
				_PrepareEffectorTransforms();
			}
			if( GUILayout.Button( "Reset" ) ) {
				_ResetEffectorTransforms();
			}
			EditorUtil.GUI.PopEnabled();
			GUILayout.EndHorizontal();

			_Header( "List" );

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			EditorUtil.GUI.ToggleLegacy( fbik, "Show Transforms", ref editorSettings.isShowEffectorTransform );
			GUILayout.EndHorizontal();

			EditorGUILayout.Separator();

			_scrollViewPos_Effectors = EditorGUILayout.BeginScrollView( _scrollViewPos_Effectors );

			EditorGUILayout.Separator();
			_Header( "Body" );
			_EffectorField( "Hips", ref bodyEffectors.hips );

			EditorGUILayout.Separator();
			_Header( "Head" );
			_EffectorField( "Neck", ref headEffectors.neck );
			_EffectorField( "Head", ref headEffectors.head );
			_EffectorField( "Eyes", ref headEffectors.eyes );

			for( int i = 0; i < 2; ++i ) {
				EditorGUILayout.Separator();
				_Header( (i == 0) ? "Left Leg" : "Right Leg" );
				var effectors = (i == 0) ? leftLegEffectors : rightLegEffectors;
				var prefix = (i == 0) ? "L " : "R ";
				_EffectorField( prefix + "Knee", ref effectors.knee );
				_EffectorField( prefix + "Foot", ref effectors.foot );
			}

			for( int i = 0; i < 2; ++i ) {
				EditorGUILayout.Separator();
				_Header( (i == 0) ? "Left Arm" : "Right Arm" );
				var effectors = (i == 0) ? leftArmEffectors : rightArmEffectors;
				var prefix = (i == 0) ? "L " : "R ";
				_EffectorField( prefix + "Arm", ref effectors.arm );
				_EffectorField( prefix + "Elbow", ref effectors.elbow );
				_EffectorField( prefix + "Wrist", ref effectors.wrist );
			}

			for( int i = 0; i < 2; ++i ) {
				EditorGUILayout.Separator();
				_Header( (i == 0) ? "Left Wrist Fingers" : "Right Wrist Fingers" );
				var effectors = (i == 0) ? leftHandFingersEffectors : rightHandFingersEffectors;
				var prefix = (i == 0) ? "L " : "R ";
				_EffectorField( prefix + "Thumb", ref effectors.thumb );
				_EffectorField( prefix + "Index", ref effectors.index );
				_EffectorField( prefix + "Middle", ref effectors.middle );
				_EffectorField( prefix + "Ring", ref effectors.ring );
				_EffectorField( prefix + "Little", ref effectors.little );
			}

			EditorGUILayout.EndScrollView();
		}

		void _ConfigureHumanoidBones()
		{
		}

		void _ResetBones()
		{
		}

		void _PrepareEffectorTransforms()
		{
		}

		void _ResetEffectorTransforms()
		{
		}
	}

	[CustomEditor( typeof( SA.FullBodyIKBehaviour ) )]
	public class FullBodyIKInspector : FullBodyIKInspectorBase
	{
	}
}