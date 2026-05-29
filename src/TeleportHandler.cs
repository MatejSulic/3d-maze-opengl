using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Reflection;
using Zpg.src;

public class TeleportHandler
{
    private GameMap gameMap;  // Reference na herní mapu, která může obsahovat informace o úrovních, překážkách atd.
    private Camera camera;    // Kamera, kterou teleportujeme (měníme její pozici)
    private Shader shader;    // Shader (momentálně nepoužitý, ale může být využitý pro efekty při teleportaci)

    // Konstruktor přijímá herní mapu a kameru
    public TeleportHandler(GameMap gameMap, Camera camera)
    {
        this.gameMap = gameMap;
        this.camera = camera;
    }

    // Metoda pro přemístění kamery na cílovou pozici
    public void Teleport(Vector3 targetPosition)
    {
        // Nastaví pozici kamery na zadanou pozici (teleportace)
        camera.pos.X = targetPosition.X;
        camera.pos.Y = targetPosition.Y;
        camera.pos.Z = targetPosition.Z;
    }
}
