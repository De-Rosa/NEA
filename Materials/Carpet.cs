using Microsoft.Xna.Framework;

namespace NEA.Materials;

public class Carpet : IMaterial
{
    public float InverseMass => 5;
    public float Restitution => 0.3f;
    public float Friction => 0.8f;
    public Color Color => Color.Gray;

}