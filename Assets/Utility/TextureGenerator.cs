using UnityEngine;

namespace Assets.Utility
{
    public static class TextureGenerator
    {
        public static Texture2D TextureFromColorMap(Color[] colorMap, Vector2Int size)
        {
            Texture2D texture = new Texture2D(size.x, size.y)
            {
                filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(colorMap);
            texture.Apply();
            return texture;
        }

        public static Texture2D TextureFromHeightmap(float[,] heightmap)
        {

            Vector2Int size = heightmap.Dimensions();
            Color[] colorMap = new Color[size.x * size.y];

            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    colorMap[y * size.x + x] = Color.Lerp(Color.black, Color.white, heightmap[x, y]);
                }
            }
            return TextureFromColorMap(colorMap, size);
        }
    }
}