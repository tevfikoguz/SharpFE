﻿

namespace SharpFE
{
	using System;
	using SharpFE.Elements;
	using SharpFE.Stiffness;
	
	/// <summary>
	/// Beam which carries moment and shear force in both major and minor axes as well as axial force and torsion.
	/// </summary>
	public class Linear3DBeam : LinearBeam
	{
		
		public Linear3DBeam(FiniteElementNode start, FiniteElementNode end, IMaterial mat, ICrossSection section)
			:base(start, end, mat, section)
		{
			// empty
		}
		
		public override bool IsASupportedLocalStiffnessDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom)
		{
			switch(degreeOfFreedom)
			{
				case DegreeOfFreedom.X:  //axial
				case DegreeOfFreedom.Y:  //minor-axis shear
				case DegreeOfFreedom.Z:  //major-axis shear
				case DegreeOfFreedom.XX: //torsion
				case DegreeOfFreedom.YY: //major-axis moment
				case DegreeOfFreedom.ZZ: //minor-axis moment
					return true;
				default:
					return false;
			}
		}
	}
}
