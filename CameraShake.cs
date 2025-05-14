using System.Numerics;

namespace ded;

public class CameraShake
{
    public Vector2 CurrentShake {get; private set; } = Vector2.Zero;

    private float trauma = 0.0f;
    private float traumaReductionRate = 1.0f;
    private Random random = new Random();

    private List<Vector2> noiseSeed = new List<Vector2>();
    private int noiseIndex = 0;

    public CameraShake()
    {
        for (int i = 0; i < 120; i++)
        {
            noiseSeed.Add(new Vector2(
                (float)random.NextDouble() * 2 - 1,
                (float)random.NextDouble() * 2 - 1
            ));
        }
    }

    public void ShakeCamera(float intensity, float duration)
    {
        trauma = Math.Min(trauma + intensity, 1.0f);
        traumaReductionRate = 1.0f / Math.Max(duration, 0.01f);
    }

    public void Update(float deltaTime)
    {
        trauma = Math.Max(0, trauma - (deltaTime * traumaReductionRate));

        if (trauma <= 0)
        {
            CurrentShake = Vector2.Zero;
            return;
        }

        float shake = trauma * trauma;

        noiseIndex = (noiseIndex + 1) % noiseSeed.Count;

        CurrentShake = noiseSeed[noiseIndex] * shake * 10.0f;
    }
}
