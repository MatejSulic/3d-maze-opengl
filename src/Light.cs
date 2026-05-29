using OpenTK.Mathematics;


namespace Zpg.src
{
    public class Light
    {
        public Vector3 position;
        public float lightHeight = 2.05f; // výška světla nad zemí
        public Vector3 color;
        public float intensity = 40f;
        public Vector3 direction;
        public float depresion = -2f;

        public Light(Vector3 posOrDir, bool isDirectional, Camera camera)
        {
            position = posOrDir;
            color = Vector3.One;
            direction = camera.forward;
        }

        public void UpdatePositionFromCamera(Camera camera)
        {
            // Aktualizace pozice světla
            position = camera.pos;
            position.Y += 0.35f;

            Vector3 forward = camera.forward.Normalized();

            // Osa rotace: right vektor
            Vector3 up = Vector3.UnitY;
            Vector3 right = Vector3.Cross(forward, up).Normalized();

            // Úhel rotace
            float angle = MathHelper.DegreesToRadians(depresion);
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            // Rodriguesova formule
            Vector3 rotated =
                forward * cos +
                Vector3.Cross(right, forward) * sin +
                right * Vector3.Dot(right, forward) * (1 - cos);

            direction = rotated.Normalized();


        }
    }
}

