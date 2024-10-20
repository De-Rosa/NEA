using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NEA.Bodies;
using NEA.Materials;

namespace NEA.Objects.RigidBodies;

// Pole class, creates a pole IObject.
public class Pole : RigidBody, IObject
{
    public void Update(List<RigidBody> rigidBodies, float deltaTime)
    {
        Step(rigidBodies, deltaTime);
    }

    private Pole(IMaterial material, Skeleton skeleton, bool isStatic, bool isFloor) : base(material, skeleton, isStatic, isFloor) {}

    public static Pole FromSize(IMaterial material, Vector2 centroid, float size, bool isStatic = false, bool isFloor = false)
    {
        Skeleton skeleton = new Skeleton();

        float adjustment = (float) 0.1 * size;
        skeleton.AddVectors(new Vector2[]
        {
            new Vector2(centroid.X + adjustment, centroid.Y + adjustment * 3.5f), // bottom right
            new Vector2(centroid.X, centroid.Y + adjustment * 3.5f), // bottom middle
            new Vector2(centroid.X - adjustment, centroid.Y + adjustment * 3.5f), // bottom left
            new Vector2(centroid.X - adjustment, centroid.Y - adjustment * 3.5f), // top left
            new Vector2(centroid.X, centroid.Y - adjustment * 3.5f), // top middle
            new Vector2(centroid.X + adjustment, centroid.Y - adjustment * 3.5f) // top right
        });
        
        return new Pole(material, skeleton, isStatic, isFloor);
    }
}