using System;
using System.IO;
using UnityEngine;

namespace WoodburySpectatorSync.Net
{
    internal sealed class ScenePresentationFrame
    {
        public Node[] nodes; public Word[] words; public int health, music;
        internal sealed class Word { public string target, text; public int progress; }
        internal sealed class TextValue { public string type, text; }
        internal sealed class TintValue { public string type; public Color color; }
        internal sealed class Node
        {
            public string path; public bool active; public Vector3 position, scale; public Quaternion rotation;
            public int animation; public float time; public TextValue[] texts; public TintValue[] tints;
            public bool hasAudio, playing, hasCamera, cameraEnabled;
            public float audioTime, volume, fieldOfView;
        }

        // Unity's serializer omits mod-defined nested arrays in the shipped player.
        // Explicit bounded binary encoding also gives the regression suite the real codec.
        public string Encode()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(1); writer.Write(health); writer.Write(music); writer.Write(nodes.Length);
                foreach (var node in nodes) WriteNode(writer, node);
                writer.Write(words.Length);
                foreach (var word in words) { writer.Write(word.target ?? ""); writer.Write(word.text ?? ""); writer.Write(word.progress); }
                if (stream.Length > 512 * 1024) throw new InvalidDataException("Scene presentation exceeds 512 KiB");
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public static string Fingerprint(Node node)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream)) { WriteNode(writer, node); return Convert.ToBase64String(stream.ToArray()); }
        }

        public static ScenePresentationFrame Decode(string payload)
        {
            if (payload == null || payload.Length > 700000) throw new InvalidDataException("Scene presentation size invalid");
            using (var stream = new MemoryStream(Convert.FromBase64String(payload), false))
            using (var reader = new BinaryReader(stream))
            {
                if (stream.Length > 512 * 1024) throw new InvalidDataException("Scene presentation exceeds 512 KiB");
                if (reader.ReadInt32() != 1) throw new InvalidDataException("Scene presentation version invalid");
                var frame = new ScenePresentationFrame { health = reader.ReadInt32(), music = Count(reader, 3), nodes = new Node[Count(reader, 8192)] };
                for (int i = 0; i < frame.nodes.Length; i++) frame.nodes[i] = ReadNode(reader);
                frame.words = new Word[Count(reader, 128)];
                for (int i = 0; i < frame.words.Length; i++)
                {
                    var word = new Word { target = Text(reader, 2048), text = Text(reader, 2048), progress = reader.ReadInt32() };
                    if (word.progress < 0 || word.progress > word.text.Length) throw new InvalidDataException("Scene presentation word progress invalid");
                    frame.words[i] = word;
                }
                if (stream.Position != stream.Length) throw new InvalidDataException("Scene presentation trailing data");
                return frame;
            }
        }

        private static void WriteNode(BinaryWriter w, Node n)
        {
            w.Write(n.path ?? ""); w.Write(n.active); Vector(w,n.position); Vector(w,n.scale);
            w.Write(n.rotation.x); w.Write(n.rotation.y); w.Write(n.rotation.z); w.Write(n.rotation.w);
            w.Write(n.animation); w.Write(n.time);
            w.Write(n.texts?.Length ?? 0);
            if (n.texts != null) foreach(var t in n.texts) { w.Write(t.type ?? ""); w.Write(t.text ?? ""); }
            w.Write(n.tints?.Length ?? 0);
            if (n.tints != null) foreach(var t in n.tints) { w.Write(t.type ?? ""); w.Write(t.color.r); w.Write(t.color.g); w.Write(t.color.b); w.Write(t.color.a); }
            w.Write(n.hasAudio); w.Write(n.playing); w.Write(n.hasCamera); w.Write(n.cameraEnabled);
            w.Write(n.audioTime); w.Write(n.volume); w.Write(n.fieldOfView);
        }
        private static Node ReadNode(BinaryReader r)
        {
            var n = new Node { path = Text(r,2048), active=r.ReadBoolean(), position=Vector(r), scale=Vector(r),
                rotation=new Quaternion(Number(r),Number(r),Number(r),Number(r)), animation=r.ReadInt32(), time=Number(r) };
            n.texts = new TextValue[Count(r,16)];
            for(int i=0;i<n.texts.Length;i++) n.texts[i]=new TextValue { type=Text(r,256), text=Text(r,16384) };
            n.tints = new TintValue[Count(r,16)];
            for(int i=0;i<n.tints.Length;i++) n.tints[i]=new TintValue { type=Text(r,256), color=new Color(Number(r),Number(r),Number(r),Number(r)) };
            n.hasAudio=r.ReadBoolean(); n.playing=r.ReadBoolean(); n.hasCamera=r.ReadBoolean(); n.cameraEnabled=r.ReadBoolean();
            n.audioTime=Number(r); n.volume=Number(r); n.fieldOfView=Number(r); return n;
        }
        private static int Count(BinaryReader r,int max) { int n=r.ReadInt32(); if(n<0 || n>max) throw new InvalidDataException("Scene presentation count invalid"); return n; }
        private static string Text(BinaryReader r,int max) { string s=r.ReadString(); if(s.Length>max) throw new InvalidDataException("Scene presentation text too long"); return s; }
        private static float Number(BinaryReader r) { float n=r.ReadSingle(); if(float.IsNaN(n)||float.IsInfinity(n)||Math.Abs(n)>10000000f) throw new InvalidDataException("Scene presentation non-finite or unbounded number"); return n; }
        private static void Vector(BinaryWriter w,Vector3 v) { w.Write(v.x);w.Write(v.y);w.Write(v.z); }
        private static Vector3 Vector(BinaryReader r) => new Vector3(Number(r),Number(r),Number(r));
    }
}
