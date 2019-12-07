
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Mathematics;
using UnityEngine;

public struct SchematicQueueStruct
{
    public SchematicQueueStruct(int3 coordinates, string Filename)
    {
        this.Position = coordinates;
        this.Filename = Filename;
    }
    public int3 Position; //origin points
    public string Filename;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
[Serializable]
public struct BlockSchematic
{
    public BlockSchematic(int X, int Y, int Z, ushort ID, bool Marched, float MarchedValue)
    {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
        this.ID = ID;
        this.Marched = Marched;
        this.MarchedValue = (byte)(MarchedValue * 255f);
    }
    public BlockSchematic(int X, int Y, int Z, ushort ID, bool Marched, byte MarchedValue)
    {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
        this.ID = ID;
        this.Marched = Marched;
        this.MarchedValue = MarchedValue;
    }
    public byte MarchedValue;
    public int X, Y, Z;
    public ushort ID;
    public boolean Marched;
}

public static class Schematics
{
    public static World world;
    public static UnityEngine.UI.Text ErrorLogBuildmode;
    public static List<SchematicQueueStruct> SchematicQueue = new List<SchematicQueueStruct>();

    public static IEnumerator CopySchematicCoroutine(int3 Point1, int3 Point2, int3 Origin, string Filename = "Clipboard")
    {
        string path = Path.Combine(Application.dataPath, "Mods", "Structures", $"{Filename}.vks");

        int SmallerX = Point1.x < Point2.x ? Point1.x : Point2.x;
        int SmallerY = Point1.y < Point2.y ? Point1.y : Point2.y;
        int SmallerZ = Point1.z < Point2.z ? Point1.z : Point2.z;
        int BiggerX = Point1.x > Point2.x ? Point1.x : Point2.x;
        int BiggerY = Point1.y > Point2.y ? Point1.y : Point2.y;
        int BiggerZ = Point1.z > Point2.z ? Point1.z : Point2.z;

        int counter = 0;
        List<BlockSchematic> schema = new List<BlockSchematic>();
        for (int x = SmallerX; x <= BiggerX; x++)
        {
            for (int y = SmallerY; y <= BiggerY; y++)
            {
                for (int z = SmallerZ; z <= BiggerZ; z++)
                {
                    BlockMetadata metadata = world.GetBlock(x, y, z);
                    //if (metadata.ID == 0) continue;
                    schema.Add(new BlockSchematic
                    {
                        X = x - Origin.x,
                        Y = y - Origin.y,
                        Z = z - Origin.z,
                        ID = metadata.ID,
                        Marched = metadata.Marched,
                        MarchedValue = metadata.MarchedValue
                    });
                    counter++;
                    if (counter >= 64)
                    {
                        counter = 0;
                        yield return null;
                    }
                }
            }
            yield return new WaitForEndOfFrame();
        }
        using (var file = File.OpenWrite(path))
        {
            var writer = new BinaryFormatter();
            writer.Serialize(file, schema); // Writes the entire list.
            counter++;
            if (counter >= 10)
            {
                counter = 0;
                yield return null;
            }
        }
        PrintMessageOnScreen($"Schematic saved! ({schema.Count.ToString()} blocks.)", Color.green);
    }

    public static IEnumerator PasteSchematicCoroutine(RaycastHit hit, string Filename = "Clipboard")
    {
        int3 originpoint = EditTerrain.GetBlockPos(hit, false);
        string path = Path.Combine(Application.dataPath, "Mods", "Structures", $"{Filename}.vks");
        List<BlockSchematic> schema = new List<BlockSchematic>();

        using (var file = File.OpenRead(path))
        {
            var reader = new BinaryFormatter();
            schema = (List<BlockSchematic>)reader.Deserialize(file); // Reads the entire list.
        }

        int counter = 0;
        foreach (var item in schema)
        {
            BlockMetadata md = new BlockMetadata
            {
                ID = item.ID,
                Marched = item.Marched,
                MarchedValue = item.MarchedValue
            };
            world.SetBlock(originpoint.x + item.X, originpoint.y + item.Y, originpoint.z + item.Z, md, false);
            counter++;
            if (counter >= 64)
            {
                counter = 0;
                yield return null;
            }
        }
        PrintMessageOnScreen("Schematic imported.", Color.green);
    }

    public static IEnumerator PasteSchematicCoroutine(int3 Position, string Filename = "Clipboard")
    {
        string path = Path.Combine(Application.dataPath, "Mods", "Structures", $"{Filename}.vks");
        if (File.Exists(path))
        {
            List<BlockSchematic> schema = new List<BlockSchematic>();

            using (var file = File.OpenRead(path))
            {
                var reader = new BinaryFormatter();
                schema = (List<BlockSchematic>)reader.Deserialize(file); // Reads the entire list.
            }

            int counter = 0;
            foreach (var item in schema)
            {
                BlockMetadata md = new BlockMetadata
                {
                    ID = item.ID,
                    Marched = item.Marched,
                    MarchedValue = item.MarchedValue
                };
                world.SetBlock(Position.x + item.X, Position.y + item.Y, Position.z + item.Z, md, false);
                counter++;
                if (counter >= 64)
                {
                    counter = 0;
                    yield return null;
                }
            }
            PrintMessageOnScreen("Schematic imported.", Color.green);
        } else
            PrintMessageOnScreen("Clipboard is empty.", Color.green);

    }

    public static IEnumerator LoadMap(string Filename = "null")
    {
        string path = Path.Combine(Application.dataPath, "Mods", "Maps", $"{Filename}.vks");
        if (File.Exists(path))
        {
            List<BlockSchematic> schema = new List<BlockSchematic>();

            using (var file = File.OpenRead(path))
            {
                var reader = new BinaryFormatter();
                schema = (List<BlockSchematic>)reader.Deserialize(file); // Reads the entire list.
            }

            int counter = 0;
            foreach (var item in schema)
            {
                BlockMetadata md = new BlockMetadata
                {
                    ID = item.ID,
                    Marched = item.Marched,
                    MarchedValue = item.MarchedValue
                };
                world.SetBlock(item.X, item.Y, item.Z, md, false, BlockUpdateMode.None);
            }
            PrintMessageOnScreen("Schematic imported.", Color.green);
        }
        else
            PrintMessageOnScreen("Clipboard is empty.", Color.green);
        yield return null;
    }

    public static IEnumerator PasteSchematicQueueCoroutine(int BlocksPerFrame = 64)
    {
        foreach (SchematicQueueStruct Schematic in SchematicQueue)
        {
            string path = Path.Combine(Application.dataPath, "Mods", "Structures", $"{Schematic.Filename}.vks");
            List<BlockSchematic> schema = new List<BlockSchematic>();

            using (var file = File.OpenRead(path))
            {
                var reader = new BinaryFormatter();
                schema = (List<BlockSchematic>)reader.Deserialize(file); // Reads the entire list.
            }

            int counter = 0;
            foreach (var item in schema)
            {
                BlockMetadata md = new BlockMetadata
                {
                    ID = item.ID,
                    Marched = item.Marched,
                    MarchedValue = item.MarchedValue
                };
                world.SetBlock(Schematic.Position.x + item.X, Schematic.Position.y + item.Y, Schematic.Position.z + item.Z, md, false);
                counter++;
                if (counter >= BlocksPerFrame)
                {
                    counter = 0;
                    yield return null;
                }
            }
        }
        SchematicQueue.Clear();
    }

    private static void PrintMessageOnScreen(string Message, Color color)
    {
        ErrorLogBuildmode.text = Message;
        ErrorLogBuildmode.color = color;
    }
}
