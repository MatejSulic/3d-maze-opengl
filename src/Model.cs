using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Zpg.src
{
    /// <summary>
    /// Jednoduchý model obsahující seznam vrcholů
    /// </summary>
    public class Model : IDisposable
    {
        public Shader Shader { get; set; }
        public Material Material { get; set; } = new Material();
        Vector3 ambient = new Vector3(0.2f, 0.2f, 0.2f);
        public Vector3 position = new Vector3(0, -1.5f, 0);
        public int level;
        public bool isFloor = false;
        int vbo;
        int ibo;
        int vao;
        int triangles;

        public Model(Vertex[] vertices, int[] indices)
        {
            Create(vertices, indices);
        }

        public Model(string objFilename)
        {
            (Vertex[] vertices, int[] indices) = SimpleObj.LoadObj(objFilename);
            Create(vertices, indices);
        }

        protected void Create(Vertex[] vertices, int[] indices)
        {
            triangles = indices.Length / 3;
            // vytvoření a připojení VAO
            GL.GenVertexArrays(1, out vao);
            GL.BindVertexArray(vao);
            // Vytvoření a připojení VBO
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Vertex.SizeOf(), vertices, BufferUsageHint.StaticDraw);
            // vytvoření a připojení IBO
            ibo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), indices, BufferUsageHint.StaticDraw);
            // namapování pointerů na lokace v shaderu
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vertex.SizeOf(), nint.Zero);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Vertex.SizeOf(), (nint)3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.BindVertexArray(0);
        }

        /// <summary>
        /// Vykreslení modelu
        /// </summary>
        /// 

        //        uniform Material material;
        //uniform vec3 cameraPosWorld;
        //uniform vec3 lightPosWorld;  // This will be our flashlight position
        //uniform vec3 lightColor;
        //uniform float lightIntensity;
        //        uniform vec3 lightDirection; // This will be our flashlight direction
        //uniform float spotCutoff;    // Cosine of spotlight cutoff angle (add this uniform)
        //        uniform float spotExponent;  // Spotlight exponent for falloff (add this uniform)

        public void Draw(Camera camera, Light light)
        {

            Matrix4 translate = Matrix4.CreateTranslation(position.X, position.Y, position.Z);
            Shader.Use();
            Shader.SetUniform("projection", camera.Projection);
            Shader.SetUniform("view", camera.View);
            Shader.SetUniform("model", translate);


            Shader.SetUniform("ambientLight", ambient);
            //Shader.SetUniform("cameraPosWorld", camera.pos);
            Shader.SetUniform("lightPosWorld", light.position);
            Shader.SetUniform("lightColor", light.color);
            Shader.SetUniform("lightIntensity", light.intensity);
            Shader.SetUniform("lightDirection", light.direction);
            Shader.SetUniform("spotCutoff", (float)Math.Cos(MathHelper.DegreesToRadians(30f)));
            Shader.SetUniform("spotExponent", 1.5f);



            Material.SetUniforms(Shader);

            // Připojení bufferu
            GL.BindVertexArray(vao);
            //GL.Enable(EnableCap.CullFace);
            //GL.LineWidth(5);
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
            // Vykreslení pole vrcholů
            GL.DrawElements(PrimitiveType.Triangles, 3 * triangles, DrawElementsType.UnsignedInt, nint.Zero);
            GL.BindVertexArray(0);
        }

        #region Dispose - uvolnění paměti
        bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                GL.DeleteBuffer(vbo);
                disposed = true;
            }
        }
        #endregion
    }
}