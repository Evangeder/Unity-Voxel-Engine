using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class ChunkManager
{
    private const string RECYCLED_CHUNK_NAME = "Recycled Chunk";
    private static GameObject _chunkPrefab;
    private static World _world;
    private static bool _chunkPrefabSet = false;
    private static int _threshold;
    private static bool _isDynamic;
    
    private static Chunk[] _chunkArray;
    private static List<Chunk> _chunkList;
    private static int _position = 0;
    private static int _objectCount = 0;

    //public static void Init(GameObject chunkPrefab, World world, int count)
    //{
    //    if (count < 0) throw new Exception("Threshold can't be negative.");
    //    _threshold = count;
    //    _chunkArray = new Chunk[count];
    //    _chunkPrefab = chunkPrefab;
    //    _chunkPrefabSet = true;
    //    _world = world;
    //}
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="chunkPrefab"></param>
    /// <param name="world"></param>
    /// <param name="bufferSize"></param>
    /// <param name="useDynamicBuffer">Fixed buffer is faster, but Dynamic is flexible (it can extend and shrink itself)</param>
    /// <exception cref="Exception"></exception>
    public static void Init(GameObject chunkPrefab, World world, int bufferSize, bool useDynamicBuffer = true)
    {
        if (bufferSize < 0) throw new Exception("Threshold can't be negative.");

        _isDynamic = useDynamicBuffer;
        _threshold = bufferSize;
        if (useDynamicBuffer)
        {
            _chunkList = new List<Chunk>();
            _chunkArray = null;
        }
        else
        {
            _chunkArray = new Chunk[bufferSize];
            _chunkList = null;
        }
        _chunkPrefab = chunkPrefab;
        _chunkPrefabSet = true;
        _world = world;
    }

    public static int Count
    {
        get { return _position; }
    }
    
    public static int Left
    {
        get { return _isDynamic ? _threshold - (_chunkList.Count > _position ? _chunkList.Count : _position): _position; }
    }
    
    public static int ObjectCount
    {
        get { return _objectCount; }
    }
    
    public static int Length
    {
        get { return _isDynamic ? _chunkList.Count : _threshold; }
    }

    public static int Max
    {
        get { return _threshold; }
    }

    public new static string GetType
    {
        get { return _isDynamic ? "Dynamic" : "Static"; }
    }
    
    /// <summary>
    /// Clears the cache of chunks.
    /// </summary>
    public static void Clear()
    {
        if (_isDynamic)
        {
            _chunkList.Clear();
            _chunkList = null;
        }
        else
            _chunkArray = null;

        _chunkPrefabSet = false;
        _world = null;
        _chunkPrefab = null;
        _position = 0;
        _objectCount = 0;
    }

    /// <summary>
    /// Empties the contents of buffer, but leaves allocations.
    /// </summary>
    public static void Empty()
    {
        for (int i = 0; i < _threshold; i++)
            if (_isDynamic)
                _chunkList[i] = default(Chunk);
            else
                _chunkArray[i] = default(Chunk);

        _objectCount = 0;
        _position = 0;
    }

    /// <summary>
    /// Tries to get Empty Chunk from buffer and returns it.
    /// </summary>
    /// <param name="world">World that the chunk belongs to.</param>
    /// <returns>Chunk class bound to GameObject. Call ChunkManager.Peek() to check if there are available Empty Chunks.</returns>
    public static Chunk GetChunk()
    {
        //Debug.Log($"Count = {_objectCount}");
        if (_position > 0)
        {
            Chunk ch = _isDynamic ? _chunkList[_position - 1] : _chunkArray[_position - 1];
            ch.isEmpty = false;
            ch.gameObject.SetActive(true);
            _position--;
            return ch;
        }

        if (_isDynamic && Push())
        {
            Chunk ch = _chunkList[_position - 1];
            ch.isEmpty = false;
            ch.gameObject.SetActive(true);
            _position--;
            return ch;
        }
        
        return null;
    }

    /// <summary>
    /// Used to recycle chunk and insert back into circular buffer.
    /// <para>Please note, that if Count >= Max, the object is going to be destroyed.</para>
    /// </summary>
    /// <param name="chunk"></param>
    public static void Dispose(this Chunk chunk)
    {
        GameObject g = chunk.gameObject;
        g.transform.position = new Vector3(0, 0, 0);
        g.name = RECYCLED_CHUNK_NAME;
        
        if (_position < _threshold)
        {
            _position++;
            chunk.isEmpty = true;
            chunk.isQueuedForDeletion = false;

            if (_isDynamic)
                if (_position < _chunkList.Count)
                    _chunkList[_position - 1] = chunk;
                else
                    _chunkList.Add(chunk);
            else
                _chunkArray[_position - 1] = chunk;
            
            g.SetActive(false);
        }
        else
            GameObject.Destroy(g);
    }

    /// <summary>
    /// Creates Empty Chunk object in buffer.
    /// </summary>
    /// <returns>True when spawned, False when failed.</returns>
    public static bool Push()
    {
        if (!_chunkPrefabSet) return false;
        if (_position >= _threshold) return false;

        GameObject newObject = GameObject.Instantiate(_chunkPrefab, new Vector3(0, 0, 0), Quaternion.Euler(Vector3.zero));
        newObject.transform.parent = _world.gameObject.transform;
        newObject.name = RECYCLED_CHUNK_NAME;
        newObject.SetActive(false);

        Chunk chunk = newObject.GetComponent<Chunk>();
        _position++;
        if (_isDynamic)
            if (_position < _chunkList.Count - 1)
                _chunkList[_position - 1] = chunk;
            else
                _chunkList.Add(chunk);
        else
            _chunkArray[_position - 1] = chunk;
        _objectCount++;
        return true;
    }

    /// <summary>
    /// Creates fixed amount of Empty Chunk objects in buffer. Stops when threshold is met.
    /// </summary>
    /// <param name="count">Amount of Empty Chunks to spawn.</param>
    /// <returns>Count of Empty Chunks that were successfully created.</returns>
    public static int Push(int count)
    {
        if (_position >= _threshold) return 0;
        for (int i = 0; i < count; i++)
            if (!Push())
                return i;
        return count;
    }
}
