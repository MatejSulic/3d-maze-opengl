
using OpenTK.Mathematics;


namespace Zpg.src
{
    public class Camera
    {
        private float zoom = 1;
        public Vector3 pos;
        public float rx = 0;
        public float ry = 0;
        public Vector3 forward;
        public float speed = 5f;
        public float sensitivity = 0.3f;

        public Viewport viewport;
        public Camera(Viewport viewport, float cameraHeight, Vector3 startingPosition)
        {
            this.viewport = viewport;
            pos = startingPosition;

            forward = new Vector3(
            (float)(Math.Cos(rx) * Math.Sin(ry)),
            (float)Math.Sin(rx),
            (float)(-Math.Cos(rx) * Math.Cos(ry))
            ).Normalized();

        }

        public virtual Matrix4 Projection
        {
            get
            {
                float ratio = (float)(viewport.Width * viewport.window.ClientSize.X / (viewport.Height * viewport.window.ClientSize.Y));
                return Matrix4.CreatePerspectiveFieldOfView(zoom, ratio, 0.1f, 50);
            }
        }



        public void Move(float xdt, float ydt)
        {
            // Calculate desired movement vector
            Vector3 desiredMovement = new Vector3(
                speed * (float)(xdt * Math.Cos(ry) + ydt * Math.Sin(ry)),
                0,
                speed * (float)(xdt * Math.Sin(ry) - ydt * Math.Cos(ry))
            );

            // Apply movement (this will be handled by the Game class)
            pos += desiredMovement;
        }

        public void RotateX(float a)
        {
            rx += a * sensitivity;
            rx = (float)Math.Max(-Math.PI / 2, Math.Min(Math.PI / 2, rx));
            UpdateForward();
        }

        public void RotateY(float a)
        {
            ry += a * sensitivity;
            UpdateForward();
        }


        public virtual Matrix4 View
        {
            get
            {
                Matrix4 view;
                view = Matrix4.Identity;
                view *= Matrix4.CreateTranslation(-pos);
                view *= Matrix4.CreateRotationY(ry);
                view *= Matrix4.CreateRotationX(rx);
                return view;
            }

        }

        private void UpdateForward()
        {
            forward = new Vector3(
         (float)(Math.Sin(ry) * Math.Cos(rx)),
         (float)-Math.Sin(rx),
         (float)(-Math.Cos(ry) * Math.Cos(rx))
     ).Normalized();
        }
    }
}
