using System.Collections.Generic;

using UnityEngine;

namespace SingleplayerCousins.Gltf;

using Unknown = SimpleJson.JsonObject;

public abstract class Base {
    public Unknown? extensions, extras;
}

public abstract class Named : Base {
    public string? name;
}

public sealed class AssetFile : Base {
    public string[] extensionsUsed = [], extensionsRequired = [];
    public AssetMetadata asset = new();

    public uint? scene;

    public Accessor[] accessors = [];
    public Animation[] animations = [];
    public Buffer[] buffers = [];
    public BufferView[] bufferViews = [];
    public Camera[] cameras = [];
    public Image[] images = [];
    public Material[] materials = [];
    public Mesh[] meshes = [];
    public Node[] nodes = [];
    public Sampler[] samplers = [];
    public Scene[] scenes = [];
    public Skin[] skins = [];
    public Texture[] textures = [];
}

public sealed class Accessor : Named {
    public uint? bufferView;
    public uint byteOffset = 0;
    public ComponentType componentType;
    public bool normalized = false;
    public uint count;
    public Shape type;
    public float[]? max, min; // length depends on element shape
    public Sparse? sparse;

    public enum ComponentType {
        I8 = 5120,
        U8 = 5121,
        I16 = 5122,
        U16 = 5123,
        U32 = 5125,
        F32 = 5126,
    }

    [StringEnum]
    public enum Shape {
        SCALAR,
        VEC2, VEC3, VEC4,
        MAT2, MAT3, MAT4,
    }

    public sealed class Sparse : Base {
        public uint count;
        public Indices indices = new();
        public Values values = new();

        public sealed class Indices : Base {
            public uint bufferView;
            public uint byteOffset = 0;
            public ComponentType componentType;
        }

        public sealed class Values : Base {
            public uint bufferView;
            public uint byteOffset = 0;
        }
    }
}

public sealed class Animation : Named {
    public Channel[] channels = [];
    public Sampler[] samplers = [];

    public sealed class Channel : Base {
        public uint sampler;
        public Target target = new();

        public sealed class Target : Base {
            public uint? node;
            public Path path;

            [StringEnum]
            public enum Path { translation, rotation, scale, weights }
        }
    }

    public sealed class Sampler : Base {
        public uint input;
        public Interpolation interpolation = Interpolation.LINEAR;
        public uint output;

        [StringEnum]
        public enum Interpolation { LINEAR, STEP, CUBICSPLINE };
    }
}

public sealed class AssetMetadata : Base {
    public string version = "";
    public string? copyright, generator, minVersion;
}

public sealed class Buffer : Named {
    public string? uri;
    public uint byteLength;
}

public sealed class BufferView : Named {
    public uint buffer;
    public uint byteOffset = 0;
    public uint byteLength;
    public uint? byteStride;
    public Target? target;

    public enum Target {
        ArrayBuffer = 39462,
        ElementArrayBuffer = 39463,
    }
}

// Meh, not the best representation but I'm not actually using this anyway.
public sealed class Camera : Named {
    public Orthographic? orthorgraphic;
    public Perspective? perspective;
    public Type type;

    public sealed class Orthographic : Base {
        public double xmag, ymag, zfar, znear;
    }

    public sealed class Perspective {
        public double aspectRatio, yfov, zfar, znear;
    }

    [StringEnum]
    public enum Type { perspective, orthographic }
}

public sealed class Image : Named {
    public string? uri, mimeType;
    public uint? bufferView;
}

public sealed class Material : Named {
    public PbrMetallicRoughness? pbrMetallicRoughness;
    public NormalTextureInfo? normalTexture;
    public OcclusionTextureInfo? occlusionTexture;
    public TextureInfo? emissiveTexture;
    public Vector3 emissiveFactor = Vector3.zero;
    public AlphaMode alphaMode = AlphaMode.OPAQUE;
    public double alphaCutoff = 0.5;
    public bool doubleSided = false;

    [StringEnum]
    public enum AlphaMode { OPAQUE, MASK, BLEND }

    public sealed class NormalTextureInfo : TextureInfo {
        public double scale = 1;
    }

    public sealed class OcclusionTextureInfo : TextureInfo {
        public double strength = 1;
    }

    public sealed class PbrMetallicRoughness : Base {
        public Vector4 baseColorFactor = Vector4.one;
        public double metallicFactor = 1, roughnessFactor = 1;
        public TextureInfo? baseColorTexture, metallicRoughnessTexture;
    }
}

public class TextureInfo : Base {
    public uint index;
    public uint texCoord = 0;
}

public sealed class Mesh : Named {
    public Primitive[] primitives = [];
    public double[]? weights;

    public sealed class Primitive : Base {
        public Dictionary<string, uint> attributes = [];
        public uint? indices, material;
        public Mode mode = Mode.Triangles;
        public Dictionary<string, uint>? targets;

        public enum Mode {
            Points,
            Lines, LineLoop, LineStrip,
            Triangles, TriangleStrip, TriangleFan,
        }
    }
}

public sealed class Node : Named {
    public uint? camera;
    public uint[] children = [];
    public uint? skin;
    public uint? mesh;

    public Vector3? translation, scale;
    public Quaternion? rotation;
    public Matrix4x4? matrix;
}

public sealed class Sampler : Named {
    public Filter? magFilter, minFilter;
    public WrapMode wrapS = WrapMode.Repeat, wrapT = WrapMode.Repeat;

    public enum Filter { 
        Nearest = 9728,
        Linear = 9729,
        NearestMipmapNearest = 9984,
        LinearMipmapNearest = 9985,
        NearestMipmapLinear = 9986,
        LinearMipmapLinear = 9987,
    }

    public enum WrapMode {
        Clamp = 33071,
        Mirror = 33648,
        Repeat = 10497,
    }
}

public sealed class Scene : Named {
    public uint[] nodes = [];
}

public sealed class Skin : Named {
    public uint? inverseBindMatrices, skeleton;
    public uint[] joints = [];
}

public sealed class Texture : Named {
    public uint? sampler, source;
}