using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace rtxtest
{ 
    struct Light
    {
        public Vector3 position;
        public float intensity;
    }

    struct Material
    {
        public Vector3 diffuse;
        public Vector2 albedo;
        public float specular;
        
    }
    struct Sphere
    {
        public Vector3 center;
        public float radius;
        public Material material;

        public Sphere(Vector3 vector, float _r, Material _m)
        {
            center = vector;
            radius = _r;
            material = _m;
        }

        public float? RayIntersected(Vector3 origin, Vector3 direction)
        {
            Vector3 L = center - origin;//vector between ray's origin & sphere's center
            float tca = Vector3.Dot(L, direction);
            float d2 = Vector3.Dot(L, L) - tca*tca;
            if (d2 > radius * radius)
                return null;
            float thc = MathF.Sqrt(radius * radius - d2);
            float t0 = tca - thc;
            float t1 = tca + thc;
            if (t0 < 0) t0 = t1;
            if (t0 < 0) return null;
            return t0;
        }
    }

    class Program
    {

        public Program()
        {
            Console.WriteLine("Dick");
            throw new Exception();
        }

        static void Main(string[] args)
        {
            Program a = null;
            try
            {
                a = new Program();
            }
            catch
            {

            }
            Console.WriteLine(a);


            Render();
            Console.WriteLine("Hello World!");
        }

        private static Vector3 Reflect(ref Vector3 I, ref Vector3 N)
        {
            return I - N * 2f * Vector3.Dot(I, N);
        }
        private static Vector3 Cast(ref Vector3 orig, ref Vector3 dir, ref Sphere[] spheres, ref Light[] lights)
        {
            float dist = System.Single.MaxValue;
            Material m = new Material();
            Vector3 color = new Vector3();
            Vector3 N = new Vector3();
            Vector3 intersection = new Vector3();
            float diffuseLightLevel = 0;
            float specularLightLevel = 0;
            foreach(var s in spheres)
            {
                float? e = s.RayIntersected(orig, dir);
                if (e != null && dist > e)
                {
                    dist = e.Value;
                    m = s.material;
                    intersection = orig + dir * dist;
                    N = intersection - s.center;//NON NORMALIZED!
                }
            }
            if (dist < 1000f)
            {
                N = Vector3.Normalize(N);
                foreach(var l in lights)
                {
                    Vector3 lightdir = Vector3.Normalize( l.position - intersection);
                    float lightDist = Vector3.Distance(l.position, intersection);


                    diffuseLightLevel += MathF.Max(0, Vector3.Dot(lightdir, N)) * l.intensity ;
                    specularLightLevel += MathF.Pow(MathF.Max(0f, Vector3.Dot(Reflect(ref lightdir, ref N), dir)), m.specular) * l.intensity;
                }
                //if (diffuseLightLevel > 1) diffuseLightLevel = 1;
                color = m.diffuse * diffuseLightLevel * m.albedo.X + (new Vector3(1f,1f,1f))*specularLightLevel*m.albedo.Y;
            }
            return color;
        }
        private static void Render()
        {
            Light[] lights = new Light[3] { new Light() { intensity = 1.5f, position = new Vector3(-20f,20f,20f)} ,
                                            new Light() { intensity = 1.8f, position = new Vector3(30f,50f,-25f)} ,
                                            new Light() { intensity = 1.7f, position = new Vector3(30f,20f,30f)} };
            Sphere[] spheres = new Sphere[4] { new Sphere(new Vector3(-3f, 0f, -16f), 2f, new Material() { diffuse = new Vector3(.3f, 0f, .8f), albedo = new Vector2(.6f,0.3f), specular = 50f }),
                                                new Sphere(new Vector3(-1f, -1.5f, -12f), 2f, new Material() { diffuse = new Vector3(.3f, 0f, .8f), albedo = new Vector2(.6f,0.3f), specular = 50f }),
                                                new Sphere(new Vector3(1.5f, -.5f, -18f), 3f, new Material() { diffuse = new Vector3(.3f, 0f, .8f), albedo = new Vector2(.6f,0.3f), specular = 50f }),
                                               new Sphere(new Vector3(7f, 5f, -18f), 4f, new Material() { diffuse = new Vector3(.3f, .1f, .1f), albedo = new Vector2(.9f,.1f), specular = 10f })};

            const int width = 1024;
            const int height = 768;
            const float fov = MathF.PI/2;
            Vector3[] framebuffer = new Vector3[width * height];

            Vector3 position = new Vector3(0, 0, 0);

            long par = 0;

            var sw = new Stopwatch();
                sw.Start();
                Parallel.For(0, 8, new Action<int>((kek) =>
                {
                    for (int j = kek * height / 8; j < height / 8 * (kek + 1); j++)
                    {
                        for (int i = 0; i < width; i++)
                        {
                            float x = (2f * ((float)i + 0.5f) / (float)width - 1) * MathF.Tan(fov / 2f) * width / (float)height;
                            float y = -(2f * ((float)j + 0.5f) / (float)height - 1) * MathF.Tan(fov / 2f);
                            Vector3 direction = Vector3.Normalize(new Vector3(x, y, -1));
                            framebuffer[j * width + i] = Cast(ref position, ref direction, ref spheres, ref lights);
                        }
                    }
                }));
                sw.Stop();
                par += sw.ElapsedMilliseconds;
                
                
            Console.WriteLine(par + "- par");

            FileStream file = new FileStream("out.ppm",FileMode.Create);
            var sr = new StreamWriter(file);

            file.Write(Encoding.ASCII.GetBytes("P6\n" + width + " " + height + "\n255\n"));
            for (int i = 0; i < height * width; ++i)
            {
                Vector3 c = framebuffer[i];
                float max = MathF.Max(c.X, MathF.Max(c.Y, c.Z));
                if (max > 1) c *= 1f / max;
                  file.WriteByte((byte)(255 * MathF.Max(.0f, MathF.Min(1f, c.X))));
                  file.WriteByte((byte)(255 * MathF.Max(.0f, MathF.Min(1f, c.Y))));
                  file.WriteByte((byte)(255 * MathF.Max(.0f, MathF.Min(1f, c.Z))));
            }

        }
    }



}
