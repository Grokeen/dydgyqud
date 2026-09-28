using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniFortress
{
    // Runtime-built low-poly 3D parts for fighters, weapons and platforms. Gameplay stays on the z = 0 plane;
    // these meshes only add depth around it. Meshes, materials and textures are shared for the whole session.
    public static class FortressModel3D
    {
        static readonly Dictionary<PrimitiveType, Mesh> primitives = new Dictionary<PrimitiveType, Mesh>();
        static readonly Dictionary<(Color32, Texture, int, int), Material> materials = new Dictionary<(Color32, Texture, int, int), Material>();
        static Shader litShader;
        static Texture2D stoneTexture, plankTexture;

        public static Texture2D StoneTexture => stoneTexture ? stoneTexture : stoneTexture = MakeTexture(false);
        public static Texture2D PlankTexture => plankTexture ? plankTexture : plankTexture = MakeTexture(true);

        static Shader LitShader
        {
            get
            {
                if (litShader) return litShader;
                foreach (string name in new[] { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Standard" })
                    if (litShader = Shader.Find(name)) break;
                return litShader;
            }
        }

        public static Mesh PrimitiveMesh(PrimitiveType type)
        {
            if (primitives.TryGetValue(type, out var mesh) && mesh) return mesh;
            var temp = GameObject.CreatePrimitive(type);
            mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return primitives[type] = mesh;
        }

        public static Material Lit(Color color, float smoothness = .2f, float metallic = 0, Texture texture = null)
        {
            var key = ((Color32)color, texture, Mathf.RoundToInt(smoothness * 100), Mathf.RoundToInt(metallic * 100));
            if (materials.TryGetValue(key, out var material) && material) return material;
            material = new Material(LitShader) { name = "Fortress 3D " + ColorUtility.ToHtmlStringRGB(color), enableInstancing = true };
            material.SetColor("_BaseColor", color); material.SetColor("_Color", color);
            material.SetFloat("_Smoothness", smoothness); material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);
            if (texture) { material.SetTexture("_BaseMap", texture); material.SetTexture("_MainTex", texture); }
            return materials[key] = material;
        }

        public static Transform Group(Transform parent, string name, Vector3 position = default, Vector3 euler = default)
        {
            var item = new GameObject(name).transform;
            item.SetParent(parent, false);
            item.localPosition = position; item.localRotation = Quaternion.Euler(euler);
            return item;
        }

        public static MeshRenderer Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale,
            Color color, Vector3 euler = default, float smoothness = .2f, float metallic = 0)
        {
            var item = Group(parent, name, position, euler);
            item.localScale = scale;
            item.gameObject.AddComponent<MeshFilter>().sharedMesh = PrimitiveMesh(type);
            var renderer = item.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Lit(color, smoothness, metallic);
            renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
            return renderer;
        }

        // A cylinder or capsule stretched between two local points.
        public static MeshRenderer Limb(Transform parent, string name, Vector3 from, Vector3 to, float thickness, Color color,
            PrimitiveType type = PrimitiveType.Capsule, float smoothness = .2f, float metallic = 0)
        {
            Vector3 along = to - from;
            var renderer = Part(parent, name, type, (from + to) * .5f, new Vector3(thickness, along.magnitude * .5f, thickness),
                color, default, smoothness, metallic);
            renderer.transform.localRotation = Quaternion.FromToRotation(Vector3.up, along);
            return renderer;
        }

        // Arrow and spear models share the sprite convention: the tip sits at the local origin and the shaft runs to -X.
        public static Transform Projectile(Transform parent, FortressWeapon weapon)
        {
            var root = Group(parent, weapon == FortressWeapon.Spear ? "Spear 3D" : "Arrow 3D");
            if (weapon == FortressWeapon.Spear)
            {
                Limb(root, "Shaft", new Vector3(-3.45f, 0, 0), new Vector3(-.3f, 0, 0), .1f, new Color(.42f, .27f, .15f), PrimitiveType.Cylinder, .3f);
                Part(root, "Blade", PrimitiveType.Sphere, new Vector3(-.28f, 0, 0), new Vector3(.62f, .17f, .07f), new Color(.78f, .8f, .84f), default, .75f, .85f);
                Part(root, "Socket", PrimitiveType.Cylinder, new Vector3(-.62f, 0, 0), new Vector3(.14f, .09f, .14f), new Color(.55f, .45f, .25f), new Vector3(0, 0, 90), .6f, .7f);
                Part(root, "Grip", PrimitiveType.Cylinder, new Vector3(-1.9f, 0, 0), new Vector3(.13f, .3f, .13f), new Color(.2f, .12f, .08f), new Vector3(0, 0, 90));
            }
            else
            {
                Limb(root, "Shaft", new Vector3(-1.85f, 0, 0), new Vector3(-.14f, 0, 0), .05f, new Color(.72f, .56f, .36f), PrimitiveType.Cylinder, .3f);
                Part(root, "Head", PrimitiveType.Sphere, new Vector3(-.12f, 0, 0), new Vector3(.28f, .11f, .04f), new Color(.75f, .78f, .82f), default, .75f, .85f);
                Color feather = new Color(.85f, .22f, .18f);
                Part(root, "Fletching", PrimitiveType.Cube, new Vector3(-1.65f, 0, 0), new Vector3(.32f, .14f, .015f), feather, new Vector3(0, 0, 8));
                Part(root, "Fletching cross", PrimitiveType.Cube, new Vector3(-1.65f, 0, 0), new Vector3(.32f, .015f, .14f), new Color(.92f, .9f, .82f));
            }
            return root;
        }

        public static Transform Bow(Transform parent, Vector3 grip)
        {
            var root = Group(parent, "Bow 3D", grip);
            Color wood = new Color(.36f, .21f, .1f);
            const int segments = 10;
            Vector3 Point(int i)
            {
                float t = i / (float)segments * 2 - 1;
                return new Vector3(.34f * (1 - t * t), 1.22f * t, 0);
            }
            for (int i = 0; i < segments; i++)
                Limb(root, "Limb " + i, Point(i), Point(i + 1), .1f - Mathf.Abs(i - segments * .5f + .5f) * .008f, wood, PrimitiveType.Capsule, .35f);
            Part(root, "Grip wrap", PrimitiveType.Cylinder, new Vector3(.34f, 0, 0), new Vector3(.15f, .16f, .15f), new Color(.15f, .1f, .07f));
            Limb(root, "String", Point(0), Point(segments), .018f, new Color(.9f, .88f, .8f), PrimitiveType.Cylinder);
            return root;
        }

        // Box mesh with world-unit UVs, so neighbouring platforms share one continuous brick or plank pattern.
        public static void BuildBox(Mesh mesh, Vector3 size, Vector3 worldCentre, float tile)
        {
            var vertices = new List<Vector3>(24); var normals = new List<Vector3>(24);
            var uvs = new List<Vector2>(24); var triangles = new List<int>(36);
            Vector3 half = size * .5f;
            void Face(Vector3 normal, Vector3 u, Vector3 v)
            {
                Vector3 centre = Vector3.Scale(normal, half), a = Vector3.Scale(u, half), b = Vector3.Scale(v, half);
                int start = vertices.Count;
                Vector3[] corners = { centre - a - b, centre + a - b, centre + a + b, centre - a + b };
                foreach (Vector3 corner in corners)
                {
                    Vector3 world = worldCentre + corner;
                    vertices.Add(corner); normals.Add(normal);
                    uvs.Add(new Vector2(Vector3.Dot(world, u), Vector3.Dot(world, v)) / tile);
                }
                bool clockwise = Vector3.Dot(Vector3.Cross(a, b), normal) > 0;
                triangles.AddRange(clockwise ? new[] { start, start + 1, start + 2, start, start + 2, start + 3 }
                                             : new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            }
            Face(Vector3.back, Vector3.right, Vector3.up); Face(Vector3.forward, Vector3.right, Vector3.up);
            Face(Vector3.left, Vector3.forward, Vector3.up); Face(Vector3.right, Vector3.forward, Vector3.up);
            Face(Vector3.up, Vector3.right, Vector3.forward); Face(Vector3.down, Vector3.right, Vector3.forward);
            mesh.Clear();
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
        }

        static float Hash(int a, int b)
        {
            unchecked
            {
                uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663);
                h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                return (h & 0xffff) / 65535f;
            }
        }

        // Greyscale 128 px tiles (tinted by the material): offset stone blocks, or horizontal boards for drop-through bridges.
        static Texture2D MakeTexture(bool planks)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            { name = planks ? "Fortress planks" : "Fortress stone", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4 };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float value;
                    if (planks)
                    {
                        int row = y / 16;
                        bool seam = y % 16 < 2 || (x + row * 37) % 96 < 2;
                        float grain = Mathf.Sin(x * .21f + Hash(row, 5) * 12 + Mathf.Sin(y * .9f) * .8f) * .06f;
                        value = seam ? .32f : .74f + Hash(row, 9) * .16f + grain + (Hash(x, y) - .5f) * .05f;
                    }
                    else
                    {
                        int row = y / 32, shifted = x + (row % 2) * 32, column = shifted / 64;
                        bool mortar = y % 32 < 3 || shifted % 64 < 3;
                        float bevel = y % 32 > 28 ? .08f : y % 32 < 6 ? -.06f : 0;
                        value = mortar ? .38f : .7f + Hash(row, column % 2 + row * 3) * .22f + bevel + (Hash(x, y) - .5f) * .12f;
                    }
                    byte v = (byte)(Mathf.Clamp01(value) * 255);
                    pixels[y * size + x] = new Color32(v, v, v, 255);
                }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }
    }
}
