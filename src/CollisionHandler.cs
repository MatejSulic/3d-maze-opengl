using OpenTK.Mathematics;
using System.Reflection;

namespace Zpg.src
{
    public class CollisionHandler
    {
        private readonly List<Model> models;          // Seznam všech modelů, se kterými kontrolujeme kolize
        private readonly float playerRadius;          // Poloměr hráče (pro kolizní oblast)
        private readonly float playerHeight;          // Výška hráče
        private readonly float cubeWidth;             // Šířka kvádru (modelu)
        private readonly float cubeHeight;            // Výška kvádru (modelu)
        private readonly float cubeDepth;             // Hloubka kvádru (modelu)
        private float fallSpeed = 3f;                  // Rychlost pádu hráče
        private const float gravity = -9.81f;          // Konstantní gravitační zrychlení

        // Konstruktor - inicializuje základní parametry a modely
        public CollisionHandler(List<Model> models, float playerHeight, float cubeWidth, float cubeHeight, float cubeDepth)
        {
            this.models = models;
            playerRadius = 0.3f;
            this.playerHeight = playerHeight;
            this.cubeWidth = cubeWidth;
            this.cubeHeight = cubeHeight;
            this.cubeDepth = cubeDepth;
        }

        // Určí aktuální patro (level) podle Y souřadnice pozice kamery/ hráče
        public int GetCameraLevel(Vector3 position)
        {
            return (int)Math.Floor(position.Y / 3.2f);
        }

        // Řeší pohyb hráče s ohledem na kolize a gravitaci
        public Vector3 HandleMovement(Vector3 currentPosition, Vector3 desiredMovement, float deltaTime)
        {
            Vector3 newPosition = currentPosition + desiredMovement;

            int cameraLevel = GetCameraLevel(currentPosition);
            bool isOnFloor = IsOnFloor(currentPosition, cameraLevel);

            // Projdi všechny modely a zkontroluj kolize
            foreach (var model in models)
            {
                if (model.isFloor) continue; // Přeskoč podlahu
                if (model.level != cameraLevel) continue; // Přeskoč modely na jiném patře
                if (CheckCollision(newPosition, model.position))
                {
                    // Pokud nastane kolize, zkus pohyb po hraně překážky (slide)
                    return SlideAgainstCollision(currentPosition, desiredMovement, cameraLevel);
                }
            }

            // Pokud je hráč na podlaze, zastav pád
            if (isOnFloor)
            {
                fallSpeed = 0f;
            }
            else
            {
                // Pokud není na podlaze, aplikuj gravitaci a pád
                fallSpeed += gravity * deltaTime;
                desiredMovement.Y += fallSpeed * deltaTime;
            }

            return desiredMovement;
        }

        // Zjistí, jestli je hráč právě na podlaze (daný level)
        private bool IsOnFloor(Vector3 position, int cameraLevel)
        {
            foreach (var model in models)
            {
                if (!model.isFloor) continue;     // Pouze modely typu podlaha
                if (model.level != cameraLevel) continue; // Pouze na správném patře

                // Kontrola kolize hráče s podlahou
                if (CheckCollision(position, model.position))
                {
                    // Hráč je na podlaze, pokud jeho spodní část je pod nebo na úrovni podlahy
                    if (position.Y - playerHeight <= model.position.Y)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Kontrola kolize mezi hráčem a kvádrem (modelem)
        public bool CheckCollision(Vector3 playerPos, Vector3 cubeCornerPos)
        {
            // Určení hranic hráče ve 3D prostoru (box kolem hráče)
            float playerMinX = playerPos.X - playerRadius;
            float playerMaxX = playerPos.X + playerRadius;
            float playerMinZ = playerPos.Z - playerRadius;
            float playerMaxZ = playerPos.Z + playerRadius;
            float playerMinY = playerPos.Y;
            float playerMaxY = playerPos.Y + playerHeight;

            // Určení hranic kvádru (modelu)
            float cubeMinX = cubeCornerPos.X;
            float cubeMaxX = cubeCornerPos.X + cubeWidth;
            float cubeMinZ = cubeCornerPos.Z;
            float cubeMaxZ = cubeCornerPos.Z + cubeDepth;
            float cubeMinY = cubeCornerPos.Y;
            float cubeMaxY = cubeCornerPos.Y + cubeHeight;

            // Detekce překrytí (kolize) v ose X, Y i Z
            return playerMaxX >= cubeMinX &&
                   playerMinX <= cubeMaxX &&
                   playerMaxY >= cubeMinY &&
                   playerMinY <= cubeMaxY &&
                   playerMaxZ >= cubeMinZ &&
                   playerMinZ <= cubeMaxZ;
        }

        // Zajišťuje, aby hráč při kolizi mohl "klouzat" podél překážky místo úplného zastavení
        private Vector3 SlideAgainstCollision(Vector3 currentPos, Vector3 desiredMovement, int cameraLevel)
        {
            // Zkus pohyb jen v ose X
            Vector3 xMove = new Vector3(desiredMovement.X, 0, 0);
            if (!CheckCollisionAtPosition(currentPos + xMove, cameraLevel))
            {
                return xMove;
            }

            // Zkus pohyb jen v ose Z
            Vector3 zMove = new Vector3(0, 0, desiredMovement.Z);
            if (!CheckCollisionAtPosition(currentPos + zMove, cameraLevel))
            {
                return zMove;
            }

            // Zkus diagonální pohyb (kombinace X i Z)
            Vector3 xzMove = new Vector3(desiredMovement.X, 0, desiredMovement.Z);
            if (!CheckCollisionAtPosition(currentPos + xzMove, cameraLevel))
            {
                return xzMove;
            }

            // Pokud žádný pohyb neprojde, hráč zůstane stát
            return Vector3.Zero;
        }

        // Zkontroluje, jestli je daná pozice volná od kolizí na daném patře
        private bool CheckCollisionAtPosition(Vector3 position, int cameraLevel)
        {
            foreach (var model in models)
            {
                if (model.isFloor) continue;        // Ignoruj podlahu
                if (model.level != cameraLevel) continue; // Jen aktuální patro

                if (CheckCollision(position, model.position))
                {
                    return true; // Našel kolizi
                }
            }

            return false; // Bez kolize
        }

        // Zkontroluje, jestli je cílová pozice teleportu v povoleném rozsahu
        public bool CheckIfTargetPositionIsValid(Vector3 targetPosition, int levelCount)
        {
            Console.WriteLine($"level Count: {levelCount}");
            // Ověření, zda Y souřadnice je v rámci mapy (není mimo patra)
            if (targetPosition.Y < 0 || targetPosition.Y > levelCount * (3 + 0.2))
            {
                Console.WriteLine("Nelze teleportovat mimo mapu.");
                return false;
            }
            return true;
        }
    }
}
