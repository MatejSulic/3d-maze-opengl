using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Zpg.src;
// Hlavní třída hry, která dědí z GameWindow (OpenTK framework)
public class Game : GameWindow
{
    // Hlavní metoda, která spustí hru
    public static void Main() => new Game().Run();

    // Proměnné pro viewport, FPS, kameru, světlo, modely, mapu atd.
    Viewport mainViewport; // Nastavení zobrazení
    FPSVisualization fpsVisualization; // Počítání FPS

    Camera mainCamera; // Kamera
    float cameraHeight = 1.7f; // Výška kamery
    Vector3 startingPosition; // Výchozí pozice kamery

    Light light; // Světlo
    float lightHeight = 2.05f; // Výška světla

    List<Model> models = new(); // Seznam modelů
    CollisionHandler collisionHandler; // Řešení kolizí

    GameMap Map; // Herní mapa
    TeleportHandler teleportHandler; // Teleportace mezi úrovněmi
    FadeRenderer fadeRenderer; // Efekt přechodu mezi úrovněmi

    // Konstruktor hry, nastavuje velikost okna
    public Game() : base(GameWindowSettings.Default, new NativeWindowSettings() { ClientSize = new Vector2i(1800, 920) }) { }

    // Metoda volaná při načtení hry
    protected override void OnLoad()
    {
        base.OnLoad();
        VSync = VSyncMode.On; // Zapnutí V-Sync (omezení FPS)

        // Debugování OpenGL
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
        GL.DebugMessageCallback(DebugCallback, nint.Zero);
        GL.Enable(EnableCap.DepthTest); // Hloubkový test

        CursorState = CursorState.Grabbed; // Uchopení kurzoru myši

        // Nastavení viewportu
        mainViewport = new Viewport()
        {
            Left = 0,
            Top = 0,
            Width = 1,
            Height = 1,
            window = this
        };

        // Načtení mapy a výchozí pozice
        Map = new GameMap(".\\src\\maps\\MapWithLevels.txt");
        startingPosition = Map.startingPosition;

        // Příprava shaderů a materiálů pro různé objekty
        Shader cubeShader = new Shader(".\\src\\shaders\\basic.vert", ".\\src\\shaders\\basic.frag");
        Material cubeMaterial = new Material()
        {
            color = new Vector3(0.3f, 0.3f, 0.3f),
            diffuse = new Vector3(0.5f, 0.5f, 0.5f),
            specular = new Vector3(1, 1, 1),
            shininess = 0
        };

        // Další shadery a materiály pro různé typy objektů
        Shader elevatorShader = new Shader(".\\src\\shaders\\basic.vert", ".\\src\\shaders\\basic.frag");
        Material elevatorMaterial = new Material()
        {
            color = new Vector3(1f, 1f, 1f),
            diffuse = new Vector3(0.5f, 0.5f, 0.5f),
            specular = new Vector3(1, 1, 1),
            shininess = 0
        };

        Shader floorTileShader = new Shader(".\\src\\shaders\\basic.vert", ".\\src\\shaders\\basic.frag");
        Material floorTileMaterial = new Material()
        {
            color = new Vector3(0.1f, 0.1f, 0.1f),
            diffuse = new Vector3(0.5f, 0.5f, 0.5f),
            specular = new Vector3(1, 1, 1),
            shininess = 0
        };

        // Naplnění modelů podle mapy
        Map.FillModel(models, cubeShader, cubeMaterial, elevatorShader, elevatorMaterial, floorTileShader, floorTileMaterial);

        // Inicializace kamery, světla, kolizí a dalších handlerů
        mainCamera = new Camera(mainViewport, cameraHeight, startingPosition);
        light = new Light(new Vector3(0, lightHeight, 0), true, mainCamera);
        collisionHandler = new CollisionHandler(models, cameraHeight, Map.cubeWidth, Map.cubeHeight, Map.cubeDepth);
        fpsVisualization = new FPSVisualization();

        // Shader pro výtah a teleportace
        elevatorShader = new Shader(".\\src\\shaders\\elevatorShader.vert", ".\\src\\shaders\\elevatorShader.frag");
        teleportHandler = new TeleportHandler(Map, mainCamera);

        // Inicializace efektu přechodu mezi úrovněmi
        fadeRenderer = new FadeRenderer(mainCamera, teleportHandler, Map.levelCount);
    }

    // Debugovací callback pro OpenGL
    private static void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, nint message, nint userParam)
    {
        string msg = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(message, length);
        Console.WriteLine($"OpenGL Debug Message: {msg}\nSource: {source}, Type: {type}, Severity: {severity}, ID: {id}");
    }

    // Metoda volaná při změně velikosti okna
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        mainViewport.Set(); // Aktualizace viewportu
    }

    // Metoda volaná při vykreslování snímku
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        fpsVisualization.Update(); // Aktualizace FPS

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // Vyčištění obrazovky
        foreach (var model in models)
        {
            model.Draw(mainCamera, light); // Vykreslení modelů
        }

        fadeRenderer.Render(); // Vykreslení efektu přechodu

        // Zobrazení FPS a úrovně v titulku okna
        int Fps = fpsVisualization.GetCurrentFps();
        int cameraLevel = collisionHandler.GetCameraLevel(mainCamera.pos);
        this.Title = $"FPS: {Fps}, LEVEL: {cameraLevel}          PRESS E/R near elevator to go UP/DOWN";

        SwapBuffers(); // Přepnutí bufferů
    }

    // Metoda volaná při aktualizaci logiky hry
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        Vector2 direction = new Vector2(0, 0);

        fadeRenderer.Update((float)args.Time); // Aktualizace přechodového efektu

        if (fadeRenderer.IsAnimating) return; // Pokud probíhá animace, neprovádíme pohyb

        // Klávesové zkratky pro teleportaci
        if (KeyboardState.IsKeyPressed(Keys.T))
        {
            fadeRenderer.StartFadeTransition(Map.startingPosition);
            return;
        }
        if (KeyboardState.IsKeyPressed(Keys.E))
        {
            fadeRenderer.StartFadeTransition(Map.GetNextPosition(mainCamera.pos, true));
            return;
        }
        if (KeyboardState.IsKeyPressed(Keys.R))
        {
            fadeRenderer.StartFadeTransition(Map.GetNextPosition(mainCamera.pos, false));
            return;
        }

        // Pohyb kamery podle vstupu
        if (KeyboardState.IsKeyDown(Keys.W)) direction += Vector2.UnitY;
        if (KeyboardState.IsKeyDown(Keys.S)) direction -= Vector2.UnitY;
        if (KeyboardState.IsKeyDown(Keys.A)) direction -= Vector2.UnitX;
        if (KeyboardState.IsKeyDown(Keys.D)) direction += Vector2.UnitX;

        if (KeyboardState.IsKeyDown(Keys.Escape)) Close(); // Ukončení hry

        // Výpočet pohybu kamery
        Vector3 desiredMovement;
        if (direction.Length > 0)
        {
            direction *= (float)args.Time / direction.Length;
            desiredMovement = new Vector3(
                mainCamera.speed * (float)(direction.X * Math.Cos(mainCamera.ry) + direction.Y * Math.Sin(mainCamera.ry)),
                0,
                mainCamera.speed * (float)(direction.X * Math.Sin(mainCamera.ry) - direction.Y * Math.Cos(mainCamera.ry))
            );
        }
        else
        {
            desiredMovement = Vector3.Zero;
        }

        // Řešení kolizí a gravitace
        Vector3 actualMovement = collisionHandler.HandleMovement(mainCamera.pos, desiredMovement, (float)args.Time);
        mainCamera.pos += actualMovement;

        if (mainCamera.pos.Y < -5) // Pokud kamera spadne, teleportujeme ji zpět
        {
            fadeRenderer.StartFadeTransition(Map.startingPosition);
        }

        // Rotace kamery podle pohybu myši
        if (mouseDelta.LengthSquared > 0.000001)
        {
            mainCamera.RotateX(mouseDelta.Y * (float)args.Time);
            mainCamera.RotateY(mouseDelta.X * (float)args.Time);
            mouseDelta = Vector2.Zero;
        }

        light.UpdatePositionFromCamera(mainCamera); // Aktualizace pozice světla
    }

    // Zpracování pohybu myši
    Vector2 mouseDelta;
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        mouseDelta += new Vector2(e.DeltaX, e.DeltaY);
    }

    // Uvolnění zdrojů při ukončení hry
    protected override void OnUnload()
    {
        base.OnUnload();
        foreach (var model in models)
        {
            model.Dispose();
        }
        fadeRenderer.Dispose();
    }
}
