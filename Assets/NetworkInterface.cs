using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using SimpleJSON;

namespace Assets
{
    public class NetworkInterface : MonoBehaviour
    {
        private readonly TerrainGenerator _generator = new TerrainGenerator();
        private TcpClient _client;
        private NetworkStream _stream;
        public event Action<ChunkData> OnChunkRecieved;
        public event Action OnDisconnected;
        private const char StartCharacter = '#';
        public bool UseLocalGenerator = false;
        public bool LogMessages = false;
        public string IpAddress = "127.0.0.1";
        public int Port = 27016;
        public bool Connected => _client?.Connected ?? false;
        void Awake()
        {

        }

        void Start()
        {

        }

        public void Connect()
        {
            if (Connected) return;
            _client = new TcpClient();
            _client.Connect(IPAddress.Parse(IpAddress), Port);
            _stream = _client.GetStream();
            StateObject state = new StateObject { Stream = _stream };
            _stream.BeginRead(state.Buffer, 0, state.Buffer.Length, ReceiveCallback, state);
        }

        public void Disconnect()
        {
            if (!Connected) return;
            _stream.Close();
            _client.Dispose();
            OnDisconnected?.Invoke();
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            if (LogMessages)
            {
                Debug.Log("Received CHUNK !!!");
            }
            try
            {
                StateObject state = (StateObject)ar.AsyncState;

                if (LogMessages)
                {
                    Debug.Log("Message : " + Encoding.ASCII.GetString(state.Buffer));
                }

                NetworkStream stream = state.Stream;
                int bytesRead = _stream.EndRead(ar);
                if (bytesRead == 0) return; // Connection Closed
                state.BytesRead += bytesRead;

                if (!state.FrameSet)
                {
                    if (bytesRead >= StateObject.Framsize)
                    {
                        if (state.Buffer[0] != StartCharacter) throw new Exception("Unkown Protocol");
                        state.MessageSize = (int)BitConverter.ToUInt64(state.Buffer, sizeof(ulong));
                        state.FrameSet = true;
                    }
                }
                else if (bytesRead >= StateObject.Framsize + state.MessageSize)
                {
                    if (LogMessages) Debug.Log("Received: " + Encoding.ASCII.GetString(state.Buffer.Select(b => b == 0 ? Convert.ToByte(' ') : b).ToArray(), 0, StateObject.Framsize + state.MessageSize));
                    ParseMessage(Encoding.ASCII.GetString(state.Buffer, StateObject.Framsize, state.MessageSize));
                    state.BytesRead -= StateObject.Framsize + state.MessageSize;
                    state.FrameSet = false;
                }

                stream.BeginRead(state.Buffer, 0, state.Buffer.Length, ReceiveCallback, state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void ParseMessage(string s)
        {
            ResultMessage message = new ResultMessage(JSON.Parse(s));
            lock (OnChunkRecieved)
            {
                OnChunkRecieved(new ChunkData(message.Index, message.Heightmap));
            }
        }

        public void WriteMessage(string s)
        {
            byte[] data = Encoding.ASCII.GetBytes(s);
            ulong size = (ulong)data.Length;
            List<byte> frame = new List<byte>(BitConverter.GetBytes(size));
            frame.Insert(0, Convert.ToByte(StartCharacter));
            byte[] message = frame.Concat(data).ToArray();
            if (LogMessages)
            {
                Debug.Log("Sending: " + Encoding.ASCII.GetString(message.Select(b => b == 0 ? Convert.ToByte(' ') : b).ToArray()));
            }
            _stream.Write(message, 0, message.Length);
        }

        public void RequestChunk(Vector2Int index)
        {

            if (UseLocalGenerator)
            {
                GenerateLocal(index);
                return;
            }

            WriteMessage(GenerateMessage(index));
        }

        public void RequestChunks(Vector2Int topLeft, Vector2Int bottomRight)
        {
            if (UseLocalGenerator)
            {
                for (int y = bottomRight.y; y <= topLeft.y; y++)
                {
                    for (int x = bottomRight.x; x < topLeft.x; x++)
                    {
                        GenerateLocal(new Vector2Int(x, y));
                    }
                }
                return;
            }

            WriteMessage(GenerateBatchMessage(topLeft, bottomRight));
        }

        private void GenerateLocal(Vector2Int index)
        {
            new Thread(() =>
            {
                if (OnChunkRecieved == null) return;
                float[,] map = _generator.GenerateChunk(index);
                lock (OnChunkRecieved)
                {
                    OnChunkRecieved(new ChunkData(index, map));
                }
            }).Start();
        }

        public void SetConfig(GeneratorParameter config)
        {
            if (UseLocalGenerator)
            {
                _generator.Config = config;
                return;
            }
            WriteMessage(ConfigMessage(config));
            /*JSONObject obj2 = new JSONObject();
            obj2["action"] = "RESULT";
            obj2["data"]["index"]["x"] = 0;
            obj2["data"]["index"]["y"] = 0;
            obj2["dimension"] = config.ChunkSize;
            JSONArray a = new JSONArray();
            for (int i = 0; i < config.ChunkSize * config.ChunkSize; i++) a.Add(0);
            obj2["data"] = a;
            WriteMessage(obj2.ToString());*/
        }

        public static string GenerateMessage(Vector2Int index)
        {
            JSONObject obj = new JSONObject();
            obj["action"] = "GENERATE";
            obj["data"]["index"]["x"] = index.x;
            obj["data"]["index"]["y"] = index.y;
            return obj.ToString();
        }

        public static string GenerateBatchMessage(Vector2Int topLeft, Vector2Int bottomRight)
        {
            JSONObject obj = new JSONObject();
            obj["action"] = "GENERATE_BATCH";
            obj["data"]["topLeft"]["x"] = topLeft.x;
            obj["data"]["topLeft"]["y"] = topLeft.y;
            obj["data"]["bottomRight"]["x"] = bottomRight.x;
            obj["data"]["bottomRight"]["y"] = bottomRight.y;
            return obj.ToString();
        }

        public string ConfigMessage(GeneratorParameter config)
        {
            JSONObject obj = new JSONObject();
            obj["action"] = "CONFIG";
            obj["data"] = config.ToJson();


            

            return obj.ToString();
        }

        public class ResultMessage
        {
            public readonly float[,] Heightmap;
            public Vector2Int Index;
            public ResultMessage(JSONNode node)
            {
                if (node["action"] != "RESULT") throw new Exception("Wrong result format");
                JSONNode dataNode = node["data"];
                Index = new Vector2Int(dataNode["index"]["x"].AsInt, dataNode["node_id"]["y"].AsInt);
                int dimension = dataNode["dimension"];
                JSONArray data = dataNode["data"].AsArray;
                Heightmap = new float[dimension, dimension];
                for (int y = 0; y < dimension; y++)
                {
                    for (int x = 0; x < dimension; x++)
                    {
                        Heightmap[x, y] = data[y * dimension + x].AsFloat;
                    }
                }
            }
        }
    }

    class StateObject
    {
        public const int Framsize = sizeof(ulong) + 1;
        public bool FrameSet = false;
        public NetworkStream Stream;

        public const int BufferSize = 4096;
        public int BytesRead = 0;
        public byte[] Buffer = new byte[BufferSize];

        public int MessageSize = 0;


    }

    public struct ChunkData
    {
        public readonly Vector2Int Position;
        public readonly float[,] Heightmap;

        public ChunkData(Vector2Int position, float[,] heightmap)
        {
            Position = position;
            Heightmap = heightmap;
        }
    }
}
