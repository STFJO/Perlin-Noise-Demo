using System;
using Assets.Utility;
using UnityEngine;

namespace Assets
{
    public class Chunk
    {
        public readonly GameObject Target;
        private readonly MeshRenderer _renderer;
        private readonly MeshFilter _filter;
        private readonly LODMesh[] _lodMeshes;
        private readonly GeneratorBehaviour _generator;

        private bool _mapDataReceived;
        private Bounds _bounds;
        private int _previousLodIndex = -1;
        private MapData _mapData = null;
        public bool HasData => _mapData != null;


        public Chunk(Vector2Int index, GeneratorBehaviour generator)
        {
            _generator = generator;
            _bounds = new Bounds(new Vector3(index.x * _generator.GridSize.x, 0, index.y * _generator.GridSize.z) + _generator.GridSize / 2, _generator.GridSize);

            Target = new GameObject("Chunk " + index);
            _renderer = Target.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = new Material(Shader.Find("Custom/SurfaceShader"));
            _filter = Target.AddComponent<MeshFilter>();
            Target.transform.parent = _generator.transform;
            Target.transform.position = _bounds.min;

            Target.SetActive(false);


            _lodMeshes = new LODMesh[_generator.LodLevels.Length];
            for (int i = 0; i < _generator.LodLevels.Length; i++) _lodMeshes[i] = new LODMesh(_generator.LodLevels[i].Lod, UpdateTerrainChunk, _generator);
        }

        public void OnMapDataReceived(MapData data)
        {
            _mapData = data;
            _mapDataReceived = true;
            _renderer.sharedMaterial.mainTexture = TextureGenerator.TextureFromColorMap(_mapData.ColorMap, new Vector2Int(_generator.Parameter.ChunkSize, _generator.Parameter.ChunkSize));

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (!_mapDataReceived) return;
            float viewerDstFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(_generator.ViewerPosition));
            bool visible = viewerDstFromNearestEdge <= _generator.MaxViewDistance;

            if (visible)
            {
                int lodIndex = 0;

                for (int i = 0; i < _generator.LodLevels.Length - 1; i++)
                {
                    if (!(viewerDstFromNearestEdge > _generator.LodLevels[i].DistanceThreshold)) break;
                    lodIndex = i + 1;
                }

                if (lodIndex != _previousLodIndex)
                {
                    LODMesh lodMesh = _lodMeshes[lodIndex];
                    if (lodMesh.HasMesh)
                    {
                        _previousLodIndex = lodIndex;
                        _filter.mesh = lodMesh.Mesh;
                    }
                    else if (!lodMesh.HasRequestedMesh)
                    {
                        lodMesh.RequestMesh(_mapData);
                    }
                }
            }

            Target.SetActive(visible);
        }
    }
    
    public class LODMesh
    {
        public Mesh Mesh;
        public bool HasRequestedMesh;
        public bool HasMesh;
        private readonly int _lod;
        private readonly Action _updateCallback;
        private readonly GeneratorBehaviour _generator;

        public LODMesh(int lod, Action updateCallback, GeneratorBehaviour generator)
        {
            _lod = lod;
            _updateCallback = updateCallback;
            _generator = generator;
        }

        private void OnMeshDataRecieved(MeshData m)
        {
            Mesh = MeshGenerator.GenerateTerrain(m);
            HasMesh = true;
            _updateCallback();
        }

        public void RequestMesh(MapData data)
        {
            HasRequestedMesh = true;
            _generator.RequestMesh(data, _lod, OnMeshDataRecieved);
        }
    }

}
