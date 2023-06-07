using Microsoft.Xna.Framework;

namespace Physics.Materials;

public class Wood : IMaterial
{
    public float Density { get; set; } = 15;
    public float Restitution { get; set; } = 0.3f;
    public float Friction { get; set; } = 0.1f;
    public Color Color { get; } = Color.SandyBrown;

}