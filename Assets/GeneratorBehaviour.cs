using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Assets.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace Assets
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(NetworkInterface))]
    [RequireComponent(typeof(GeneratorParameter))]
    public class GeneratorBehaviour : MonoBehaviour
    {
        private GameObject _waterPanePrefab;

        public TerrainType[] TerrainTypes =
        {
            new TerrainType(0.4f, new Color(73/255f,73/255f,200/255f)),
            new TerrainType(0.45f, new Color(229/255f, 211/255f, 154/255f)),
            new TerrainType(0.55f, new Color(34/255f, 139/255f, 34/255f)),
            new TerrainType(0.6f, new Color(52/255f,212/255f, 15/255f)),
            new TerrainType(0.7f, new Color(89/255f,39/255f,5/255f)),
            new TerrainType(0.9f, new Color(79/255f,47/255f,27/255f)),
            new TerrainType(1f, new Color(255/255f,255/255f,255/255f))
        };
        public Vector3 GridSize = new Vector3(100, 100, 100);
        public float MaxViewDistance = 1000;
        public Vector3 ViewerPosition;
        #region MeshRes & LOD

        public int MaxLevelOfDetail => _lodFactors.Count - 1;

        private void RecaluclateLODLevels()
        {
            LODInfo[] levels = new LODInfo[_lodFactors.Count];
            for (int i = 0; i < _lodFactors.Count; i++)
            {
                levels[i] = new LODInfo(_lodFactors[i], LodCurve.Evaluate((i + 1f) / _lodFactors.Count) * MaxViewDistance);
            }
            LodLevels = levels;
        }

        private List<int> _lodFactors = new List<int> { 1 };

        //private int _levelOfDetailIndex;

        /*public int LevelOfDetailIndex
        {
            get => _levelOfDetailIndex;
            set => _levelOfDetailIndex = Math.Max(Math.Min(value, MaxLevelOfDetail), 0);
        }

        public int LevelOfDetail => _lodFactors[LevelOfDetailIndex];*/

        public LODInfo[] LodLevels = new LODInfo[0];

        #endregion

        public AnimationCurve HeightCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        public AnimationCurve LodCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        public bool Water = true;

        private readonly Queue<ThreadInfo<MapData>> _mapDataQueue = new Queue<ThreadInfo<MapData>>();
        private readonly Queue<ThreadInfo<MeshData>> _meshQueue = new Queue<ThreadInfo<MeshData>>();

        private readonly Dictionary<Vector2Int, Chunk> _chunks = new Dictionary<Vector2Int, Chunk>();
        private int _receivedChunks = 0;
        public bool PendingChunks => _receivedChunks != _chunks.Count;

        private NetworkInterface _network;
        public NetworkInterface Network
        {
            get => _network;
            set
            {
                if( Network != null )
                {
                    Network.OnChunkRecieved -= ChunkRecieved;
                    Network.OnDisconnected -= OnNetworkDisconnected;
                }
                _network = value;
                if (Network != null)
                {
                    Network.OnChunkRecieved += ChunkRecieved;
                    Network.OnDisconnected += OnNetworkDisconnected;
                }
            }
        }

        public GeneratorParameter Parameter;

        public bool UseBatchGeneration = false;
        void Awake()
        {
            _waterPanePrefab = (GameObject)Resources.Load("WaterPanePrefab");
            Network = GetComponent<NetworkInterface>();
        }

        void Start()
        {
            Parameter = GetComponent<GeneratorParameter>();
            if (Parameter) Parameter.ResolutionChangedEvent = OnChunkResChgange;

            Network = GetComponent<NetworkInterface>();

            InputField text;
            try
            {
                if ((text = GameObject.Find("InputChunksize").GetComponent<InputField>()) != null) text.text = Parameter.ChunkSize.ToString();
                if ((text = GameObject.Find("InputFactor").GetComponent<InputField>()) != null) text.text = Parameter.Factor.ToString(CultureInfo.InvariantCulture);
                if ((text = GameObject.Find("InputOctaves").GetComponent<InputField>()) != null) text.text = Parameter.Octaves.ToString();
                if ((text = GameObject.Find("InputPersistance").GetComponent<InputField>()) != null) text.text = Parameter.Persistance.ToString(CultureInfo.InvariantCulture);
                if ((text = GameObject.Find("InputSeed").GetComponent<InputField>()) != null) text.text = Parameter.Seed.ToString();
                if ((text = GameObject.Find("InputRadius").GetComponent<InputField>()) != null) text.text = Parameter.Radius.ToString();
            }
            catch (NullReferenceException)
            {
                Debug.Log("There be no Input Fields here.");
            }
            


            Camera cam = FindObjectOfType<Camera>();
            if (cam == null) return;
            CameraBehaviour camBeh = cam.GetComponent<CameraBehaviour>();
            if (camBeh == null) return;
            MaxViewDistance = cam.farClipPlane;
            RecaluclateLODLevels();
            ViewerPosition = camBeh.transform.position;
            camBeh.OnPositionChanged += newPos => ViewerPosition = newPos;
        }
        private void OnNetworkDisconnected()
        {
            if (PendingChunks) DeleteAllChunks();
        }

        private void OnChunkResChgange(int e)
        {
            _lodFactors = new List<int>((e - 1).Factors());
            if (_lodFactors.Count == 0) _lodFactors.Add(1);
            _lodFactors.Sort();
            //_levelOfDetailIndex = Math.Min(_levelOfDetailIndex, MaxLevelOfDetail);
            RecaluclateLODLevels();
        }

        void Update()
        {
            lock (_mapDataQueue)
            {
                while (_mapDataQueue.Count > 0) _mapDataQueue.Dequeue().Execute();
            }

            lock (_meshQueue)
            {
                while (_meshQueue.Count > 0) _meshQueue.Dequeue().Execute();
            }

            foreach (KeyValuePair<Vector2Int, Chunk> chunk in _chunks)
            {
                chunk.Value.UpdateTerrainChunk();
            }
        }

        private void ChunkRecieved(ChunkData data)
        {
            if (!_chunks.TryGetValue(data.Position, out Chunk c)) throw new Exception("Unkown Chunk " + data.Position);
            _receivedChunks++;
            MapData mapData = GenerateMapData(data.Heightmap);
            lock (_mapDataQueue)
            {
                _mapDataQueue.Enqueue(new ThreadInfo<MapData>(c.OnMapDataReceived, mapData));
            }
        }

        public void GenerateMap()
        {
            DeleteAllChunks();
            Network.SetConfig(Parameter);
            if (!UseBatchGeneration)
            {
                int width = Parameter.Radius * 2 + 1;
                int height = Parameter.Radius * 2 + 1;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        GenerateChunk(new Vector2Int(x - Parameter.Radius, y - Parameter.Radius));
                    }
                }
            }
            else GenerateChunks(new Vector2Int(-Parameter.Radius, Parameter.Radius), new Vector2Int(Parameter.Radius, -Parameter.Radius));
        }

        public void GenerateChunks(Vector2Int topLeft, Vector2Int bottomRight)
        {
            for (int y = bottomRight.y; y < topLeft.y; y++)
            {
                for (int x = topLeft.x; x < bottomRight.x; x++)
                {
                    Vector2Int index = new Vector2Int(x, y);
                    _chunks.Add(index, new Chunk(index, this));
                }
            }
            Network.RequestChunks(topLeft, bottomRight);
        }

        public void GenerateChunk(Vector2Int index)
        {

            if (_chunks.ContainsKey(index)) return;
            _chunks.Add(index, new Chunk(index, this));
            Network.RequestChunk(index);
        }

        private void DeleteAllChunks()
        {
            foreach (KeyValuePair<Vector2Int, Chunk> chunk in _chunks)
            {
                if( !chunk.Value.HasData ) Debug.LogWarning($"Deleting chunk that hasn't recieved data yet {chunk.Key}");
                DestroyImmediate(chunk.Value.Target);
            }
            _chunks.Clear();
            _receivedChunks = 0;
        }

        public void RequestMesh(MapData data, int lod, Action<MeshData> callback)
        {
            new Thread(() =>
            {
                MeshData meshData = MeshGenerator.GenerateMeshData(data.HeightMap, GridSize, lod);
                lock (_meshQueue)
                {
                    _meshQueue.Enqueue(new ThreadInfo<MeshData>(callback, meshData));
                }
            }).Start();
        }

        private MapData GenerateMapData(float[,] map)
        {
            Vector2Int size = map.Dimensions();

            Color[] colorMap = new Color[size.x * size.y];
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    float currentHeight = map[x, y];
                    for (int i = 0; i < TerrainTypes.Length; i++)
                    {
                        if (!(currentHeight <= TerrainTypes[i].Height)) continue;
                        colorMap[y * size.x + x] = TerrainTypes[i].Color;
                        break;
                    }
                }
            }
            return new MapData(map, colorMap);
        }
    }

    public struct ThreadInfo<T>
    {
        public readonly Action<T> Callback;
        public readonly T Parameter;

        public ThreadInfo(Action<T> callback, T parameter)
        {
            Parameter = parameter;
            Callback = callback;
        }

        public void Execute() => Callback(Parameter);
    }

    [Serializable]
    public struct TerrainType
    {
        public TerrainType(float height, Color color)
        {
            Height = height;
            Color = color;
        }
        public float Height;
        public Color Color;
    }

    [Serializable]
    public struct LODInfo
    {
        public int Lod;
        public float DistanceThreshold;

        public LODInfo(int lod, float distanceThreshold)
        {
            Lod = lod;
            DistanceThreshold = distanceThreshold;
        }
    }

    public class MapData
    {
        public readonly float[,] HeightMap;
        public readonly Color[] ColorMap;

        public MapData(float[,] heightMap, Color[] colorMap)
        {
            HeightMap = heightMap;
            ColorMap = colorMap;
        }
    }

    public struct MeshData
    {
        public readonly Vector3[] Vertices;
        public readonly int[] Triangles;
        public readonly Vector2[] Uvs;

        public MeshData(Vector3[] vertices, int[] triangles, Vector2[] uvs)
        {
            Vertices = vertices;
            Triangles = triangles;
            Uvs = uvs;
        }

    }


}
