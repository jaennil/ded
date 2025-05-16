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
    private Camera _camera;
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
    private List<FireEffect> _fireEffects = [];
    private List<Confetti> _confetti = [];
    private int _numBolts = 3;
    private Cursor _cursor;
    private Vector2 _mousePosition;
    private Vector2 _mouseCursorPos;
    private float _confettiSpeedMultiplier = 200.0f;
    private float _confettiLifetimeMultiplier = 2.0f;
    private float _confettiStartLifetime = 2.0f;
    private float _frameTime;
    private Vector2 _center;

    public App()
    {
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "ded");
        Raylib.SetTargetFPS(144);

        _center = new Vector2(ScreenWidth / 2, ScreenHeight / 2);

        _camera = new Camera(_center);

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
            _frameTime = Raylib.GetFrameTime();
            _camera.Update(_frameTime);
        }

        Cleanup();
    }

    private void MousePosToCursorPos()
    {
        _mouseCursorPos = (_mousePosition - _center + _cursor.WorldCoordinates) / new Vector2(_fontCharacterWidth, FontSize);
    }

    private void HandleMouseInput()
    {
        _mousePosition = Raylib.GetMousePosition();
        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            MousePosToCursorPos();
            var x = (int)_mouseCursorPos.X;
            var y = (int)_mouseCursorPos.Y;
            if (lines.Count > y && y >= 0)
            {
                if (lines[y].Length > x && x >= 0)
                {
                    lines[y] = lines[y].Remove(x, 1).Insert(x, " ");

                    var target = new Vector2(
                        x * _fontCharacterWidth + _fontCharacterWidth/2,
                        y * FontSize + FontSize / 2
                    );
                    CreateFireEffect(target, _fireEffects, _random);
                    _camera.Shake(1.0f, 0.1f);
                }
            }
        }
    }

    private void HandleInput()
    {
        HandleMouseInput();

        for (var chr = Raylib.GetCharPressed(); chr > 0; chr = Raylib.GetCharPressed())
        {
            float shakeIntensity = 2.0f + (float)_random.NextDouble() * 2.0f;
            _camera.Shake(shakeIntensity, 0.2f);
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
            _camera.Shake(1.5f, 0.15f);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            lines.Add("");
            CreateConfettiEffect(_cursor.WorldCoordinates, _confetti, _random, 100);
            _cursor.Down(0);
            _camera.Shake(1.5f, 0.2f);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.F2))
        {
            _debug = !_debug;
        }
        _camera.Target = _cursor.WorldCoordinates;
    }

    private void DrawConfetti()
    {
        foreach (var confetti in _confetti)
        {
            Color colorWithAlpha = ColorAlpha(confetti.Color, confetti.Alpha);
            
            // Calculate rotated vertices
            float cos = MathF.Cos(confetti.Angle);
            float sin = MathF.Sin(confetti.Angle);
            
            switch (confetti.Shape)
            {
                case 0: // Square
                    float halfSize = confetti.Size / 2;
                    
                    // Calculate rotated corners
                    Vector2 p1 = new Vector2(
                        confetti.Position.X + (-halfSize * cos - (-halfSize) * sin),
                        confetti.Position.Y + (-halfSize * sin + (-halfSize) * cos)
                    );
                    Vector2 p2 = new Vector2(
                        confetti.Position.X + (halfSize * cos - (-halfSize) * sin),
                        confetti.Position.Y + (halfSize * sin + (-halfSize) * cos)
                    );
                    Vector2 p3 = new Vector2(
                        confetti.Position.X + (halfSize * cos - halfSize * sin),
                        confetti.Position.Y + (halfSize * sin + halfSize * cos)
                    );
                    Vector2 p4 = new Vector2(
                        confetti.Position.X + (-halfSize * cos - halfSize * sin),
                        confetti.Position.Y + (-halfSize * sin + halfSize * cos)
                    );
                    
                    // Draw rotated rectangle using two triangles
                    Raylib.DrawTriangle(p1, p2, p3, colorWithAlpha);
                    Raylib.DrawTriangle(p1, p3, p4, colorWithAlpha);
                    break;
                    
                case 1: // Circle (no rotation needed)
                    Raylib.DrawCircleV(confetti.Position, confetti.Size / 2, colorWithAlpha);
                    break;
                    
                case 2: // Triangle
                    float height = confetti.Size / 2;
                    float base_half = confetti.Size / 2;
                    
                    // Calculate rotated vertices
                    Vector2 tp1 = new Vector2(
                        confetti.Position.X + (0 * cos - (-height) * sin),
                        confetti.Position.Y + (0 * sin + (-height) * cos)
                    );
                    Vector2 tp2 = new Vector2(
                        confetti.Position.X + (-base_half * cos - height * sin),
                        confetti.Position.Y + (-base_half * sin + height * cos)
                    );
                    Vector2 tp3 = new Vector2(
                        confetti.Position.X + (base_half * cos - height * sin),
                        confetti.Position.Y + (base_half * sin + height * cos)
                    );
                    
                    Raylib.DrawTriangle(tp1, tp2, tp3, colorWithAlpha);
                    break;
            }
        }
    }

    private void DrawFireEffects()
    {
        foreach (var fire in _fireEffects)
        {
            foreach (var particle in fire.Particles)
            {
                Raylib.DrawCircleV(particle.Position, particle.Radius, ColorAlpha(particle.Color, particle.Alpha));
            }
        }
    }

    private void DrawExplosions()
    {
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
    }

    void DrawLightnings()
    {
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
    }

    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(_backgroundColor);
        Raylib.BeginMode2D(_camera.Handle);
        if (_grid) DrawGrid();
        if (_coordinateAxis) DrawCoordinateAxis();
        DrawText();
        _cursor.Draw();
        DrawConfetti();
        DrawFireEffects();
        DrawExplosions();
        DrawLightnings();

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
            ImGui.Text($"Screen Width: {Raylib.GetScreenWidth()}");
            ImGui.Text($"Screen Height: {Raylib.GetScreenHeight()}");
            ImGui.Text($"Render Width: {Raylib.GetRenderWidth()}");
            ImGui.Text($"Render Height: {Raylib.GetRenderHeight()}");
            ImGui.Text($"CurrentMonitorWidth: {Raylib.GetMonitorWidth(Raylib.GetCurrentMonitor())}");
            ImGui.Text($"CurrentMonitorHeight: {Raylib.GetMonitorHeight(Raylib.GetCurrentMonitor())}");
            ImGui.Text($"Mouse Position {_mousePosition}");
            ImGui.Text($"Font Character Width: {_fontCharacterWidth}");
            ImGui.Text($"Cursor World Position: {_cursor.WorldCoordinates}");
            ImGui.Text($"Cursor Editor Position: {_cursor.EditorCoordinates}");
            ImGui.Text($"Mouse Cursor Pos: {_mouseCursorPos}");
            ImGui.Text($"Camera Offset: {_camera.Offset}");
            // ImGui.SliderFloat("Camera Zoom", ref _camera.Zoom, 0.1f, 100.0f);
            ImGui.Text($"Camera Target: {_camera.Target}");
            // ImGui.SliderFloat("Camera Rotation", ref _camera.Rotation, 0.0f, 360.0f);
            ImGui.SliderFloat("Grid Size", ref _gridSize, 0.0f, 500.0f);
            ImGui.SliderFloat("Confetti Speed Multiplier", ref _confettiSpeedMultiplier, 0.0f, 500.0f);
            ImGui.SliderFloat("Confetti Lifetimu Multiplier", ref _confettiLifetimeMultiplier, 0.0f, 500.0f);
            ImGui.SliderFloat("Confetti Start Lifetime", ref _confettiStartLifetime, 0.0f, 500.0f);
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

    struct FireEffect
    {
        public Vector2 Position;
        public float Lifetime;
        public float MaxLifetime;
        public List<Particle> Particles;
    }

    struct Confetti
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 AngularVelocity; // Rotation speed
        public float Size;
        public float Angle;
        public float Alpha;
        public Color Color;
        public float Lifetime;
        public float MaxLifetime;
        public int Shape; // 0 = square, 1 = circle, 2 = triangle
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

    void CreateConfettiEffect(Vector2 position, List<Confetti> confettiList, Random rand, int count = 50)
    {
         // Confetti colors - vibrant and varied
        Color[] confettiColors = new Color[]
        {
            new Color(255, 0, 0, 255),     // Red
            new Color(0, 255, 0, 255),     // Green
            new Color(0, 0, 255, 255),     // Blue
            new Color(255, 255, 0, 255),   // Yellow
            new Color(255, 0, 255, 255),   // Magenta
            new Color(0, 255, 255, 255),   // Cyan
            new Color(255, 165, 0, 255),   // Orange
            new Color(128, 0, 128, 255),   // Purple
            new Color(255, 192, 203, 255), // Pink
            new Color(173, 216, 230, 255)  // Light blue
        };

        for (int i = 0; i < count; i++)
        {
            // Random angle and speed for confetti
            float angle = (float)(rand.NextDouble() * Math.PI * 2);
            float speed = (float)(rand.NextDouble() * _confettiSpeedMultiplier + 3.0f);
            
            // Random size
            float size = (float)(rand.NextDouble() * 8.0f + 4.0f);
            
            // Random rotation and rotational velocity
            float startingAngle = (float)(rand.NextDouble() * Math.PI * 2);
            float rotationSpeed = (float)((rand.NextDouble() - 0.5) * 10.0f);
            
            // Random lifetime for varied falling duration
            float lifetime = (float)(rand.NextDouble() * _confettiLifetimeMultiplier + _confettiStartLifetime);
            
            // Choose a random color
            Color color = confettiColors[rand.Next(0, confettiColors.Length)];
            
            // Choose a random shape (0 = square, 1 = circle, 2 = triangle)
            int shape = rand.Next(0, 3);
            
            confettiList.Add(new Confetti
            {
                Position = position,
                Velocity = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed
                ),
                AngularVelocity = new Vector2(0, 0),
                Size = size,
                Angle = startingAngle,
                Alpha = 1.0f,
                Color = color,
                Lifetime = 0,
                MaxLifetime = lifetime,
                Shape = shape
            });
        }
    }

    void CreateFireEffect(Vector2 position, List<FireEffect> fireEffects, Random rand)
    {
         // Fire colors (red/orange/yellow gradient)
        Color[] fireColors = new Color[]
        {
            new Color(255, 0, 0, 255),     // Red
            new Color(255, 50, 0, 255),    // Red-orange
            new Color(255, 100, 0, 255),   // Orange-red
            new Color(255, 150, 0, 255),   // Orange
            new Color(255, 200, 0, 255),   // Yellow-orange
            new Color(255, 255, 0, 255)    // Yellow
        };
        
        // Create fire particles
        List<Particle> particles = new List<Particle>();
        int particleCount = rand.Next(20, 35); // Number of fire particles
        
        for (int i = 0; i < particleCount; i++)
        {
            // Random angle and speed for fire particles, mainly upward
            float angle = (float)(Math.PI * 0.5f + (rand.NextDouble() - 0.5) * Math.PI * 0.8);
            float speed = (float)(rand.NextDouble() * 3.0f + 1.5f);
            
            // Create particle with randomized properties
            float particleLifetime = (float)(rand.NextDouble() * 0.7f + 0.3f);
            
            // Choose a color for this fire particle
            Color particleColor = fireColors[rand.Next(0, fireColors.Length)];
            
            particles.Add(new Particle
            {
                Position = position,
                Velocity = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed
                ),
                Radius = (float)(rand.NextDouble() * 4.0f + 2.0f),
                Alpha = 0.9f,
                Color = particleColor,
                Lifetime = 0,
                MaxLifetime = particleLifetime
            });
        }
        
        // Create fire effect
        fireEffects.Add(new FireEffect
        {
            Position = position,
            Lifetime = 0,
            MaxLifetime = 1.0f,
            Particles = particles
        });
        
        // Also add some smoke particles that linger
        for (int i = 0; i < 8; i++)
        {
            float angle = (float)(Math.PI * 0.5f + (rand.NextDouble() - 0.5) * Math.PI * 0.6);
            float speed = (float)(rand.NextDouble() * 1.5f + 0.7f);
            
            _particles.Add(new Particle
            {
                Position = position,
                Velocity = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed
                ),
                Radius = (float)(rand.NextDouble() * 3.5f + 2.5f),
                Alpha = 0.5f,
                Color = new Color(100, 100, 100, 200),  // Gray smoke
                Lifetime = 0,
                MaxLifetime = (float)(rand.NextDouble() * 1.0f + 0.8f)
            });
        }
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
            // Update confetti particles
            for (int i = _confetti.Count - 1; i >= 0; i--)
            {
                var confetti = _confetti[i];
                
                // Update position
                confetti.Position.X += confetti.Velocity.X * Raylib.GetFrameTime();
                confetti.Position.Y += confetti.Velocity.Y * Raylib.GetFrameTime();
                
                // Apply gravity and air resistance
                confetti.Velocity.Y += 9.8f * Raylib.GetFrameTime(); // Gravity
                confetti.Velocity.X *= 0.99f; // Air resistance X
                confetti.Velocity.Y *= 0.99f; // Air resistance Y
                
                // Update rotation
                confetti.Angle += confetti.Velocity.X * 0.01f + (float)_random.NextDouble() * 0.02f; 
                
                // Add some horizontal wobble for realistic falling
                confetti.Position.X += (float)(_random.NextDouble() - 0.5) * 0.5f;
                
                // Update lifetime
                confetti.Lifetime += Raylib.GetFrameTime();
                
                // Calculate alpha - stay solid for a while, then fade out
                float normalizedTime = confetti.Lifetime / confetti.MaxLifetime;
                if (normalizedTime < 0.7f)
                {
                    confetti.Alpha = 1.0f;
                }
                else
                {
                    confetti.Alpha = 1.0f - ((normalizedTime - 0.7f) / 0.3f);
                }
                
                // Remove expired confetti
                if (confetti.Lifetime >= confetti.MaxLifetime)
                {
                    _confetti.RemoveAt(i);
                }
                else
                {
                    _confetti[i] = confetti;
                }
            }

            for (int i = _fireEffects.Count - 1; i >= 0; i--)
            {
                var fire = _fireEffects[i];
                
                // Update lifetime
                fire.Lifetime += Raylib.GetFrameTime();
                
                // Update all fire particles
                for (int j = fire.Particles.Count - 1; j >= 0; j--)
                {
                    var particle = fire.Particles[j];
                    
                    // Update position
                    particle.Position.X += particle.Velocity.X;
                    particle.Position.Y += particle.Velocity.Y;
                    
                    // Add some flickering movement
                    particle.Position.X += (float)((_random.NextDouble() - 0.5) * 0.8);
                    
                    // Make particles rise faster over time and get smaller
                    particle.Velocity.Y -= 0.05f;
                    particle.Radius *= 0.98f;
                    
                    // Update lifetime
                    particle.Lifetime += Raylib.GetFrameTime();
                    
                    // Calculate alpha - start at full, then fade out
                    float normalizedTime = particle.Lifetime / particle.MaxLifetime;
                    if (normalizedTime < 0.3f)
                    {
                        particle.Alpha = normalizedTime / 0.3f;
                    }
                    else
                    {
                        particle.Alpha = 1.0f - ((normalizedTime - 0.3f) / 0.7f);
                    }
                    
                    // Remove expired particles
                    if (particle.Lifetime >= particle.MaxLifetime)
                    {
                        fire.Particles.RemoveAt(j);
                    }
                    else
                    {
                        fire.Particles[j] = particle;
                    }
                }
                
                // Remove fire effect if all particles are gone or lifetime exceeded
                if (fire.Particles.Count == 0 || fire.Lifetime >= fire.MaxLifetime)
                {
                    _fireEffects.RemoveAt(i);
                }
                else
                {
                    _fireEffects[i] = fire;
                }
            }

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
