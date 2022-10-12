// Copyright (c) 2016 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

#if SAFULLBODYIK_DEBUG // Currentry Debug Only
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#define SAFULLBODYIK_NATIVEPLUGIN
#endif
#endif

using UnityEngine;
using System.Runtime.InteropServices;

namespace SA
{
	public partial class FullBodyIK
	{
		[System.Serializable]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct Float2
		{
			[MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
			public float[] v;
		}

		[System.Serializable]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct Matrix3x3
		{
			public Vector3 column0, column1, column2;

			public static readonly Matrix3x3 identity = new Matrix3x3(
				1.0f, 0.0f, 0.0f,
				0.0f, 1.0f, 0.0f,
				0.0f, 0.0f, 1.0f );

			public Vector3 row0 { get { return new Vector3( column0.x, column1.x, column2.x ); } }
			public Vector3 row1 { get { return new Vector3( column0.y, column1.y, column2.y ); } }
			public Vector3 row2 { get { return new Vector3( column0.z, column1.z, column2.z ); } }

			public bool isFuzzyIdentity
			{
				get
				{
					if( column0.x < 1.0f - IKEpsilon || column1.x < -IKEpsilon || column2.x < -IKEpsilon ||
						column0.y < -IKEpsilon || column1.y < 1.0f - IKEpsilon || column2.y < -IKEpsilon ||
						column0.z < -IKEpsilon || column1.z < -IKEpsilon || column2.z < 1.0f - IKEpsilon ||
						column0.x > 1.0f + IKEpsilon || column1.x > IKEpsilon || column2.x > IKEpsilon ||
						column0.y > IKEpsilon || column1.y > 1.0f + IKEpsilon || column2.y > IKEpsilon ||
						column0.z > IKEpsilon || column1.z > IKEpsilon || column2.z > 1.0f + IKEpsilon ) {
						return false;
					}

					return true;
				}
			}

			public Matrix3x3 transpose
			{
				get
				{
					return new Matrix3x3(
						column0.x, column0.y, column0.z,
						column1.x, column1.y, column1.z,
						column2.x, column2.y, column2.z );
				}
			}

			public Matrix3x3(
				float _11, float _12, float _13,
				float _21, float _22, float _23,
				float _31, float _32, float _33 )
			{
				column0 = new Vector3( _11, _21, _31 );
				column1 = new Vector3( _12, _22, _32 );
				column2 = new Vector3( _13, _23, _33 );
			}

			public Matrix3x3( Vector3 axis, float cos )
			{
				SAFBIKMatSetAxisAngle( out this, ref axis, cos );
			}

			public Matrix3x3( ref Vector3 axis, float cos )
			{
				SAFBIKMatSetAxisAngle( out this, ref axis, cos );
			}

			public Matrix3x3( Matrix4x4 m )
			{
				column0 = new Vector3( m.m00, m.m10, m.m20 );
				column1 = new Vector3( m.m01, m.m11, m.m21 );
				column2 = new Vector3( m.m02, m.m12, m.m22 );
			}

			public Matrix3x3( ref Matrix4x4 m )
			{
				column0 = new Vector3( m.m00, m.m10, m.m20 );
				column1 = new Vector3( m.m01, m.m11, m.m21 );
				column2 = new Vector3( m.m02, m.m12, m.m22 );
			}

			public Matrix3x3( Quaternion q )
			{
				SAFBIKMatSetRot( out this, ref q );
			}

			public Matrix3x3( ref Quaternion q )
			{
				SAFBIKMatSetRot( out this, ref q );
			}

			public static Matrix3x3 FromColumn( Vector3 column0, Vector3 column1, Vector3 column2 )
			{
				Matrix3x3 r = new Matrix3x3();
				r.SetColumn( ref column0, ref column1, ref column2 );
				return r;
			}

			public static Matrix3x3 FromColumn( ref Vector3 column0, ref Vector3 column1, ref Vector3 column2 )
			{
				Matrix3x3 r = new Matrix3x3();
				r.SetColumn( ref column0, ref column1, ref column2 );
				return r;
			}

			public void SetValue(
				float _11, float _12, float _13,
				float _21, float _22, float _23,
				float _31, float _32, float _33 )
			{
				column0.x = _11; column1.x = _12; column2.x = _13;
				column0.y = _21; column1.y = _22; column2.y = _23;
				column0.z = _31; column1.z = _32; column2.z = _33;
			}

			public void SetValue( Matrix4x4 m )
			{
				column0.x = m.m00;
				column1.x = m.m01;
				column2.x = m.m02;

				column0.y = m.m10;
				column1.y = m.m11;
				column2.y = m.m12;

				column0.z = m.m20;
				column1.z = m.m21;
				column2.z = m.m22;
			}

			public void SetValue( ref Matrix4x4 m )
			{
				column0.x = m.m00;
				column1.x = m.m01;
				column2.x = m.m02;

				column0.y = m.m10;
				column1.y = m.m11;
				column2.y = m.m12;

				column0.z = m.m20;
				column1.z = m.m21;
				column2.z = m.m22;
			}

			public void SetColumn( Vector3 c0, Vector3 c1, Vector3 c2 )
			{
				column0 = c0;
				column1 = c1;
				column2 = c2;
			}

			public void SetColumn( ref Vector3 c0, ref Vector3 c1, ref Vector3 c2 )
			{
				column0 = c0;
				column1 = c1;
				column2 = c2;
			}

			public static implicit operator Matrix4x4( Matrix3x3 m )
			{
				Matrix4x4 r = Matrix4x4.identity;
				r.m00 = m.column0.x;
				r.m01 = m.column1.x;
				r.m02 = m.column2.x;

				r.m10 = m.column0.y;
				r.m11 = m.column1.y;
				r.m12 = m.column2.y;

				r.m20 = m.column0.z;
				r.m21 = m.column1.z;
				r.m22 = m.column2.z;
				return r;
			}

			public static implicit operator Matrix3x3( Matrix4x4 m )
			{
				return new Matrix3x3( ref m );
			}

			public override string ToString()
			{
				System.Text.StringBuilder str = new System.Text.StringBuilder();
				str.Append( row0.ToString() );
				str.Append( " : " );
				str.Append( row1.ToString() );
				str.Append( " : " );
				str.Append( row2.ToString() );
				return str.ToString();
			}

			public string ToString( string format )
			{
				System.Text.StringBuilder str = new System.Text.StringBuilder();
				str.Append( row0.ToString( format ) );
				str.Append( " : " );
				str.Append( row1.ToString( format ) );
				str.Append( " : " );
				str.Append( row2.ToString( format ) );
				return str.ToString();
			}

			public string ToStringColumn()
			{
				System.Text.StringBuilder str = new System.Text.StringBuilder();
				str.Append( column0.ToString() );
				str.Append( "(" );
				str.Append( column0.magnitude );
				str.Append( ") : " );
				str.Append( column1.ToString() );
				str.Append( "(" );
				str.Append( column1.magnitude );
				str.Append( ") : " );
				str.Append( column2.ToString() );
				str.Append( "(" );
				str.Append( column2.magnitude );
				str.Append( ")" );
				return str.ToString();
			}

			public string ToStringColumn( string format )
			{
				System.Text.StringBuilder str = new System.Text.StringBuilder();
				str.Append( column0.ToString( format ) );
				str.Append( "(" );
				str.Append( column0.magnitude );
				str.Append( ") : " );
				str.Append( column1.ToString( format ) );
				str.Append( "(" );
				str.Append( column1.magnitude );
				str.Append( ") : " );
				str.Append( column2.ToString( format ) );
				str.Append( "(" );
				str.Append( column2.magnitude );
				str.Append( ")" );
				return str.ToString();
			}

			public bool Normalize()
			{
				float n0 = SAFBIKSqrt( column0.x * column0.x + column0.y * column0.y + column0.z * column0.z );
				float n1 = SAFBIKSqrt( column1.x * column1.x + column1.y * column1.y + column1.z * column1.z );
				float n2 = SAFBIKSqrt( column2.x * column2.x + column2.y * column2.y + column2.z * column2.z );

				bool valid = true;

				if( n0 > IKEpsilon ) {
					n0 = 1.0f / n0;
					column0.x *= n0;
					column0.y *= n0;
					column0.z *= n0;
				} else {
					valid = false;
					column0.x = 1.0f;
					column0.y = 0.0f;
					column0.z = 0.0f;
				}

				if( n1 > IKEpsilon ) {
					n1 = 1.0f / n1;
					column1.x *= n1;
					column1.y *= n1;
					column1.z *= n1;
				} else {
					valid = false;
					column1.x = 0.0f;
					column1.y = 1.0f;
					column1.z = 0.0f;
				}

				if( n2 > IKEpsilon ) {
					n2 = 1.0f / n2;
					column2.x *= n2;
					column2.y *= n2;
					column2.z *= n2;
				} else {
					valid = false;
					column2.x = 0.0f;
					column2.y = 0.0f;
					column2.z = 1.0f;
				}

				return valid;
			}
		}

		[System.Serializable]
		public struct Matrix3x4
		{
			public Matrix3x3 basis;
			public Vector3 origin;

			public static readonly Matrix3x4 identity = new Matrix3x4( Matrix3x3.identity, Vector3.zero );

			public Matrix3x4 inverse
			{
				get
				{
					Matrix3x3 basis_inv = basis.transpose;
					Vector3 origin_minus = -origin;
					Vector3 tmp;
					SAFBIKMatMultVec( out tmp, ref basis_inv, ref origin_minus );
					return new Matrix3x4( ref basis_inv, ref tmp );
				}
			}

			public Matrix3x4( Matrix3x3 _basis, Vector3 _origin )
			{
				basis = _basis;
				origin = _origin;
			}

			public Matrix3x4( ref Matrix3x3 _basis, ref Vector3 _origin )
			{
				basis = _basis;
				origin = _origin;
			}

			public Matrix3x4( Quaternion _q, Vector3 _origin )
			{
				SAFBIKMatSetRot( out basis, ref _q );
				origin = _origin;
			}

			public Matrix3x4( ref Quaternion _q, ref Vector3 _origin )
			{
				SAFBIKMatSetRot( out basis, ref _q );
				origin = _origin;
			}

			public Matrix3x4( Matrix4x4 m )
			{
				basis = new Matrix3x3( ref m );
				origin = new Vector3( m.m03, m.m13, m.m23 );
			}

			public Matrix3x4( ref Matrix4x4 m )
			{
				basis = new Matrix3x3( ref m );
				origin = new Vector3( m.m03, m.m13, m.m23 );
			}

			public static implicit operator Matrix4x4( Matrix3x4 t )
			{
				Matrix4x4 m = Matrix4x4.identity;
				m.m00 = t.basis.column0.x;
				m.m01 = t.basis.column1.x;
				m.m02 = t.basis.column2.x;

				m.m10 = t.basis.column0.y;
				m.m11 = t.basis.column1.y;
				m.m12 = t.basis.column2.y;

				m.m20 = t.basis.column0.z;
				m.m21 = t.basis.column1.z;
				m.m22 = t.basis.column2.z;

				m.m03 = t.origin.x;
				m.m13 = t.origin.y;
				m.m23 = t.origin.z;
				return m;
			}

			public static implicit operator Matrix3x4( Matrix4x4 m )
			{
				return new Matrix3x4( ref m );
			}

			public Vector3 Multiply( Vector3 v )
			{
				Vector3 tmp;
				SAFBIKMatMultVecAdd( out tmp, ref basis, ref v, ref origin );
				return tmp;
			}

			public Vector3 Multiply( ref Vector3 v )
			{
				Vector3 tmp;
				SAFBIKMatMultVecAdd( out tmp, ref basis, ref v, ref origin );
				return tmp;
			}

			public static Vector3 operator *( Matrix3x4 t, Vector3 v )
			{
				Vector3 tmp;
				SAFBIKMatMultVecAdd( out tmp, ref t.basis, ref v, ref t.origin );
				return tmp;
			}

			public Matrix3x4 Multiply( Matrix3x4 t )
			{
				Matrix3x3 tmp_basis;
				Vector3 tmp_origin;
				SAFBIKMatMult( out tmp_basis, ref basis, ref t.basis );
				SAFBIKMatMultVecAdd( out tmp_origin, ref basis, ref t.origin, ref origin );
                return new Matrix3x4( ref tmp_basis, ref tmp_origin );
			}

			public Matrix3x4 Multiply( ref Matrix3x4 t )
			{
				Matrix3x3 tmp_basis;
				Vector3 tmp_origin;
				SAFBIKMatMult( out tmp_basis, ref basis, ref t.basis );
				SAFBIKMatMultVecAdd( out tmp_origin, ref basis, ref t.origin, ref origin );
				return new Matrix3x4( ref tmp_basis, ref tmp_origin );
			}

			public static Matrix3x4 operator *( Matrix3x4 t1, Matrix3x4 t2 )
			{
				Matrix3x3 tmp_basis;
				Vector3 tmp_origin;
				SAFBIKMatMult( out tmp_basis, ref t1.basis, ref t2.basis );
				SAFBIKMatMultVecAdd( out tmp_origin, ref t1.basis, ref t2.origin, ref t1.origin );
				return new Matrix3x4( ref tmp_basis, ref tmp_origin );
			}

			public override string ToString()
			{
				System.Text.StringBuilder str = new System.Text.StringBuilder();
				str.Append( "basis: " );
				str.Append( basis.ToString() );
				str.Append( " origin: " );
				str.Append( origin.ToString() );
				return str.ToString();
			}

			public string ToString( string format )
			{
				System.Text.StringBuilder str = new System.Text.StringBuilder();
				str.Append( "basis: " );
				str.Append( basis.ToString( format ) );
				str.Append( " origin: " );
				str.Append( origin.ToString( format ) );
				return str.ToString();
			}
		}

		//--------------------------------------------------------------------------------------------------------------------

#if SAFULLBODYIK_NATIVEPLUGIN
		const string PluginName = "SAFullBodyIKPlugin";

		[DllImport( PluginName )]
		public static extern float SAFBIKSqrt( float a );
		[DllImport( PluginName )]
		public static extern float SAFBIKSqrtClamp01( float a );
		[DllImport( PluginName )]
		public static extern float SAFBIKSin( float a );
		[DllImport( PluginName )]
		public static extern float SAFBIKCos( float a );
		[DllImport( PluginName )]
		public static extern void  SAFBIKCosSin( out float cos, out float sin, float a );
		[DllImport( PluginName )]
		public static extern float SAFBIKTan( float a );
		[DllImport( PluginName )]
		public static extern float SAFBIKAcos( float cos );
		[DllImport( PluginName )]
		public static extern float SAFBIKAsin( float sin );

		[DllImport( PluginName )]
		public static extern void SAFBIKVecCross( out Vector3 ret, ref Vector3 lhs, ref Vector3 rhs );

		[DllImport( PluginName )]
		public static extern float SAFBIKVecLength( ref Vector3 v );
		[DllImport( PluginName )]
		public static extern float SAFBIKVecLengthAndLengthSq( out float lengthSq, ref Vector3 v );
		[DllImport( PluginName )]
		public static extern float SAFBIKVecLength2( ref Vector3 lhs, ref Vector3 rhs );
		[DllImport( PluginName )]
		public static extern float SAFBIKVecLengthSq2( ref Vector3 lhs, ref Vector3 rhs );
		[DllImport( PluginName )]
		public static extern float SAFBIKVecLengthAndLengthSq2( out float lengthSq, ref Vector3 lhs, ref Vector3 rhs );

		[DllImport( PluginName )]
		public static extern bool SAFBIKVecNormalize( ref Vector3 v );
		[DllImport( PluginName )]
		public static extern bool SAFBIKVecNormalizeXZ( ref Vector3 v );
		[DllImport( PluginName )]
		public static extern bool SAFBIKVecNormalizeYZ( ref Vector3 v );
		[DllImport( PluginName )]
		public static extern bool SAFBIKVecNormalize2( ref Vector3 v0, ref Vector3 v1 );
		[DllImport( PluginName )]
		public static extern bool SAFBIKVecNormalize3( ref Vector3 v0, ref Vector3 v1, ref Vector3 v2 );
		[DllImport( PluginName )]
		public static extern bool SAFBIKVecNormalize4( ref Vector3 v0, ref Vector3 v1, ref Vector3 v2, ref Vector3 v3 );

		[DllImport( PluginName )]
		public static extern void SAFBIKMatMult( out Matrix3x3 ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs );
		[DllImport( PluginName )]
		public static extern void SAFBIKMatMultRet0( ref Matrix3x3 lhs, ref Matrix3x3 rhs );

		[DllImport( PluginName )]
		public static extern void SAFBIKMatMultCol0( out Vector3 ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs );
		[DllImport( PluginName )]
		public static extern void SAFBIKMatMultCol1( out Vector3 ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs );
		[DllImport( PluginName )]
		public static extern void SAFBIKMatMultCol2( out Vector3 ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs );

		[DllImport( PluginName )]
		public static extern void SAFBIKMatMultVec( out Vector3 ret, ref Matrix3x3 mat, ref Vector3 vec );

		[DllImport( PluginName )]
		public static extern void SAFBIKMatGetRot( out Quaternion quat, ref Matrix3x3 mat );
		[DllImport( PluginName )]
		public static extern void SAFBIKMatSetRot( out Matrix3x3 mat, ref Quaternion quat );
		[DllImport( PluginName )]
		public static extern void SAFBIKMatSetAxisAngle( out Matrix3x3 mat, ref Vector3 axis, float angle );

		[DllImport( PluginName )]
		public static extern void SAFBIKMatFastLerp( out Matrix3x3 ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs, float rate );
		[DllImport( PluginName )]
		public static extern void SAFBIKMatFastLerpToIdentity( ref Matrix3x3 m, float rate );

		[DllImport( PluginName )]
		public static extern void SAFBIKMatMultVecInv( out Vector3 ret, ref Matrix3x3 mat, ref Vector3 vec );
		[DllImport( PluginName )]
		public static extern void SAFBIKMatMultVecAdd( out Vector3 ret, ref Matrix3x3 mat, ref Vector3 vec, ref Vector3 addVec );
		[DllImport( PluginName )]
		public static extern void SAFBIKMatMultVecPreSub( out Vector3 ret, ref Matrix3x3 mat, ref Vector3 vec, ref Vector3 subVec );
		[DllImport( PluginName )]
		public static extern void SAFBIKMatMultVecPreSubAdd( out Vector3 ret, ref Matrix3x3 mat, ref Vector3 vec, ref Vector3 subVec, ref Vector3 addVec );
		[DllImport( PluginName )]
		public static extern void SAFBIKMatMultInv0( out Matrix3x3 ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs );
		[DllImport( PluginName )]
		public static extern void SAFBIKMatMultInv1( out Matrix3x3 ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs );
		[DllImport( PluginName )]
		public static extern void SAFBIKMatMultGetRot( out Quaternion ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs );
		[DllImport( PluginName )]
		public static extern void SAFBIKMatSetRotMult( out Matrix3x3 ret, ref Quaternion lhs, ref Quaternion rhs );
		[DllImport( PluginName )]
		public static extern void SAFBIKMatSetRotMultInv1( out Matrix3x3 ret, ref Quaternion lhs, ref Quaternion rhs );

		[DllImport( PluginName )]
		public static extern void SAFBIKQuatMult( out Quaternion ret, ref Quaternion q0, ref Quaternion q1 );
		[DllImport( PluginName )]
		public static extern void SAFBIKQuatMultInv0( out Quaternion ret, ref Quaternion q0, ref Quaternion q1 );
		[DllImport( PluginName )]
		public static extern void SAFBIKQuatMultNorm( out Quaternion ret, ref Quaternion q0, ref Quaternion q1 );
		[DllImport( PluginName )]
		public static extern void SAFBIKQuatMultNormInv0( out Quaternion ret, ref Quaternion q0, ref Quaternion q1 );
		[DllImport( PluginName )]
		public static extern void SAFBIKQuatMult3( out Quaternion ret, ref Quaternion q0, ref Quaternion q1, ref Quaternion q2 );
		[DllImport( PluginName )]
		public static extern void SAFBIKQuatMult3Inv0( out Quaternion ret, ref Quaternion q0, ref Quaternion q1, ref Quaternion q2 );
		[DllImport( PluginName )]
		public static extern void SAFBIKQuatMult3Inv1( out Quaternion ret, ref Quaternion q0, ref Quaternion q1, ref Quaternion q2 );
		[DllImport( PluginName )]
		public static extern void SAFBIKQuatMultNorm3( out Quaternion ret, ref Quaternion q0, ref Quaternion q1, ref Quaternion q2 );
		[DllImport( PluginName )]
		public static extern void SAFBIKQuatMultNorm3Inv0( out Quaternion ret, ref Quaternion q0, ref Quaternion q1, ref Quaternion q2 );
		[DllImport( PluginName )]
		public static extern void SAFBIKQuatMultNorm3Inv1( out Quaternion ret, ref Quaternion q0, ref Quaternion q1, ref Quaternion q2 );

		[DllImport( PluginName )]
		public static extern bool SAFBIKComputeBasisFromXZLockX( out Matrix3x3 basis, ref Vector3 dirX, ref Vector3 dirZ );
		[DllImport( PluginName )]
		public static extern bool SAFBIKComputeBasisFromXYLockX( out Matrix3x3 basis, ref Vector3 dirX, ref Vector3 dirY );
		[DllImport( PluginName )]
		public static extern bool SAFBIKComputeBasisFromXYLockY( out Matrix3x3 basis, ref Vector3 dirX, ref Vector3 dirY );
		[DllImport( PluginName )]
		public static extern bool SAFBIKComputeBasisFromXZLockZ( out Matrix3x3 basis, ref Vector3 dirX, ref Vector3 dirZ );
		[DllImport( PluginName )]
		public static extern bool SAFBIKComputeBasisFromYZLockY( out Matrix3x3 basis, ref Vector3 dirY, ref Vector3 dirZ );
		[DllImport( PluginName )]
		public static extern bool SAFBIKComputeBasisFromYZLockZ( out Matrix3x3 basis, ref Vector3 dirY, ref Vector3 dirZ );
		[DllImport( PluginName )]
		public static extern bool SAFBIKComputeBasisLockX( out Matrix3x3 basis, ref Vector3 dirX, ref Vector3 dirY, ref Vector3 dirZ );
		[DllImport( PluginName )]
		public static extern bool SAFBIKComputeBasisLockY( out Matrix3x3 basis, ref Vector3 dirX, ref Vector3 dirY, ref Vector3 dirZ );
		[DllImport( PluginName )]
		public static extern bool SAFBIKComputeBasisLockZ( out Matrix3x3 basis, ref Vector3 dirX, ref Vector3 dirY, ref Vector3 dirZ );
#else
		public static float SAFBIKSqrt( float a )
		{
			CheckNaN( a );
			if( a <= FLOAT_EPSILON ) { // Counts as 0
				return 0.0f;
			}

			return (float)System.Math.Sqrt( (double)a );
		}

		public static float SAFBIKSqrtClamp01( float a )
		{
			CheckNaN( a );
			if( a <= FLOAT_EPSILON ) { // Counts as 0
				return 0.0f;
			} else if( a >= 1.0f - FLOAT_EPSILON ) {
				return 1.0f;
			}

			return (float)System.Math.Sqrt( (double)a );
		}

		public static float SAFBIKSin( float a )
		{
			CheckNaN( a );
			return (float)System.Math.Sin( (double)a );
		}

		public static float SAFBIKCos( float a )
		{
			CheckNaN( a );
			return (float)System.Math.Cos( (double)a );
		}

		public static void SAFBIKCosSin( out float cos, out float sin, float a )
		{
			CheckNaN( a );
			cos = (float)System.Math.Cos( (double)a );
			sin = (float)System.Math.Sin( (double)a );
		}

		public static float SAFBIKTan( float a )
		{
			CheckNaN( a );
			return (float)System.Math.Tan( (double)a );
		}

		public static float SAFBIKAcos( float cos )
		{
			CheckNaN( cos );
			if( cos >= 1.0f - FLOAT_EPSILON ) {
				return 0.0f;
			}
			if( cos <= -1.0f + FLOAT_EPSILON ) {
				return 180.0f * Mathf.Deg2Rad;
			}

			return (float)System.Math.Acos( (double)cos );
		}

		public static float SAFBIKAsin( float sin )
		{
			CheckNaN( sin );
			if( sin >= 1.0f - FLOAT_EPSILON ) {
				return 90.0f * Mathf.Deg2Rad;
			}
			if( sin <= -1.0f + FLOAT_EPSILON ) {
				return -90.0f * Mathf.Deg2Rad;
			}

			return (float)System.Math.Asin( (double)sin );
		}

		public static void SAFBIKVecCross( out Vector3 ret, ref Vector3 lhs, ref Vector3 rhs )
		{
			CheckNaN( lhs );
			CheckNaN( rhs );
			ret = new Vector3(
				lhs.y * rhs.z - lhs.z * rhs.y,
				lhs.z * rhs.x - lhs.x * rhs.z,
				lhs.x * rhs.y - lhs.y * rhs.x );
		}

		public static float SAFBIKVecLength( ref Vector3 v )
		{
			CheckNaN( v );
			float sq = v.x * v.x + v.y * v.y + v.z * v.z;
			if( sq > FLOAT_EPSILON ) {
				return (float)System.Math.Sqrt( (double)sq );
			}

			return 0.0f;
		}

		public static float SAFBIKVecLengthAndLengthSq( out float lengthSq, ref Vector3 v )
		{
			CheckNaN( v );
			lengthSq = v.x * v.x + v.y * v.y + v.z * v.z;
			if( lengthSq > FLOAT_EPSILON ) {
				return (float)System.Math.Sqrt( (double)lengthSq );
			}

			return 0.0f;
		}

		public static float SAFBIKVecLength2( ref Vector3 lhs, ref Vector3 rhs )
		{
			CheckNaN( lhs );
			CheckNaN( rhs );
			float rx = lhs.x - rhs.x;
			float ry = lhs.y - rhs.y;
			float rz = lhs.z - rhs.z;
			float sq = rx * rx + ry * ry + rz * rz;
			if( sq > FLOAT_EPSILON ) {
				return (float)System.Math.Sqrt( (double)sq );
			}

			return 0.0f;
		}

		public static float SAFBIKVecLengthSq2( ref Vector3 lhs, ref Vector3 rhs )
		{
			CheckNaN( lhs );
			CheckNaN( rhs );
			float rx = lhs.x - rhs.x;
			float ry = lhs.y - rhs.y;
			float rz = lhs.z - rhs.z;
			return rx * rx + ry * ry + rz * rz;
		}

		public static float SAFBIKVecLengthAndLengthSq2( out float lengthSq, ref Vector3 lhs, ref Vector3 rhs )
		{
			CheckNaN( lhs );
			CheckNaN( rhs );
			float rx = lhs.x - rhs.x;
			float ry = lhs.y - rhs.y;
			float rz = lhs.z - rhs.z;
			lengthSq = rx * rx + ry * ry + rz * rz;
			if( lengthSq > FLOAT_EPSILON ) {
				return (float)System.Math.Sqrt( (double)lengthSq );
			}

			return 0.0f;
		}

		public static bool SAFBIKVecNormalize( ref Vector3 v0 )
		{
			CheckNaN( v0 );
			float sq0 = v0.x * v0.x + v0.y * v0.y + v0.z * v0.z;
			if( sq0 > FLOAT_EPSILON ) {
				float len0 = (float)System.Math.Sqrt( (double)sq0 );
				if( len0 > IKEpsilon ) {
					len0 = 1.0f / len0;
					v0.x *= len0;
					v0.y *= len0;
					v0.z *= len0;
					return true;
				}
			}

			v0.x = v0.y = v0.z = 0.0f;
			return false;
		}

		public static bool SAFBIKVecNormalizeXZ( ref Vector3 v0 )
		{
			CheckNaN( v0 );
			float sq0 = v0.x * v0.x + v0.z * v0.z;
			if( sq0 > FLOAT_EPSILON ) {
				float len0 = (float)System.Math.Sqrt( (double)sq0 );
				if( len0 > IKEpsilon ) {
					len0 = 1.0f / len0;
					v0.x *= len0;
					v0.z *= len0;
					return true;
				}
			}

			v0.x = v0.z = 0.0f;
			return false;
		}

		public static bool SAFBIKVecNormalizeYZ( ref Vector3 v0 )
		{
			CheckNaN( v0 );
			float sq0 = v0.y * v0.y + v0.z * v0.z;
			if( sq0 > FLOAT_EPSILON ) {
				float len0 = (float)System.Math.Sqrt( (double)sq0 );
				if( len0 > IKEpsilon ) {
					len0 = 1.0f / len0;
					v0.y *= len0;
					v0.z *= len0;
					return true;
				}
			}

			v0.y = v0.z = 0.0f;
			return false;
		}

		public static bool SAFBIKVecNormalize2( ref Vector3 v0, ref Vector3 v1 )
		{
			bool r0 = SAFBIKVecNormalize( ref v0 );
			bool r1 = SAFBIKVecNormalize( ref v1 );
			return r0 && r1;
		}

		public static bool SAFBIKVecNormalize3( ref Vector3 v0, ref Vector3 v1, ref Vector3 v2 )
		{
			bool r0 = SAFBIKVecNormalize( ref v0 );
			bool r1 = SAFBIKVecNormalize( ref v1 );
			bool r2 = SAFBIKVecNormalize( ref v2 );
			return r0 && r1 && r2;
		}

		public static bool SAFBIKVecNormalize4( ref Vector3 v0, ref Vector3 v1, ref Vector3 v2, ref Vector3 v3 )
		{
			bool r0 = SAFBIKVecNormalize( ref v0 );
			bool r1 = SAFBIKVecNormalize( ref v1 );
			bool r2 = SAFBIKVecNormalize( ref v2 );
			bool r3 = SAFBIKVecNormalize( ref v3 );
			return r0 && r1 && r2 && r3;
		}

		public static void SAFBIKMatMult( out Matrix3x3 ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs )
		{
			ret = new Matrix3x3(
				lhs.column0.x * rhs.column0.x + lhs.column1.x * rhs.column0.y + lhs.column2.x * rhs.column0.z,
				lhs.column0.x * rhs.column1.x + lhs.column1.x * rhs.column1.y + lhs.column2.x * rhs.column1.z,
				lhs.column0.x * rhs.column2.x + lhs.column1.x * rhs.column2.y + lhs.column2.x * rhs.column2.z,

				lhs.column0.y * rhs.column0.x + lhs.column1.y * rhs.column0.y + lhs.column2.y * rhs.column0.z,
				lhs.column0.y * rhs.column1.x + lhs.column1.y * rhs.column1.y + lhs.column2.y * rhs.column1.z,
				lhs.column0.y * rhs.column2.x + lhs.column1.y * rhs.column2.y + lhs.column2.y * rhs.column2.z,

				lhs.column0.z * rhs.column0.x + lhs.column1.z * rhs.column0.y + lhs.column2.z * rhs.column0.z,
				lhs.column0.z * rhs.column1.x + lhs.column1.z * rhs.column1.y + lhs.column2.z * rhs.column1.z,
				lhs.column0.z * rhs.column2.x + lhs.column1.z * rhs.column2.y + lhs.column2.z * rhs.column2.z );
		}

		public static void SAFBIKMatMultRet0( ref Matrix3x3 lhs, ref Matrix3x3 rhs )
		{
			lhs = new Matrix3x3(
				lhs.column0.x * rhs.column0.x + lhs.column1.x * rhs.column0.y + lhs.column2.x * rhs.column0.z,
				lhs.column0.x * rhs.column1.x + lhs.column1.x * rhs.column1.y + lhs.column2.x * rhs.column1.z,
				lhs.column0.x * rhs.column2.x + lhs.column1.x * rhs.column2.y + lhs.column2.x * rhs.column2.z,

				lhs.column0.y * rhs.column0.x + lhs.column1.y * rhs.column0.y + lhs.column2.y * rhs.column0.z,
				lhs.column0.y * rhs.column1.x + lhs.column1.y * rhs.column1.y + lhs.column2.y * rhs.column1.z,
				lhs.column0.y * rhs.column2.x + lhs.column1.y * rhs.column2.y + lhs.column2.y * rhs.column2.z,

				lhs.column0.z * rhs.column0.x + lhs.column1.z * rhs.column0.y + lhs.column2.z * rhs.column0.z,
				lhs.column0.z * rhs.column1.x + lhs.column1.z * rhs.column1.y + lhs.column2.z * rhs.column1.z,
				lhs.column0.z * rhs.column2.x + lhs.column1.z * rhs.column2.y + lhs.column2.z * rhs.column2.z );
		}

		public static void SAFBIKMatMultCol0( out Vector3 ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs )
		{
			ret = new Vector3(
				lhs.column0.x * rhs.column0.x + lhs.column1.x * rhs.column0.y + lhs.column2.x * rhs.column0.z,
				lhs.column0.y * rhs.column0.x + lhs.column1.y * rhs.column0.y + lhs.column2.y * rhs.column0.z,
				lhs.column0.z * rhs.column0.x + lhs.column1.z * rhs.column0.y + lhs.column2.z * rhs.column0.z );
		}

		public static void SAFBIKMatMultCol1( out Vector3 ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs )
		{
			ret = new Vector3(
				lhs.column0.x * rhs.column1.x + lhs.column1.x * rhs.column1.y + lhs.column2.x * rhs.column1.z,
				lhs.column0.y * rhs.column1.x + lhs.column1.y * rhs.column1.y + lhs.column2.y * rhs.column1.z,
				lhs.column0.z * rhs.column1.x + lhs.column1.z * rhs.column1.y + lhs.column2.z * rhs.column1.z );
		}

		public static void SAFBIKMatMultCol2( out Vector3 ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs )
		{
			ret = new Vector3(
				lhs.column0.x * rhs.column2.x + lhs.column1.x * rhs.column2.y + lhs.column2.x * rhs.column2.z,
				lhs.column0.y * rhs.column2.x + lhs.column1.y * rhs.column2.y + lhs.column2.y * rhs.column2.z,
				lhs.column0.z * rhs.column2.x + lhs.column1.z * rhs.column2.y + lhs.column2.z * rhs.column2.z );
		}

		public static void SAFBIKMatMultVec( out Vector3 ret, ref Matrix3x3 m, ref Vector3 v )
		{
			ret = new Vector3(
				m.column0.x * v.x + m.column1.x * v.y + m.column2.x * v.z,
				m.column0.y * v.x + m.column1.y * v.y + m.column2.y * v.z,
				m.column0.z * v.x + m.column1.z * v.y + m.column2.z * v.z );
		}

		public static void SAFBIKMatGetRot( out Quaternion q, ref Matrix3x3 m )
		{
			q = new Quaternion();
			float t = m.column0.x + m.column1.y + m.column2.z;
			if( t > 0.0f ) {
				float s = (float)System.Math.Sqrt( t + 1.0f );
				CheckNaN( s );
				q.w = s * 0.5f;
				s = 0.5f / s;
				q.x = (m.column1.z - m.column2.y) * s;
				q.y = (m.column2.x - m.column0.z) * s;
				q.z = (m.column0.y - m.column1.x) * s;
				CheckNaN( q );
			} else {
				if( m.column0.x > m.column1.y && m.column0.x > m.column2.z ) {
					float s = m.column0.x - m.column1.y - m.column2.z + 1.0f;
					if( s <= FLOAT_EPSILON ) {
						q = Quaternion.identity;
						return;
					}
					s = (float)System.Math.Sqrt( s );
					CheckNaN( s );
					q.x = s * 0.5f;
					s = 0.5f / s;
					q.w = (m.column1.z - m.column2.y) * s;
					q.y = (m.column0.y + m.column1.x) * s;
					q.z = (m.column0.z + m.column2.x) * s;
					CheckNaN( q );
				} else if( m.column1.y > m.column2.z ) {
					float s = m.column1.y - m.column0.x - m.column2.z + 1.0f;
					if( s <= FLOAT_EPSILON ) {
						q = Quaternion.identity;
						return;
					}
					s = (float)System.Math.Sqrt( s );
					CheckNaN( s );
					q.y = s * 0.5f;
					s = 0.5f / s;
					q.w = (m.column2.x - m.column0.z) * s;
					q.z = (m.column1.z + m.column2.y) * s;
					q.x = (m.column1.x + m.column0.y) * s;
					CheckNaN( q );
				} else {
					float s = m.column2.z - m.column0.x - m.column1.y + 1.0f;
					if( s <= FLOAT_EPSILON ) {
						q = Quaternion.identity;
						return;
					}
					s = (float)System.Math.Sqrt( s );
					CheckNaN( s );
					q.z = s * 0.5f;
					s = 0.5f / s;
					q.w = (m.column0.y - m.column1.x) * s;
					q.x = (m.column2.x + m.column0.z) * s;
					q.y = (m.column2.y + m.column1.z) * s;
					CheckNaN( q );
				}
			}
		}

		public static void SAFBIKMatSetRot( out Matrix3x3 m, ref Quaternion q )
		{
			float d = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
			float s = (d > FLOAT_EPSILON) ? (2.0f / d) : 0.0f;
			float xs = q.x * s, ys = q.y * s, zs = q.z * s;
			float wx = q.w * xs, wy = q.w * ys, wz = q.w * zs;
			float xx = q.x * xs, xy = q.x * ys, xz = q.x * zs;
			float yy = q.y * ys, yz = q.y * zs, zz = q.z * zs;
			m.column0.x = 1.0f - (yy + zz);
			m.column1.x = xy - wz;
			m.column2.x = xz + wy;
			m.column0.y = xy + wz;
			m.column1.y = 1.0f - (xx + zz);
			m.column2.y = yz - wx;
			m.column0.z = xz - wy;
			m.column1.z = yz + wx;
			m.column2.z = 1.0f - (xx + yy);
			CheckNaN( m.column0 );
			CheckNaN( m.column1 );
			CheckNaN( m.column2 );
		}

		public static void SAFBIKMatSetAxisAngle( out Matrix3x3 m, ref Vector3 axis, float cos )
		{
			if( cos >= -FLOAT_EPSILON && cos <= FLOAT_EPSILON ) {
				m = Matrix3x3.identity;
				return;
			}

			m = new Matrix3x3();

			float sin = 1.0f - cos * cos;
			sin = (sin <= FLOAT_EPSILON) ? 0.0f : ((sin >= 1.0f - FLOAT_EPSILON) ? 1.0f : (float)System.Math.Sqrt( (float)sin ));

			float axis_x_sin = axis.x * sin;
			float axis_y_sin = axis.y * sin;
			float axis_z_sin = axis.z * sin;

			m.column0.x = cos;
			m.column0.y = axis_z_sin;
			m.column0.z = -axis_y_sin;

			m.column1.x = -axis_z_sin;
			m.column1.y = cos;
			m.column1.z = axis_x_sin;

			m.column2.x = axis_y_sin;
			m.column2.y = -axis_x_sin;
			m.column2.z = cos;

			float cosI = 1.0f - cos;
			float axis_x_cosI = axis.x * cosI;
			float axis_y_cosI = axis.y * cosI;
			float axis_z_cosI = axis.z * cosI;

			m.column0.x += axis.x * axis_x_cosI;
			m.column0.y += axis.y * axis_x_cosI;
			m.column0.z += axis.z * axis_x_cosI;

			m.column1.x += axis.x * axis_y_cosI;
			m.column1.y += axis.y * axis_y_cosI;
			m.column1.z += axis.z * axis_y_cosI;

			m.column2.x += axis.x * axis_z_cosI;
			m.column2.y += axis.y * axis_z_cosI;
			m.column2.z += axis.z * axis_z_cosI;

			CheckNaN( m.column0 );
			CheckNaN( m.column1 );
			CheckNaN( m.column2 );
		}

		public static void SAFBIKMatFastLerp( out Matrix3x3 ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs, float rate )
		{
			if( rate <= IKEpsilon ) {
				ret = lhs;
				return;
			} else if( rate >= 1.0f - IKEpsilon ) {
				ret = rhs;
				return;
			} else {
				Vector3 x = lhs.column0;
				Vector3 y = lhs.column1;
				x = x + (rhs.column0 - x) * rate;
				y = y + (rhs.column1 - y) * rate;

				Vector3 z = Vector3.Cross( x, y );
				x = Vector3.Cross( y, z );

				if( SAFBIKVecNormalize3( ref x, ref y, ref z ) ) {
					ret = Matrix3x3.FromColumn( ref x, ref y, ref z );
				} else {
					ret = lhs;
				}
			}
		}

		public static void SAFBIKMatFastLerpToIdentity( ref Matrix3x3 m, float rate )
		{
			if( rate <= IKEpsilon ) {
				// Nothing
			} else if( rate >= 1.0f - IKEpsilon ) {
				m = Matrix3x3.identity;
			} else {
				Vector3 x = m.column0;
				Vector3 y = m.column1;
				x = x + (new Vector3(1.0f, 0.0f, 0.0f) - x) * rate;
				y = y + (new Vector3(0.0f, 1.0f, 0.0f) - y) * rate;

				Vector3 z = Vector3.Cross( x, y );
				x = Vector3.Cross( y, z );

				if( SAFBIKVecNormalize3( ref x, ref y, ref z ) ) {
					m = Matrix3x3.FromColumn( ref x, ref y, ref z );
				}
			}
		}

		public static void SAFBIKMatMultVecInv( out Vector3 ret, ref Matrix3x3 mat, ref Vector3 vec )
		{
			Matrix3x3 tmpMat = mat.transpose;
			SAFBIKMatMultVec( out ret, ref tmpMat, ref vec );
		}

		public static void SAFBIKMatMultVecAdd( out Vector3 ret, ref Matrix3x3 mat, ref Vector3 vec, ref Vector3 addVec )
		{
			SAFBIKMatMultVec( out ret, ref mat, ref vec );
			ret += addVec;
        }

		public static void SAFBIKMatMultVecPreSub( out Vector3 ret, ref Matrix3x3 mat, ref Vector3 vec, ref Vector3 subVec )
		{
			Vector3 tmpVec = vec - subVec;
			SAFBIKMatMultVec( out ret, ref mat, ref tmpVec );
		}

		public static void SAFBIKMatMultVecPreSubAdd( out Vector3 ret, ref Matrix3x3 mat, ref Vector3 vec, ref Vector3 subVec, ref Vector3 addVec )
		{
			Vector3 tmpVec = vec - subVec;
			SAFBIKMatMultVec( out ret, ref mat, ref tmpVec );
			ret += addVec;
		}

		public static void SAFBIKMatMultInv0( out Matrix3x3 ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs )
		{
			ret = new Matrix3x3(
				lhs.column0.x * rhs.column0.x + lhs.column0.y * rhs.column0.y + lhs.column0.z * rhs.column0.z,
				lhs.column0.x * rhs.column1.x + lhs.column0.y * rhs.column1.y + lhs.column0.z * rhs.column1.z,
				lhs.column0.x * rhs.column2.x + lhs.column0.y * rhs.column2.y + lhs.column0.z * rhs.column2.z,

				lhs.column1.x * rhs.column0.x + lhs.column1.y * rhs.column0.y + lhs.column1.z * rhs.column0.z,
				lhs.column1.x * rhs.column1.x + lhs.column1.y * rhs.column1.y + lhs.column1.z * rhs.column1.z,
				lhs.column1.x * rhs.column2.x + lhs.column1.y * rhs.column2.y + lhs.column1.z * rhs.column2.z,

				lhs.column2.x * rhs.column0.x + lhs.column2.y * rhs.column0.y + lhs.column2.z * rhs.column0.z,
				lhs.column2.x * rhs.column1.x + lhs.column2.y * rhs.column1.y + lhs.column2.z * rhs.column1.z,
				lhs.column2.x * rhs.column2.x + lhs.column2.y * rhs.column2.y + lhs.column2.z * rhs.column2.z );
		}

		public static void SAFBIKMatMultInv1( out Matrix3x3 ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs )
		{
			ret = new Matrix3x3(
				lhs.column0.x * rhs.column0.x + lhs.column1.x * rhs.column1.x + lhs.column2.x * rhs.column2.x,
				lhs.column0.x * rhs.column0.y + lhs.column1.x * rhs.column1.y + lhs.column2.x * rhs.column2.y,
				lhs.column0.x * rhs.column0.z + lhs.column1.x * rhs.column1.z + lhs.column2.x * rhs.column2.z,

				lhs.column0.y * rhs.column0.x + lhs.column1.y * rhs.column1.x + lhs.column2.y * rhs.column2.x,
				lhs.column0.y * rhs.column0.y + lhs.column1.y * rhs.column1.y + lhs.column2.y * rhs.column2.y,
				lhs.column0.y * rhs.column0.z + lhs.column1.y * rhs.column1.z + lhs.column2.y * rhs.column2.z,

				lhs.column0.z * rhs.column0.x + lhs.column1.z * rhs.column1.x + lhs.column2.z * rhs.column2.x,
				lhs.column0.z * rhs.column0.y + lhs.column1.z * rhs.column1.y + lhs.column2.z * rhs.column2.y,
				lhs.column0.z * rhs.column0.z + lhs.column1.z * rhs.column1.z + lhs.column2.z * rhs.column2.z );
		}

		public static void SAFBIKMatMultGetRot( out Quaternion ret, ref Matrix3x3 lhs, ref Matrix3x3 rhs )
		{
			Matrix3x3 tmpMat;
			SAFBIKMatMult( out tmpMat, ref lhs, ref rhs );
			SAFBIKMatGetRot( out ret, ref tmpMat );
        }

		public static void SAFBIKMatSetRotMult( out Matrix3x3 ret, ref Quaternion lhs, ref Quaternion rhs )
		{
			Quaternion q = lhs * rhs;
			SAFBIKMatSetRot( out ret, ref q );
		}

		public static void SAFBIKMatSetRotMultInv1( out Matrix3x3 ret, ref Quaternion lhs, ref Quaternion rhs )
		{
			Quaternion q = lhs * Inverse( rhs );
			SAFBIKMatSetRot( out ret, ref q );
		}

		//--------------------------------------------------------------------------------------------------------------------

		public static void SAFBIKQuatMult( out Quaternion ret, ref Quaternion q0, ref Quaternion q1 )
		{
			ret = q0 * q1;
		}

		public static void SAFBIKQuatMultInv0( out Quaternion ret, ref Quaternion q0, ref Quaternion q1 )
		{
			ret = Inverse( q0 ) * q1;
		}

		public static void SAFBIKQuatMultNorm( out Quaternion ret, ref Quaternion q0, ref Quaternion q1 )
		{
			ret = Normalize( q0 * q1 );
		}

		public static void SAFBIKQuatMultNormInv0( out Quaternion ret, ref Quaternion q0, ref Quaternion q1 )
		{
			ret = Normalize( Inverse( q0 ) * q1 );
		}

		public static void SAFBIKQuatMult3( out Quaternion ret, ref Quaternion q0, ref Quaternion q1, ref Quaternion q2 )
		{
			ret = q0 * q1 * q2;
		}

		public static void SAFBIKQuatMult3Inv0( out Quaternion ret, ref Quaternion q0, ref Quaternion q1, ref Quaternion q2 )
		{
			ret = Inverse( q0 ) * q1 * q2;
		}

		public static void SAFBIKQuatMult3Inv1( out Quaternion ret, ref Quaternion q0, ref Quaternion q1, ref Quaternion q2 )
		{
			ret = q0 * Inverse( q1 ) * q2;
		}

		public static void SAFBIKQuatMultNorm3( out Quaternion ret, ref Quaternion q0, ref Quaternion q1, ref Quaternion q2 )
		{
			ret = Normalize( q0 * q1 * q2 );
		}

		public static void SAFBIKQuatMultNorm3Inv0( out Quaternion ret, ref Quaternion q0, ref Quaternion q1, ref Quaternion q2 )
		{
			ret = Normalize( Inverse( q0 ) * q1 * q2 );
		}

		public static void SAFBIKQuatMultNorm3Inv1( out Quaternion ret, ref Quaternion q0, ref Quaternion q1, ref Quaternion q2 )
		{
			ret = Normalize( q0 * Inverse( q1 ) * q2 );
		}

		//--------------------------------------------------------------------------------------------------------------------

		public static bool SAFBIKComputeBasisFromXZLockX( out Matrix3x3 basis, ref Vector3 dirX, ref Vector3 dirZ )
		{
			CheckNormalized( dirX );
			Vector3 baseY = Vector3.Cross( dirZ, dirX );
			Vector3 baseZ = Vector3.Cross( dirX, baseY );
			if( SAFBIKVecNormalize2( ref baseY, ref baseZ ) ) {
				basis = Matrix3x3.FromColumn( ref dirX, ref baseY, ref baseZ );
				return true;
			} else {
				basis = Matrix3x3.identity;
				return false;
			}
		}

		public static bool SAFBIKComputeBasisFromXYLockX( out Matrix3x3 basis, ref Vector3 dirX, ref Vector3 dirY )
		{
			CheckNormalized( dirX );
			Vector3 baseZ = Vector3.Cross( dirX, dirY );
			Vector3 baseY = Vector3.Cross( baseZ, dirX );
			if( SAFBIKVecNormalize2( ref baseY, ref baseZ ) ) {
				basis = Matrix3x3.FromColumn( ref dirX, ref baseY, ref baseZ );
				return true;
			} else {
				basis = Matrix3x3.identity;
				return false;
			}
		}

		public static bool SAFBIKComputeBasisFromXYLockY( out Matrix3x3 basis, ref Vector3 dirX, ref Vector3 dirY )
		{
			CheckNormalized( dirY );
			Vector3 baseZ = Vector3.Cross( dirX, dirY );
			Vector3 baseX = Vector3.Cross( dirY, baseZ );
			if( SAFBIKVecNormalize2( ref baseX, ref baseZ ) ) {
				basis = Matrix3x3.FromColumn( ref baseX, ref dirY, ref baseZ );
				return true;
			} else {
				basis = Matrix3x3.identity;
				return false;
			}
		}

		public static bool SAFBIKComputeBasisFromXZLockZ( out Matrix3x3 basis, ref Vector3 dirX, ref Vector3 dirZ )
		{
			CheckNormalized( dirZ );
			Vector3 baseY = Vector3.Cross( dirZ, dirX );
			Vector3 baseX = Vector3.Cross( baseY, dirZ );
			if( SAFBIKVecNormalize2( ref baseX, ref baseY ) ) {
				basis = Matrix3x3.FromColumn( ref baseX, ref baseY, ref dirZ );
				return true;
			} else {
				basis = Matrix3x3.identity;
				return false;
			}
		}

		public static bool SAFBIKComputeBasisFromYZLockY( out Matrix3x3 basis, ref Vector3 dirY, ref Vector3 dirZ )
		{
			CheckNormalized( dirY );
			Vector3 baseX = Vector3.Cross( dirY, dirZ );
			Vector3 baseZ = Vector3.Cross( baseX, dirY );
			if( SAFBIKVecNormalize2( ref baseX, ref baseZ ) ) {
				basis = Matrix3x3.FromColumn( ref baseX, ref dirY, ref baseZ );
				return true;
			} else {
				basis = Matrix3x3.identity;
				return false;
			}
		}

		public static bool SAFBIKComputeBasisFromYZLockZ( out Matrix3x3 basis, ref Vector3 dirY, ref Vector3 dirZ )
		{
			CheckNormalized( dirZ );
			Vector3 baseX = Vector3.Cross( dirY, dirZ );
			Vector3 baseY = Vector3.Cross( dirZ, baseX );
			if( SAFBIKVecNormalize2( ref baseX, ref baseY ) ) {
				basis = Matrix3x3.FromColumn( ref baseX, ref baseY, ref dirZ );
				return true;
			} else {
				basis = Matrix3x3.identity;
				return false;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------

		public static bool SAFBIKComputeBasisLockX( out Matrix3x3 basis, ref Vector3 dirX, ref Vector3 dirY, ref Vector3 dirZ )
		{
			Matrix3x3 basisY;
			Matrix3x3 basisZ;
			bool solveY = SAFBIKComputeBasisFromXYLockX( out basisY, ref dirX, ref dirY );
			bool solveZ = SAFBIKComputeBasisFromXZLockX( out basisZ, ref dirX, ref dirZ );
			if( solveY && solveZ ) {
				float nearY = Mathf.Abs( Vector3.Dot( dirX, dirY ) );
				float nearZ = Mathf.Abs( Vector3.Dot( dirX, dirZ ) );
				if( nearZ <= IKEpsilon ) {
					basis = basisZ;
					return true;
				} else if( nearY <= IKEpsilon ) {
					basis = basisY;
					return true;
				} else {
					SAFBIKMatFastLerp( out basis, ref basisY, ref basisZ, nearY / (nearY + nearZ) );
					return true;
				}
			} else if( solveY ) {
				basis = basisY;
				return true;
			} else if( solveZ ) {
				basis = basisZ;
				return true;
			}

			basis = Matrix3x3.identity;
			return false;
		}

		public static bool SAFBIKComputeBasisLockY( out Matrix3x3 basis, ref Vector3 dirX, ref Vector3 dirY, ref Vector3 dirZ )
		{
			Matrix3x3 basisX;
			Matrix3x3 basisZ;
			bool solveX = SAFBIKComputeBasisFromXYLockY( out basisX, ref dirX, ref dirY );
			bool solveZ = SAFBIKComputeBasisFromYZLockY( out basisZ, ref dirY, ref dirZ );
			if( solveX && solveZ ) {
				float nearX = Mathf.Abs( Vector3.Dot( dirY, dirX ) );
				float nearZ = Mathf.Abs( Vector3.Dot( dirY, dirZ ) );
				if( nearZ <= IKEpsilon ) {
					basis = basisZ;
					return true;
				} else if( nearX <= IKEpsilon ) {
					basis = basisX;
					return true;
				} else {
					SAFBIKMatFastLerp( out basis, ref basisX, ref basisZ, nearX / (nearX + nearZ) );
					return true;
				}
			} else if( solveX ) {
				basis = basisX;
				return true;
			} else if( solveZ ) {
				basis = basisZ;
				return true;
			}

			basis = Matrix3x3.identity;
			return false;
		}

		public static bool SAFBIKComputeBasisLockZ( out Matrix3x3 basis, ref Vector3 dirX, ref Vector3 dirY, ref Vector3 dirZ )
		{
			Matrix3x3 basisX;
			Matrix3x3 basisY;
			bool solveX = SAFBIKComputeBasisFromXZLockZ( out basisX, ref dirX, ref dirZ );
			bool solveY = SAFBIKComputeBasisFromYZLockZ( out basisY, ref dirY, ref dirZ );
			if( solveX && solveY ) {
				float nearX = Mathf.Abs( Vector3.Dot( dirZ, dirX ) );
				float nearY = Mathf.Abs( Vector3.Dot( dirZ, dirY ) );
				if( nearY <= IKEpsilon ) {
					basis = basisY;
					return true;
				} else if( nearX <= IKEpsilon ) {
					basis = basisX;
					return true;
				} else {
					SAFBIKMatFastLerp( out basis, ref basisX, ref basisY, nearX / (nearX + nearY) );
					return true;
				}
			} else if( solveX ) {
				basis = basisX;
				return true;
			} else if( solveY ) {
				basis = basisY;
				return true;
			}

			basis = Matrix3x3.identity;
			return false;
		}
#endif

		//--------------------------------------------------------------------------------------------------------------------------------------------

		public const float FLOAT_EPSILON = 1.401298e-45f;
		public const float IKEpsilon = 1e-7f;
		public const float IKMoveEpsilon = 1e-05f;
		public const float IKWritebackEpsilon = 0.01f;

		public static bool IsFuzzy( float lhs, float rhs, float epsilon = IKEpsilon )
		{
			float t = lhs - rhs;
			return t >= -epsilon && t <= epsilon;
		}

		public static bool IsFuzzy( Vector3 lhs, Vector3 rhs, float epsilon = IKEpsilon )
		{
			float x = lhs.x - rhs.x;
			if( x >= -epsilon && x <= epsilon ) {
				x = lhs.y - rhs.y;
				if( x >= -epsilon && x <= epsilon ) {
					x = lhs.z - rhs.z;
					if( x >= -epsilon && x <= epsilon ) {
						return true;
					}
				}
			}

			return false;
		}

		public static bool IsFuzzy( ref Vector3 lhs, ref Vector3 rhs, float epsilon = IKEpsilon )
		{
			float x = lhs.x - rhs.x;
			if( x >= -epsilon && x <= epsilon ) {
				x = lhs.y - rhs.y;
				if( x >= -epsilon && x <= epsilon ) {
					x = lhs.z - rhs.z;
					if( x >= -epsilon && x <= epsilon ) {
						return true;
					}
				}
			}

			return false;
		}

		public static bool _IsNear( ref Vector3 lhs, ref Vector3 rhs )
		{
			return IsFuzzy( ref lhs, ref rhs, IKMoveEpsilon );
		}

		//--------------------------------------------------------------------------------------------------------------------

		public static Vector3 _Rotate( ref Vector3 dirX, ref Vector3 dirY, float cosR, float sinR )
		{
			return dirX * cosR + dirY * sinR;
		}

		public static Vector3 _Rotate( ref Vector3 dirX, ref Vector3 dirY, float r )
		{
			float cosR = Mathf.Cos( r );
			float sinR = Mathf.Sin( r );
			return dirX * cosR + dirY * sinR;
		}

		public static Vector3 _Rotate( ref Vector3 dirX, ref Vector3 dirY, ref FastAngle angle )
		{
			return dirX * angle.cos + dirY * angle.sin;
		}

		public static bool _NormalizedTranslate( ref Vector3 dir, ref Vector3 fr, ref Vector3 to )
		{
			Vector3 t = to - fr;
			float length = t.magnitude;
			if( length > IKEpsilon ) {
				dir = t * (1.0f / length);
				return true;
			}

			dir = Vector3.zero;
			return false;
		}
		
		public static Quaternion Inverse( Quaternion q )
		{
			return new Quaternion( -q.x, -q.y, -q.z, q.w );
		}

		public static Quaternion Normalize( Quaternion q )
		{
			float lenSq = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
			if( lenSq > IKEpsilon ) {
				if( lenSq >= 1.0f - IKEpsilon && lenSq <= 1.0 + IKEpsilon ) {
					return q;
				} else {
					float s = 1.0f / Mathf.Sqrt( lenSq );
					return new Quaternion( q.x * s, q.y * s, q.z * s, q.w * s );
				}
			}

			return q; // Failsafe.
	    }

		public static bool SafeNormalize( ref Quaternion q )
		{
			float lenSq = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
			if( lenSq > IKEpsilon ) {
				if( lenSq >= 1.0f - IKEpsilon && lenSq <= 1.0 + IKEpsilon ) {
					return true;
				} else {
					float s = 1.0f / Mathf.Sqrt( lenSq );
					q.x *= s;
					q.y *= s;
					q.z *= s;
					q.w *= s;
					return true;
				}
			}

			return false;
		}

		public static bool IsFuzzyIdentity( Quaternion q )
		{
			return IsFuzzyIdentity( ref q );
		}

		public static bool IsFuzzyIdentity( ref Quaternion q )
		{
			return
				q.x >= -IKEpsilon && q.x <= IKEpsilon &&
				q.y >= -IKEpsilon && q.y <= IKEpsilon &&
				q.z >= -IKEpsilon && q.z <= IKEpsilon &&
				q.w >= 1.0f - IKEpsilon && q.w <= 1.0f + IKEpsilon;
		}

		//--------------------------------------------------------------------------------------------------------------------

		[System.Serializable]
		public struct FastLength
		{
			public float length;
			public float lengthSq;

			FastLength( float length_ )
			{
				length = length_;
				lengthSq = length_ * length_;
			}

			FastLength( float length_, float lengthSq_ )
			{
				length = length_;
				lengthSq = lengthSq_;
			}

			public static FastLength FromLength( float length )
			{
				return new FastLength( length );
			}

			public static FastLength FromLengthSq( float lengthSq )
			{
				return new FastLength( SAFBIKSqrt( lengthSq ), lengthSq );
			}

			public static FastLength FromVector3( Vector3 v )
			{
				float lengthSq;
				float length = SAFBIKVecLengthAndLengthSq( out lengthSq, ref v );
				return new FastLength( length, lengthSq );
			}

			public static FastLength FromVector3( Vector3 v0, Vector3 v1 )
			{
				float lengthSq;
				float length = SAFBIKVecLengthAndLengthSq2( out lengthSq, ref v0, ref v1 );
				return new FastLength( length, lengthSq );
			}

			public static FastLength FromVector3( ref Vector3 v )
			{
				float lengthSq;
				float length = SAFBIKVecLengthAndLengthSq( out lengthSq, ref v );
				return new FastLength( length, lengthSq );
			}

			public static FastLength FromVector3( ref Vector3 v0, ref Vector3 v1 )
			{
				float lengthSq;
				float length = SAFBIKVecLengthAndLengthSq2( out lengthSq, ref v0, ref v1 );
				return new FastLength( length, lengthSq );
			}
		}

		[System.Serializable]
		public struct FastAngle
		{
			public float angle; // Radian
			public float cos;
			public float sin;

			public static readonly FastAngle zero = new FastAngle( 0.0f, 1.0f, 0.0f );

			public FastAngle( float angle_ )
			{
				angle = angle_;
				cos = (float)System.Math.Cos( angle_ );
				sin = (float)System.Math.Sin( angle_ );
			}

			public FastAngle( float angle_, float cos_, float sin_ )
			{
				angle = angle_;
				cos = cos_;
				sin = sin_;
			}

			public void Reset()
			{
				angle = 0.0f;
				cos = 1.0f;
				sin = 0.0f;
			}

			public void Reset( float angle_ )
			{
				angle = angle_;
				cos = (float)System.Math.Cos( angle_ );
				sin = (float)System.Math.Sin( angle_ );
			}

			public void Reset( float angle_, float cos_, float sin_ )
			{
				angle = angle_;
				cos = cos_;
				sin = sin_;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------

		public struct CachedRate01
		{
			public float _value;
			public float value;
			public bool isGreater0;
			public bool isLess1;

			public static readonly CachedRate01 zero = new CachedRate01( 0.0f );

			public CachedRate01( float v )
			{
				_value = v;
				value = Mathf.Clamp01( v );
				isGreater0 = value > IKEpsilon;
				isLess1 = value < 1.0f - IKEpsilon;
			}

			public void _Reset( float v )
			{
				_value = v;
				value = Mathf.Clamp01( v );
				isGreater0 = value > IKEpsilon;
				isLess1 = value < 1.0f - IKEpsilon;
			}
		}

		public struct CachedDegreesToSin
		{
			public float _degrees;
			public float sin;

			public static readonly CachedDegreesToSin zero = new CachedDegreesToSin( 0.0f, 0.0f );

			public CachedDegreesToSin( float degrees )
			{
				_degrees = degrees;
				sin = (float)System.Math.Sin( degrees * Mathf.Deg2Rad );
			}

			public CachedDegreesToSin( float degrees, float sin_ )
			{
				_degrees = degrees;
				sin = sin_;
			}

			public void _Reset( float degrees )
			{
				_degrees = degrees;
				sin = (float)System.Math.Sin( degrees * Mathf.Deg2Rad );
			}
		}

		public struct CachedDegreesToCos
		{
			public float _degrees;
			public float cos;

			public static readonly CachedDegreesToCos zero = new CachedDegreesToCos( 0.0f, 1.0f );

			public CachedDegreesToCos( float degrees )
			{
				_degrees = degrees;
				cos = (float)System.Math.Cos( degrees * Mathf.Deg2Rad );
			}

			public CachedDegreesToCos( float degrees, float cos_ )
			{
				_degrees = degrees;
				cos = cos_;
			}

			public void _Reset( float degrees )
			{
				_degrees = degrees;
				cos = (float)System.Math.Cos( degrees * Mathf.Deg2Rad );
			}
		}

		public struct CachedDegreesToCosSin
		{
			public float _degrees;
			public float cos;
			public float sin;

			public static readonly CachedDegreesToCosSin zero = new CachedDegreesToCosSin( 0.0f, 1.0f, 0.0f );

			public CachedDegreesToCosSin( float degrees )
			{
				_degrees = degrees;
				cos = (float)System.Math.Cos( degrees * Mathf.Deg2Rad );
				sin = (float)System.Math.Sin( degrees * Mathf.Deg2Rad );
			}

			public CachedDegreesToCosSin( float degrees, float cos_, float sin_ )
			{
				_degrees = degrees;
				cos = cos_;
				sin = sin_;
			}

			public void _Reset( float degrees )
			{
				_degrees = degrees;
				cos = (float)System.Math.Cos( degrees * Mathf.Deg2Rad );
				sin = (float)System.Math.Sin( degrees * Mathf.Deg2Rad );
			}
		}

		public struct CachedScaledValue
		{
			public float _a;
			public float _b;
			public float value;

			public static readonly CachedScaledValue zero = new CachedScaledValue( 0.0f, 0.0f, 0.0f );

			public CachedScaledValue( float a, float b )
			{
				_a = a;
				_b = b;
				value = a * b;
			}

			public CachedScaledValue( float a, float b, float value_ )
			{
				_a = a;
				_b = b;
				value = value_;
			}

			public void Reset( float a, float b )
			{
				if( _a != a || _b != b ) {
					_a = a;
					_b = b;
					value = a * b;
				}
			}

			public void _Reset( float a, float b )
			{
				_a = a;
				_b = b;
				value = a * b;
			}
		}

		public struct CachedDeg2RadScaledValue
		{
			public float _a;
			public float _b;
			public float value;

			public static readonly CachedDeg2RadScaledValue zero = new CachedDeg2RadScaledValue( 0.0f, 0.0f, 0.0f );

			public CachedDeg2RadScaledValue( float a, float b )
			{
				_a = a;
				_b = b;
				value = a * b * Mathf.Deg2Rad;
			}

			public CachedDeg2RadScaledValue( float a, float b, float value_ )
			{
				_a = a;
				_b = b;
				value = value_;
			}

			public void Reset( float a, float b )
			{
				if( _a != a || _b != b ) {
					_a = a;
					_b = b;
					value = a * b * Mathf.Deg2Rad;
				}
			}

			public void _Reset( float a, float b )
			{
				_a = a;
				_b = b;
				value = a * b * Mathf.Deg2Rad;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------

		public static bool SAFBIKComputeBasisFromXZLockX( out Matrix3x3 basis, Vector3 dirX, Vector3 dirZ )
		{
			return SAFBIKComputeBasisFromXZLockX( out basis, ref dirX, ref dirZ );
		}

		public static bool SAFBIKComputeBasisFromXYLockX( out Matrix3x3 basis, Vector3 dirX, Vector3 dirY )
		{
			return SAFBIKComputeBasisFromXYLockX( out basis, ref dirX, ref dirY );
		}

		public static bool SAFBIKComputeBasisFromXYLockY( out Matrix3x3 basis, Vector3 dirX, Vector3 dirY )
		{
			return SAFBIKComputeBasisFromXYLockY( out basis, ref dirX, ref dirY );
		}

		public static bool SAFBIKComputeBasisFromXZLockZ( out Matrix3x3 basis, Vector3 dirX, Vector3 dirZ )
		{
			return SAFBIKComputeBasisFromXZLockZ( out basis, ref dirX, ref dirZ );
		}

		public static bool SAFBIKComputeBasisFromYZLockY( out Matrix3x3 basis, Vector3 dirY, Vector3 dirZ )
		{
			return SAFBIKComputeBasisFromYZLockY( out basis, ref dirY, ref dirZ );
		}

		public static bool SAFBIKComputeBasisFromYZLockZ( out Matrix3x3 basis, Vector3 dirY, Vector3 dirZ )
		{
			return SAFBIKComputeBasisFromYZLockZ( out basis, ref dirY, ref dirZ );
		}

		//--------------------------------------------------------------------------------------------------------------------

		public static bool SAFBIKComputeBasisFrom( out Matrix3x3 basis, ref Matrix3x3 rootBasis, ref Vector3 dir, _DirectionAs directionAs )
		{
			CheckNormalized( dir );

			switch( directionAs ) {
			case _DirectionAs.XPlus:
				return SAFBIKComputeBasisFromXYLockX( out basis, dir, rootBasis.column1 );
			case _DirectionAs.XMinus:
				return SAFBIKComputeBasisFromXYLockX( out basis, -dir, rootBasis.column1 );
			case _DirectionAs.YPlus:
				return SAFBIKComputeBasisFromXYLockY( out basis, rootBasis.column0, dir );
			case _DirectionAs.YMinus:
				return SAFBIKComputeBasisFromXYLockY( out basis, rootBasis.column0, -dir );
			}

			basis = Matrix3x3.identity;
			return false;
		}

		//--------------------------------------------------------------------------------------------------------------------

		// Trigonometry
		public static float ComputeCosTheta(
			float lenASq,
			float lenBSq,
			float lenCSq,
			float lenB,
			float lenC )
		{
			float bc2 = lenB * lenC * 2.0f;
			if( bc2 > IKEpsilon ) {
				return (lenBSq + lenCSq - lenASq) / bc2;
			}

			return 1.0f;
		}

		// Trigonometry to A
		public static float ComputeCosTheta(
			FastLength lenA,
			FastLength lenB,
			FastLength lenC )
		{
			float bc2 = lenB.length * lenC.length * 2.0f;
			if( bc2 > IKEpsilon ) {
				return (lenB.lengthSq + lenC.lengthSq - lenA.lengthSq) / bc2;
			}

			return 0.0f;
		}

		// Trigonometry
		public static float ComputeSinTheta(
			float lenASq,
			float lenBSq,
			float lenCSq,
			float lenB,
			float lenC )
		{
			float bc2 = lenB * lenC * 2.0f;
			if( bc2 > IKEpsilon ) {
				float cs = (lenBSq + lenCSq - lenASq) / bc2;
				return SAFBIKSqrtClamp01( 1.0f - cs * cs );
			}

			return 0.0f;
		}

		// Trigonometry to A
		public static float ComputeSinTheta(
			FastLength lenA,
			FastLength lenB,
			FastLength lenC )
		{
			float bc2 = lenB.length * lenC.length * 2.0f;
			if( bc2 > IKEpsilon ) {
				float cs = (lenB.lengthSq + lenC.lengthSq - lenA.lengthSq) / bc2;
				return SAFBIKSqrtClamp01( 1.0f - cs * cs );
			}

			return 0.0f;
		}
		
		//--------------------------------------------------------------------------------------------------------------------

		static bool _ComputeThetaAxis(
			ref Vector3 origPos,
			ref Vector3 fromPos,
			ref Vector3 toPos,
			out float theta,
			out Vector3 axis )
		{
			Vector3 dirFrom = fromPos - origPos;
			Vector3 dirTo = toPos - origPos;
			if( !SAFBIKVecNormalize2( ref dirFrom, ref dirTo ) ) {
				theta = 0.0f;
				axis = new Vector3( 0.0f, 0.0f, 1.0f );
				return false;
			}

			return _ComputeThetaAxis( ref dirFrom, ref dirTo, out theta, out axis );
		}

		static bool _ComputeThetaAxis(
			ref Vector3 dirFrom,
			ref Vector3 dirTo,
			out float theta,
			out Vector3 axis )
		{
			CheckNormalized( dirFrom );
			CheckNormalized( dirTo );
			axis = Vector3.Cross( dirFrom, dirTo );
			if( !SAFBIKVecNormalize( ref axis ) ) {
				theta = 0.0f;
				axis = new Vector3( 0.0f, 0.0f, 1.0f );
				return false;
			}

			theta = Vector3.Dot( dirFrom, dirTo );
			return true;
		}

		//--------------------------------------------------------------------------------------------------------------------

		public static Vector3 Scale( Vector3 lhs, Vector3 rhs )
		{
			return new Vector3( lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z );
		}

		public static Vector3 Scale( ref Vector3 lhs, ref Vector3 rhs )
		{
			return new Vector3( lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z );
		}

		//--------------------------------------------------------------------------------------------------------------------

		// Limited Square.
		public static bool _LimitXY_Square(
			ref Vector3 dir,                // dirZ
			float limitXMinus,               // X-
			float limitXPlus,                // X+
			float limitYMinus,               // Z-
			float limitYPlus )               // Z+
		{
			bool isXLimited = false;
			bool isYLimited = false;

			if( dir.x < -limitXMinus ) {
				dir.x = -limitXMinus;
				isXLimited = true;
			} else if( dir.x > limitXPlus ) {
				dir.x = limitXPlus;
				isXLimited = true;
			}

			if( dir.y < -limitYMinus ) {
				dir.y = -limitYMinus;
				isYLimited = true;
			} else if( dir.y > limitYPlus ) {
				dir.y = limitYPlus;
				isYLimited = true;
			}

			if( isXLimited || isYLimited ) {
				dir.z = SAFBIKSqrt( 1.0f - (dir.x * dir.x + dir.y * dir.y) );
				return true;
			} else {
				if( dir.z < 0.0f ) {
					dir.z = -dir.z;
					return true;
                }
			}

			return false;
		}

		public static bool _LimitYZ_Square(
			bool isRight,
			ref Vector3 dir,                    // dirX
			float limitYMinus,                  // Y-
			float limitYPlus,                   // Y+
			float limitZMinus,                  // Z-
			float limitZPlus )                  // Z+
		{
			bool isYLimited = false;
			bool isZLimited = false;

			if( dir.y < -limitYMinus ) {
				dir.y = -limitYMinus;
				isYLimited = true;
			} else if( dir.y > limitYPlus ) {
				dir.y = limitYPlus;
				isYLimited = true;
			}

			if( dir.z < -limitZMinus ) {
				dir.z = -limitZMinus;
				isZLimited = true;
			} else if( dir.z > limitZPlus ) {
				dir.z = limitZPlus;
				isZLimited = true;
			}

			if( isYLimited || isZLimited ) {
				dir.x = SAFBIKSqrt( 1.0f - (dir.y * dir.y + dir.z * dir.z) );
				if( !isRight ) {
					dir.x = -dir.x;
				}
				return true;
			} else {
				if( isRight ) {
					if( dir.x < 0.0f ) {
						dir.x = -dir.x;
						return true;
					}
				} else {
					if( dir.x >= 0.0f ) {
						dir.x = -dir.x;
						return true;
					}
				}
			}

			return false;
		}

		// Limited Square.
		public static bool _LimitXZ_Square(
			ref Vector3 dir,                // dirZ
			float limitXMinus,               // X-
			float limitXPlus,                // X+
			float limitZMinus,               // Z-
			float limitZPlus )               // Z+
		{
			bool isXLimited = false;
			bool isZLimited = false;

			if( dir.x < -limitXMinus ) {
				dir.x = -limitXMinus;
				isXLimited = true;
			} else if( dir.x > limitXPlus ) {
				dir.x = limitXPlus;
				isXLimited = true;
			}

			if( dir.z < -limitZMinus ) {
				dir.z = -limitZMinus;
				isZLimited = true;
			} else if( dir.z > limitZPlus ) {
				dir.z = limitZPlus;
				isZLimited = true;
			}

			if( isXLimited || isZLimited ) {
				dir.y = SAFBIKSqrt( 1.0f - (dir.x * dir.x + dir.z * dir.z) );
				return true;
			} else {
				if( dir.y < 0.0f ) {
					dir.y = -dir.y;
					return true;
				}
			}

			return false;
		}

		//--------------------------------------------------------------------------------------------------------------------

		public static bool _LimitXY(
			ref Vector3 dir,				// dirZ
			float limitXMinus,				// X-
			float limitXPlus,				// X+
			float limitYMinus,				// Z-
			float limitYPlus )				// Z+
		{
			bool isXPlus = (dir.x >= 0.0f);
			bool isYPlus = (dir.y >= 0.0f);
			float xLimit = isXPlus ? limitXPlus : limitXMinus;
			float yLimit = isYPlus ? limitYPlus : limitYMinus;

			bool isLimited = false;
			if( xLimit <= IKEpsilon && yLimit <= IKEpsilon ) {
				Vector3 limitedDir = new Vector3( 0.0f, 0.0f, 1.0f );
				Vector3 temp = limitedDir - dir;
				if( Mathf.Abs( temp.x ) > IKEpsilon || Mathf.Abs( temp.y ) > IKEpsilon || Mathf.Abs( temp.z ) > IKEpsilon ) {
					dir = limitedDir;
					isLimited = true;
				}
			} else {
				float inv_xLimit = (xLimit >= IKEpsilon) ? (1.0f / xLimit) : 0.0f;
				float inv_yLimit = (yLimit >= IKEpsilon) ? (1.0f / yLimit) : 0.0f;
				float localX = dir.x * inv_xLimit;
				float localY = dir.y * inv_yLimit;
				float localLen = SAFBIKSqrt( localX * localX + localY * localY + dir.z * dir.z );

				float inv_localLen = (localLen > IKEpsilon) ? (1.0f / localLen) : 0.0f;
				float nrm_localX = localX * inv_localLen; // Counts as sinTheta
				float nrm_localY = localY * inv_localLen; // Counts as cosTheta

				if( localLen > 1.0f ) { // Outer circle.
					if( !isLimited ) {
						isLimited = true;
						localX = nrm_localX;
						localY = nrm_localY;
					}
				}

				float worldX = isLimited ? (localX * xLimit) : dir.x;
				float worldY = isLimited ? (localY * yLimit) : dir.y;

				bool isInverse = (dir.z < 0.0f);

				if( isLimited ) {
					float limitSinSq = (worldX * worldX + worldY * worldY);
					float limitSin = SAFBIKSqrt( limitSinSq );
					float limitCos = SAFBIKSqrt( 1.0f - limitSin * limitSin );
					dir.x = worldX;
					dir.y = worldY;
					dir.z = limitCos;
				} else if( isInverse ) {
					isLimited = true;
					dir.z = -dir.z;
				}
			}

			return isLimited;
		}

		public static bool _LimitXZ(
			ref Vector3 dir,				// dirY
			float limiXMinus,				// X-
			float limiXPlus,				// X+
			float limiZMinus,				// Z-
			float limiZPlus )				// Z+
		{
			bool isXPlus = (dir.x >= 0.0f);
			bool isZPlus = (dir.z >= 0.0f);
			float xLimit = isXPlus ? limiXPlus : limiXMinus;
			float zLimit = isZPlus ? limiZPlus : limiZMinus;

			bool isLimited = false;
			if( xLimit <= IKEpsilon && zLimit <= IKEpsilon ) {
				Vector3 limitedDir = new Vector3( 0.0f, 1.0f, 0.0f );
				Vector3 temp = limitedDir - dir;
				if( Mathf.Abs( temp.x ) > IKEpsilon || Mathf.Abs( temp.y ) > IKEpsilon || Mathf.Abs( temp.z ) > IKEpsilon ) {
					dir = limitedDir;
					isLimited = true;
				}
			} else {
				float inv_xLimit = (xLimit >= IKEpsilon) ? (1.0f / xLimit) : 0.0f;
				float inv_zLimit = (zLimit >= IKEpsilon) ? (1.0f / zLimit) : 0.0f;
				float localX = dir.x * inv_xLimit;
				float localZ = dir.z * inv_zLimit;
				float localLen = SAFBIKSqrt( localX * localX + localZ * localZ + dir.y * dir.y );

				float inv_localLen = (localLen > IKEpsilon) ? (1.0f / localLen) : 0.0f;
				float nrm_localX = localX * inv_localLen; // Counts as sinTheta
				float nrm_localZ = localZ * inv_localLen; // Counts as cosTheta

				if( localLen > 1.0f ) { // Outer circle.
					if( !isLimited ) {
						isLimited = true;
						localX = nrm_localX;
						localZ = nrm_localZ;
					}
				}

				float worldX = isLimited ? (localX * xLimit) : dir.x;
				float worldZ = isLimited ? (localZ * zLimit) : dir.z;

				bool isInverse = (dir.y < 0.0f);

				if( isLimited ) {
					float limitSinSq = (worldX * worldX + worldZ * worldZ);
					float limitSin = SAFBIKSqrt( limitSinSq );
					float limitCos = SAFBIKSqrt( 1.0f - limitSin * limitSin );
					dir.x = worldX;
					dir.y = limitCos;
					dir.z = worldZ;
				} else if( isInverse ) {
					isLimited = true;
					dir.y = -dir.y;
				}
			}

			return isLimited;
		}

		public static bool _LimitYZ(
			bool isRight,
			ref Vector3 dir,					// dirX
			float limitYMinus,					// Y-
			float limitYPlus,					// Y+
			float limitZMinus,					// Z-
			float limitZPlus )					// Z+
		{
			bool isYPlus = (dir.y >= 0.0f);
			bool isZPlus = (dir.z >= 0.0f);
			float yLimit = isYPlus ? limitYPlus : limitYMinus;
			float zLimit = isZPlus ? limitZPlus : limitZMinus;

			bool isLimited = false;
			if( yLimit <= IKEpsilon && zLimit <= IKEpsilon ) {
				Vector3 limitedDir = isRight ? new Vector3( 1.0f, 0.0f, 0.0f ) : new Vector3( -1.0f, 0.0f, 0.0f );
				Vector3 temp = limitedDir - dir;
				if( Mathf.Abs( temp.x ) > IKEpsilon || Mathf.Abs( temp.y ) > IKEpsilon || Mathf.Abs( temp.z ) > IKEpsilon ) {
					dir = limitedDir;
					isLimited = true;
				}
			} else {
				float inv_yLimit = (yLimit >= IKEpsilon) ? (1.0f / yLimit) : 0.0f;
				float inv_zLimit = (zLimit >= IKEpsilon) ? (1.0f / zLimit) : 0.0f;
				float localY = dir.y * inv_yLimit;
				float localZ = dir.z * inv_zLimit;
				float localLen = SAFBIKSqrt( dir.x * dir.x + localY * localY + localZ * localZ );

				float inv_localLen = (localLen > IKEpsilon) ? (1.0f / localLen) : 0.0f;
				float nrm_localY = localY * inv_localLen; // Counts as sinTheta
				float nrm_localZ = localZ * inv_localLen; // Counts as cosTheta

				if( localLen > 1.0f ) { // Outer circle.
					if( !isLimited ) {
						isLimited = true;
						localY = nrm_localY;
						localZ = nrm_localZ;
					}
				}

				float worldY = isLimited ? (localY * yLimit) : dir.y;
				float worldZ = isLimited ? (localZ * zLimit) : dir.z;

				bool isInverse = ((dir.x >= 0.0f) != isRight);

				if( isLimited ) {
					float limitSinSq = (worldY * worldY + worldZ * worldZ);
					float limitSin = SAFBIKSqrt( limitSinSq );
					float limitCos = SAFBIKSqrt( 1.0f - limitSin * limitSin );
					dir.x = isRight ? limitCos : -limitCos;
					dir.y = worldY;
					dir.z = worldZ;
				} else if( isInverse ) {
					isLimited = true;
					dir.x = -dir.x;
				}
			}

			return isLimited;
		}

		//--------------------------------------------------------------------------------------------------------------------

		public static Vector3 _FitToPlane( Vector3 pos, Vector3 planeDir )
		{
			float d = Vector3.Dot( pos, planeDir );
			if( d <= IKEpsilon && d >= -IKEpsilon ) {
				return pos; // Cross.
			}

			return pos - planeDir * d;
		}

		public static bool _FitToPlaneDir( ref Vector3 dir, Vector3 planeDir )
		{
			float d = Vector3.Dot( dir, planeDir );
			if( d <= IKEpsilon && d >= -IKEpsilon ) {
				return false;
			}

			Vector3 tmp = dir - planeDir * d;
			if( !SAFBIKVecNormalize( ref tmp ) ) {
				return false;
			}

			dir = tmp;
			return true;
		}

		public static bool _LimitToPlaneDirY( ref Vector3 dir, Vector3 planeDir, float thetaY )
		{
			float d = Vector3.Dot( dir, planeDir );
			if( d <= IKEpsilon && d >= -IKEpsilon ) {
				return false;
			}

			if( d <= thetaY && d >= -thetaY ) {
				return true;
			}

			Vector3 tmp = dir - planeDir * d;
			float tmpLen = SAFBIKVecLength( ref tmp );
			if( tmpLen <= FLOAT_EPSILON ) {
				return false;
			}

			float targetLen = SAFBIKSqrt( 1.0f - thetaY * thetaY );

			tmp *= targetLen / tmpLen;

			dir = tmp;
			if( d >= 0.0f ) {
				dir += planeDir * thetaY;
			} else {
				dir -= planeDir * thetaY;
			}

			return true;
		}

		//--------------------------------------------------------------------------------------------------------------------

		static void _LerpRotateBasis( out Matrix3x3 basis, ref Vector3 axis, float cos, float rate )
		{
			if( rate <= IKEpsilon ) {
				basis = Matrix3x3.identity;
				return;
			}

			if( rate <= 1.0f - IKEpsilon ) {
				float acos = (cos >= 1.0f - IKEpsilon) ? 0.0f : ((cos <= -1.0f + IKEpsilon) ? (180.0f * Mathf.Deg2Rad) : (float)System.Math.Acos( (float)cos ));
				cos = (float)System.Math.Cos( (float)(acos * rate) );
			}

			SAFBIKMatSetAxisAngle( out basis, ref axis, cos );
		}

		public static Vector3 _LerpDir( ref Vector3 src, ref Vector3 dst, float r )
		{
			float theta;
			Vector3 axis;
			if( _ComputeThetaAxis( ref src, ref dst, out theta, out axis ) ) {
				Matrix3x3 basis;
				_LerpRotateBasis( out basis, ref axis, theta, r );
				Vector3 tmp;
				SAFBIKMatMultVec( out tmp, ref basis, ref src );
				return tmp;
			}

			return dst;
		}

		public static Vector3 _FastLerpDir( ref Vector3 src, ref Vector3 dst, float r )
		{
			if( r <= IKEpsilon ) {
				return src;
			} else if( r >= 1.0f - IKEpsilon ) {
				return dst;
			}

			Vector3 tmp = src + (dst - src) * r;
			float len = tmp.magnitude;
			if( len > IKEpsilon ) {
				return tmp * (1.0f / len);
			}

			return dst;
		}

		//--------------------------------------------------------------------------------------------------------------------

		// for Finger.
		public static bool _LimitFingerNotThumb(
			bool isRight,
			ref Vector3 dir, // dirX
			ref FastAngle limitYPlus,
			ref FastAngle limitYMinus,
			ref FastAngle limitZ )
		{
			bool isLimited = false;

			// Yaw
			if( limitZ.cos > IKEpsilon ) {
				// Memo: Unstable when dir.z near 1.
				if( dir.z < -limitZ.sin || dir.z > limitZ.sin ) {
					isLimited = true;
					bool isPlus = (dir.z >= 0.0f);
					float lenXY = SAFBIKSqrt( dir.x * dir.x + dir.y * dir.y );
					if( limitZ.sin <= IKEpsilon ) { // Optimized.
						if( lenXY > IKEpsilon ) {
							dir.z = 0.0f;
							dir = dir * (1.0f / lenXY);
						} else { // Failsafe.
							dir.Set( isRight ? limitZ.cos : -limitZ.cos, 0.0f, isPlus ? limitZ.sin : -limitZ.sin );
						}
					} else {
						float lenZ = limitZ.sin * lenXY / limitZ.cos;
						dir.z = isPlus ? lenZ : -lenZ;

						float len = dir.magnitude;
						if( len > IKEpsilon ) {
							dir *= (1.0f / len);
						} else { // Failsafe.
							dir.Set( isRight ? limitZ.cos : -limitZ.cos, 0.0f, isPlus ? limitZ.sin : -limitZ.sin );
						}
					}
				}
			}

			// Pitch
			{
				// Memo: Not use z.( For yaw limit. )
				bool isPlus = (dir.y >= 0.0f);
				float cosPitchLimit = isPlus ? limitYPlus.cos : limitYMinus.cos;
				if( (isRight && dir.x < cosPitchLimit) || (!isRight && dir.x > -cosPitchLimit) ) {
					float lenY = SAFBIKSqrt( 1.0f - (cosPitchLimit * cosPitchLimit + dir.z * dir.z) );
					dir.x = (isRight ? cosPitchLimit : -cosPitchLimit);
					dir.y = (isPlus ? lenY : -lenY);
				}
			}

			return isLimited;
		}
	}

}