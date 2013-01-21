﻿namespace SharpFE
{
    using System;
    
    /// <summary>
    /// Description of GenericElasticMaterial.
    /// </summary>
    public class GenericElasticMaterial : ILinearElasticMaterial
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "G")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "E")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "G")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "E")]
        public GenericElasticMaterial(double rho, double E, double nu, double G)
        {
            this.Density = rho;
            this.YoungsModulus = E;
            this.PoissonsRatio = nu;
            this.ShearModulusElasticity = G;
        }
        
        public double Density
        {
            get;
            private set;
        }
        
        public double YoungsModulus
        {
            get;
            private set;
        }
        
        public double PoissonsRatio
        {
            get;
            private set;
        }
        
        public double ShearModulusElasticity
        {
            get;
            private set;
        }
    }
}
