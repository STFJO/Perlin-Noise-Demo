using System;
using System.Globalization;
using SimpleJSON;
using UnityEngine;

namespace Assets
{
    public class GeneratorParameter : MonoBehaviour
    {
        private const int MinRes = 3;
        private const int MaxRes = 255;

        private int _chunkSize = MaxRes;
        public float Factor = 2;
        public int Seed = 1234;
        public int Octaves = 8;
        public int minOctave = 0;
        [Range(0, 1)]
        public float Persistance = 0.5f;
        public int Radius = 0;

        public Action<int> ResolutionChangedEvent;
        public int ChunkSize
        {
            get
            {
                return _chunkSize;
            }
            set
            {
                if (value == ChunkSize) return;
                if ((value - 1) % 2 != 0) value--; // value-1 must be a factor of 2
                _chunkSize = Math.Max(Math.Min(value, MaxRes), MinRes);
                ResolutionChangedEvent?.Invoke(ChunkSize);
            }
        }

        public JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["seed"] = Seed;
            node["chunksize"] = _chunkSize;
            node["scale"] = Factor;
            node["octaves"] = Octaves;
            node["persistance"] = Persistance;
            return node;
        }

        public void SetFactor(string s)
        {
            if (float.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float result))
            {
                Factor = result;
            }
        }

        public void SetSeed(string s)
        {
            if (int.TryParse(s, out int result))
            {
                Seed = result;
            }
        }

        public void SetOctaves(string s)
        {
            if (int.TryParse(s, out int result))
            {
                Octaves = result;
            }
        }

        public void SetPersistance(string s)
        {
            if (float.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float result))
            {
                Persistance = result;
            }
        }

        public void SetChunkSize(string s)
        {
            if (int.TryParse(s, out int result))
            {
                ChunkSize = result;
            }
        }

        public void SetRadius(string s)
        {
            if (int.TryParse(s, out int result))
            {
                Radius = result;
            }
        }
    }
}