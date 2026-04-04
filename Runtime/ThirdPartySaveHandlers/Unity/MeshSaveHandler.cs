using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.UtilScripts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using Theblueway.Core.Runtime.Packages.com.blueutils.core.Runtime.ArrayUtilScripts;
using Theblueway.Core.Runtime.Packages.com.blueutils.core.Runtime.BinaryUtilScripts;
using Theblueway.Core.Runtime.Packages.com.blueutils.core.Runtime.DataStructures;
using Unity.Collections;
using UnityEngine;
using static Theblueway.Core.Runtime.Packages.com.blueutils.core.Runtime.Misc.JsonCompression;


namespace Assets._Project.Scripts.SaveAndLoad.ThirdPartySaveHandlers.UnitySHs
{
    public class MeshInitContext : AssetInitContext
    {
        public RandomId initFromMesh;
    }


    [SaveHandler(118760465134539926, "Mesh", typeof(UnityEngine.Mesh), order: -9)]
    public class MeshSaveHandler : AssetSaveHandlerBase<Mesh, MeshSaveData>
    {
        public List<Vector2> _uvFlatList = new();
        public List<int> _indicesFlatList = new();

        public List<Vector2> _tempVector2List = new();
        public List<Vector3> _tempVector3List = new();
        public List<Vector4> _tempVector4List = new();
        public List<Color> _tempColorList = new();
        public List<int> _tempIntList = new();


        public MeshInitContext _context;

        public bool ShouldWriteOrLoad => __saveData.ShouldWriteOrLoad;


        public override void Init(object instance, InitContext context)
        {
            _context = context as MeshInitContext;

            if (_context != null)
            {
                var other = SaveAndLoadManager.S.GetSaveHandlerById<MeshSaveHandler>(_context.initFromMesh);

                ///base <see cref="Init(object)"/> will save the values from context
                _context.instantiatedFromAssetId = other.AssetId;
                _context.mutable = other.Mutable;

                //return;
            }

            base.Init(instance, context);
        }



        public override void Init(object instance)
        {
            base.Init(instance);


            __saveData.isReadable = __instance.isReadable;

            if (Mutable && !__instance.isReadable)
            {
                __saveData.ShouldWriteOrLoad = false;

                Debug.LogError($"Mesh instance is registered as mutable but it is not readable. " +
                    $"Will not be able to save infos about it and will produce errors on load.\n" +
                    $"objectid: {HandledObjectId} | name: {__instance.name} | assetid: {__saveData._AssetId_}"); // assetid can be default if none was provided, which can be valid
            }
            else if (__saveData._AssetId_.IsNotDefault && !Mutable)
            {
                __saveData.ShouldWriteOrLoad = false;
            }
            else if (Mutable && __instance.isReadable)
            {
                __saveData.ShouldWriteOrLoad = true;
                //dev test
                //Debug.Log((__instance.name, HandledObjectId, "Mesh will be dynamically recreated."));
            }
        }


        public override void WriteSaveData()
        {
            base.WriteSaveData();

            //__saveData.ShouldWriteOrLoad = ShouldWriteOrLoad;
            if (!ShouldWriteOrLoad) return;


            __saveData.subMeshCount = __instance.subMeshCount;
            __saveData.indexFormat = __instance.indexFormat;


            _tempVector3List.Clear();
            __instance.GetVertices(_tempVector3List);
            ArrayUtils.CopyToPooled(_tempVector3List, ref __saveData._vertices);


            __saveData.indexSizes.Clear();
            __saveData.submeshTopologies.Clear();
            _indicesFlatList.Clear();
            for (int i = 0; i < __instance.subMeshCount; ++i)
            {
                __saveData.submeshTopologies.Add(__instance.GetTopology(i));
                __saveData.indexSizes.Add((int)__instance.GetIndexCount(i));

                _tempIntList.Clear();
                __instance.GetIndices(_tempIntList, i);

                _indicesFlatList.AddRange(_tempIntList);
            }

            ArrayUtils.CopyToPooled(_indicesFlatList, ref __saveData._indices);


            _tempVector3List.Clear();
            __instance.GetNormals(_tempVector3List);
            ArrayUtils.CopyToPooled(_tempVector3List, ref __saveData._normals);


            _tempVector3List.Clear();
            __instance.GetTangents(_tempVector4List);
            ArrayUtils.CopyToPooled(_tempVector4List, ref __saveData._tangents);


            _tempVector3List.Clear();
            __instance.GetColors(_tempColorList);
            ArrayUtils.CopyToPooled(_tempColorList, ref __saveData._colors);


            _uvFlatList.Clear();
            for (int i = 0; i < 8; i++)
            {
                _tempVector2List.Clear();
                __instance.GetUVs(i, _tempVector2List);
                _uvFlatList.AddRange(_tempVector2List);
            }

            ArrayUtils.CopyToPooled(_uvFlatList, ref __saveData._uvs);


            __saveData.boneWeights = __instance.boneWeights;
            __saveData.bindposes = __instance.bindposes;

            __saveData.blendShapes = ExtractBlendShapes(__instance);

            __saveData.vertexBufferTarget = __instance.vertexBufferTarget;
            __saveData.indexBufferTarget = __instance.indexBufferTarget;

            __saveData.name = __instance.name;
        }



        public override void LoadPhase1()
        {
            if (!ShouldWriteOrLoad) return;
            int start;


            __instance.subMeshCount = __saveData.subMeshCount;
            __instance.indexFormat = __saveData.indexFormat;

            __instance.SetVertices(__saveData._vertices, 0, __saveData.vertices.originalBufferElementCount);

            start = 0;

            for (int i = 0; i < __saveData.subMeshCount; i++)
            {
                int size = __saveData.indexSizes[i];
                var topology = __saveData.submeshTopologies[i];
                __instance.SetIndices(__saveData._indices, start, size, topology, i, calculateBounds: false);
                start += size;
            }


            __instance.SetNormals(__saveData._normals, 0, __saveData.normals.originalBufferElementCount);
            __instance.SetTangents(__saveData._tangents, 0, __saveData.tangents.originalBufferElementCount);
            __instance.SetColors(__saveData._colors, 0, __saveData.colors.originalBufferElementCount);

            start = 0;

            for (int ch = 0; ch < 8; ch++)
            {
                int channelSize = __saveData.uvSizes[ch];
                __instance.SetUVs(ch, __saveData._uvs, start, channelSize);
                start += channelSize;
            }


            __instance.boneWeights = __saveData.boneWeights;
            __instance.bindposes = __saveData.bindposes;

            ApplyBlendShapes(__instance, __saveData.blendShapes);

            __instance.vertexBufferTarget = __saveData.vertexBufferTarget;
            __instance.indexBufferTarget = __saveData.indexBufferTarget;

            __instance.RecalculateBounds();

            __instance.name = __saveData.name;
        }



        public override void _AssignInstance()
        {
            if (__saveData.ShouldWriteOrLoad)
            {
                __instance = new Mesh();
            }
            else base._AssignInstance();
        }


        public override void ArrangeSaveDataForSerialization()
        {
            base.ArrangeSaveDataForSerialization();

            CompressBuffer(__saveData._vertices, __saveData.vertices, BinaryPacking.Vector3PackSize);
            CompressBuffer(__saveData._indices, __saveData.indices, sizeof(int));
            CompressBuffer(__saveData._normals, __saveData.normals, BinaryPacking.Vector3PackSize);
            CompressBuffer(__saveData._tangents, __saveData.tangents, BinaryPacking.Vector4PackSize);
            CompressBuffer(__saveData._colors, __saveData.colors, BinaryPacking.ColorPackSize);
            CompressBuffer(__saveData._uvs, __saveData.uvs, BinaryPacking.Vector2PackSize);
        }



        public override void Deserialize(string json)
        {
            base.Deserialize(json);

            DecompressBufferIntoPooled(__saveData.vertices, ref __saveData._vertices, BinaryPacking.Vector3PackSize);
            DecompressBufferIntoPooled(__saveData.indices, ref __saveData._indices, sizeof(int));
            DecompressBufferIntoPooled(__saveData.normals, ref __saveData._normals, BinaryPacking.Vector3PackSize);
            DecompressBufferIntoPooled(__saveData.tangents, ref __saveData._tangents, BinaryPacking.Vector4PackSize);
            DecompressBufferIntoPooled(__saveData.colors, ref __saveData._colors, BinaryPacking.ColorPackSize);
            DecompressBufferIntoPooled(__saveData.uvs, ref __saveData._uvs, BinaryPacking.Vector2PackSize);
        }





        //todo: make this non-alloc
        public static BlendShapeData[] ExtractBlendShapes(Mesh mesh)
        {
            int shapeCount = mesh.blendShapeCount;
            var result = new BlendShapeData[shapeCount];

            for (int shapeIndex = 0; shapeIndex < shapeCount; shapeIndex++)
            {
                var shapeData = new BlendShapeData();
                shapeData.Name = mesh.GetBlendShapeName(shapeIndex);

                int frameCount = mesh.GetBlendShapeFrameCount(shapeIndex);
                shapeData.Frames = new BlendShapeFrameData[frameCount];

                int vertexCount = mesh.vertexCount;

                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    var frame = new BlendShapeFrameData();

                    frame.Weight = mesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);

                    frame.DeltaVertices = new Vector3[vertexCount];
                    frame.DeltaNormals = new Vector3[vertexCount];
                    frame.DeltaTangents = new Vector3[vertexCount];

                    mesh.GetBlendShapeFrameVertices(
                        shapeIndex,
                        frameIndex,
                        frame.DeltaVertices,
                        frame.DeltaNormals,
                        frame.DeltaTangents);

                    shapeData.Frames[frameIndex] = frame;
                }

                result[shapeIndex] = shapeData;
            }

            return result;
        }


        public static void ApplyBlendShapes(Mesh mesh, BlendShapeData[] shapes)
        {
            if (shapes == null) return;

            foreach (var shape in shapes)
            {
                foreach (var frame in shape.Frames)
                {
                    mesh.AddBlendShapeFrame(
                        shape.Name,
                        frame.Weight,
                        frame.DeltaVertices,
                        frame.DeltaNormals,
                        frame.DeltaTangents);
                }
            }
        }




        static MeshSaveHandler()
        {
            Dictionary<string, long> methodToId = new()
            {
                {"SetIndexBufferParams(mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.IndexFormat):mscorlib System.Void", 605665140375966075},
                {"GetVertexAttribute(mscorlib System.Int32):UnityEngine.CoreModule UnityEngine.Rendering.VertexAttributeDescriptor", 736500501346727093},
                {"HasVertexAttribute(UnityEngine.CoreModule UnityEngine.Rendering.VertexAttribute):mscorlib System.Boolean", 245252918926855429},
                {"GetVertexAttributeDimension(UnityEngine.CoreModule UnityEngine.Rendering.VertexAttribute):mscorlib System.Int32", 900416489455887283},
                {"GetVertexAttributeFormat(UnityEngine.CoreModule UnityEngine.Rendering.VertexAttribute):UnityEngine.CoreModule UnityEngine.Rendering.VertexAttributeFormat", 157468257449020719},
                {"GetVertexAttributeStream(UnityEngine.CoreModule UnityEngine.Rendering.VertexAttribute):mscorlib System.Int32", 362887255304966247},
                {"GetVertexAttributeOffset(UnityEngine.CoreModule UnityEngine.Rendering.VertexAttribute):mscorlib System.Int32", 343125893942516882},
                {"GetVertexBufferStride(mscorlib System.Int32):mscorlib System.Int32", 116910294358378256},
                {"GetNativeVertexBufferPtr(mscorlib System.Int32):mscorlib System.IntPtr", 514708421379717413},
                {"GetNativeIndexBufferPtr():mscorlib System.IntPtr", 123383044566425027},
                {"ClearBlendShapes():mscorlib System.Void", 831848991563096522},
                {"GetBlendShapeName(mscorlib System.Int32):mscorlib System.String", 422065496485468058},
                {"GetBlendShapeIndex(mscorlib System.String):mscorlib System.Int32", 227439158047099360},
                {"GetBlendShapeFrameCount(mscorlib System.Int32):mscorlib System.Int32", 577535241609503820},
                {"GetBlendShapeFrameWeight(mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Single", 598380644949500758},
                {"GetBlendShapeFrameVertices(mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Vector3[],UnityEngine.CoreModule UnityEngine.Vector3[],UnityEngine.CoreModule UnityEngine.Vector3[]):mscorlib System.Void", 168219854853426987},
                {"AddBlendShapeFrame(mscorlib System.String,mscorlib System.Single,UnityEngine.CoreModule UnityEngine.Vector3[],UnityEngine.CoreModule UnityEngine.Vector3[],UnityEngine.CoreModule UnityEngine.Vector3[]):mscorlib System.Void", 308799139110975476},
                {"SetBoneWeights(UnityEngine.CoreModule NativeArray<mscorlib System.Byte>,UnityEngine.CoreModule NativeArray<UnityEngine.CoreModule UnityEngine.BoneWeight1>):mscorlib System.Void", 227953588142252254},
                {"GetAllBoneWeights():UnityEngine.CoreModule NativeArray<UnityEngine.CoreModule UnityEngine.BoneWeight1>", 937918964487894976},
                {"GetBonesPerVertex():UnityEngine.CoreModule NativeArray<mscorlib System.Byte>", 935510098795762053},
                {"GetBindposes():UnityEngine.CoreModule NativeArray<UnityEngine.CoreModule UnityEngine.Matrix4x4>", 921809809135828622},
                {"SetBindposes(UnityEngine.CoreModule NativeArray<UnityEngine.CoreModule UnityEngine.Matrix4x4>):mscorlib System.Void", 931681016871573094},
                {"SetSubMesh(mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.SubMeshDescriptor,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 502661096296614011},
                {"GetSubMesh(mscorlib System.Int32):UnityEngine.CoreModule UnityEngine.Rendering.SubMeshDescriptor", 234988293856173312},
                {"MarkModified():mscorlib System.Void", 937058572025755286},
                {"GetUVDistributionMetric(mscorlib System.Int32):mscorlib System.Single", 151436723902330218},
                {"GetVertices(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector3>):mscorlib System.Void", 517541617700098636},
                {"SetVertices(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector3>):mscorlib System.Void", 149023456079553777},
                {"SetVertices(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector3>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 940071802391369426},
                {"SetVertices(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector3>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 848418464824621079},
                {"SetVertices(UnityEngine.CoreModule UnityEngine.Vector3[]):mscorlib System.Void", 597812116560903793},
                {"SetVertices(UnityEngine.CoreModule UnityEngine.Vector3[],mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 953151707767939082},
                {"SetVertices(UnityEngine.CoreModule UnityEngine.Vector3[],mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 902629299491182712},
                {"SetVertices<T>(UnityEngine.CoreModule NativeArray<T>):mscorlib System.Void", 633501444853091089},
                {"SetVertices<T>(UnityEngine.CoreModule NativeArray<T>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 222396120657411735},
                {"SetVertices<T>(UnityEngine.CoreModule NativeArray<T>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 939018433639900368},
                {"GetNormals(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector3>):mscorlib System.Void", 924638088744135614},
                {"SetNormals(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector3>):mscorlib System.Void", 901385359165705271},
                {"SetNormals(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector3>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 220103979850338545},
                {"SetNormals(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector3>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 754838892304985459},
                {"SetNormals(UnityEngine.CoreModule UnityEngine.Vector3[]):mscorlib System.Void", 480731640973616952},
                {"SetNormals(UnityEngine.CoreModule UnityEngine.Vector3[],mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 728220167496498905},
                {"SetNormals(UnityEngine.CoreModule UnityEngine.Vector3[],mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 591329891878803372},
                {"SetNormals<T>(UnityEngine.CoreModule NativeArray<T>):mscorlib System.Void", 115967314348571035},
                {"SetNormals<T>(UnityEngine.CoreModule NativeArray<T>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 139943829869211588},
                {"SetNormals<T>(UnityEngine.CoreModule NativeArray<T>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 153491444330776823},
                {"GetTangents(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector4>):mscorlib System.Void", 307114844502698298},
                {"SetTangents(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector4>):mscorlib System.Void", 264850848918249728},
                {"SetTangents(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector4>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 284864693273148866},
                {"SetTangents(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector4>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 945736683526387001},
                {"SetTangents(UnityEngine.CoreModule UnityEngine.Vector4[]):mscorlib System.Void", 430026657477047387},
                {"SetTangents(UnityEngine.CoreModule UnityEngine.Vector4[],mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 344727106798613633},
                {"SetTangents(UnityEngine.CoreModule UnityEngine.Vector4[],mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 692817911657990076},
                {"SetTangents<T>(UnityEngine.CoreModule NativeArray<T>):mscorlib System.Void", 660281996885331368},
                {"SetTangents<T>(UnityEngine.CoreModule NativeArray<T>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 532781655979854707},
                {"SetTangents<T>(UnityEngine.CoreModule NativeArray<T>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 430191103521653153},
                {"GetColors(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Color>):mscorlib System.Void", 488863501602153303},
                {"SetColors(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Color>):mscorlib System.Void", 844167802432660031},
                {"SetColors(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Color>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 947659040036725031},
                {"SetColors(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Color>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 922005116903102145},
                {"SetColors(UnityEngine.CoreModule UnityEngine.Color[]):mscorlib System.Void", 618502777560663910},
                {"SetColors(UnityEngine.CoreModule UnityEngine.Color[],mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 994153213682457941},
                {"SetColors(UnityEngine.CoreModule UnityEngine.Color[],mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 231023865524029125},
                {"GetColors(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Color32>):mscorlib System.Void", 246529809400371178},
                {"SetColors(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Color32>):mscorlib System.Void", 886139869966323180},
                {"SetColors(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Color32>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 560212335425284124},
                {"SetColors(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Color32>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 900766093317718160},
                {"SetColors(UnityEngine.CoreModule UnityEngine.Color32[]):mscorlib System.Void", 766479333368120828},
                {"SetColors(UnityEngine.CoreModule UnityEngine.Color32[],mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 970422802770611436},
                {"SetColors(UnityEngine.CoreModule UnityEngine.Color32[],mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 743033262519789019},
                {"SetColors<T>(UnityEngine.CoreModule NativeArray<T>):mscorlib System.Void", 134466766375668317},
                {"SetColors<T>(UnityEngine.CoreModule NativeArray<T>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 593715938399169230},
                {"SetColors<T>(UnityEngine.CoreModule NativeArray<T>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 338627699784046151},
                {"SetUVs(mscorlib System.Int32,mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector2>):mscorlib System.Void", 531843746711533717},
                {"SetUVs(mscorlib System.Int32,mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector3>):mscorlib System.Void", 986501614425200770},
                {"SetUVs(mscorlib System.Int32,mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector4>):mscorlib System.Void", 939598198824133817},
                {"SetUVs(mscorlib System.Int32,mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector2>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 593022832040076287},
                {"SetUVs(mscorlib System.Int32,mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector2>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 848840388028239227},
                {"SetUVs(mscorlib System.Int32,mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector3>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 781665604775894507},
                {"SetUVs(mscorlib System.Int32,mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector3>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 865273381381174859},
                {"SetUVs(mscorlib System.Int32,mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector4>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 627323550313363145},
                {"SetUVs(mscorlib System.Int32,mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector4>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 341728431369193507},
                {"SetUVs(mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Vector2[]):mscorlib System.Void", 954172306792893850},
                {"SetUVs(mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Vector3[]):mscorlib System.Void", 889988128112986518},
                {"SetUVs(mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Vector4[]):mscorlib System.Void", 117941163956623261},
                {"SetUVs(mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Vector2[],mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 806594318480628474},
                {"SetUVs(mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Vector2[],mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 483772527259871470},
                {"SetUVs(mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Vector3[],mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 351262378322814998},
                {"SetUVs(mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Vector3[],mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 803406706951825722},
                {"SetUVs(mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Vector4[],mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 302049319443597356},
                {"SetUVs(mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Vector4[],mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 784446642740709168},
                {"SetUVs<T>(mscorlib System.Int32,UnityEngine.CoreModule NativeArray<T>):mscorlib System.Void", 685309635246346893},
                {"SetUVs<T>(mscorlib System.Int32,UnityEngine.CoreModule NativeArray<T>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 876640766973432987},
                {"SetUVs<T>(mscorlib System.Int32,UnityEngine.CoreModule NativeArray<T>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 137481039649481538},
                {"GetUVs(mscorlib System.Int32,mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector2>):mscorlib System.Void", 293563275280336876},
                {"GetUVs(mscorlib System.Int32,mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector3>):mscorlib System.Void", 639450002649280378},
                {"GetUVs(mscorlib System.Int32,mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector4>):mscorlib System.Void", 456320684920351599},
                {"GetVertexAttributes():UnityEngine.CoreModule UnityEngine.Rendering.VertexAttributeDescriptor[]", 973355153699095742},
                {"GetVertexAttributes(UnityEngine.CoreModule UnityEngine.Rendering.VertexAttributeDescriptor[]):mscorlib System.Int32", 921977517138779140},
                {"GetVertexAttributes(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Rendering.VertexAttributeDescriptor>):mscorlib System.Int32", 500026708483551966},
                {"SetVertexBufferParams(mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.VertexAttributeDescriptor[]):mscorlib System.Void", 899573702482094523},
                {"SetVertexBufferParams(mscorlib System.Int32,UnityEngine.CoreModule NativeArray<UnityEngine.CoreModule UnityEngine.Rendering.VertexAttributeDescriptor>):mscorlib System.Void", 280427912468781335},
                {"SetVertexBufferData<T>(UnityEngine.CoreModule NativeArray<T>,mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 193622421549803777},
                {"SetVertexBufferData<T>(T[],mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 636166133479176942},
                {"SetVertexBufferData<T>(mscorlib System.Collections.Generic.List<T>,mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 155800646265401379},
                {"GetVertexBuffer(mscorlib System.Int32):UnityEngine.CoreModule UnityEngine.GraphicsBuffer", 883749895237386061},
                {"GetIndexBuffer():UnityEngine.CoreModule UnityEngine.GraphicsBuffer", 372652996896789615},
                {"GetBoneWeightBuffer(UnityEngine.CoreModule UnityEngine.SkinWeights):UnityEngine.CoreModule UnityEngine.GraphicsBuffer", 831500440997943000},
                {"GetBlendShapeBuffer(UnityEngine.CoreModule UnityEngine.Rendering.BlendShapeBufferLayout):UnityEngine.CoreModule UnityEngine.GraphicsBuffer", 713175290845777023},
                {"GetBlendShapeBuffer():UnityEngine.CoreModule UnityEngine.GraphicsBuffer", 166221109985377999},
                {"GetBlendShapeBufferRange(mscorlib System.Int32):UnityEngine.CoreModule UnityEngine.BlendShapeBufferRange", 266520069421081004},
                {"GetTriangles(mscorlib System.Int32):mscorlib System.Int32[]", 449206530547777785},
                {"GetTriangles(mscorlib System.Int32,mscorlib System.Boolean):mscorlib System.Int32[]", 743526178890337364},
                {"GetTriangles(mscorlib System.Collections.Generic.List<mscorlib System.Int32>,mscorlib System.Int32):mscorlib System.Void", 196999051264420525},
                {"GetTriangles(mscorlib System.Collections.Generic.List<mscorlib System.Int32>,mscorlib System.Int32,mscorlib System.Boolean):mscorlib System.Void", 172303874011041378},
                {"GetTriangles(mscorlib System.Collections.Generic.List<mscorlib System.UInt16>,mscorlib System.Int32,mscorlib System.Boolean):mscorlib System.Void", 985457490436080238},
                {"GetIndices(mscorlib System.Int32):mscorlib System.Int32[]", 279302593022215164},
                {"GetIndices(mscorlib System.Int32,mscorlib System.Boolean):mscorlib System.Int32[]", 152580243702081309},
                {"GetIndices(mscorlib System.Collections.Generic.List<mscorlib System.Int32>,mscorlib System.Int32):mscorlib System.Void", 967295347712182517},
                {"GetIndices(mscorlib System.Collections.Generic.List<mscorlib System.Int32>,mscorlib System.Int32,mscorlib System.Boolean):mscorlib System.Void", 668806879094190529},
                {"GetIndices(mscorlib System.Collections.Generic.List<mscorlib System.UInt16>,mscorlib System.Int32,mscorlib System.Boolean):mscorlib System.Void", 776624353051046374},
                {"SetIndexBufferData<T>(UnityEngine.CoreModule NativeArray<T>,mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 105131624361266284},
                {"SetIndexBufferData<T>(T[],mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 194445272704872541},
                {"SetIndexBufferData<T>(mscorlib System.Collections.Generic.List<T>,mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 463500908188586138},
                {"GetIndexStart(mscorlib System.Int32):mscorlib System.UInt32", 165985982059972471},
                {"GetIndexCount(mscorlib System.Int32):mscorlib System.UInt32", 158454709321764357},
                {"GetBaseVertex(mscorlib System.Int32):mscorlib System.UInt32", 648751785921601507},
                {"SetTriangles(mscorlib System.Int32[],mscorlib System.Int32):mscorlib System.Void", 951127655128551483},
                {"SetTriangles(mscorlib System.Int32[],mscorlib System.Int32,mscorlib System.Boolean):mscorlib System.Void", 339766888872025749},
                {"SetTriangles(mscorlib System.Int32[],mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 741110566746911717},
                {"SetTriangles(mscorlib System.Int32[],mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 729434878309271174},
                {"SetTriangles(mscorlib System.UInt16[],mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 221919914295020891},
                {"SetTriangles(mscorlib System.UInt16[],mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 111624225646474769},
                {"SetTriangles(mscorlib System.Collections.Generic.List<mscorlib System.Int32>,mscorlib System.Int32):mscorlib System.Void", 571941068328838015},
                {"SetTriangles(mscorlib System.Collections.Generic.List<mscorlib System.Int32>,mscorlib System.Int32,mscorlib System.Boolean):mscorlib System.Void", 349779326771136131},
                {"SetTriangles(mscorlib System.Collections.Generic.List<mscorlib System.Int32>,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 107966689780746831},
                {"SetTriangles(mscorlib System.Collections.Generic.List<mscorlib System.Int32>,mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 170262529619270691},
                {"SetTriangles(mscorlib System.Collections.Generic.List<mscorlib System.UInt16>,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 838318964502529505},
                {"SetTriangles(mscorlib System.Collections.Generic.List<mscorlib System.UInt16>,mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 284443796950014499},
                {"SetIndices(mscorlib System.Int32[],UnityEngine.CoreModule UnityEngine.MeshTopology,mscorlib System.Int32):mscorlib System.Void", 365229792618976451},
                {"SetIndices(mscorlib System.Int32[],UnityEngine.CoreModule UnityEngine.MeshTopology,mscorlib System.Int32,mscorlib System.Boolean):mscorlib System.Void", 739562191458803924},
                {"SetIndices(mscorlib System.Int32[],UnityEngine.CoreModule UnityEngine.MeshTopology,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 995475017808356275},
                {"SetIndices(mscorlib System.Int32[],mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.MeshTopology,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 170302833471594246},
                {"SetIndices(mscorlib System.UInt16[],UnityEngine.CoreModule UnityEngine.MeshTopology,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 636345439300252795},
                {"SetIndices(mscorlib System.UInt16[],mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.MeshTopology,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 468354263621310064},
                {"SetIndices<T>(UnityEngine.CoreModule NativeArray<T>,UnityEngine.CoreModule UnityEngine.MeshTopology,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 962597352311870898},
                {"SetIndices<T>(UnityEngine.CoreModule NativeArray<T>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.MeshTopology,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 838117170898919882},
                {"SetIndices(mscorlib System.Collections.Generic.List<mscorlib System.Int32>,UnityEngine.CoreModule UnityEngine.MeshTopology,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 386099184185142123},
                {"SetIndices(mscorlib System.Collections.Generic.List<mscorlib System.Int32>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.MeshTopology,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 674431656430299761},
                {"SetIndices(mscorlib System.Collections.Generic.List<mscorlib System.UInt16>,UnityEngine.CoreModule UnityEngine.MeshTopology,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 263893209565679620},
                {"SetIndices(mscorlib System.Collections.Generic.List<mscorlib System.UInt16>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.MeshTopology,mscorlib System.Int32,mscorlib System.Boolean,mscorlib System.Int32):mscorlib System.Void", 884544375498538195},
                {"SetSubMeshes(UnityEngine.CoreModule UnityEngine.Rendering.SubMeshDescriptor[],mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 977113208175167897},
                {"SetSubMeshes(UnityEngine.CoreModule UnityEngine.Rendering.SubMeshDescriptor[],UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 662852781093657364},
                {"SetSubMeshes(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Rendering.SubMeshDescriptor>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 366133619932174043},
                {"SetSubMeshes(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Rendering.SubMeshDescriptor>,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 302533211542779252},
                {"SetSubMeshes<T>(UnityEngine.CoreModule NativeArray<T>,mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 252923274882860405},
                {"SetSubMeshes<T>(UnityEngine.CoreModule NativeArray<T>,UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 372281217625381199},
                {"GetBindposes(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Matrix4x4>):mscorlib System.Void", 138826832868237408},
                {"GetBoneWeights(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.BoneWeight>):mscorlib System.Void", 186171665950076137},
                {"Clear(mscorlib System.Boolean):mscorlib System.Void", 734644834848888200},
                {"Clear():mscorlib System.Void", 296726098279109976},
                {"RecalculateBounds():mscorlib System.Void", 434123671606176694},
                {"RecalculateNormals():mscorlib System.Void", 191864665573981287},
                {"RecalculateTangents():mscorlib System.Void", 606215439200251497},
                {"RecalculateBounds(UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 557956800871498998},
                {"RecalculateNormals(UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 888532535521149363},
                {"RecalculateTangents(UnityEngine.CoreModule UnityEngine.Rendering.MeshUpdateFlags):mscorlib System.Void", 974040566771135260},
                {"RecalculateUVDistributionMetric(mscorlib System.Int32,mscorlib System.Single):mscorlib System.Void", 991531128862102502},
                {"RecalculateUVDistributionMetrics(mscorlib System.Single):mscorlib System.Void", 474807685349697947},
                {"MarkDynamic():mscorlib System.Void", 243291260357953289},
                {"UploadMeshData(mscorlib System.Boolean):mscorlib System.Void", 618485678926526535},
                {"Optimize():mscorlib System.Void", 918263632848530823},
                {"OptimizeIndexBuffers():mscorlib System.Void", 875861334382041737},
                {"OptimizeReorderVertexBuffer():mscorlib System.Void", 144575272938660266},
                {"GetTopology(mscorlib System.Int32):UnityEngine.CoreModule UnityEngine.MeshTopology", 104682825970679388},
                {"CombineMeshes(UnityEngine.CoreModule UnityEngine.CombineInstance[],mscorlib System.Boolean,mscorlib System.Boolean,mscorlib System.Boolean):mscorlib System.Void", 839059949671287572},
                {"CombineMeshes(UnityEngine.CoreModule UnityEngine.CombineInstance[],mscorlib System.Boolean,mscorlib System.Boolean):mscorlib System.Void", 335164039225828133},
                {"CombineMeshes(UnityEngine.CoreModule UnityEngine.CombineInstance[],mscorlib System.Boolean):mscorlib System.Void", 695574325369310604},
                {"CombineMeshes(UnityEngine.CoreModule UnityEngine.CombineInstance[]):mscorlib System.Void", 366402961374304397}
            };
            Infra.Singleton.AddMethodSignatureToMethodIdMap(_typeReference, methodToId);
            Infra.Singleton.AddMethodIdToMethodMap(_typeReference, _idToMethod);
            Infra.Singleton.AddMethodIdToMethodInfoMap(_typeReference, _idToMethodInfo);
        }
        static Type _typeReference = typeof(Mesh);
        static Type _typeDefinition = typeof(UnityEngine.Mesh);
        static Type[] _args = _typeReference.IsGenericType ? _typeReference.GetGenericArguments() : null;
        public static Func<object, Delegate> _idToMethod(long id)
        {
            Func<object, Delegate> method = id switch
            {
                605665140375966075 => new Func<object, Delegate>((instance) => new Action<System.Int32, UnityEngine.Rendering.IndexFormat>(((Mesh)instance).SetIndexBufferParams)),
                736500501346727093 => new Func<object, Delegate>((instance) => new Func<System.Int32, UnityEngine.Rendering.VertexAttributeDescriptor>(((Mesh)instance).GetVertexAttribute)),
                245252918926855429 => new Func<object, Delegate>((instance) => new Func<UnityEngine.Rendering.VertexAttribute, System.Boolean>(((Mesh)instance).HasVertexAttribute)),
                900416489455887283 => new Func<object, Delegate>((instance) => new Func<UnityEngine.Rendering.VertexAttribute, System.Int32>(((Mesh)instance).GetVertexAttributeDimension)),
                157468257449020719 => new Func<object, Delegate>((instance) => new Func<UnityEngine.Rendering.VertexAttribute, UnityEngine.Rendering.VertexAttributeFormat>(((Mesh)instance).GetVertexAttributeFormat)),
                362887255304966247 => new Func<object, Delegate>((instance) => new Func<UnityEngine.Rendering.VertexAttribute, System.Int32>(((Mesh)instance).GetVertexAttributeStream)),
                343125893942516882 => new Func<object, Delegate>((instance) => new Func<UnityEngine.Rendering.VertexAttribute, System.Int32>(((Mesh)instance).GetVertexAttributeOffset)),
                116910294358378256 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Int32>(((Mesh)instance).GetVertexBufferStride)),
                514708421379717413 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.IntPtr>(((Mesh)instance).GetNativeVertexBufferPtr)),
                123383044566425027 => new Func<object, Delegate>((instance) => new Func<System.IntPtr>(((Mesh)instance).GetNativeIndexBufferPtr)),
                831848991563096522 => new Func<object, Delegate>((instance) => new Action(((Mesh)instance).ClearBlendShapes)),
                422065496485468058 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.String>(((Mesh)instance).GetBlendShapeName)),
                227439158047099360 => new Func<object, Delegate>((instance) => new Func<System.String, System.Int32>(((Mesh)instance).GetBlendShapeIndex)),
                577535241609503820 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Int32>(((Mesh)instance).GetBlendShapeFrameCount)),
                598380644949500758 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Int32, System.Single>(((Mesh)instance).GetBlendShapeFrameWeight)),
                168219854853426987 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Int32, UnityEngine.Vector3[], UnityEngine.Vector3[], UnityEngine.Vector3[]>(((Mesh)instance).GetBlendShapeFrameVertices)),
                308799139110975476 => new Func<object, Delegate>((instance) => new Action<System.String, System.Single, UnityEngine.Vector3[], UnityEngine.Vector3[], UnityEngine.Vector3[]>(((Mesh)instance).AddBlendShapeFrame)),
                227953588142252254 => new Func<object, Delegate>((instance) => new Action<NativeArray<System.Byte>, NativeArray<UnityEngine.BoneWeight1>>(((Mesh)instance).SetBoneWeights)),
                937918964487894976 => new Func<object, Delegate>((instance) => new Func<NativeArray<UnityEngine.BoneWeight1>>(((Mesh)instance).GetAllBoneWeights)),
                935510098795762053 => new Func<object, Delegate>((instance) => new Func<NativeArray<System.Byte>>(((Mesh)instance).GetBonesPerVertex)),
                921809809135828622 => new Func<object, Delegate>((instance) => new Func<NativeArray<UnityEngine.Matrix4x4>>(((Mesh)instance).GetBindposes)),
                931681016871573094 => new Func<object, Delegate>((instance) => new Action<NativeArray<UnityEngine.Matrix4x4>>(((Mesh)instance).SetBindposes)),
                502661096296614011 => new Func<object, Delegate>((instance) => new Action<System.Int32, UnityEngine.Rendering.SubMeshDescriptor, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetSubMesh)),
                234988293856173312 => new Func<object, Delegate>((instance) => new Func<System.Int32, UnityEngine.Rendering.SubMeshDescriptor>(((Mesh)instance).GetSubMesh)),
                937058572025755286 => new Func<object, Delegate>((instance) => new Action(((Mesh)instance).MarkModified)),
                151436723902330218 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Single>(((Mesh)instance).GetUVDistributionMetric)),
                517541617700098636 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Vector3>>(((Mesh)instance).GetVertices)),
                149023456079553777 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Vector3>>(((Mesh)instance).SetVertices)),
                940071802391369426 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Vector3>, System.Int32, System.Int32>(((Mesh)instance).SetVertices)),
                848418464824621079 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Vector3>, System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetVertices)),
                597812116560903793 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Vector3[]>(((Mesh)instance).SetVertices)),
                953151707767939082 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Vector3[], System.Int32, System.Int32>(((Mesh)instance).SetVertices)),
                902629299491182712 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Vector3[], System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetVertices)),
                924638088744135614 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Vector3>>(((Mesh)instance).GetNormals)),
                901385359165705271 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Vector3>>(((Mesh)instance).SetNormals)),
                220103979850338545 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Vector3>, System.Int32, System.Int32>(((Mesh)instance).SetNormals)),
                754838892304985459 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Vector3>, System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetNormals)),
                480731640973616952 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Vector3[]>(((Mesh)instance).SetNormals)),
                728220167496498905 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Vector3[], System.Int32, System.Int32>(((Mesh)instance).SetNormals)),
                591329891878803372 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Vector3[], System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetNormals)),
                307114844502698298 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Vector4>>(((Mesh)instance).GetTangents)),
                264850848918249728 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Vector4>>(((Mesh)instance).SetTangents)),
                284864693273148866 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Vector4>, System.Int32, System.Int32>(((Mesh)instance).SetTangents)),
                945736683526387001 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Vector4>, System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetTangents)),
                430026657477047387 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Vector4[]>(((Mesh)instance).SetTangents)),
                344727106798613633 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Vector4[], System.Int32, System.Int32>(((Mesh)instance).SetTangents)),
                692817911657990076 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Vector4[], System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetTangents)),
                488863501602153303 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Color>>(((Mesh)instance).GetColors)),
                844167802432660031 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Color>>(((Mesh)instance).SetColors)),
                947659040036725031 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Color>, System.Int32, System.Int32>(((Mesh)instance).SetColors)),
                922005116903102145 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Color>, System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetColors)),
                618502777560663910 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Color[]>(((Mesh)instance).SetColors)),
                994153213682457941 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Color[], System.Int32, System.Int32>(((Mesh)instance).SetColors)),
                231023865524029125 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Color[], System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetColors)),
                246529809400371178 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Color32>>(((Mesh)instance).GetColors)),
                886139869966323180 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Color32>>(((Mesh)instance).SetColors)),
                560212335425284124 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Color32>, System.Int32, System.Int32>(((Mesh)instance).SetColors)),
                900766093317718160 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Color32>, System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetColors)),
                766479333368120828 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Color32[]>(((Mesh)instance).SetColors)),
                970422802770611436 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Color32[], System.Int32, System.Int32>(((Mesh)instance).SetColors)),
                743033262519789019 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Color32[], System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetColors)),
                531843746711533717 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Collections.Generic.List<UnityEngine.Vector2>>(((Mesh)instance).SetUVs)),
                986501614425200770 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Collections.Generic.List<UnityEngine.Vector3>>(((Mesh)instance).SetUVs)),
                939598198824133817 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Collections.Generic.List<UnityEngine.Vector4>>(((Mesh)instance).SetUVs)),
                593022832040076287 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Collections.Generic.List<UnityEngine.Vector2>, System.Int32, System.Int32>(((Mesh)instance).SetUVs)),
                848840388028239227 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Collections.Generic.List<UnityEngine.Vector2>, System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetUVs)),
                781665604775894507 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Collections.Generic.List<UnityEngine.Vector3>, System.Int32, System.Int32>(((Mesh)instance).SetUVs)),
                865273381381174859 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Collections.Generic.List<UnityEngine.Vector3>, System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetUVs)),
                627323550313363145 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Collections.Generic.List<UnityEngine.Vector4>, System.Int32, System.Int32>(((Mesh)instance).SetUVs)),
                341728431369193507 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Collections.Generic.List<UnityEngine.Vector4>, System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetUVs)),
                954172306792893850 => new Func<object, Delegate>((instance) => new Action<System.Int32, UnityEngine.Vector2[]>(((Mesh)instance).SetUVs)),
                889988128112986518 => new Func<object, Delegate>((instance) => new Action<System.Int32, UnityEngine.Vector3[]>(((Mesh)instance).SetUVs)),
                117941163956623261 => new Func<object, Delegate>((instance) => new Action<System.Int32, UnityEngine.Vector4[]>(((Mesh)instance).SetUVs)),
                806594318480628474 => new Func<object, Delegate>((instance) => new Action<System.Int32, UnityEngine.Vector2[], System.Int32, System.Int32>(((Mesh)instance).SetUVs)),
                483772527259871470 => new Func<object, Delegate>((instance) => new Action<System.Int32, UnityEngine.Vector2[], System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetUVs)),
                351262378322814998 => new Func<object, Delegate>((instance) => new Action<System.Int32, UnityEngine.Vector3[], System.Int32, System.Int32>(((Mesh)instance).SetUVs)),
                803406706951825722 => new Func<object, Delegate>((instance) => new Action<System.Int32, UnityEngine.Vector3[], System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetUVs)),
                302049319443597356 => new Func<object, Delegate>((instance) => new Action<System.Int32, UnityEngine.Vector4[], System.Int32, System.Int32>(((Mesh)instance).SetUVs)),
                784446642740709168 => new Func<object, Delegate>((instance) => new Action<System.Int32, UnityEngine.Vector4[], System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetUVs)),
                293563275280336876 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Collections.Generic.List<UnityEngine.Vector2>>(((Mesh)instance).GetUVs)),
                639450002649280378 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Collections.Generic.List<UnityEngine.Vector3>>(((Mesh)instance).GetUVs)),
                456320684920351599 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Collections.Generic.List<UnityEngine.Vector4>>(((Mesh)instance).GetUVs)),
                973355153699095742 => new Func<object, Delegate>((instance) => new Func<UnityEngine.Rendering.VertexAttributeDescriptor[]>(((Mesh)instance).GetVertexAttributes)),
                921977517138779140 => new Func<object, Delegate>((instance) => new Func<UnityEngine.Rendering.VertexAttributeDescriptor[], System.Int32>(((Mesh)instance).GetVertexAttributes)),
                500026708483551966 => new Func<object, Delegate>((instance) => new Func<System.Collections.Generic.List<UnityEngine.Rendering.VertexAttributeDescriptor>, System.Int32>(((Mesh)instance).GetVertexAttributes)),
                899573702482094523 => new Func<object, Delegate>((instance) => new Action<System.Int32, UnityEngine.Rendering.VertexAttributeDescriptor[]>(((Mesh)instance).SetVertexBufferParams)),
                280427912468781335 => new Func<object, Delegate>((instance) => new Action<System.Int32, NativeArray<UnityEngine.Rendering.VertexAttributeDescriptor>>(((Mesh)instance).SetVertexBufferParams)),
                883749895237386061 => new Func<object, Delegate>((instance) => new Func<System.Int32, UnityEngine.GraphicsBuffer>(((Mesh)instance).GetVertexBuffer)),
                372652996896789615 => new Func<object, Delegate>((instance) => new Func<UnityEngine.GraphicsBuffer>(((Mesh)instance).GetIndexBuffer)),
                831500440997943000 => new Func<object, Delegate>((instance) => new Func<UnityEngine.SkinWeights, UnityEngine.GraphicsBuffer>(((Mesh)instance).GetBoneWeightBuffer)),
                713175290845777023 => new Func<object, Delegate>((instance) => new Func<UnityEngine.Rendering.BlendShapeBufferLayout, UnityEngine.GraphicsBuffer>(((Mesh)instance).GetBlendShapeBuffer)),
                166221109985377999 => new Func<object, Delegate>((instance) => new Func<UnityEngine.GraphicsBuffer>(((Mesh)instance).GetBlendShapeBuffer)),
                266520069421081004 => new Func<object, Delegate>((instance) => new Func<System.Int32, UnityEngine.BlendShapeBufferRange>(((Mesh)instance).GetBlendShapeBufferRange)),
                449206530547777785 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Int32[]>(((Mesh)instance).GetTriangles)),
                743526178890337364 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Boolean, System.Int32[]>(((Mesh)instance).GetTriangles)),
                196999051264420525 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.Int32>, System.Int32>(((Mesh)instance).GetTriangles)),
                172303874011041378 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.Int32>, System.Int32, System.Boolean>(((Mesh)instance).GetTriangles)),
                985457490436080238 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.UInt16>, System.Int32, System.Boolean>(((Mesh)instance).GetTriangles)),
                279302593022215164 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Int32[]>(((Mesh)instance).GetIndices)),
                152580243702081309 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Boolean, System.Int32[]>(((Mesh)instance).GetIndices)),
                967295347712182517 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.Int32>, System.Int32>(((Mesh)instance).GetIndices)),
                668806879094190529 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.Int32>, System.Int32, System.Boolean>(((Mesh)instance).GetIndices)),
                776624353051046374 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.UInt16>, System.Int32, System.Boolean>(((Mesh)instance).GetIndices)),
                165985982059972471 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.UInt32>(((Mesh)instance).GetIndexStart)),
                158454709321764357 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.UInt32>(((Mesh)instance).GetIndexCount)),
                648751785921601507 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.UInt32>(((Mesh)instance).GetBaseVertex)),
                951127655128551483 => new Func<object, Delegate>((instance) => new Action<System.Int32[], System.Int32>(((Mesh)instance).SetTriangles)),
                339766888872025749 => new Func<object, Delegate>((instance) => new Action<System.Int32[], System.Int32, System.Boolean>(((Mesh)instance).SetTriangles)),
                741110566746911717 => new Func<object, Delegate>((instance) => new Action<System.Int32[], System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetTriangles)),
                729434878309271174 => new Func<object, Delegate>((instance) => new Action<System.Int32[], System.Int32, System.Int32, System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetTriangles)),
                221919914295020891 => new Func<object, Delegate>((instance) => new Action<System.UInt16[], System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetTriangles)),
                111624225646474769 => new Func<object, Delegate>((instance) => new Action<System.UInt16[], System.Int32, System.Int32, System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetTriangles)),
                571941068328838015 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.Int32>, System.Int32>(((Mesh)instance).SetTriangles)),
                349779326771136131 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.Int32>, System.Int32, System.Boolean>(((Mesh)instance).SetTriangles)),
                107966689780746831 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.Int32>, System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetTriangles)),
                170262529619270691 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.Int32>, System.Int32, System.Int32, System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetTriangles)),
                838318964502529505 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.UInt16>, System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetTriangles)),
                284443796950014499 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.UInt16>, System.Int32, System.Int32, System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetTriangles)),
                365229792618976451 => new Func<object, Delegate>((instance) => new Action<System.Int32[], UnityEngine.MeshTopology, System.Int32>(((Mesh)instance).SetIndices)),
                739562191458803924 => new Func<object, Delegate>((instance) => new Action<System.Int32[], UnityEngine.MeshTopology, System.Int32, System.Boolean>(((Mesh)instance).SetIndices)),
                995475017808356275 => new Func<object, Delegate>((instance) => new Action<System.Int32[], UnityEngine.MeshTopology, System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetIndices)),
                170302833471594246 => new Func<object, Delegate>((instance) => new Action<System.Int32[], System.Int32, System.Int32, UnityEngine.MeshTopology, System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetIndices)),
                636345439300252795 => new Func<object, Delegate>((instance) => new Action<System.UInt16[], UnityEngine.MeshTopology, System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetIndices)),
                468354263621310064 => new Func<object, Delegate>((instance) => new Action<System.UInt16[], System.Int32, System.Int32, UnityEngine.MeshTopology, System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetIndices)),
                386099184185142123 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.Int32>, UnityEngine.MeshTopology, System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetIndices)),
                674431656430299761 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.Int32>, System.Int32, System.Int32, UnityEngine.MeshTopology, System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetIndices)),
                263893209565679620 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.UInt16>, UnityEngine.MeshTopology, System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetIndices)),
                884544375498538195 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<System.UInt16>, System.Int32, System.Int32, UnityEngine.MeshTopology, System.Int32, System.Boolean, System.Int32>(((Mesh)instance).SetIndices)),
                977113208175167897 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Rendering.SubMeshDescriptor[], System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetSubMeshes)),
                662852781093657364 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Rendering.SubMeshDescriptor[], UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetSubMeshes)),
                366133619932174043 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Rendering.SubMeshDescriptor>, System.Int32, System.Int32, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetSubMeshes)),
                302533211542779252 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Rendering.SubMeshDescriptor>, UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).SetSubMeshes)),
                138826832868237408 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Matrix4x4>>(((Mesh)instance).GetBindposes)),
                186171665950076137 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.BoneWeight>>(((Mesh)instance).GetBoneWeights)),
                734644834848888200 => new Func<object, Delegate>((instance) => new Action<System.Boolean>(((Mesh)instance).Clear)),
                296726098279109976 => new Func<object, Delegate>((instance) => new Action(((Mesh)instance).Clear)),
                434123671606176694 => new Func<object, Delegate>((instance) => new Action(((Mesh)instance).RecalculateBounds)),
                191864665573981287 => new Func<object, Delegate>((instance) => new Action(((Mesh)instance).RecalculateNormals)),
                606215439200251497 => new Func<object, Delegate>((instance) => new Action(((Mesh)instance).RecalculateTangents)),
                557956800871498998 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).RecalculateBounds)),
                888532535521149363 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).RecalculateNormals)),
                974040566771135260 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Rendering.MeshUpdateFlags>(((Mesh)instance).RecalculateTangents)),
                991531128862102502 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Single>(((Mesh)instance).RecalculateUVDistributionMetric)),
                474807685349697947 => new Func<object, Delegate>((instance) => new Action<System.Single>(((Mesh)instance).RecalculateUVDistributionMetrics)),
                243291260357953289 => new Func<object, Delegate>((instance) => new Action(((Mesh)instance).MarkDynamic)),
                618485678926526535 => new Func<object, Delegate>((instance) => new Action<System.Boolean>(((Mesh)instance).UploadMeshData)),
                918263632848530823 => new Func<object, Delegate>((instance) => new Action(((Mesh)instance).Optimize)),
                875861334382041737 => new Func<object, Delegate>((instance) => new Action(((Mesh)instance).OptimizeIndexBuffers)),
                144575272938660266 => new Func<object, Delegate>((instance) => new Action(((Mesh)instance).OptimizeReorderVertexBuffer)),
                104682825970679388 => new Func<object, Delegate>((instance) => new Func<System.Int32, UnityEngine.MeshTopology>(((Mesh)instance).GetTopology)),
                839059949671287572 => new Func<object, Delegate>((instance) => new Action<UnityEngine.CombineInstance[], System.Boolean, System.Boolean, System.Boolean>(((Mesh)instance).CombineMeshes)),
                335164039225828133 => new Func<object, Delegate>((instance) => new Action<UnityEngine.CombineInstance[], System.Boolean, System.Boolean>(((Mesh)instance).CombineMeshes)),
                695574325369310604 => new Func<object, Delegate>((instance) => new Action<UnityEngine.CombineInstance[], System.Boolean>(((Mesh)instance).CombineMeshes)),
                366402961374304397 => new Func<object, Delegate>((instance) => new Action<UnityEngine.CombineInstance[]>(((Mesh)instance).CombineMeshes)),
                _ => Infra.Singleton.GetIdToMethodMapForType(_typeReference.BaseType)(id),
            };
            return method;
        }
        public static MethodInfo _idToMethodInfo(long id)
        {
            MethodInfo methodDef = id switch
            {
                633501444853091089 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetVertices), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) }, null),
                222396120657411735 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetVertices), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32) }, null),
                939018433639900368 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetVertices), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32), typeof(UnityEngine.Rendering.MeshUpdateFlags) }, null),
                115967314348571035 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetNormals), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) }, null),
                139943829869211588 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetNormals), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32) }, null),
                153491444330776823 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetNormals), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32), typeof(UnityEngine.Rendering.MeshUpdateFlags) }, null),
                660281996885331368 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetTangents), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) }, null),
                532781655979854707 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetTangents), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32) }, null),
                430191103521653153 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetTangents), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32), typeof(UnityEngine.Rendering.MeshUpdateFlags) }, null),
                134466766375668317 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetColors), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) }, null),
                593715938399169230 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetColors), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32) }, null),
                338627699784046151 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetColors), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32), typeof(UnityEngine.Rendering.MeshUpdateFlags) }, null),
                685309635246346893 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetUVs), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(System.Int32), typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) }, null),
                876640766973432987 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetUVs), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(System.Int32), typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32) }, null),
                137481039649481538 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetUVs), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(System.Int32), typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32), typeof(UnityEngine.Rendering.MeshUpdateFlags) }, null),
                193622421549803777 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetVertexBufferData), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32), typeof(System.Int32), typeof(System.Int32), typeof(UnityEngine.Rendering.MeshUpdateFlags) }, null),
                636166133479176942 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetVertexBufferData), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { Type.MakeGenericMethodParameter(0).MakeArrayType(), typeof(System.Int32), typeof(System.Int32), typeof(System.Int32), typeof(System.Int32), typeof(UnityEngine.Rendering.MeshUpdateFlags) }, null),
                155800646265401379 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetVertexBufferData), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(System.Collections.Generic.List<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32), typeof(System.Int32), typeof(System.Int32), typeof(UnityEngine.Rendering.MeshUpdateFlags) }, null),
                105131624361266284 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetIndexBufferData), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32), typeof(System.Int32), typeof(UnityEngine.Rendering.MeshUpdateFlags) }, null),
                194445272704872541 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetIndexBufferData), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { Type.MakeGenericMethodParameter(0).MakeArrayType(), typeof(System.Int32), typeof(System.Int32), typeof(System.Int32), typeof(UnityEngine.Rendering.MeshUpdateFlags) }, null),
                463500908188586138 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetIndexBufferData), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(System.Collections.Generic.List<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32), typeof(System.Int32), typeof(UnityEngine.Rendering.MeshUpdateFlags) }, null),
                962597352311870898 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetIndices), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(UnityEngine.MeshTopology), typeof(System.Int32), typeof(System.Boolean), typeof(System.Int32) }, null),
                838117170898919882 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetIndices), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32), typeof(UnityEngine.MeshTopology), typeof(System.Int32), typeof(System.Boolean), typeof(System.Int32) }, null),
                252923274882860405 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetSubMeshes), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(System.Int32), typeof(System.Int32), typeof(UnityEngine.Rendering.MeshUpdateFlags) }, null),
                372281217625381199 => typeof(UnityEngine.Mesh).GetMethod(nameof(Mesh.SetSubMeshes), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(NativeArray<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(UnityEngine.Rendering.MeshUpdateFlags) }, null),
                _ => Infra.Singleton.GetMethodInfoIdToMethodMapForType(_typeReference.BaseType)(id),
            };
            return methodDef;
        }
    }


    public class MeshSaveData : AssetSaveData
    {
        public bool isReadable;
        public CompressedBuffer vertices = new();
        public CompressedBuffer indices = new();
        public CompressedBuffer normals = new();
        public CompressedBuffer tangents = new();
        public CompressedBuffer colors = new();
        public CompressedBuffer uvs = new();
        public int[] uvSizes = new int[8];
        public List<int> indexSizes = new();
        [NonSerialized, JsonIgnore] public PooledArray<Vector3> _vertices;
        [NonSerialized, JsonIgnore] public PooledArray<int> _indices;
        [NonSerialized, JsonIgnore] public PooledArray<Vector3> _normals;
        [NonSerialized, JsonIgnore] public PooledArray<Vector4> _tangents;
        [NonSerialized, JsonIgnore] public PooledArray<Color> _colors;
        [NonSerialized, JsonIgnore] public PooledArray<Vector2> _uvs;

        public int subMeshCount;

        public BoneWeight[] boneWeights;
        public Matrix4x4[] bindposes;

        public BlendShapeData[] blendShapes;

        public List<MeshTopology> submeshTopologies = new();
        public UnityEngine.Rendering.IndexFormat indexFormat;
        public UnityEngine.GraphicsBuffer.Target vertexBufferTarget;
        public UnityEngine.GraphicsBuffer.Target indexBufferTarget;

        public string name;
        public bool ShouldWriteOrLoad; //for debugging purposes, no need to load it back


        ~MeshSaveData()
        {
            _vertices.Dispose();
            _normals.Dispose();
            _tangents.Dispose();
            _colors.Dispose();
            _uvs.Dispose();
        }
    }


    public class BlendShapeData
    {
        public string Name;
        public BlendShapeFrameData[] Frames;
    }

    public class BlendShapeFrameData
    {
        public float Weight;
        public Vector3[] DeltaVertices;
        public Vector3[] DeltaNormals;
        public Vector3[] DeltaTangents;
    }





}

/*
1. Create new Mesh
2. Set indexFormat
3. Assign vertices
4. Assign triangles
5. Assign submeshes
6. Assign UVs / normals / tangents / colors
7. Assign skinning data
8. Assign blendshapes
9. Recalculate bounds (RecalculateBounds())
*/






//compression

/*

2. **Reduce mesh data size structurally**
3. **Binary packing**
4. **General-purpose compression**



## Option 1 — Structural mesh size reduction (VERY powerful)

This beats compression because it reduces entropy.

---

### Quantization (huge win)

Instead of float32 everywhere:

| Channel   | Typical   | Compressed           |
| --------- | --------- | -------------------- |
| positions | float32×3 | float16 or quantized |
| normals   | float32×3 | octahedral / snorm16 |
| tangents  | float32×4 | snorm16              |
| UVs       | float32×2 | half                 |

---

### Position quantization example

Instead of:

```csharp
Vector3 (12 bytes)
```

Store:

* mesh bounds
* ushort per axis (6 bytes total)

Typical savings: **50–70% on vertex data**

---

### Normal compression

Normals are unit vectors — perfect for packing.

Common encodings:

* octahedral encoding (best)
* spherical
* snorm16

Savings: **60–75%**

---

### Index compression

Unity indices are usually:

* int32 (4 bytes)

But many meshes fit in:

* uint16 (2 bytes)

Check:

```csharp
if (vertexCount < 65536)
```

Savings: **50% on indices**

---

## Option 2 — Binary packing (you should absolutely do this)

Use:

* tight binary layout
* no per-element overhead
* contiguous arrays

---

### Good formats

* custom binary writer
* MemoryPack
* protobuf (less ideal for raw arrays)
* MessagePack


## Option 3 — General-purpose compression

This is the final polish layer.

Good when applied **after structural optimization**.

---

## Compression algorithms comparison

### LZ4 (Unity’s favorite)

Pros:

* extremely fast
* low CPU
* streaming-friendly
* works great on mesh data

Cons:

* medium compression ratio

Best for:

* runtime saves
* frequent autosaves
* background thread use

---

### Deflate / GZip

Pros:

* better ratio than LZ4

Cons:

* slower
* more CPU

Best for:

* cloud saves
* infrequent saves

---

### Zstd (best modern choice if available)

Pros:

* excellent ratio
* very fast decompression
* tunable

Cons:

* requires external lib

Best for:

* serious systems
* large worlds

---

### Brotli

Great ratio, but usually too slow for runtime saves.

---

# Recommended stack for Unity save systems

2. Quantization
3. Binary packing
4. LZ4 or Zstd


## Pattern C — job batching (advanced)

Batch multiple meshes into one compression stream.

Benefits:

* better compression ratio
* fewer allocations
* better IO

---

# One subtle trap (advanced but important)

If you compress each mesh separately:

worse compression ratio
more headers
more allocations

Often better to:

* serialize many meshes into one buffer
* then compress once


**Phase 1 (quick win)**

* switch to binary if not already
* add LZ4
* background thread compression

**Phase 2 (big win)**

* vertex quantization
* index downcast
* batch compression

**Phase 3 (elite tier)**

* octahedral normals
* Zstd
* streaming save chunks
*/