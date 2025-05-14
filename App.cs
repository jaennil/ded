using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

namespace ded;

public class App
{
    private Random _random = new Random();
    private const int ScreenWidth = 2000;
    private const int ScreenHeight = 1200;
    private readonly Font _font;
    private const int FontSize = 32;
    private int _fontSpacing = 1;
    private float _gridSize = 50.0f;
    private Camera2D _camera;
    private List<string> lines = [""];
    private readonly Color _textColor = Color.White;
    private readonly Color _backgroundColor = Color.Black;
    private readonly int _fontCharacterWidth;
    private bool _debug;
    private bool _coordinateAxis;
    private bool _grid;
    private List<Lightning> _bolts = [];
    private List<Particle> _particles = [];
    private List<Explosion> _explosions = [];
    private int _numBolts = 3;
    private CameraShake _cameraShake = new CameraShake();
    private Cursor _cursor;

    public App()
    {
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "ded");
        Raylib.SetTargetFPS(144);

        _camera = new Camera2D
        {
            Target = Vector2.Zero,
            Offset = new Vector2(ScreenWidth / 2, ScreenHeight / 2),
            Rotation = 0,
            Zoom = 1.0f,
        };

        _font = Raylib.LoadFontEx("FiraCodeNerdFont-Regular.ttf", FontSize, null, 0); 

        _fontCharacterWidth = (int)Raylib.MeasureTextEx(_font, "W", FontSize, _fontSpacing).X;

        _cursor = new Cursor(_fontCharacterWidth, FontSize, Color.White);

        rlImGui.Setup();
    }

    public void Run()
    {
        while (!Raylib.WindowShouldClose())
        {
            HandleInput();
            Draw();
            UpdateEffects();
            _cameraShake.Update(Raylib.GetFrameTime());
            _camera.Offset = new Vector2(
                ScreenWidth / 2 + _cameraShake.CurrentShake.X,
                ScreenHeight / 2 + _cameraShake.CurrentShake.Y
            );
        }

        Cleanup();
    }

    private void HandleInput()
    {
        for (var chr = Raylib.GetCharPressed(); chr > 0; chr = Raylib.GetCharPressed())
        {
            float shakeIntensity = 2.0f + (float)_random.NextDouble() * 2.0f;
            _cameraShake.ShakeCamera(shakeIntensity, 0.2f);
            var c = (char)chr;
            lines[(int)_cursor.EditorCoordinates.Y] += c;
            _cursor.Forward();
            if (c == ' ')
            {
                var target = _cursor.WorldCoordinates;
                target.X += _fontCharacterWidth / 2;
                target.Y += FontSize / 2;
                CreateLightningEffect(target, _bolts, _random);
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
        {
            var currentLine = lines[(int)_cursor.EditorCoordinates.Y];
            if (currentLine == "")
            {
                lines.Remove(currentLine);
                _cursor.Up(lines.Last().Length);
            }
            else
            {
                lines[(int)_cursor.EditorCoordinates.Y] = lines[(int)_cursor.EditorCoordinates.Y][..^1];
                _cursor.Back();
            }
            _cursor.UpdateWorldCoordinates();
            var target = _cursor.WorldCoordinates;
            target.X += _fontCharacterWidth / 2;
            target.Y += FontSize / 2;
            CreateExplosionEffect(target, _explosions, _random);
            _cameraShake.ShakeCamera(1.5f, 0.15f);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            lines.Add("");
            _cursor.Down(0);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.F2))
        {
            _debug = !_debug;
        }
        _camera.Target = _cursor.WorldCoordinates;
    }

    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(_backgroundColor);
        Raylib.BeginMode2D(_camera);
        if (_grid) DrawGrid();
        if (_coordinateAxis) DrawCoordinateAxis();
        DrawText();
        _cursor.Draw();
        foreach (var explosion in _explosions)
        {
            // Draw outer explosion
            Raylib.DrawCircleV(explosion.Position, explosion.Radius, 
                             ColorAlpha(explosion.OuterColor, explosion.Alpha * 0.6f));

            // Draw inner explosion core
            float coreRadius = explosion.Radius * 0.6f;
            Raylib.DrawCircleV(explosion.Position, coreRadius, 
                             ColorAlpha(explosion.CoreColor, explosion.Alpha * 0.8f));

            // Draw explosion debris particles
            foreach (var debris in explosion.Debris)
            {
                Raylib.DrawCircleV(debris.Position, debris.Radius, 
                                 ColorAlpha(debris.Color, debris.Alpha));
            }
        }
        foreach (var lightning in _bolts)
        {
            for (int i = 0; i < lightning.Segments.Count - 1; i++)
            {
                Raylib.DrawLineEx(
                    lightning.Segments[i],
                    lightning.Segments[i+1],
                    lightning.IsBranch ? 1.0f : 2.0f,
                    ColorAlpha(lightning.Color, lightning.Alpha)
                );
            }
        }
        Raylib.EndMode2D();
        if (_debug) DrawDebug();
        Raylib.EndDrawing();
    }

    private void DrawText()
    {
        Raylib.DrawTextEx(_font, string.Join('\n', lines), Vector2.Zero, FontSize, _fontSpacing, _textColor);
    }

    private void DrawDebug()
    {
        rlImGui.Begin();

        if (ImGui.Begin("Debug Panel"))
        {
            ImGui.Text($"Font Character Width: {_fontCharacterWidth}");
            ImGui.Text($"Cursor World Position: {_cursor.WorldCoordinates}");
            ImGui.Text($"Cursor Editor Position: {_cursor.EditorCoordinates}");
            ImGui.Text($"Camera Offset: {_camera.Offset}");
            ImGui.SliderFloat("Camera Zoom", ref _camera.Zoom, 0.1f, 100.0f);
            ImGui.Text($"Camera Target: {_camera.Target}");
            ImGui.SliderFloat("Camera Rotation", ref _camera.Rotation, 0.0f, 360.0f);
            ImGui.SliderFloat("Grid Size", ref _gridSize, 0.0f, 500.0f);
            ImGui.SliderInt("Font Spacing", ref _fontSpacing, 0, 100);
            ImGui.SliderInt("Num bolts", ref _numBolts, 0, 100);
            ImGui.Checkbox("Coordinate Axis", ref _coordinateAxis);
            ImGui.Checkbox("Grid", ref _grid);
        }
        ImGui.End();

        rlImGui.End();
    }

    private void Cleanup()
    {
        rlImGui.Shutdown();
        Raylib.CloseWindow();
    }

    private void DrawCoordinateAxis()
    {
        Raylib.DrawLine(-10000, 0, 10000, 0, Color.Green);
        Raylib.DrawLine(0, 10000, 0, -10000, Color.Green);
    }

    private void DrawGrid()
    {
        var _gridColor = Color.Red;
        float visibleWidth = Raylib.GetScreenWidth() / _camera.Zoom;
        float visibleHeight = Raylib.GetScreenHeight() / _camera.Zoom;

        // Calculate grid boundaries based on camera view
        float leftBound = _camera.Target.X - visibleWidth / 2 - _gridSize;
        float rightBound = _camera.Target.X + visibleWidth / 2 + _gridSize;
        float topBound = _camera.Target.Y - visibleHeight / 2 - _gridSize;
        float bottomBound = _camera.Target.Y + visibleHeight / 2 + _gridSize;

        // Adjust to nearest grid line
        leftBound = (float)Math.Floor(leftBound / _gridSize) * _gridSize;
        rightBound = (float)Math.Ceiling(rightBound / _gridSize) * _gridSize;
        topBound = (float)Math.Floor(topBound / _gridSize) * _gridSize;
        bottomBound = (float)Math.Ceiling(bottomBound / _gridSize) * _gridSize;

        // Draw vertical grid lines
        for (float x = leftBound; x <= rightBound; x += _gridSize)
        {
            Raylib.DrawLine((int)x, (int)topBound, (int)x, (int)bottomBound, _gridColor);
        }

        // Draw horizontal grid lines
        for (float y = topBound; y <= bottomBound; y += _gridSize)
        {
            Raylib.DrawLine((int)leftBound, (int)y, (int)rightBound, (int)y, _gridColor);
        }
    }

    struct Lightning
    {
        public Vector2 Start;
        public Vector2 End;
        public List<Vector2> Segments;
        public float Alpha;
        public float Lifetime;
        public float MaxLifetime;
        public Color Color;
        public int BranchCount;
        public bool IsBranch;
    }

    struct Explosion
    {
        public Vector2 Position;
        public float Radius;
        public float MaxRadius;
        public float Alpha;
        public float Lifetime;
        public float MaxLifetime;
        public Color CoreColor;
        public Color OuterColor;
        public List<Particle> Debris;
    }

    struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Radius;
        public float Alpha;
        public Color Color;
        public float Lifetime;
        public float MaxLifetime;
    }

    void CreateExplosionEffect(Vector2 position, List<Explosion> explosions, Random rand)
    {
        // Colors for explosion (orange/red tones)
        Color coreColor = new Color(255, 100, 20, 255);   // Bright orange-red
        Color outerColor = new Color(250, 170, 20, 255);  // Orange-yellow
        
        // Create debris particles
        List<Particle> debris = new List<Particle>();
        int debrisCount = rand.Next(15, 25); // Number of debris particles
        
        for (int i = 0; i < debrisCount; i++)
        {
            // Random angle and speed for debris
            float angle = (float)(rand.NextDouble() * Math.PI * 2);
            float speed = (float)(rand.NextDouble() * 5.0f + 2.0f);
            
            // Create particle with randomized properties
            float particleLifetime = (float)(rand.NextDouble() * 0.4f + 0.3f);
            
            // Choose a color variant for this debris particle
            Color debrisColor;
            float colorChoice = (float)rand.NextDouble();
            if (colorChoice < 0.4f)
                debrisColor = coreColor;
            else if (colorChoice < 0.7f)
                debrisColor = outerColor;
            else
                debrisColor = new Color(220, 220, 220, 255); // Some light smoke color
            
            debris.Add(new Particle
            {
                Position = position,
                Velocity = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed
                ),
                Radius = (float)(rand.NextDouble() * 5.0f + 1.0f),
                Alpha = 1.0f,
                Color = debrisColor,
                Lifetime = 0,
                MaxLifetime = particleLifetime
            });
        }
        
        // Create explosion
        explosions.Add(new Explosion
        {
            Position = position,
            Radius = 3.0f,  // Start small
            MaxRadius = 15.0f + (float)(rand.NextDouble() * 5.0f),
            Alpha = 1.0f,
            Lifetime = 0,
            MaxLifetime = 0.4f,
            CoreColor = coreColor,
            OuterColor = outerColor,
            Debris = debris
        });
        
        // Also add some smoke particles that linger
        for (int i = 0; i < 5; i++)
        {
            float angle = (float)(rand.NextDouble() * Math.PI * 2);
            float speed = (float)(rand.NextDouble() * 1.0f + 0.5f);
            
            _particles.Add(new Particle
            {
                Position = position,
                Velocity = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed - 0.3f  // Slight upward drift for smoke
                ),
                Radius = (float)(rand.NextDouble() * 3.0f + 2.0f),
                Alpha = 0.6f,
                Color = new Color(150, 150, 150, 200),  // Gray smoke
                Lifetime = 0,
                MaxLifetime = (float)(rand.NextDouble() * 0.8f + 0.6f)
            });
        }
    }

    void CreateLightningEffect(Vector2 position, List<Lightning> lightnings, Random rand)
    {
        Vector2 endPos = position;
        var top = Raylib.GetScreenHeight() / _camera.Zoom / 2;
        position = new Vector2(position.X, -top);

        // should be random
        // _numBolts = rand.Next(2, 4);
        
        for (int j = 0; j < _numBolts; j++)
        {
            // Determine end position (somewhere on screen edge)
            float angle = (float)(rand.NextDouble() * Math.PI * 2);
            float length = 200.0f + (float)(rand.NextDouble() * 200.0f);
            
            // Vector2 endPos = new Vector2(
            //     position.X + (float)Math.Cos(angle) * length,
            //     position.Y + (float)Math.Sin(angle) * length
            // );
            
            // Create lightning bolt
            Lightning bolt = new Lightning
            {
                Start = position,
                End = endPos,
                Segments = new List<Vector2>(),
                Alpha = 0.9f,
                Lifetime = 0,
                MaxLifetime = 0.5f + (float)(rand.NextDouble() * 0.2f),
                Color = new Color(255, 255, 100, 255),  // Yellow lightning
                BranchCount = 0,
                IsBranch = false
            };
            
            // Generate lightning path
            GenerateLightningPath(bolt.Start, bolt.End, bolt.Segments, 0.5f, 30.0f, rand);
            
            // Add main bolt to list
            lightnings.Add(bolt);
            
            // Add branches (smaller offshoots from main bolt)
            if (!bolt.IsBranch)
            {
                int branches = rand.Next(1, 3);
                for (int i = 0; i < branches; i++)
                {
                    // Pick a random segment to branch from
                    int segmentIndex = rand.Next(1, bolt.Segments.Count - 1);
                    Vector2 branchStart = bolt.Segments[segmentIndex];
                    
                    // Create a branch at a random angle from this point
                    float branchAngle = angle + (float)(rand.NextDouble() * Math.PI - Math.PI/2);
                    float branchLength = length * 0.3f;
                    
                    Vector2 branchEnd = new Vector2(
                        branchStart.X + (float)Math.Cos(branchAngle) * branchLength,
                        branchStart.Y + (float)Math.Sin(branchAngle) * branchLength
                    );
                    
                    // Create branch bolt
                    Lightning branch = new Lightning
                    {
                        Start = branchStart,
                        End = branchEnd,
                        Segments = new List<Vector2>(),
                        Alpha = 0.7f,
                        Lifetime = 0,
                        MaxLifetime = bolt.MaxLifetime * 0.8f,
                        Color = bolt.Color,
                        BranchCount = 0,
                        IsBranch = true
                    };
                    
                    // Generate branch path
                    GenerateLightningPath(branch.Start, branch.End, branch.Segments, 0.4f, 15.0f, rand);
                    
                    // Add branch to list
                    lightnings.Add(branch);
                }
            }
        }
    }
        
        // Helper method to generate a lightning path
        static void GenerateLightningPath(Vector2 start, Vector2 end, List<Vector2> segments, float chaosFactor, float displacement, Random rand)
        {
            segments.Add(start);
            
            // Calculate midpoint and then displace it
            if (Vector2.Distance(start, end) > 10)
            {
                Vector2 mid = new Vector2(
                    (start.X + end.X) / 2,
                    (start.Y + end.Y) / 2
                );
                
                // Apply random displacement to midpoint perpendicular to line
                float dx = end.X - start.X;
                float dy = end.Y - start.Y;
                float orthX = -dy;
                float orthY = dx;
                
                // Normalize and apply displacement
                float len = (float)Math.Sqrt(orthX * orthX + orthY * orthY);
                if (len > 0)
                {
                    orthX /= len;
                    orthY /= len;
                    
                    // Random displacement perpendicular to the line
                    float randDisplacement = ((float)rand.NextDouble() * 2 - 1) * displacement;
                    mid.X += orthX * randDisplacement;
                    mid.Y += orthY * randDisplacement;
                }
                
                // Recursively generate the lightning segments
                GenerateLightningPath(start, mid, segments, chaosFactor * 0.8f, displacement * 0.7f, rand);
                GenerateLightningPath(mid, end, segments, chaosFactor * 0.8f, displacement * 0.7f, rand);
            }
            else
            {
                segments.Add(end);
            }
        }

        void UpdateEffects()
        {
            for (int i = _bolts.Count - 1; i >= 0; i--)
            {
                var bolt = _bolts[i];

                bolt.Lifetime += Raylib.GetFrameTime();

                float normalizedTime = bolt.Lifetime / bolt.MaxLifetime;
                if (normalizedTime < 0.1f)
                {
                    bolt.Alpha = normalizedTime * 10.0f;
                }
                else
                {
                    bolt.Alpha = 1.0f - ((normalizedTime - 0.1f) / 0.9f);

                    if (normalizedTime > 0.3f)
                    {
                        Random rand = new Random();
                        bolt.Alpha *= (float)(0.7f + 0.3f * rand.NextDouble());
                    }
                }

                if (bolt.Lifetime >= bolt.MaxLifetime)
                {
                    _bolts.RemoveAt(i);
                }
                else
                {
                    _bolts[i] = bolt;
                }
            }

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var particle = _particles[i];
                
                // Update position
                particle.Position.X += particle.Velocity.X;
                particle.Position.Y += particle.Velocity.Y;
                
                // Update lifetime
                particle.Lifetime += Raylib.GetFrameTime();
                particle.Alpha = 1.0f - (particle.Lifetime / particle.MaxLifetime);
                
                // Remove expired particles
                if (particle.Lifetime >= particle.MaxLifetime)
                {
                    _particles.RemoveAt(i);
                }
                else
                {
                    _particles[i] = particle;
                }
            }

            for (int i = _explosions.Count - 1; i >= 0; i--)
            {
                var explosion = _explosions[i];
                
                // Update lifetime
                explosion.Lifetime += Raylib.GetFrameTime();
                float normalizedTime = explosion.Lifetime / explosion.MaxLifetime;
                
                // Update explosion radius - quick expand, then slight contraction
                if (normalizedTime < 0.7f)
                {
                    // Fast expansion
                    explosion.Radius = explosion.MaxRadius * (float)Math.Sin(normalizedTime * Math.PI * 0.7f);
                }
                else
                {
                    // Slight contraction at the end
                    explosion.Radius = explosion.MaxRadius * (0.9f - 0.1f * (normalizedTime - 0.7f) / 0.3f);
                }
                
                // Update alpha - fade out
                explosion.Alpha = 1.0f - normalizedTime;
                
                // Update debris particles
                for (int j = explosion.Debris.Count - 1; j >= 0; j--)
                {
                    var debris = explosion.Debris[j];
                    
                    // Update position
                    debris.Position.X += debris.Velocity.X;
                    debris.Position.Y += debris.Velocity.Y;
                    
                    // Apply gravity to debris
                    debris.Velocity.Y += 0.05f;
                    
                    // Update lifetime
                    debris.Lifetime += Raylib.GetFrameTime();
                    debris.Alpha = 1.0f - (debris.Lifetime / debris.MaxLifetime);
                    
                    // Remove expired debris
                    if (debris.Lifetime >= debris.MaxLifetime)
                    {
                        explosion.Debris.RemoveAt(j);
                    }
                    else
                    {
                        explosion.Debris[j] = debris;
                    }
                }
                
                // Remove expired _explosions
                if (explosion.Lifetime >= explosion.MaxLifetime)
                {
                    _explosions.RemoveAt(i);
                }
                else
                {
                    _explosions[i] = explosion;
                }
            }
        }

        static Color ColorAlpha(Color color, float alpha)
        {
            if (alpha < 0.0f) alpha = 0.0f;
            if (alpha > 1.0f) alpha = 1.0f;
            
            return new Color(color.R, color.G, color.B, (int)(255.0f * alpha));
        }
}
