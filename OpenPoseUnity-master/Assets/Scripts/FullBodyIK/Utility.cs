// Copyright (c) 2016 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

#if SAFULLBODYIK_DEBUG
//#define SAFULLBODYIK_DEBUG_CHECKEVAL
#endif

using UnityEngine;

namespace SA
{

	public partial class FullBodyIK
	{
		public static void SafeNew<TYPE_>( ref TYPE_ obj )
			where TYPE_ : class, new()
		{
			if( obj == null ) {
				obj = new TYPE_();
			}
		}

		public static void SafeResize<TYPE_>( ref TYPE_[] objArray, int length )
		{
			if( objArray == null ) {
				objArray = new TYPE_[length];
			} else {
				System.Array.Resize( ref objArray, length );
			}
		}

		public static void PrepareArray< TypeA, TypeB >( ref TypeA[] dstArray, TypeB[] srcArray )
		{
			if( srcArray != null ) {
				if( dstArray == null || dstArray.Length != srcArray.Length ) {
					dstArray = new TypeA[srcArray.Length];
				}
			} else {
				dstArray = null;
			}
		}

		public static void CloneArray< Type >( ref Type[] dstArray, Type[] srcArray )
		{
			if( srcArray != null ) {
				if( dstArray == null || dstArray.Length != srcArray.Length ) {
					dstArray = new Type[srcArray.Length];
				}
				for( int i = 0; i < srcArray.Length; ++i ) {
					dstArray[i] = srcArray[i];
				}
			} else {
				dstArray = null;
			}
		}
		
		public static void DestroyImmediate( ref Transform transform )
		{
			if( transform != null ) {
				Object.DestroyImmediate( transform.gameObject );
				transform = null;
			} else {
				transform = null; // Optimized. Because Object is weak reference.
			}
		}
		
		public static void DestroyImmediate( ref Transform transform, bool allowDestroyingAssets )
		{
			if( transform != null ) {
				Object.DestroyImmediate( transform.gameObject, allowDestroyingAssets );
				transform = null;
			} else {
				transform = null; // Optimized. Because Object is weak reference.
			}
		}
		
		public static bool CheckAlive< Type >( ref Type obj )
			where Type : UnityEngine.Object
		{
			if( obj != null ) {
				return true;
			} else {
				obj = null; // Optimized. Because Object is weak reference.
				return false;
			}
		}

		public static bool IsParentOfRecusively( Transform parent, Transform child )
		{
			while( child != null ) {
				if( child.parent == parent ) {
					return true;
				}

				child = child.parent;
			}

			return false;
		}

		//----------------------------------------------------------------------------------------------------------------

		static Bone _PrepareBone( Bone bone )
		{
			return (bone != null && bone.transformIsAlive) ? bone : null;
		}

		static Bone[] _PrepareBones( Bone leftBone, Bone rightBone )
		{
			Assert( leftBone != null && rightBone != null );
			if( leftBone != null && rightBone != null ) {
				if( leftBone.transformIsAlive && rightBone.transformIsAlive ) {
					var bones = new Bone[2];
					bones[0] = leftBone;
					bones[1] = rightBone;
					return bones;
				}
			}

			return null;
		}

		//----------------------------------------------------------------------------------------------------------------

		static bool _ComputeEyesRange( ref Vector3 eyesDir, float rangeTheta )
		{
			if( rangeTheta >= -IKEpsilon ) { // range
				if( eyesDir.z < 0.0f ) {
					eyesDir.z = -eyesDir.z;
                }

				return true;
			} else if( rangeTheta >= -1.0f + IKEpsilon ) {
				float shiftZ = -rangeTheta;
				eyesDir.z = (eyesDir.z + shiftZ);
				if( eyesDir.z < 0.0f ) {
					eyesDir.z *= 1.0f / (1.0f - shiftZ);
				} else {
					eyesDir.z *= 1.0f / (1.0f + shiftZ);
				}

				float xyLen = SAFBIKSqrt( eyesDir.x * eyesDir.x + eyesDir.y * eyesDir.y );
				if( xyLen > FLOAT_EPSILON ) {
					float xyLenTo = SAFBIKSqrt( 1.0f - eyesDir.z * eyesDir.z );
					float xyLenScale = xyLenTo / xyLen;
					eyesDir.x *= xyLenScale;
					eyesDir.y *= xyLenScale;
					return true;
				} else {
					eyesDir.x = 0.0f;
					eyesDir.y = 0.0f;
					eyesDir.z = 1.0f;
					return false;
				}
			} else {
				return true;
			}
		}

		//----------------------------------------------------------------------------------------------------------------

		public static string _GetAvatarName( Transform rootTransform )
		{
			if( rootTransform != null ) {
				var animator = rootTransform.GetComponent<Animator>();
				if( animator != null ) {
					var avatar = animator.avatar;
					if( avatar != null ) {
						return avatar.name;
					}
				}
			}

			return null;
		}

		//----------------------------------------------------------------------------------------------------------------

#if SAFULLBODYIK_DEBUG
		public enum DebugValueType
		{
			Int,
			Float,
			Bool,
		}

		public class DebugValue
		{
			public DebugValueType valueType;

			public int intValue;
			public float floatValue;
			public bool boolValue;

			public DebugValue( int i )
			{
				this.valueType = DebugValueType.Int;
				this.intValue = i;
				this.floatValue = (float)i;
				this.boolValue = (i != 0);
			}

			public DebugValue( float f )
			{
				this.valueType = DebugValueType.Float;
				this.intValue = (int)f;
				this.floatValue = f;
				this.boolValue = (f != 0.0f);
			}

			public DebugValue( bool b )
			{
				this.valueType = DebugValueType.Bool;
				this.intValue = b ? 1 : 0;
				this.floatValue = b ? 1.0f : 0.0f;
				this.boolValue = b;
			}
		}

		public struct DebugPoint
		{
			public Vector3 pos;
			public Color color;
			public float size;

			public DebugPoint( Vector3 pos )
			{
				this.pos = pos;
				this.color = Color.red;
				this.size = 0.03f;
			}

			public DebugPoint( Vector3 pos, Color color )
			{
				this.pos = pos;
				this.color = color;
				this.size = 0.03f;
			}

			public DebugPoint( Vector3 pos, Color color, float size )
			{
				this.pos = pos;
				this.color = color;
				this.size = size;
			}
		}

		public class DebugData
		{
			public System.Collections.Generic.List<DebugPoint> debugPoints = new System.Collections.Generic.List<DebugPoint>();
			public System.Collections.Generic.Dictionary<string, DebugValue> debugValues = new System.Collections.Generic.Dictionary<string, DebugValue>();

			public void UpdateValue( string name, ref int v )
			{
				DebugValue debugValue;
				if( this.debugValues.TryGetValue( name, out debugValue ) ) {
					v = debugValue.intValue;
					return;
				}

				this.debugValues.Add( name, new DebugValue( v ) );
			}

			public void UpdateValue( string name, ref float v )
			{
				DebugValue debugValue;
				if( this.debugValues.TryGetValue( name, out debugValue ) ) {
					v = debugValue.floatValue;
					return;
				}

				this.debugValues.Add( name, new DebugValue( v ) );
			}

			public void UpdateValue( string name, ref bool v )
			{
				DebugValue debugValue;
				if( this.debugValues.TryGetValue( name, out debugValue ) ) {
					v = debugValue.boolValue;
					return;
				}

				this.debugValues.Add( name, new DebugValue( v ) );
			}
		}

#endif

		//----------------------------------------------------------------------------------------------------------------

		[System.Diagnostics.Conditional( "SAFULLBODYIK_DEBUG" )]
		public static void DebugLog( object msg )
		{
			Debug.Log( msg );
		}

		[System.Diagnostics.Conditional( "SAFULLBODYIK_DEBUG" )]
		public static void DebugLogWarning( object msg )
		{
			Debug.LogWarning( msg );
		}

		[System.Diagnostics.Conditional( "SAFULLBODYIK_DEBUG" )]
		public static void DebugLogError( object msg )
		{
			Debug.LogError( msg );
		}

		[System.Diagnostics.Conditional( "SAFULLBODYIK_DEBUG" )]
		public static void Assert( bool cmp )
		{
			if( !cmp ) {
				Debug.LogError( "Assert" );
				Debug.Break();
			}
		}

		//----------------------------------------------------------------------------------------------------------------

		[System.Diagnostics.Conditional( "SAFULLBODYIK_DEBUG_CHECKEVAL" )]
		public static void CheckNormalized( Vector3 v )
		{
			float epsilon = 1e-4f;
			float n = v.x * v.x + v.y * v.y + v.z * v.z;
			if( n < 1.0f - epsilon || n > 1.0f + epsilon ) {
				Debug.LogError( "CheckNormalized:" + n.ToString( "F6" ) );
				Debug.Break();
			}
		}

		[System.Diagnostics.Conditional( "SAFULLBODYIK_DEBUG_CHECKEVAL" )]
		public static void CheckNaN( float f )
		{
			if( float.IsNaN( f ) ) {
				Debug.LogError( "NaN" );
			}
		}

		[System.Diagnostics.Conditional( "SAFULLBODYIK_DEBUG_CHECKEVAL" )]
		public static void CheckNaN( Vector3 v )
		{
			if( float.IsNaN( v.x ) || float.IsNaN( v.y ) || float.IsNaN( v.z ) ) {
				Debug.LogError( "NaN:" + v );
			}
		}

		[System.Diagnostics.Conditional( "SAFULLBODYIK_DEBUG_CHECKEVAL" )]
		public static void CheckNaN( Quaternion q )
		{
			if( float.IsNaN( q.x ) || float.IsNaN( q.y ) || float.IsNaN( q.z ) || float.IsNaN( q.w ) ) {
				Debug.LogError( "NaN:" + q );
			}
		}
	}

}