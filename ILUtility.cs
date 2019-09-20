using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace ILUtility
{
    static class ILUtility
    {
        public static void Verify(params AssemblyBuilder[] builders)
        {
            var path = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\x64\PEVerify.exe";

            foreach (var targetDll in builders)
            {
                var psi = new ProcessStartInfo(path, targetDll.GetName().Name + ".dll")
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                var p = Process.Start(psi);
                var data = p.StandardOutput.ReadToEnd();
                Console.WriteLine(data);
            }
        }

        /// <summary>
        /// 第一引数を取得
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        public static void LdargFirst(this ILGenerator il, MethodBase method)
        {
            //staticメソッドならldarg.0
            if (method.IsStatic) il.Emit(OpCodes.Ldarg_0);
            //インスタンスメソッドならldarg.1
            else il.Emit(OpCodes.Ldarg_1);
        }

        /// <summary>
        /// インスタンスメソッドのthisを取得する
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        public static void LdargThis(this ILGenerator il, MethodBase method)
        {
            if (method.IsStatic) throw new System.ArgumentException();
            il.Emit(OpCodes.Ldarg_0);
        }

        public static void Ldarg(this ILGenerator il,int value)
        {
            switch (value)
            {
                case 0:
                    il.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    if (value <= 255 && value >= 4) il.Emit(OpCodes.Ldarg_S, (byte)value);
                    else il.Emit(OpCodes.Ldarg, value);
                    break;
            }
        }

        /// <summary>
        /// 指定した引数を取得
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        /// <param name="value"></param>
        public static void Ldarg(this ILGenerator il, MethodBase method,int value)
        {
            //インスタンスメソッドなら第一引数はthisのため一個ずらす
            if (!method.IsStatic) value++;

            switch (value)
            {
                case 0:
                    il.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    if (value <= 255 && value >= 4) il.Emit(OpCodes.Ldarg_S, (byte)value);
                    else il.Emit(OpCodes.Ldarg, value);
                    break;
            }
        }

        public static void LdcI4(this ILGenerator il,int value)
        {
            //-1～8までは専用のコードが存在する
            //sbyteまでの範囲ならsbyteにした方が最適化がかかるらしい
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    break;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    if (value <= 127 && value >= -127) il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    else il.Emit(OpCodes.Ldc_I4, value);
                    break;
            }
        }

        /// <summary>
        /// 仮想メソッドか否かで呼ぶcallを切り替える
        /// </summary>
        /// <param name="il"></param>
        /// <param name="method"></param>
        public static void Call(this ILGenerator il,MethodInfo method)
        {
            if (method.IsFinal || !method.IsVirtual) il.Emit(OpCodes.Call, method);
            else il.Emit(OpCodes.Callvirt, method);
        }

        /// <summary>
        /// 保存されているローカルの値を取り出す
        /// </summary>
        /// <param name="il"></param>
        /// <param name="value"></param>
        public static void Ldloc(this ILGenerator il, int value)
        {
            switch (value)
            {
                case 0:
                    il.Emit(OpCodes.Ldloc_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldloc_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldloc_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldloc_3);
                    break;
                default:
                    if (value <= 255 && value >= 0) il.Emit(OpCodes.Ldloc_S, (byte)value);
                    else il.Emit(OpCodes.Ldloc, value);
                    break;
            }
        }

        /// <summary>
        /// ローカルに値を保存する
        /// </summary>
        /// <param name="il"></param>
        /// <param name="value"></param>
        public static void Stloc(this ILGenerator il, int value)
        {
            switch (value)
            {
                case 0:
                    il.Emit(OpCodes.Stloc_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Stloc_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Stloc_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Stloc_3);
                    break;
                default:
                    if (value <= 255 && value >= 0) il.Emit(OpCodes.Stloc_S, (byte)value);
                    else il.Emit(OpCodes.Stloc, value);
                    break;
            }
        }
    }
}
