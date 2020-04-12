namespace VoxaNovus
{
    public enum ChunkUpdateMode
    {
        ForceSingle = 0,
        ForceNeighbours,
        QueueSingle,
        QueueNeighbours,
        QueueNeighboursForceSingle,
        QueueMarchingCubesFix,
        ForceMarchingCubesFix,
        DontUpdate
    }
}