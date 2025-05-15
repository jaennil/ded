using Raylib_cs;
using System.Numerics;

namespace ded;

public class Camera
{
    private Camera2D _handle;
    private Vector2 _center;
    private CameraShake _shake = new CameraShake();
    public Camera2D Handle => _handle;
    public Vector2 Offset => Handle.Offset;
    public float Zoom => Handle.Zoom;
    public float Rotation => Handle.Rotation;
    public Vector2 Target
    {
        get => Handle.Target;
        set
        {
            _handle.Target = value;
        }
    }

    public Camera(Vector2 center)
    {
        _center = center;

        _handle = new Camera2D
        {
            Target = Vector2.Zero,
            Offset = center,
            Rotation = 0,
            Zoom = 1
        };
    }

    public void Shake(float intensity, float duration)
    {
        _shake.ShakeCamera(intensity, duration);
    }

    public void Update(float frameTime)
    {
        _shake.Update(frameTime);
        _handle.Offset = _center + _shake.CurrentShake;
    }
}
