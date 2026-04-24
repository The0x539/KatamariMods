using System;
using System.IO;
using System.Text;

namespace SingleplayerCousins.Gltf;

static class Glb {
    public const int
        GLTF_MAGIC = 0x46546c67,
        JSON_CHUNK = 0x4e4f534a,
        BIN__CHUNK = 0x004e4942;

    public static void Parse(BinaryReader r, out string json, out byte[] binary) {
        var magic = r.ReadUInt32();
        if (magic != GLTF_MAGIC) {
            throw new Exception($"bad magic: {magic:08x} (expected {GLTF_MAGIC:08x})");
        }

        var version = r.ReadUInt32();
        if (version != 2) {
            throw new Exception($"bad version: {version} (expected 2)");
        }

        var _fileLength = r.ReadUInt32();

        var chunkLength = r.ReadUInt32();
        var chunkType = r.ReadUInt32();
        if (chunkType != JSON_CHUNK) {
            throw new Exception($"bad chunk type for JSON chunk: {chunkType:08x} (expected {JSON_CHUNK:08x})");
        }
        var jsonData = r.ReadBytes((int)chunkLength);
        json = Encoding.UTF8.GetString(jsonData);

        // Align to the next chunk.
        var pos = r.BaseStream.Position;
        while (pos % 4 != 0) pos++;
        r.BaseStream.Seek(pos, SeekOrigin.Begin);

        chunkLength = r.ReadUInt32();
        chunkType = r.ReadUInt32();
        if (chunkType != BIN__CHUNK) {
            throw new Exception($"bad chunk type for binary chunk: {chunkType:08x} (expected {BIN__CHUNK:08x})");
        }
        binary = r.ReadBytes((int)chunkLength);
        // Ignore any extension chunks afterward
    }
}

