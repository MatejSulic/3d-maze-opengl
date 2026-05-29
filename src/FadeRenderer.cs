using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Zpg.src;

// Třída pro vykreslování přechodové animace "fade" (zesvětlení/ztmavení obrazovky) při teleportaci
public class FadeRenderer
{
    // Stav animace přechodu
    private enum FadeState
    {
        Idle,          // Neprobíhá žádná animace
        FadingIn,      // Zesvětlování (fade in)
        Teleporting,   // Teleportace - pozice se změní během tohoto stavu
        FadingOut      // Ztmavení (fade out)
    }

    private FadeState state = FadeState.Idle;   // Aktuální stav animace
    private float fadeProgress = 0.0f;          // Pokrok animace (0 = žádný efekt, 1 = plný efekt)
    private float fadeSpeed = 2.0f;             // Rychlost přechodu (fade)
    private Vector3 targetPosition;              // Cílová pozice teleportace

    private Shader fadeShader;                   // Shader pro vykreslení přechodu
    private uint fullscreenQuadVAO;              // VAO pro fullscreen čtverec
    private Camera camera;                       // Kamera pro scénu
    private TeleportHandler teleportHandler;    // Objekt, který se stará o samotnou teleportaci
    private CollisionHandler collisionHandler = new CollisionHandler(new List<Model>(), 1.7f, 0.5f, 0.5f, 0.5f);
    private int levelCount = 1;                  // Počet úrovní (patro, levelů) ve hře

    // Konstruktor - nastaví kameru, teleport handler, počet úrovní a inicializuje shader a fullscreen čtverec
    public FadeRenderer(Camera camera, TeleportHandler teleportHandler, int levelCount)
    {
        this.camera = camera;
        this.teleportHandler = teleportHandler;
        fadeShader = new Shader(".\\src\\shaders\\fade.vert", ".\\src\\shaders\\fade.frag");
        this.levelCount = levelCount;
        SetupFullscreenQuad();
    }

    // Nastaví VAO a VBO pro fullscreen čtverec, na který se aplikuje fade shader
    private void SetupFullscreenQuad()
    {
        float[] quadVertices = {
            // Pozice XY  |  Texturové souřadnice UV
            -1.0f,  1.0f,  0.0f, 1.0f, // levý horní roh
            -1.0f, -1.0f,  0.0f, 0.0f, // levý dolní roh
             1.0f,  1.0f,  1.0f, 1.0f, // pravý horní roh
             1.0f, -1.0f,  1.0f, 0.0f  // pravý dolní roh
        };

        GL.GenVertexArrays(1, out fullscreenQuadVAO);
        GL.GenBuffers(1, out uint quadVBO);

        GL.BindVertexArray(fullscreenQuadVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, quadVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);

        // Nastavení atributů vertexů - pozice (2 float), textura (2 float)
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

        GL.BindVertexArray(0);
    }

    // Zahájí přechodovou animaci na novou pozici
    public void StartFadeTransition(Vector3 newPosition)
    {
        if (state != FadeState.Idle) return; // pokud už animace běží, nedělej nic

        // Zkontroluj, jestli je cílová pozice validní (není mimo mapu)
        if (!collisionHandler.CheckIfTargetPositionIsValid(newPosition, levelCount))
            return;

        targetPosition = newPosition;  // nastav cílovou pozici
        fadeProgress = 0.0f;           // resetuj průběh přechodu
        state = FadeState.FadingIn;    // začni zesvětlovat obrazovku
    }

    // Aktualizace stavu animace - volat každý frame s delta časem
    public void Update(float deltaTime)
    {
        switch (state)
        {
            case FadeState.FadingIn:
                fadeProgress += fadeSpeed * deltaTime; // zvyšuj průběh animace fade in
                if (fadeProgress >= 1.0f)
                {
                    fadeProgress = 1.0f;           // max průběh
                    state = FadeState.Teleporting; // přepni do teleportace
                }
                break;

            case FadeState.Teleporting:
                teleportHandler.Teleport(targetPosition); // vykonej teleportaci
                state = FadeState.FadingOut;              // začni ztmavovat obrazovku (fade out)
                break;

            case FadeState.FadingOut:
                fadeProgress -= fadeSpeed * deltaTime;   // snižuj průběh animace fade out
                if (fadeProgress <= 0.0f)
                {
                    fadeProgress = 0.0f;
                    state = FadeState.Idle;                // animace skončila, zpět do klidu
                }
                break;

            case FadeState.Idle:
            default:
                // Nic se neděje, čeká se na start animace
                break;
        }
    }

    // Vykreslení přechodu (full screen quad s fade efektem)
    public void Render()
    {
        if (fadeProgress <= 0.0f)
            return; // pokud není žádný fade, nic nevybarvuj

        // Ulož stav GL nastavení, abychom mohli po vykreslení vše obnovit
        bool depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
        bool blendEnabled = GL.IsEnabled(EnableCap.Blend);

        int[] blendSrc = new int[1];
        int[] blendDst = new int[1];
        GL.GetInteger(GetPName.BlendSrc, blendSrc);
        GL.GetInteger(GetPName.BlendDst, blendDst);

        // Vypni test hloubky, zapni míchání průhlednosti a nastav správné míchání
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        fadeShader.Use(); // aktivuj shader
        int progressLocation = GL.GetUniformLocation(fadeShader.GetProgram(), "Progress");
        GL.Uniform1(progressLocation, fadeProgress); // pošli průběh fade do shaderu

        GL.BindVertexArray(fullscreenQuadVAO);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4); // vykresli čtverec přes celou obrazovku
        GL.BindVertexArray(0);

        // Obnov původní nastavení testu hloubky a míchání
        if (depthTestEnabled)
            GL.Enable(EnableCap.DepthTest);
        else
            GL.Disable(EnableCap.DepthTest);

        if (blendEnabled)
            GL.Enable(EnableCap.Blend);
        else
            GL.Disable(EnableCap.Blend);

        GL.BlendFunc((BlendingFactor)blendSrc[0], (BlendingFactor)blendDst[0]);
    }

    // Vlastnost - vrací, zda momentálně probíhá animace přechodu
    public bool IsAnimating => state != FadeState.Idle;

    // Uvolnění zdrojů - odstranění VAO
    public void Dispose()
    {
        GL.DeleteVertexArrays(1, ref fullscreenQuadVAO);
    }
}
