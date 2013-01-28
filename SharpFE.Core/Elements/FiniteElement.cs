﻿//-----------------------------------------------------------------------
// <copyright file="FiniteElement.cs" company="SharpFE">
//     Copyright Iain Sproat, 2012.
// </copyright>
//-----------------------------------------------------------------------

namespace SharpFE
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using MathNet.Numerics.LinearAlgebra.Double;
    using SharpFE.Stiffness;

    /// <summary>
    /// Finite elements connect nodes and define the relationship between these nodes.
    /// The finite element class defines the topology between nodes.
    /// The finite element class defines the local coordinate frame for the finite element in relation to the global frame.
    /// The StiffnessBuilder property connects to a separate object implementing IStiffnessBuilder - that class calculates the stiffness matrices, strain-displacement matrices, rotation matrices and shape functions for this element.
    /// </summary>
    public abstract class FiniteElement : IFiniteElement
    {
        /// <summary>
        /// The nodes of this element.
        /// </summary>
        private IList<FiniteElementNode> nodeStore = new List<FiniteElementNode>();
        
        /// <summary>
        /// The nodal degrees of freedom supported by this element.
        /// </summary>
        /// <param name="stiffness"></param>
        private IList<NodalDegreeOfFreedom> supportedNodalDof;
        
        /// <summary>
        /// 
        /// </summary>
        private int hashAtWhichNodalDegreesOfFreedomWereLastBuilt;
        
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
        public IList<FiniteElementNode> Nodes
        {
            get
            {
                return new List<FiniteElementNode>(this.nodeStore);
            }
        }
        
        /// <summary>
        /// Gets the local x-axis of this finite element
        /// </summary>
        public abstract KeyedVector<DegreeOfFreedom> LocalXAxis
        {
            get;
        }
        
        /// <summary>
        /// Gets the local y-axis of this finite element.
        /// </summary>
        public abstract KeyedVector<DegreeOfFreedom> LocalYAxis
        {
            get;
        }
        
        /// <summary>
        /// Gets the local z-axis of this finite element.
        /// </summary>
        /// <remarks>
        /// Calculates the normalised cross-product of the x and y axes.
        /// </remarks>
        public KeyedVector<DegreeOfFreedom> LocalZAxis
        {
            get
            {
                return this.LocalXAxis.CrossProduct(this.LocalYAxis).Normalize(2);
            }
        }
        
        /// <summary>
        /// Gets the nodal degrees of freedom supported by this finite element
        /// </summary>
        internal IList<NodalDegreeOfFreedom> SupportedNodalDegreeOfFreedoms
        {
            get
            {
                if (this.IsDirty(this.hashAtWhichNodalDegreesOfFreedomWereLastBuilt))
                {
                    this.SupportedNodalDegreeOfFreedoms = this.BuildSupportedGlobalNodalDegreeOfFreedoms();
                    this.hashAtWhichNodalDegreesOfFreedomWereLastBuilt = this.GetHashCode();
                }
                
                return this.supportedNodalDof;
            }
            
            private set
            {
                this.supportedNodalDof = value;
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
            FiniteElement other = obj as FiniteElement;
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
            int hashCode = 0;
            int i = 0;
            unchecked
            {
                foreach (FiniteElementNode node in this.nodeStore)
                {
                    hashCode += (1000000000 + i++) * node.GetHashCode();
                }
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
            sb.Append("[");
            sb.Append(this.GetType().FullName);
            sb.Append(", ");
            
            int numNodes = this.nodeStore.Count;
            if (numNodes == 0)
            {
                sb.Append("<no nodes>]");
                return sb.ToString();
            }
            
            for (int i = 0; i < numNodes; i++)
            {
                sb.Append("<");
                sb.Append(this.nodeStore[i].ToString());
                sb.Append(">");
                if (i < numNodes - 1)
                {
                    sb.Append(", ");
                }
            }
            
            sb.Append("]");
            
            return sb.ToString();
        }

        #endregion

        /// <summary>
        /// Determines whether a degree of freedom is supported by this element
        /// </summary>
        /// <param name="degreeOfFreedom"></param>
        /// <returns></returns>
        public abstract bool IsASupportedBoundaryConditionDegreeOfFreedom(DegreeOfFreedom degreeOfFreedom);
        
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
        internal void RemoveNode(FiniteElementNode nodeToRemove)
        {
            this.nodeStore.Remove(nodeToRemove);
        }
        
        /// <summary>
        /// Adds a new node to the element.
        /// </summary>
        /// <param name="nodeToAdd">The node to add to the element</param>
        /// <exception cref="ArgumentNullException">Thrown if the node to add is null</exception>
        /// <exception cref="ArgumentException">Thrown if the node is already part of the finite element</exception>
        protected void AddNode(FiniteElementNode nodeToAdd)
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
        protected abstract void ThrowIfNodeCannotBeAdded(FiniteElementNode nodeToAdd);
        
        /// <summary>
        /// Builds the list of possible nodal degree of freedoms for this element which are expected by the model
        /// </summary>
        /// <returns>A list of all the possible nodal degree of freedoms for this element</returns>
        protected IList<NodalDegreeOfFreedom> BuildSupportedGlobalNodalDegreeOfFreedoms() ////TODO make abstract and require derived classes to implement for their specific requirements
        {
            IList<NodalDegreeOfFreedom> nodalDegreeOfFreedoms = new List<NodalDegreeOfFreedom>();
            foreach (FiniteElementNode node in this.nodeStore)
            {
                nodalDegreeOfFreedoms.Add(new NodalDegreeOfFreedom(node, DegreeOfFreedom.X));
                nodalDegreeOfFreedoms.Add(new NodalDegreeOfFreedom(node, DegreeOfFreedom.Y));
                nodalDegreeOfFreedoms.Add(new NodalDegreeOfFreedom(node, DegreeOfFreedom.Z));
                nodalDegreeOfFreedoms.Add(new NodalDegreeOfFreedom(node, DegreeOfFreedom.XX));
                nodalDegreeOfFreedoms.Add(new NodalDegreeOfFreedom(node, DegreeOfFreedom.YY));
                nodalDegreeOfFreedoms.Add(new NodalDegreeOfFreedom(node, DegreeOfFreedom.ZZ));
            }
            
            return nodalDegreeOfFreedoms;
        }
    }
}
