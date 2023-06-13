using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Physics.Bodies;
using Physics.Bodies.Physics;

namespace Physics.Objects.RigidBodies;

public class Joint
{
    private readonly RigidBody _bodyA;
    private readonly RigidBody _bodyB;

    private readonly int _indexA;
    private readonly int _indexB;

    private const float MaximumTorque = 15;
    private float _currentTorque;

    public Joint(RigidBody bodyA, RigidBody bodyB, int pointIndexA, int pointIndexB)
    {
        _bodyA = bodyA;
        _bodyB = bodyB;
        _indexA = pointIndexA;
        _indexB = pointIndexB;

        _currentTorque = 0;
    }
    
    public void Step()
    {
        Vector2 vectorAB = GetPointB() - GetPointA();
        if (vectorAB == Vector2.Zero) return;
        float depth = vectorAB.Length();
        vectorAB.Normalize();
        _bodyA.GetSkeleton().Move(vectorAB * depth / 2);
        _bodyB.GetSkeleton().Move(-vectorAB * depth / 2);

        Impulses.ResolveJoint(_bodyB, _bodyA, new List<Vector2>(){GetPointA(), GetPointB()}, vectorAB);
    }

    public Vector2 GetPointA()
    {
        return _bodyA.GetSkeleton().GetVectors()[_indexA];
    }
    
    public Vector2 GetPointB()
    {
        return _bodyB.GetSkeleton().GetVectors()[_indexB];
    }

    public RigidBody GetBodyA()
    {
        return _bodyA;
    }

    public RigidBody GetBodyB()
    {
        return _bodyB;
    }

    public void SetTorque(float amount)
    {
        float changeInTorque = amount - _currentTorque;
        if (changeInTorque is > MaximumTorque or < -MaximumTorque) return;
        
        _currentTorque = amount;
        _bodyB.AddAngularVelocity(changeInTorque * 3);
    }

    public float GetTorque()
    {
        return _currentTorque;
    }
}