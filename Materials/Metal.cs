using Microsoft.Xna.Framework;

namespace Physics.Materials;

public class Rubber : IMaterial
{
    public float Density { get; } = 11;
    public float Restitution { get; } = 0.7f;
    public float Friction { get;  } = 0.5f;
    public Color Color { get; } = Color.Gray;
}