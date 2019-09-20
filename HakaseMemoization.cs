using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using ILUtility;

namespace Hakase
{
    public static class Memoization
    {
        private static Dictionary<Type, Type> sTypeDictionary = new Dictionary<Type, Type>();
        private static AssemblyBuilder sAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(cAssemblyName), AssemblyBuilderAccess.RunAndSave);
        private static ModuleBuilder sModuleBuilder = sAssemblyBuilder.DefineDynamicModule(cAssemblyName, cAssemblyName + ".dll");

        private const string cAssemblyName = "HakaseMemoization";

        public static T Create<T>(params object[] parameters)
        {
            if (!sTypeDictionary.TryGetValue(typeof(T), out Type type))
            {
                //指定したクラスを親とするクラスを作る
                var typeBuilder = sModuleBuilder.DefineType(cAssemblyName + Guid.NewGuid(), TypeAttributes.Class, typeof(T));
                //メモに使うDictionaryの型
                var dictionaryType = typeof(Dictionary<Param, T>);
                //メモ用Dictionaryを定義する
                var dictionaryField = typeBuilder.DefineField(
                    "Field_" + Guid.NewGuid().ToString("N"),
                    dictionaryType,
                    FieldAttributes.Private);

                //dictionaryField.SetValue(dictionaryField, new Dictionary<Param, T>());
                //それぞれメソッド取得
                var dictionaryConstructor = dictionaryType.GetConstructor(Type.EmptyTypes);
                var tryGetValue = dictionaryType.GetMethod("TryGetValue");
                var setItem = dictionaryType.GetMethod("set_Item");
                var paramConstructor = typeof(Param).GetConstructors()[0];
                //メモ化できるメソッドを取得
                var methods = typeof(T)
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                    .Where(a => a.IsVirtual && !a.IsFinal)
                    .Where(a => a.ReturnParameter.ParameterType != typeof(void))
                    .Where(a => a.GetParameters().Length > 0)
                    .Where(a => a.GetCustomAttribute<HakaseMemoizationAttribute>() != null);

                //コンストラクタ作る
                var ctor = typeBuilder.DefineConstructor
                    (MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    CallingConventions.Standard,
                    null);
                //IL打ち込む
                var ctorIl = ctor.GetILGenerator();
                //ローカル変数宣言
                ctorIl.DeclareLocal(dictionaryType);
                //継承元であるobjcetのコンストラクターを呼ぶ
                ctorIl.Emit(OpCodes.Ldarg_0);
                ctorIl.Emit(OpCodes.Call, typeof(T).GetConstructor(Type.EmptyTypes));
                //Dictionaryの生成を行う
                ctorIl.Emit(OpCodes.Ldarg_0);
                ctorIl.Emit(OpCodes.Newobj, dictionaryConstructor);
                ctorIl.Emit(OpCodes.Stfld, dictionaryField);
                ctorIl.Emit(OpCodes.Ret);


                //実際にメモ化するILを吐く
                foreach (var method in methods)
                {
                    //引数の型を取得
                    var parameterTypes = method
                        .GetParameters()
                        .Select(a => a.ParameterType)
                        .ToArray();
                    //継承元となるメソッドのアクセス修飾子によってつけるものを変える
                    var methodAttributes = method.IsAbstract
                        ? MethodAttributes.Public | MethodAttributes.Virtual
                        : method.Attributes;
                    //メソッド定義
                    var methodBuilder = typeBuilder.DefineMethod(
                        method.Name,
                        methodAttributes,
                        method.CallingConvention,
                        method.ReturnParameter.ParameterType,
                        parameterTypes);

                    //こっからはいつもどおりILを打ち込んでいく
                    var il = methodBuilder.GetILGenerator();

                    // ローカル変数の宣言
                    il.DeclareLocal(typeof(Param));
                    il.DeclareLocal(method.ReturnParameter.ParameterType); // result
                    il.DeclareLocal(typeof(bool));
                    il.DeclareLocal(typeof(bool));

                    // ラベルの宣言
                    var label2 = il.DefineLabel();
                    var label3 = il.DefineLabel();

                    // object[] を用意
                    il.LdcI4(parameterTypes.Length);
                    il.Emit(OpCodes.Newarr, typeof(object));

                    // object[] に引数をセット
                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        il.Emit(OpCodes.Dup);
                        il.LdcI4(i);
                        il.Ldarg(i + 1);
                        if (parameterTypes[i].IsValueType)
                        {
                            il.Emit(OpCodes.Box, parameterTypes[i]);
                        }
                        il.Emit(OpCodes.Stelem_Ref);
                    }

                    // Param を作成してローカル変数 0 に保存
                    il.Emit(OpCodes.Newobj, paramConstructor);
                    il.Stloc(0);

                    // dictionary.TryGetValue(param, out result)
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, dictionaryField);
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ldloca_S,1);
                    il.Emit(OpCodes.Callvirt, tryGetValue);
                    il.Emit(OpCodes.Stloc_3);

                    // TryGetValue の結果が false なら label2 へ
                    il.Emit(OpCodes.Ldloc_3);
                    il.Emit(OpCodes.Brfalse_S, label2);

                    // リターン位置へジャンプ
                    il.Emit(OpCodes.Br_S, label3);

                    il.MarkLabel(label2);

                    // 継承したメソッドの実行
                    for (int i = 0; i < parameterTypes.Length + 1; i++)
                    {
                        il.Ldarg(i);
                    }
                    il.Emit(OpCodes.Call, method);
                    il.Emit(OpCodes.Stloc_1);

                    // メモ
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, dictionaryField);
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ldloc_1);
                    il.Emit(OpCodes.Callvirt, setItem);

                    il.MarkLabel(label3);

                    il.Emit(OpCodes.Ldloc_1);
                    il.Emit(OpCodes.Ret);
                    typeBuilder.DefineMethodOverride(methodBuilder, method);
                }
                type = typeBuilder.CreateType();
                sTypeDictionary[typeof(T)] = type;
                //デバッグのためセーブ
                sAssemblyBuilder.Save(cAssemblyName + ".dll");
                ILUtility.ILUtility.Verify(sAssemblyBuilder);
            }           
            return (T)Activator.CreateInstance(type, parameters);
        }

        public class Param 
        {
            private object[] items;
            private int hashCode = 0;

            public Param(params object[] items)
            {
                this.items = items;
                for(int i = 0;i < Count;i++)
                {
                    hashCode ^= items[i] == null ? 0 : items[i].GetHashCode();
                }
            }

            public int Count { get => items.Length; }

            public override bool Equals(object obj)
            {
                var param = obj as Param;
                if (param == null) return false;
                if (Count != param.Count) return false;
                if (hashCode != param.GetHashCode()) return false;
                return true;
            }

            public override int GetHashCode()
            {
                return hashCode;
            }
        }
    }

    public class HakaseMemoizationAttribute : Attribute { }

    public class HakaseMemo
    {
        public void MeasureTime(string title, int index)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            int result = Fibonacci(index);
            stopwatch.Stop();
            Console.WriteLine($"[{title}]");
            Console.WriteLine($"Index: {index}");
            Console.WriteLine($"Answer: {result}");
            Console.WriteLine($"Time: {stopwatch.Elapsed}");
            Console.WriteLine("");
        }

        [HakaseMemoization]
        public virtual int Fibonacci(int index)
        {
            if (index < 0) throw new ArgumentException();
            return index < 2 ? index : Fibonacci(index - 1) + Fibonacci(index - 2);
        }
    }
}
