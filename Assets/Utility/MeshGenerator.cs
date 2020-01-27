using UnityEngine;

namespace Assets.Utility
{
    public static class MeshGenerator
    {
        public static MeshData GenerateMeshData(float[,] heightMap, Vector3 dimensions, int levelOfDetail = 1)
        {
            Vector2Int size = heightMap.Dimensions();
            size.x = (size.x - 1) / levelOfDetail + 1;
            size.y = (size.y - 1) / levelOfDetail + 1;

            Vector2 res = new Vector2(dimensions.x / (size.x - 1), dimensions.z / (size.y - 1));

            Vector3[] vertices = new Vector3[size.x * size.y];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[(size.x - 1) * (size.y - 1) * 6];
            int triangleIndex = 0;

            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    vertices[y * size.x + x] = new Vector3(x * res.x, heightMap[x * levelOfDetail, y * levelOfDetail] * dimensions.y, y * res.y);
                    uvs[y * size.x + x] = new Vector2((float)x / size.x, (float)y / size.y);

                    if (x >= size.x - 1 || y >= size.y - 1) continue;
                    int ind1 = x + y * size.x;
                    int ind2 = ind1 + size.x;
                    //first triangle
                    triangles[triangleIndex] = ind1;
                    triangles[triangleIndex + 1] = ind2;
                    triangles[triangleIndex + 2] = ind2 + 1;
                    //second triangle
                    triangles[triangleIndex + 3] = ind1;
                    triangles[triangleIndex + 4] = ind2 + 1;
                    triangles[triangleIndex + 5] = ind1 + 1;
                    triangleIndex += 6;
                }
            }
            return new MeshData(vertices, triangles, uvs);
        }

        public static Mesh GenerateTerrain(MeshData data)
        {
            Mesh mesh = new Mesh
            {
                vertices = data.Vertices,
                triangles = data.Triangles,
                uv = data.Uvs
            };
            mesh.RecalculateNormals();
            return mesh;
        }
        public static Mesh GenerateTerrain(float[,] heightMap, Vector3 dimensions, int levelOfDetail = 1) => GenerateTerrain( GenerateMeshData(heightMap, dimensions, levelOfDetail) );

        public static void UpdateTerrain(Mesh mesh, float[,] heightMap, int levelOfDetail = 1)
        {
            Vector2Int size = heightMap.Dimensions();
            size.x = (size.x - 1) / levelOfDetail + 1;
            size.y = (size.y - 1) / levelOfDetail + 1;
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    mesh.vertices[y * size.x + x].y = heightMap[x * levelOfDetail, y * levelOfDetail];
                }
            }
            mesh.RecalculateNormals();
        }
    }
}
