using Microsoft.Xna.Framework;

namespace NEA.Materials;

public class Ice : IMaterial
{
    public float InverseMass { get; set; } = 11;
    public float Restitution { get; set; } = 0.3f;
    public float Friction { get; set; } = 0f;
    public Color Color { get; } = Color.Cyan;

}