﻿//-----------------------------------------------------------------------
// <copyright file="FiniteElement.cs" company="SharpFE">
//     Copyright Iain Sproat, 2012.
// </copyright>
//-----------------------------------------------------------------------

namespace SharpFE
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using SharpFE.Stiffness;
    using SharpFE.Geometry;

    /// <summary>
    /// Finite elements connect nodes and define the relationship between these nodes.
    /// The finite element class defines the topology between nodes.
    /// The finite element class defines the local coordinate frame for the finite element in relation to the global frame.
    /// The StiffnessBuilder property connects to a separate object implementing IStiffnessBuilder - that class calculates the stiffness matrices, strain-displacement matrices, rotation matrices and shape functions for this element.
    /// </summary>
    public abstract class FiniteElement : IFiniteElement, IEquatable<FiniteElement>
    {
        /// <summary>
        /// The nodes of this element.
        /// </summary>
        private IList<IFiniteElementNode> nodeStore = new List<IFiniteElementNode>();
        
        /// <summary>
        /// The nodal degrees of freedom supported by this element.
        /// </summary>
        /// <param name="stiffness"></param>
        private IList<NodalDegreeOfFreedom> supportedGlobalNodalDof;
        
        /// <summary>
        /// The nodal degrees of freedom supported by this element.
        /// </summary>
        /// <param name="stiffness"></param>
        private IList<NodalDegreeOfFreedom> supportedLocalNodalDof;
        
        /// <summary>
        /// 
        /// </summary>
        private int hashAtWhichGlobalNodalDegreesOfFreedomWereLastBuilt;
        
        /// <summary>
        /// 
        /// </summary>
        private int hashAtWhichLocalNodalDegreesOfFreedomWereLastBuilt;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FiniteElement" /> class.
        /// </summary>
        protected FiniteElement()
        {
            // empty
        }
        
        /// <summary>
        /// Gets the nodes which comprise this element.
        /// </summary>
        /// <returns>Returns a shallow copy of the list of nodes which comprise this element.</returns>
        public IList<IFiniteElementNode> Nodes
        {
            get
            {
                return new List<IFiniteElementNode>(this.nodeStore);
            }
        }
        
        /// <summary>
        /// The point in the global coordinate frame which represents the origin in the local coordinate frame.
        /// </summary>
        public CartesianPoint LocalOrigin
        {
            get
            {
                return this.Nodes[0].Location;
            }
        }
        
        /// <summary>
        /// Gets the local x-axis of this finite element
        /// </summary>
        public abstract GeometricVector LocalXAxis
        {
            get;
        }
        
        /// <summary>
        /// Gets the local y-axis of this finite element.
        /// </summary>
        public abstract GeometricVector LocalYAxis
        {
            get;
        }
        
        /// <summary>
        /// Gets the local z-axis of this finite element.
        /// </summary>
        /// <remarks>
        /// Calculates the normalised cross-product of the x and y axes.
        /// </remarks>
        public GeometricVector LocalZAxis
        {
            get
            {
                return this.LocalXAxis.CrossProduct(this.LocalYAxis).Normalize(2);
            }
        }
        
        public abstract IList<DegreeOfFreedom> SupportedLocalBoundaryConditionDegreeOfFreedom
        {
            get;
        }
        
        public IList<DegreeOfFreedom> SupportedGlobalBoundaryConditionDegreeOfFreedom
        {
            get
            {
                IList<DegreeOfFreedom> globalBoundaryConditionDegreeOfFreedoms = new List<DegreeOfFreedom>(6);
                bool linear = this.IsASupportedBoundaryConditionDegreeOfFreedom(DegreeOfFreedom.X) || this.IsASupportedBoundaryConditionDegreeOfFreedom(DegreeOfFreedom.Y) || this.IsASupportedBoundaryConditionDegreeOfFreedom(DegreeOfFreedom.Z);
                bool rotational = this.IsASupportedBoundaryConditionDegreeOfFreedom(DegreeOfFreedom.XX) || this.IsASupportedBoundaryConditionDegreeOfFreedom(DegreeOfFreedom.YY) || this.IsASupportedBoundaryConditionDegreeOfFreedom(DegreeOfFreedom.ZZ);
                
                if (linear)
                {
                    globalBoundaryConditionDegreeOfFreedoms.Add(DegreeOfFreedom.X);
                    globalBoundaryConditionDegreeOfFreedoms.Add(DegreeOfFreedom.Y);
                    globalBoundaryConditionDegreeOfFreedoms.Add(DegreeOfFreedom.Z);
                }
                
                if (rotational)
                {
                    globalBoundaryConditionDegreeOfFreedoms.Add(DegreeOfFreedom.XX);
                    globalBoundaryConditionDegreeOfFreedoms.Add(DegreeOfFreedom.YY);
                    globalBoundaryConditionDegreeOfFreedoms.Add(DegreeOfFreedom.ZZ);
                }
                
                return globalBoundaryConditionDegreeOfFreedoms;
            }
        }
        
        /// <summary>
        /// Gets the nodal degrees of freedom supported by this finite element
        /// </summary>
        internal IList<NodalDegreeOfFreedom> SupportedGlobalNodalDegreeOfFreedoms
        {
            get
            {
                if (this.IsDirty(this.hashAtWhichGlobalNodalDegreesOfFreedomWereLastBuilt))
                {
                    this.SupportedGlobalNodalDegreeOfFreedoms = this.BuildSupportedGlobalNodalDegreeOfFreedoms();
                    this.hashAtWhichGlobalNodalDegreesOfFreedomWereLastBuilt = this.GetHashCode();
                }
                
                return this.supportedGlobalNodalDof;
            }
            
            private set
            {
                this.supportedGlobalNodalDof = value;
            }
        }
        
        /// <summary>
        /// Gets the nodal degrees of freedom supported by this finite element
        /// </summary>
        internal IList<NodalDegreeOfFreedom> SupportedLocalNodalDegreeOfFreedoms
        {
            get
            {
                if (this.IsDirty(this.hashAtWhichLocalNodalDegreesOfFreedomWereLastBuilt))
                {
                    this.SupportedLocalNodalDegreeOfFreedoms = this.BuildSupportedLocalNodalDegreeOfFreedoms();
                    this.hashAtWhichLocalNodalDegreesOfFreedomWereLastBuilt = this.GetHashCode();
                }
                
                return this.supportedLocalNodalDof;
            }
            
            private set
            {
                this.supportedLocalNodalDof = value;
            }
        }
        
        #region Equals and GetHashCode implementation
        /// <summary>
        /// 
        /// </summary>
        /// <param name="leftHandSide"></param>
        /// <param name="rightHandSide"></param>
        /// <returns></returns>
        public static bool operator ==(FiniteElement leftHandSide, FiniteElement rightHandSide)
        {
            if (ReferenceEquals(leftHandSide, rightHandSide))
            {
                return true;
            }
            
            if (ReferenceEquals(leftHandSide, null) || ReferenceEquals(rightHandSide, null))
            {
                return false;
            }
            
            return leftHandSide.Equals(rightHandSide);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="leftHandSide"></param>
        /// <param name="rightHandSide"></param>
        /// <returns></returns>
        public static bool operator !=(FiniteElement leftHandSide, FiniteElement rightHandSide)
        {
            return !(leftHandSide == rightHandSide);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as FiniteElement);
            
        }
        
        public bool Equals(FiniteElement other)
        {
            if (other == null)
            {
                return false;
            }
            
            int numberNodes = this.nodeStore.Count;
            if (numberNodes != other.nodeStore.Count)
            {
                return false;
            }
            
            for (int i = 0; i < numberNodes; i++)
            {
                if (!this.nodeStore[i].Equals(other.nodeStore[i]))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            //TODO hashcode should be calculated when the element is created (assuming the nodes
            int hashCode = 0;
            int i = 0;
            unchecked
            {
                foreach (IFiniteElementNode node in this.nodeStore)
                {
                    hashCode += (1000000000 + i++) * node.GetHashCode();
                }
                
                hashCode += 1000000007 * this.LocalOrigin.GetHashCode();
            }
            
            return hashCode;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append(this.GetType().FullName);
            sb.Append(", ");
            
            int numNodes = this.nodeStore.Count;
            if (numNodes == 0)
            {
                sb.Append("<no nodes>");
                sb.Append("]");
                return sb.ToString();
            }

            sb.Append("[");
            sb.Append(this.NodesToString());
            sb.Append("]}");
            
            return sb.ToString();
        }
        
        private string NodesToString()
        {
            const string delimiter = ", ";
            IEnumerable<string> nodes = this.nodeStore.Select(node => {
                                                                  return node.ToString();
                                                              });
            string nodesJoined = nodes.Aggregate((current, next) => current.ToString() + delimiter + next.ToString());
            return nodesJoined;
        }

        #endregion

        public CartesianPoint ConvertGlobalCoordinatesToLocalCoordinates(CartesianPoint globalPoint)
        {
            GeometricVector localCoordRelativeToLocalOrigin = globalPoint.Subtract(this.LocalOrigin);
            
            KeyedSquareMatrix<DegreeOfFreedom> rotationMatrix = CalculateElementRotationMatrix();
            CartesianPoint localCoord = new CartesianPoint(rotationMatrix.Multiply(localCoordRelativeToLocalOrigin));
            
            return new CartesianPoint(localCoord);
        }
        
        public CartesianPoint ConvertLocalCoordinatesToGlobalCoordinates(CartesianPoint localPoint)
        {
            KeyedSquareMatrix<DegreeOfFreedom> rotationMatrix = CalculateElementRotationMatrix().Transpose();
            CartesianPoint globalCoordRelativeToLocalOrigin = new CartesianPoint(rotationMatrix.Multiply(localPoint));
            
            GeometricVector globalCoord = globalCoordRelativeToLocalOrigin.Add(this.LocalOrigin);
            return new CartesianPoint(globalCoord);
        }
        
        public IDictionary<IFiniteElementNode, XYZ> CalculateLocalPositionsOfNodes()
        {
            IDictionary<IFiniteElementNode, XYZ> localPositions = new Dictionary<IFiniteElementNode, XYZ>(this.Nodes.Count);
            foreach (IFiniteElementNode node in this.Nodes)
            {
                localPositions.Add(node, this.ConvertGlobalCoordinatesToLocalCoordinates(node.Location));
            }
            
            return localPositions;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KeyedSquareMatrix<DegreeOfFreedom> CalculateElementRotationMatrix()
        {
            KeyedSquareMatrix<DegreeOfFreedom> rotationMatrix = FiniteElement.CreateFromRows(this.LocalXAxis, this.LocalYAxis, this.LocalZAxis);
            rotationMatrix = rotationMatrix.NormalizeRows(2);
            return rotationMatrix;
        }
        
        /// <summary>
        /// Determines whether a degree of freedom is supported by this element
        /// </summary>
        /// <param name="degreeOfFreedom"></param>
        /// <returns></returns>
        public bool IsASupportedBoundaryConditionDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom)
        {
            return this.SupportedLocalBoundaryConditionDegreeOfFreedom.Contains(degreeOfFreedom);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="previousHash"></param>
        /// <returns></returns>
        public bool IsDirty(int previousHash)
        {
            return this.GetHashCode() != previousHash;
        }
        
        /// <summary>
        /// Removes a node from the element.
        /// </summary>
        /// <param name="nodeToRemove">The node to remove from the element</param>
        internal void RemoveNode(IFiniteElementNode nodeToRemove)
        {
            this.nodeStore.Remove(nodeToRemove);
        }
        
        /// <summary>
        /// Adds a new node to the element.
        /// </summary>
        /// <param name="nodeToAdd">The node to add to the element</param>
        /// <exception cref="ArgumentNullException">Thrown if the node to add is null</exception>
        /// <exception cref="ArgumentException">Thrown if the node is already part of the finite element</exception>
        protected void AddNode(IFiniteElementNode nodeToAdd)
        {
            Guard.AgainstNullArgument(nodeToAdd, "nodeToAdd");
            
            if (this.Nodes.Contains(nodeToAdd))
            {
                throw new ArgumentException("Node is already part of this element");
            }
            
            this.ThrowIfNodeCannotBeAdded(nodeToAdd);
            this.nodeStore.Add(nodeToAdd);
        }
        
        /// <summary>
        /// Checks as to whether a new node can actually be added
        /// </summary>
        /// <param name="nodeToAdd">The candidate node to add to this element</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the node cannot be added.
        /// This might be because there would be too many nodes for the type of element (e.g. greater than 2 nodes for a spring),
        /// the node is a duplicate of an existing node, is too close to an existing node,
        /// would be out of the plane of the other nodes if this element is planar,
        /// or would be out of an acceptable order (e.g. create a 'twist' in a quadrilateral)
        /// </exception>
        protected abstract void ThrowIfNodeCannotBeAdded(IFiniteElementNode nodeToAdd);
        
        /// <summary>
        /// Builds the list of possible nodal degree of freedoms for this element which are expected by the model
        /// </summary>
        /// <returns>A list of all the possible nodal degree of freedoms for this element</returns>
        protected IList<NodalDegreeOfFreedom> BuildSupportedGlobalNodalDegreeOfFreedoms()
        {
            IList<NodalDegreeOfFreedom> nodalDegreeOfFreedoms = new List<NodalDegreeOfFreedom>();
            foreach (IFiniteElementNode node in this.nodeStore)
            {
                foreach (DegreeOfFreedom dof in this.SupportedGlobalBoundaryConditionDegreeOfFreedom)
                {
                    nodalDegreeOfFreedoms.Add(new NodalDegreeOfFreedom(node, dof));
                }
            }
            
            return nodalDegreeOfFreedoms;
        }
        
        protected IList<NodalDegreeOfFreedom> BuildSupportedLocalNodalDegreeOfFreedoms()
        {
            IList<NodalDegreeOfFreedom> nodalDegreeOfFreedoms = new List<NodalDegreeOfFreedom>();
            foreach (IFiniteElementNode node in this.nodeStore)
            {
                foreach (DegreeOfFreedom dof in this.SupportedLocalBoundaryConditionDegreeOfFreedom)
                {
                    nodalDegreeOfFreedoms.Add(new NodalDegreeOfFreedom(node, dof));
                }
            }
            
            return nodalDegreeOfFreedoms;
        }
        
        /// <summary>
        /// Creates a new keyed matrix from keyed vectors representing the rows of the new matrix
        /// </summary>
        /// <param name="axis1">The vector representing the first row</param>
        /// <param name="axis2">The vector representing the second row</param>
        /// <param name="axis3">The vector representing the third row</param>
        /// <returns>A matrix built from the vectors</returns>
        protected static KeyedSquareMatrix<DegreeOfFreedom> CreateFromRows(KeyedVector<DegreeOfFreedom> axis1, KeyedVector<DegreeOfFreedom> axis2, KeyedVector<DegreeOfFreedom> axis3)
        {
            ////TODO this should be devolved to the KeyedMatrix class
            Guard.AgainstBadArgument(
                "axis1",
                () => { return axis1.Count != 3; },
                "All axes should be 3D, i.e. have 3 items");
            Guard.AgainstBadArgument(
                "axis2",
                () => { return axis2.Count != 3; },
                "All axes should be 3D, i.e. have 3 items");
            Guard.AgainstBadArgument(
                "axis3",
                () => { return axis3.Count != 3; },
                "All axes should be 3D, i.e. have 3 items");
            Guard.AgainstBadArgument(
                "axis1",
                () => { return axis1.SumMagnitudes().IsApproximatelyEqualTo(0.0); },
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Axis should not be zero: {0}",
                    axis1));
            Guard.AgainstBadArgument(
                "axis2",
                () => { return axis2.SumMagnitudes().IsApproximatelyEqualTo(0.0); },
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Axis should not be zero: {0}",
                    axis2));
            Guard.AgainstBadArgument(
                "axis3",
                () => { return axis3.SumMagnitudes().IsApproximatelyEqualTo(0.0); },
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Axis should not be zero: {0}",
                    axis3));
            
            KeyedVector<DegreeOfFreedom> axis1Norm = axis1.Normalize(2);
            KeyedVector<DegreeOfFreedom> axis2Norm = axis2.Normalize(2);
            KeyedVector<DegreeOfFreedom> axis3Norm = axis3.Normalize(2);
            
            IList<DegreeOfFreedom> dof = new List<DegreeOfFreedom>(3) { DegreeOfFreedom.X, DegreeOfFreedom.Y, DegreeOfFreedom.Z };
            
            KeyedSquareMatrix<DegreeOfFreedom> result = new KeyedSquareMatrix<DegreeOfFreedom>(dof);
            result.At(DegreeOfFreedom.X, DegreeOfFreedom.X, axis1Norm[DegreeOfFreedom.X]);
            result.At(DegreeOfFreedom.X, DegreeOfFreedom.Y, axis1Norm[DegreeOfFreedom.Y]);
            result.At(DegreeOfFreedom.X, DegreeOfFreedom.Z, axis1Norm[DegreeOfFreedom.Z]);
            result.At(DegreeOfFreedom.Y, DegreeOfFreedom.X, axis2Norm[DegreeOfFreedom.X]);
            result.At(DegreeOfFreedom.Y, DegreeOfFreedom.Y, axis2Norm[DegreeOfFreedom.Y]);
            result.At(DegreeOfFreedom.Y, DegreeOfFreedom.Z, axis2Norm[DegreeOfFreedom.Z]);
            result.At(DegreeOfFreedom.Z, DegreeOfFreedom.X, axis3Norm[DegreeOfFreedom.X]);
            result.At(DegreeOfFreedom.Z, DegreeOfFreedom.Y, axis3Norm[DegreeOfFreedom.Y]);
            result.At(DegreeOfFreedom.Z, DegreeOfFreedom.Z, axis3Norm[DegreeOfFreedom.Z]);
            
            return result;
        }
    }
}
