using System;
using SimpleJSON;
using UnityEngine;
using Random = System.Random;
using Vector2float = System.Numerics.Vector2;

namespace Assets
{
    [Serializable]
    public struct TerrainGeneratorConfig
    {
        public int ChunkSize;
        public float Factor;
        public int Seed;
        public int Octaves;
        public int minOctaves;
        [Range(0,1)]
        public float Persistance;
        public static TerrainGeneratorConfig Default => new TerrainGeneratorConfig(255, 2, 1234, 8, 0.5f);
        public TerrainGeneratorConfig(int chunkSize, float factor, int seed, int octaves, float persistance, int minoctaves=0)
        {
            ChunkSize = chunkSize;
            Factor = factor;
            Seed = seed;
            Octaves = octaves;
            Persistance = persistance;
            minOctaves = minoctaves;
        }

        public JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["seed"] = Seed;
            node["chunksize"] = ChunkSize;
            node["scale"] = Factor;
            node["octaves"] = Octaves;
            node["persistance"] = Persistance;
            return node;
        }
    }
    public class TerrainGenerator
    {
        private GeneratorParameter _config;

        public GeneratorParameter Config
        {
            get
            {
                return _config;
            }

            set
            {
                _config = value;
                Init();
            }
        }
        private const int GradCount = 1024;
        private const int MaxRng = Int32.MaxValue;
        private Vector2float GridSize => new Vector2float(Config.ChunkSize * Config.Factor, Config.ChunkSize * Config.Factor);
        private Random _rng;
        private float[] _gradients;


        public TerrainGenerator() => Init();

        public void Init()
        {
            if( Config == null ) return;
            _rng = new Random(Config.Seed);
            _gradients = new float[GradCount];
            for (int i = 0; i < GradCount; i++)
            {
                _gradients[i] = ((float)_rng.Next(MaxRng)) / MaxRng;
                _gradients[i] *= _rng.Next(MaxRng) > MaxRng / 2 ? 1 : -1;
            }
        }

        private float GetGradient(int x, int y)
        {
            return (_gradients[Ghash((uint)x, (uint)y, (uint)Config.Seed) % _gradients.Length]);
        }


        public float GetHeight(Vector2Int pos, int octaves = 1, int minoctaves=0)
        {
            float amp = 1.0f;
            float max = 0;
            float res = 0.0f;
            for (int i = 0; i < octaves; i++)
            {
                Vector2float gridPos = new Vector2float(pos.x / GridSize.X, pos.y / GridSize.Y);
                Vector2Int topLeft = new Vector2Int((int)Math.Floor(gridPos.X), (int)Math.Floor(gridPos.Y));
                Vector2Int bottomRight = topLeft + new Vector2Int(1, 1);
                Vector2float offset = new Vector2float(gridPos.X - topLeft.x, gridPos.Y - topLeft.y);

                offset.X = Fade(offset.X);
                offset.Y = Fade(offset.Y);

                float tl = GetGradient(topLeft.x, topLeft.y);
                float tr = GetGradient(bottomRight.x, topLeft.y);
                float bl = GetGradient(topLeft.x, bottomRight.y);
                float br = GetGradient(bottomRight.x, bottomRight.y);

                float val = Lerp(Lerp(tl, tr, offset.X), Lerp(bl, br, offset.X), offset.Y);
                if (i >= minoctaves)
                {
                    res += val * amp;

                    
                }
                max += amp;
                amp *= Config.Persistance;
                pos.x *= 2;
                pos.y *= 2;
            }

            return (res / max + 1) * 0.5f; // +1*0.5 --> von -1..1 zu 0..1
        }

        public static float Fade(float t) => (t * t * t * (t * (t * 6 - 15) + 10));
        //public static float Fade(float t) => t;
        public static float Lerp(float a0, float a1, float w) => (a0 + w * (a1 - a0));

        private const uint OffsetBasis = 2166136261;
        private const uint Prime = 16777619;

        public static uint Ghash(uint x, uint y, uint z)
        {
            return (((OffsetBasis ^ x) * Prime ^ y) * Prime ^ z) * Prime;
        }

        public float[,] GenerateChunk(int x, int y) => GenerateChunk(new Vector2Int(x, y));
        public float[,] GenerateChunk(Vector2Int index)
        {
            if (_config == null) return null;
            float[,] chunk = new float[Config.ChunkSize, Config.ChunkSize];
            for (int x = 0; x < chunk.GetLength(0); x++)
            {
                for (int y = 0; y < chunk.GetLength(1); y++)
                {
                    chunk[x, y] = GetHeight(index * (Config.ChunkSize - 1) + new Vector2Int(x, y), Config.Octaves,Config.minOctave);
                }
            }
            return chunk;
        }
    }
}