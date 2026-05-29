
using OpenTK.Mathematics;
using System.Globalization;


namespace Zpg.src
{
    public class SimpleObj
    {
        public static (Vertex[] verticecs, int[] indices) LoadObj(string filename)
        {
            string[] lines = File.ReadAllLines(filename);
            int cnt = 0;
            for (cnt = 0; cnt < lines.Length; cnt++)
            {
                if (lines[cnt][0] != 'v') break;
            }
            Vertex[] vertices = new Vertex[cnt];
            for (int i = 0; i < cnt; i++)
            {
                var tokens = lines[i].Split(' ');
                float.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
                float.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
                float.TryParse(tokens[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z);
                vertices[i] = new Vertex(new Vector3(x, y, z), Vector3.Zero);
            }

            int[] indices = new int[3 * (lines.Length - cnt)];
            int k = 0;
            for (int i = cnt; i < lines.Length; i++)
            {
                var tokens = lines[i].Split(' ');
                int.TryParse(tokens[1], out int i1);
                int.TryParse(tokens[2], out int i2);
                int.TryParse(tokens[3], out int i3);
                indices[k + 0] = i1 - 1;
                indices[k + 1] = i2 - 1;
                indices[k + 2] = i3 - 1;
                k += 3;
            }

            SimpleNormals(vertices, indices);

            return (vertices, indices);
        }

        private static void SimpleNormals(Vertex[] vertices, int[] indices)
        {
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 v1 = vertices[indices[i + 1]].position - vertices[indices[i]].position;
                Vector3 v2 = vertices[indices[i + 2]].position - vertices[indices[i]].position;

                Vector3 norm = Vector3.Cross(v1, v2).Normalized();

                vertices[indices[i]].normal += norm;
                vertices[indices[i + 1]].normal += norm;
                vertices[indices[i + 2]].normal += norm;
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].normal.Normalize();
            }
        }
    }
}
