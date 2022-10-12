// Copyright (c) 2016 Nora
// Released under the "Unity-Chan" license
// http://unity-chan.com/contents/license_en/
// http://unity-chan.com/contents/license_jp/

using UnityEngine;

namespace SA
{
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	public class FullBodyIKUnityChanBehaviour : FullBodyIKBehaviourBase
	{
		[SerializeField]
		FullBodyIKUnityChan _fullBodyIK;

		public override FullBodyIK fullBodyIK
		{
			get
			{
				if( _fullBodyIK == null ) {
					_fullBodyIK = new FullBodyIKUnityChan();
                }

				return _fullBodyIK;
			}
		}
	}
}
