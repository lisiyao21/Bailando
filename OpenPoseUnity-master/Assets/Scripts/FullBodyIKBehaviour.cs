// Copyright (c) 2016 Nora
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using UnityEngine;
using System.Collections.Generic;

namespace SA
{
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	public class FullBodyIKBehaviour : FullBodyIKBehaviourBase
	{
		[SerializeField]
		FullBodyIK _fullBodyIK;

		public override FullBodyIK fullBodyIK
		{
			get
			{
				if( _fullBodyIK == null ) {
					_fullBodyIK = new FullBodyIK();
				}

				return _fullBodyIK;
			}
		}
	}
}