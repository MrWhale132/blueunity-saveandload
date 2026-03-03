using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad.SavableDelegates;
using Assets._Project.Scripts.UtilScripts;
using Assets._Project.Scripts.UtilScripts.CodeGen;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Theblueway.Core.Runtime;
using Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime;
using UnityEngine;

namespace Assets._Project.Scripts.SaveAndLoad
{
    //todo, optimization: create an other ctor that accepts the "type" of T so it doesn't have to be checked every time
    //todo: should this be a struct instead?
    //todo: create a MigrationData<T> that inherits from Data<T> or from Data to separate migration logic?
    public class Data<T> : Data
    {
        public T _Value;
        public CustomSaveData<T> _SaveData;
        public CustomSaveData _RawSaveData;
        public RandomId _ObjectId;
        public InvocationList _InvocationList;



        [JsonIgnore]
        public Action<T, Data<T>> _setter;
        [JsonIgnore]
        public Func<Data<T>, T> _getter;

        [JsonIgnore]
        public T Value { get => Get(); internal set => Set(value, ReferencedBy); }


        public override RandomId ReferencedBy { get; set; }


        public Data()
        {
            ///<see cref="CopyFrom(Data)"/> relies on this to being set to None at the begining
            _dataType = DataType.None;

            SetValueOrNoneAccessors(this);
        }



        public void Set(T value, RandomId referencedBy)
        {
            _Reset();

            ReferencedBy = referencedBy;

            Type typeToVersion = value?.GetType() ?? typeof(T);
            _versionedType = VersionedType.From(typeToVersion);

            Type instanceType = value?.GetType();
            SetAccessors(DetermineDataType(instanceType));
            SetHelpers(this, instanceType, _dataType);

            _Set(value);
        }

        public void _Set(T value)
        {
            _setter(value, this);
        }
        public T Get()
        {
            return _getter(this);
        }




        public DataType DetermineDataType(Type type)
        {
            if (type == null) return DataType.None;

            if (SaveAndLoadManager.Singleton.HasSaveHandlerForType(type) || ( typeof(T).IsAssignableTo(typeof(SaveDataBase))))
            {
                return DataType.Reference;
            }
            else if (typeof(Delegate).IsAssignableFrom(typeof(T)))
            {
                return DataType.Delegate;
            }
            else if (SaveAndLoadManager.Singleton.HasCustomSaveData(type))
            {
                //if the struct instance referenced through one if its interfaces or as an object.
                if (typeof(T).IsInterface || typeof(T) == typeof(object))
                    return DataType.RawCustomSaveData;
                else
                    return DataType.CustomSaveData;
            }
            else
            {
                return DataType.Value;
            }
        }




        public void SetAccessors(DataType dataType)
        {
            _dataType = dataType;

            if (dataType is DataType.Reference) SetReferenceAccessors(this);
            else if (dataType is DataType.Delegate) SetDelegateAccessors(this);
            else if (dataType is DataType.CustomSaveData) SetCustomSaveDataAccessors(this);
            else if (dataType is DataType.RawCustomSaveData) SetRawCustomSaveDataAccessors(this);
            else if (dataType is DataType.Value or DataType.None) SetValueOrNoneAccessors(this);
        }


        public static void SetReferenceAccessors(Data<T> data)
        {
            data._setter = (value, instance) =>
            {
                //we assume that who ever is setting the value, if it is migrating, than the value to be set is migrating too
                if (SaveAndLoadManager.Singleton.IsObjectMigrating(instance.ReferencedBy, out _))
                {
                    var savedata = (SaveDataBase)(object)value;
                    instance._ObjectId = savedata._ObjectId_;
                }
                else
                {
                    instance._ObjectId = Infra.Singleton.GetObjectId(value, instance.ReferencedBy, setLoadingOrder: true);
                }
            };
            data._getter = (instance) =>
            {
                if (SaveAndLoadManager.Singleton.IsObjectMigrating(instance._ObjectId, out var migrationContext))
                {
                    return migrationContext.GetObject<T>(instance._ObjectId, inheritsOrImplements: true);
                }
                else
                {
                    return Infra.Singleton.GetObjectById<T>(instance._ObjectId);
                }
            };
        }


        public static void SetDelegateAccessors(Data<T> data)
        {
            data._setter = (value, instance) => instance._InvocationList = Infra.Singleton.GetInvocationList(value as Delegate);
            data._getter = (instance) => Infra.Singleton.GetDelegate<T>(instance._InvocationList);
        }


        public static void SetCustomSaveDataAccessors(Data<T> data)
        {
            data._setter = (val, instance) => instance._SaveData.ReadFrom(in val);
            data._getter = (instance) =>
            {
                instance._SaveData.WriteInto(ref instance._Value);
                return instance._Value;
            };
        }


        public static void SetRawCustomSaveDataAccessors(Data<T> data)
        {

            data._setter = (val, instance) =>
            {
                instance._RawSaveData.SlowReadFrom(in val);
            };
            data._getter = (instance) =>
            {
                instance._RawSaveData.SlowWriteInto(ref data._Value);
                return instance._Value;
            };
        }


        public static void SetValueOrNoneAccessors(Data<T> data)
        {
            data._setter = (value, instance) => instance._Value = value;
            data._getter = (instance) => instance._Value;
        }




        public static void SetHelpers(Data<T> data, Type instanceType, DataType dataType)
        {
            if (dataType is DataType.CustomSaveData)
            {
                //todo: object pool
                data._SaveData = SaveAndLoadManager.Singleton.CreateCustomSaveDataInstanceFor<T>(instanceType);

                data._Value ??= ObjectFactory.CreateInstance<T>(instanceType);
            }
            else if (dataType is DataType.RawCustomSaveData)
            {
                data._RawSaveData = SaveAndLoadManager.Singleton.CreateCustomSaveDataInstanceFor(instanceType);

                data._Value = ObjectFactory.CreateInstance<T>(instanceType);
            }
        }




        public void _Reset()
        {
            _ObjectId = default;
            _Value = default;
            _SaveData = default;
            _RawSaveData = default;
            _InvocationList = default;
            ReferencedBy = default;
            _getter = null;
            _setter = null;
            _versionedType = default;
        }


        public override void _SetUnderlyingObject(object data, DataType dataType)
        {
            if (dataType is DataType.Value)
            {
                _Value = (T)data;
            }
            else if (dataType is DataType.Reference)
            {
                _ObjectId = (RandomId)data;
            }
            else if (dataType is DataType.CustomSaveData)
            {
                _SaveData = (CustomSaveData<T>)data;
            }
            else if (dataType is DataType.RawCustomSaveData)
            {
                _RawSaveData = (CustomSaveData)data;
            }
            else if (dataType is DataType.Delegate)
            {
                _InvocationList = (InvocationList)data;
            }
            else if (dataType is DataType.None) { }//do nothing
            else
                throw new ArgumentOutOfRangeException(nameof(dataType), $"Unsupported data type: {dataType}");

            SetAccessors(dataType);
        }




        public override object _GetUnderlyingObject()
        {
            object obj = _dataType switch
            {
                DataType.Reference => _ObjectId,
                DataType.CustomSaveData => _SaveData,
                DataType.RawCustomSaveData => _RawSaveData,
                DataType.Delegate => _InvocationList,
                DataType.Value => _Value,
                DataType.None => null,
                _ => null
            };

            return obj;
        }






        public override void CopyFrom(Data from, MigrationContext context)
        {
            _dataType = from._dataType;
            _versionedType = from._versionedType;
            _migratedUnderlyingObject = from._migratedUnderlyingObject;
            _originalDataTypeBeforeMigration = from._originalDataTypeBeforeMigration;
            _lastKnownAppVersion = from._lastKnownAppVersion;
            ReferencedBy = from.ReferencedBy;


            if (from.HasMigratedUnderlyingObject)
            {
                object prevValue = from._GetUnderlyingObject();

                if (prevValue?.Equals(default(T)) is false)
                {
                    context.CopyIdenticalMembers(from: prevValue, to: _migratedUnderlyingObject);
                }

                MigrateIfNeeded(context);

                _Value = ObjectFactory.CreateInstance<T>(typeof(T));

                //_Value can be struct
                object val = _Value;
                context.CopyIdenticalMembers(from: _migratedUnderlyingObject, to: val);
                _Value = (T)val;

                context._dataInstances.Remove(from);
                context._dataInstances.Add(this);

                SetAccessors(_dataType);
            }
            else
            {
                _SetUnderlyingObject(from._GetUnderlyingObject(), from._dataType);
            }
        }





        public void MigrateIfNeeded(MigrationContext context)
        {
            if (!HasMigratedUnderlyingObject)
            {
                return;
            }

            if (_lastKnownAppVersion == context.CurrentStep.appVersion)
            {
                return;
            }

            _lastKnownAppVersion = context.CurrentStep.appVersion;


            if (_dataType is DataType.Value)
            {
                if (_migratedUnderlyingObject is CustomSaveData csd)
                {
                    var migrated = csd.MigrateIfNeeded(context, out bool didMigrate);

                    if (didMigrate)
                    {
                        _migratedUnderlyingObject = migrated;
                    }
                }
            }
            else if (_dataType is DataType.CustomSaveData or DataType.RawCustomSaveData)
            {
                Debug.LogError($"During migration the data type of a Data instance should never be {_dataType}");
            }
        }




        public override void FinalizeMigrationStep(MigrationContext context)
        {
            CopyCurrentValueIntoMigratedObjectIfNeeded(context);

            MigrateIfNeeded(context);
        }

        internal void CopyCurrentValueIntoMigratedObjectIfNeeded(MigrationContext context)
        {
            if (_Value?.Equals(default(T)) is false)
            {
                context.CopyIdenticalMembers(from: _Value, to: _migratedUnderlyingObject);
            }
        }
    }









    public abstract class Data
    {
        public enum DataType
        {
            None,
            Value,
            Delegate,
            Reference,
            //todo: in order to use this, we would need to tell to this Data instance when the migration is stopped so it can switch back to normal Reference type
            //update: ids are marked as migrating before they are serialized, and they can be removed from migration before copying/redeserilazing them back
            MigrationReference,
            CustomSaveData,
            RawCustomSaveData,
        }

        internal DataType _dataType;
        internal RandomId _versionedType;
        public virtual RandomId ReferencedBy { get; set; }


        [JsonIgnore]
        public object _migratedUnderlyingObject;
        [JsonIgnore]
        internal bool HasMigratedUnderlyingObject => _migratedUnderlyingObject != null;
        [JsonIgnore]
        public DataType _originalDataTypeBeforeMigration;
        [JsonIgnore]
        internal int _lastKnownAppVersion = -1;

        public abstract object _GetUnderlyingObject();
        public abstract void _SetUnderlyingObject(object data, DataType dataType);
        public abstract void FinalizeMigrationStep(MigrationContext context);
        public abstract void CopyFrom(Data from, MigrationContext context);
    }
















    public sealed class DataJsonConverter<T> : MyJsonConverter<Data<T>>
    {
        public override void WriteJson(JsonWriter writer, Data<T> value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            if (value.HasMigratedUnderlyingObject)
            {
                value._dataType = value._originalDataTypeBeforeMigration;
            }

            writer.WritePropertyName(nameof(Data<int>._dataType));
            writer.WriteValue(value._dataType.ToString());

            writer.WritePropertyName(nameof(Data<int>._versionedType));
            serializer.Serialize(writer, value._versionedType);

            writer.WritePropertyName(nameof(Data<int>.ReferencedBy));
            serializer.Serialize(writer, value.ReferencedBy);


            if (value.HasMigratedUnderlyingObject)
            {
                string propName = value._originalDataTypeBeforeMigration switch
                {
                    Data.DataType.Value => nameof(Data<int>._Value),
                    Data.DataType.CustomSaveData => nameof(Data<int>._SaveData),
                    Data.DataType.RawCustomSaveData => nameof(Data<int>._RawSaveData),
                    Data.DataType.Reference => nameof(Data<int>._ObjectId),
                    Data.DataType.Delegate => nameof(Data<int>._InvocationList),
                    Data.DataType.None => null, //do nothing
                    _ => throw new JsonSerializationException($"Unknown DataType: {value._originalDataTypeBeforeMigration}"),
                };


                if (propName != null)
                {
                    writer.WritePropertyName(propName);
                    serializer.Serialize(writer, value._migratedUnderlyingObject);
                }
            }
            else
            {
                switch (value._dataType)
                {
                    case Data.DataType.Value:
                        writer.WritePropertyName(nameof(Data<int>._Value));
                        serializer.Serialize(writer, value._Value);
                        break;

                    case Data.DataType.CustomSaveData:
                        writer.WritePropertyName(nameof(Data<int>._SaveData));
                        serializer.Serialize(writer, value._SaveData);
                        break;

                    case Data.DataType.RawCustomSaveData:
                        writer.WritePropertyName(nameof(Data<int>._RawSaveData));
                        serializer.Serialize(writer, value._RawSaveData);
                        break;

                    case Data.DataType.Reference:
                        writer.WritePropertyName(nameof(Data<int>._ObjectId));
                        serializer.Serialize(writer, value._ObjectId);
                        break;

                    case Data.DataType.Delegate:
                        writer.WritePropertyName(nameof(Data<int>._InvocationList));
                        serializer.Serialize(writer, value._InvocationList);
                        break;

                    case Data.DataType.None:
                        //do nothing
                        break;

                    default:
                        throw new JsonSerializationException($"Unknown DataType: {value._dataType}");
                }
            }

            writer.WriteEndObject();
        }



        public override Data<T> ReadJson(
            JsonReader reader,
            Type objectType,
            Data<T> existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject jobject = JObject.Load(reader);

            var result = hasExistingValue ? existingValue : new Data<T>();

            result._dataType = Enum.Parse<Data.DataType>(jobject[nameof(Data<int>._dataType)]!.Value<string>());
            result._versionedType = jobject[nameof(Data<int>._versionedType)]!.ToObject<RandomId>(serializer);
            result.ReferencedBy = jobject[nameof(Data<int>.ReferencedBy)]!.ToObject<RandomId>(serializer);


            if (result._dataType is Data.DataType.None)
            {
                result.SetAccessors(result._dataType);
                return result;
            }


            if (result._versionedType == default)
            {
                Debug.LogError(jobject);
            }



            bool isMigrating = false;

            if (SaveAndLoadManager.Singleton.IsVersionedTypeLoading(result._versionedType, out var loadContext))
            {
                if (loadContext.IsMigrating())
                {
                    isMigrating = true;
                }
            }



            Type handledType;

            if (isMigrating)
            {
                result._originalDataTypeBeforeMigration = result._dataType;
                handledType = VersionedType.ResolveForVersionedHandledType(result._versionedType, out _);
            }
            else
            {
                handledType = VersionedType.ResolveForCurrentHandledType(result._versionedType);
                //migration can alter the data type, so lets determine from the type the migration is ended up
                //update: for that. _versionedType would need to be updated during migration when a different type of underlying object is assigned to this Data instance
                //which is currently not solved
                //result._dataType = result.DetermineDataType(handledType);
            }



            //todo: if it is value type, do not create an instance here

            switch (result._dataType)
            {
                case Data.DataType.Value:
                    try
                    {
                        var obj = jobject[nameof(Data<int>._Value)]!.ToObject(handledType, serializer);

                        if ((handledType.IsStruct() || handledType.IsClass) && handledType != typeof(string) && isMigrating)
                        {
                            result._migratedUnderlyingObject = obj;
                        }
                        else
                        {
                            if (!handledType.IsAssignableTo(typeof(T)))
                            {
                                Debug.LogError($"Type mismatch during deserialization of Data<{typeof(T).CleanAssemblyQualifiedName()}>. Handled type: {handledType.CleanAssemblyQualifiedName()}");
                            }

                            result._Value = (T)obj;
                        }
                    }
                    catch (Exception)
                    {
                        Debug.Log(handledType == null);
                        Debug.LogError(handledType.CleanAssemblyQualifiedName());
                        Debug.LogError(jobject.ToString());
                        throw;
                    }
                    break;

                case Data.DataType.CustomSaveData or Data.DataType.RawCustomSaveData:

                    Type handlerType = SaveAndLoadManager.Singleton.GetCustomSaveDataHandlerTypeByHandledType(handledType);

                    //try
                    //{
                    //        result._Value = ObjectFactory.CreateInstance<T>(handledType);
                    //}
                    //catch (Exception)
                    //{
                    //    Debug.Log(result._isMigrating);
                    //    Debug.LogError(jobject.ToString());
                    //    Debug.LogError(handledType.CleanAssemblyQualifiedName());
                    //    Debug.LogError(typeof(T).CleanAssemblyQualifiedName());
                    //    throw;
                    //}


                    if (result._dataType is Data.DataType.CustomSaveData)
                    {
                        var property = jobject[nameof(Data<int>._SaveData)];

                        var obj = property.ToObject(handlerType, serializer);

                        if (isMigrating)
                        {
                            result._dataType = Data.DataType.Value;
                            result._migratedUnderlyingObject = obj;
                        }
                        else
                        {
                            result._SaveData = obj as CustomSaveData<T>;
                        }
                    }
                    else
                    {
                        var obj = jobject[nameof(Data<int>._RawSaveData)]!.ToObject(handlerType, serializer) as CustomSaveData;


                        if (isMigrating)
                        {
                            result._dataType = Data.DataType.Value;
                            result._migratedUnderlyingObject = obj;
                        }
                        else
                        {
                            result._Value = ObjectFactory.CreateInstance<T>(handledType);
                            result._RawSaveData = obj;
                        }
                    }
                    break;

                case Data.DataType.Reference:
                    result._ObjectId = jobject[nameof(Data<int>._ObjectId)]!.ToObject<RandomId>(serializer);
                    break;

                case Data.DataType.Delegate:
                    result._InvocationList = jobject[nameof(Data<int>._InvocationList)]!.ToObject<InvocationList>(serializer);
                    break;

                default:
                    throw new JsonSerializationException($"Unknown DataType: {result._dataType}");
            }


            result.SetAccessors(result._dataType);

            if (isMigrating)
            {
                loadContext.MigrationContext.AddDataInstance(result);
            }

            return result;
        }
    }









    //this is how I quickly solved to stop newtonsoft from picking up my generic converter that never meant to be used by it self
    public abstract class MyJsonConverter
    {
        public abstract void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);
        public abstract object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            bool hasExistingValue,
            JsonSerializer serializer);
    }
    public abstract class MyJsonConverter<T> : MyJsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var val = (T)value;
            WriteJson(writer, val, serializer);
        }
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var existingVal = hasExistingValue ? (T)existingValue : default;
            return ReadJson(reader, objectType, existingVal, hasExistingValue, serializer);
        }

        public abstract void WriteJson(JsonWriter writer, T value, JsonSerializer serializer);
        public abstract T ReadJson(
            JsonReader reader,
            Type objectType,
            T existingValue,
            bool hasExistingValue,
            JsonSerializer serializer);
    }






    //dont delete this, its collected via reflection on bootstrap
    [JsonConverter(typeof(DataJsonConverterFactory))]
    public sealed class DataJsonConverterFactory : JsonConverter
    {
        public Dictionary<Type, MyJsonConverter> _cachedConverters = new();

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType &&
                   objectType.GetGenericTypeDefinition() == typeof(Data<>);
        }

        public MyJsonConverter GetConverter(Type type)
        {
            if (!_cachedConverters.ContainsKey(type))
            {
                Type wrappedType = type.GetGenericArguments()[0];

                Type genericConverterType = typeof(DataJsonConverter<>).MakeGenericType(wrappedType);

                var converter = ObjectFactory.CreateInstance(genericConverterType) as MyJsonConverter;

                _cachedConverters.Add(type, converter);
            }

            return _cachedConverters[type];
        }


        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Type dataType = value.GetType();

            var converter = GetConverter(dataType);
            converter.WriteJson(writer, value, serializer);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var converter = GetConverter(objectType);
            return converter.ReadJson(reader, objectType, existingValue, false, serializer);
        }
    }
}
