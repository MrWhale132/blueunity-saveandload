#pragma warning disable 8321 // Unused local function
using System;
using System.Collections.Generic;
using Theblueway.TypeBinding.Runtime.TypeInterfaceScripts;
using UnityEngine.UI;
using static Theblueway.CodeGen.Runtime.TypeUtils;

namespace Theblueway.SaveAndLoad.Runtime.TypeInterfaces
{
    //[TypeInterface(280351672890743277, typeof(Graphic))]
    public class TestTypeInterface : TypeInterface<Graphic>
    {
        static TypeMember[] _members = new TypeMember[]
        {
            (0, runInEditMode),
            (1, GetMethodSignature<Action>(OnRebuildRequested)),
            (3, "test2"),
            (4, "test3"),
            (5, GetMethodSignature<Action<int>>(OnRebuildRequested)),
            (6, GetMethodSignature<Action<TypeArg0, TypeArg1>>(Gen<TypeArg0, TypeArg1>)),
            (7, GetMethodSignature<Action<TypeArg1, TypeArg0>>(Gen<TypeArg0, TypeArg1>)),
            (8, GetMethodSignature<Func<TypeArg0, TypeArg1, TypeArg2>>(Gen<TypeArg0, TypeArg1, TypeArg2>)),
            (9, GetMethodSignature<Action<List<TypeArg1>, TypeArg0>>(Gen<TypeArg0, TypeArg1>)),
        };

        public override IEnumerable<TypeMember> GetMembers()
        {
            return _members;
        }


#if UNITY_EDITOR
        static string runInEditMode => nameof(Graphic.runInEditMode);
#else
        string runInEditMode => "";
#endif
        static void OnRebuildRequested()
        {
#if UNITY_EDITOR
            Action _ = instance.OnRebuildRequested;
#endif
        }
        static void OnRebuildRequested(int i)
        {
        }
        static void Gen<T, U>(T t, U u)
        {
            void Test<T0_, T1_>() where T0_ : struct
            {
                Action<T0_, T1_> _ = Gen<T0_, T1_>;
            }
        }
        static void Gen<U, T>(T t, U u) { }
        static void Gen<U, T>() { }
        static void Gen<U, T>(List<T> t, U u) { }
        static V Gen<T, U, V>(T t, U u)
        {
            void Test<T0_, T1_, T2_>() where T0_ : struct
            {
                Func<T0_, T1_, T2_> _ = Gen<T0_, T1_, T2_>;
            }

            return default;
        }
    }
}
