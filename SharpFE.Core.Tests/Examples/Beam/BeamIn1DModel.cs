﻿//-----------------------------------------------------------------------
// <copyright file="BeamIn1DModel.cs" company="Iain Sproat">
//     Copyright Iain Sproat, 2012.
// </copyright>
//-----------------------------------------------------------------------
using System;
using NUnit.Framework;

namespace SharpFE.Examples.Beam
{
	[TestFixture]
	public class BeamIn1DModel
	{
		/// <summary>
        /// Creates a cantilevered beam between two nodes spaced 1 metre apart.
        /// The beam is fully fixed at the first node and is free at the second node.
        /// We apply a downwards force at the second node
        /// </summary>
        [Test]
        public void Cantilever()
        {
            FiniteElementModel model = new FiniteElementModel(ModelType.Beam1D); // we will create and analyze a 1D beam system
            FiniteElementNode node1 = model.NodeFactory.Create(0); // create a node at the origin
            model.ConstrainNode(node1, DegreeOfFreedom.Z); // constrain the node from moving in the Z-axis
            model.ConstrainNode(node1, DegreeOfFreedom.YY); // constrain this node from rotating around the Y-axis

            FiniteElementNode node2 = model.NodeFactory.Create(1.0); // create a second node at a distance 1 metre along the X axis
            
            // TODO
            IMaterial material = new GenericElasticMaterial(0, 200000, 0, 0);
			ICrossSection section = new SolidRectangle(0.5, 0.1);
			
			model.ElementFactory.CreateLinear3DBeam(node1, node2, material, section); // create a spring between the two nodes of a stiffness of 2000 Newtons per metre
            
            ForceVector force = model.ForceFactory.CreateFor1DBeam(-10.0, 0); // Create a force of 10 Newtons in the z direction
            model.ApplyForceToNode(force, node2); // Apply that force to the second node
            
            IFiniteElementSolver solver = new LinearSolver(model); // Create a new instance of the solver class and pass it the model to solve
            FiniteElementResults results = solver.Solve(); // ask the solver to solve the model and return results
            
            ReactionVector reaction = results.GetReaction(node1); //get the reaction at the first node
            Assert.AreEqual(10, reaction.Z, 0.001);   // Check that we have calculated a reaction of 10 Newtons in the Z-axis
            Assert.AreEqual(-10, reaction.YY, 0.001); // Check that we have calculated a reaction of -10 NewtonMetres around the YY axis.
            
            DisplacementVector displacement = results.GetDisplacement(node2);  // get the displacement at the second node
            Assert.AreNotEqual(0.0, displacement.Z); // TODO calculate the actual value, rather than just checking we have any value
            Assert.AreNotEqual(0.0, displacement.YY); // TODO calculate the actual value, rather than just checking we have any value
        }
        
        /// <summary>
        /// Creates a beam between two nodes spaced 1 metre apart.
        /// The beam is pinned at the nodes, so is restricted from translating but not from rotating
        /// We apply a moment at node 1 and check that the same moment is transferred along the beam to node 2
        /// </summary>
        [Test]
        public void MomentThroughSimplySupportedBeam()
        {
            FiniteElementModel model = new FiniteElementModel(ModelType.Beam1D); // we will create and analyze a 1D beam system
            FiniteElementNode node1 = model.NodeFactory.Create(0); // create a node at the origin
            model.ConstrainNode(node1, DegreeOfFreedom.Z); // constrain the node from moving in the Z-axis

            FiniteElementNode node2 = model.NodeFactory.Create(1.0); // create a second node at a distance 1 metre along the X axis
            model.ConstrainNode(node2, DegreeOfFreedom.Z); // constrain the node from moving in the Z-axis
            
            // TODO
            IMaterial material = new GenericElasticMaterial(0, 200000, 0, 0);
			ICrossSection section = new SolidRectangle(0.5, 0.1);
			
			model.ElementFactory.CreateLinear3DBeam(node1, node2, material, section); // create a spring between the two nodes of a stiffness of 2000 Newtons per metre
            
            ForceVector moment = model.ForceFactory.CreateFor1DBeam(0, 10); // Create a clockwise(?) moment of 10 Newtonmetres around the yy axis
            model.ApplyForceToNode(moment, node1); // Apply that moment to the first node
            
            IFiniteElementSolver solver = new LinearSolver(model); // Create a new instance of the solver class and pass it the model to solve
            FiniteElementResults results = solver.Solve(); // ask the solver to solve the model and return results
            
            ReactionVector reaction = results.GetReaction(node1); //get the reaction at the first node
            Assert.AreEqual(-10, reaction.Z, 0.001);   // Check that we have calculated a reaction of 10 Newtons in the Z-axis
            Assert.AreEqual(0, reaction.YY, 0.001); // Check that we have calculated a reaction of -10 NewtonMetres around the YY axis.
            
            reaction = results.GetReaction(node2);
            Assert.AreEqual(10, reaction.Z, 0.001);
            Assert.AreEqual(0, reaction.YY, 0.001);
            
            DisplacementVector displacement = results.GetDisplacement(node1);  // get the displacement at the second node
            Assert.AreEqual(0, displacement.Z, 0.001); // Check that there is no displacement at the node
            double angleAtNode1 = displacement.YY; 
            
            displacement = results.GetDisplacement(node2);
            Assert.AreEqual(0, displacement.Z, 0.001);
            Assert.AreEqual(-1.0 * angleAtNode1 / 2.0, displacement.YY);
        }
        
        /// <summary>
        /// 10---->    (3)
        ///            / \
        ///           /   \
        ///          /     \
        ///         /       \
        ///       (2)       (4)
        ///        |         |
        ///        |         |
        ///        |         |
        ///        |         |
        ///       (1)       (5)
        /// </summary>
        [Test]
        public void PortalFrame()
        {
            FiniteElementModel model = new FiniteElementModel(ModelType.Frame2D); // we will create and analyze a 3D frame system
            FiniteElementNode node1 = model.NodeFactory.CreateForTruss(-10,0); 
            model.ConstrainNode(node1, DegreeOfFreedom.X);
            model.ConstrainNode(node1, DegreeOfFreedom.Z);
            model.ConstrainNode(node1, DegreeOfFreedom.YY);

            FiniteElementNode node2 = model.NodeFactory.CreateForTruss(-10,10);
            FiniteElementNode node3 = model.NodeFactory.CreateForTruss(0,14);
            FiniteElementNode node4 = model.NodeFactory.CreateForTruss(10,10);
            
            FiniteElementNode node5 = model.NodeFactory.CreateForTruss(10,0);
            model.ConstrainNode(node5, DegreeOfFreedom.X);
            model.ConstrainNode(node5, DegreeOfFreedom.Z);
            model.ConstrainNode(node5, DegreeOfFreedom.YY);
    
            IMaterial material = new GenericElasticMaterial(0, 2000, 0.2, 1000);
            ICrossSection section = new SolidRectangle(0.5, 0.1);
            
            model.ElementFactory.CreateLinear1DBeam(node1, node2, material, section);
            model.ElementFactory.CreateLinear1DBeam(node2,node3,material,section);
            model.ElementFactory.CreateLinear1DBeam(node3,node4,material,section);
            model.ElementFactory.CreateLinear1DBeam(node4,node5,material,section);
            
            ForceVector force = model.ForceFactory.Create(10, 0, 0,0,0,0);
            model.ApplyForceToNode(force, node3);
            
            IFiniteElementSolver solver = new LinearSolver(model);
            FiniteElementResults results = solver.Solve();
            
            DisplacementVector displacement = results.GetDisplacement(node2);
            Assert.AreNotEqual(0.0, displacement.X);
            Assert.AreEqual(0.0, displacement.Y, 0.001);
        }
	}
}
