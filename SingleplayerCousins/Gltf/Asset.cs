using UnityEngine;

namespace SingleplayerCousins.Gltf;

public sealed class AssetFile {
    public AssetInfo asset = new();
    public int scene;
    public Scene[] scenes = [];
    public Material[] materials = [];
    public Mesh[] meshes = [];
    public Texture[] textures = [];
    public Image[] images = [];
    public Accessor[] accessors = [];
    public Sampler[] samplers = [];
    public Buffer[] buffers = [];
}

public sealed class AssetInfo {
    public string version = "";
}

public sealed class Scene {
    public string name = "";
    public int[] nodes = [];
}

public sealed class Node {
    public string name = "";

    public int? mesh, skin;

    public int[] children = [];

    public Quaternion rotation;
    public Vector3 translation, scale;

    //public double[] rotation = [0, 0, 0, 0];
    //public Quaternion Rotation => new((float)this.rotation[0], (float)this.rotation[1], (float)this.rotation[2], (float)this.rotation[3]);

    //public double[] translation = [0, 0, 0];
    //public Vector3 Translation => new((float)this.rotation[0], (float)this.rotation[1], (float)this.rotation[2]);

    //public double[] scale = [1, 1, 1];
    //public Vector3 Scale => new((float)this.rotation[0], (float)this.rotation[1], (float)this.rotation[2]);
}

public sealed class Material {
    public string name = "";
}

public sealed class Mesh {
    public string name = "";
    public Primitive[] primitives = [];
}

public sealed class Primitive {
    public Attributes attributes = new();
    public int indices, material;
}

public sealed class Attributes {
    public int POSITION, NORMAL, TEXCOORD_0, JOINTS_0, WEIGHTS_0;
}

public sealed class Texture {
    public int sampler, source;
}

public sealed class Image {
    public int bufferView;
    public string mimeType = "";
    public string name = "";
}

public sealed class Skin {
    public int inverseBindMatrices;
    public int[] joints = [];
    public string name = "";
}

public sealed class Accessor {
    public int bufferView, componentType, count;
    
    public Vector3? max, min;
    //public Vector3? Max => this.max is double[] m ? new((float)m[0], (float)m[1], (float)m[2]) : null;
    //public Vector3? Min => this.min is double[] m ? new((float)m[0], (float)m[1], (float)m[2]) : null;

    public string type = "";
}

public sealed class Sampler {
    public int magFilter, minFilter;
}

public sealed class Buffer {
    public int byteLength;
}