using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace BlueHeron.Collections.Trie.Tests;

#nullable disable

/// <summary>
/// Crude way to get a reasonable size of an object.
/// Adapted from: https://github.com/CyberSaving/MemoryUsage/blob/master/Main/Program.cs.
/// </summary>
/// <typeparam name="T">The type of the object to measure for size</typeparam>
internal sealed class SizeOf<T>
{
    private static int SizeOfClass(object obj)
    {
        var fields = obj.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        var size = 0;

        for (var i = 0; i < fields.Length; i++)
        {
            size += 4 + SizeOfObject(fields[i].FieldType, fields[i].GetValue(obj));
        }
        return size;
    }

    private static int SizeOfStruct(object obj)
    {
        var fields = obj.GetType().GetFields();
        var size = 0;

        for (var i = 0; i < fields.Length; i++)
        {
            size += SizeOfObject(fields[i].FieldType, fields[i].GetValue(obj));
        }
        return size;
    }

    private static int SizeOfObject(Type type, object obj)
    {
        var size = 0;
        if (type.IsValueType) // breaks on tuple
        {
            try
            {
                var nullType = Nullable.GetUnderlyingType(type);
                size = Marshal.SizeOf(nullType ?? type);
            }
            catch
            {
                size = SizeOfStruct(obj);
            }
        }
        else if (obj == null)
        {
            return 0;
        }
        else if (obj is string)
        {
            size = Encoding.Default.GetByteCount(obj as string);
        }
        else if (type.IsArray)
        {
            var elementType = type.GetElementType();
            var arr = (Array)obj;

            if (elementType.IsValueType)
            {
                try
                {
                    if (elementType.IsGenericType)
                    {
                        var enumerator = arr.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            size += SizeOfStruct(enumerator.Current);
                        }
                    }
                    else
                    {
                        size = arr.GetLength(0) * Marshal.SizeOf(elementType);
                    }
                }
                catch { }
            }
            else
            {
                var enumerator = arr.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    size += SizeOfObject(elementType, enumerator.Current);
                }
            }
        }
        //else if (type.IsAssignableTo(typeof(IEnumerable)))
        //{
        //    var elementType = type.GetGenericArguments().FirstOrDefault();
            
        //    if (elementType == null)
        //    {
        //        return SizeOfClass(obj);
        //    }
        //    else
        //    {
        //        var arr = (IEnumerable)obj;

        //        if (elementType.IsValueType)
        //        {
        //            try
        //            {
        //                if (elementType.IsGenericType)
        //                {
        //                    var enumerator = arr.GetEnumerator();
        //                    while (enumerator.MoveNext())
        //                    {
        //                        size += SizeOfStruct(enumerator.Current);
        //                    }
        //                }
        //                else
        //                {
        //                    size = ((ICollection)arr).Count * Marshal.SizeOf(elementType);
        //                }
        //            }
        //            catch { }
        //        }
        //        else
        //        {
        //            var enumerator = arr.GetEnumerator();

        //            while (enumerator.MoveNext())
        //            {
        //                size += SizeOfObject(elementType, enumerator.Current);
        //            }
        //        }
        //    }
        //}
        else if (type.IsClass)
        {
            size += SizeOfClass(obj);
        }
        if (size == 0)
        {
            try
            {
                size = Marshal.SizeOf(obj);
            }
            catch
            {
                size = (int)Marshal.ReadIntPtr(type.TypeHandle.Value, 4);
            }
        }
        return size;
    }

    /// <summary>
    /// Returns the size of the given object, in number of bytes.
    /// </summary>
    /// <param name="value">The object to measure up for size</param>
    /// <returns>The number of bytes allocated to this object</returns>
    public static int Get(T value)
    {
        return SizeOfObject(typeof(T), value);
    }
}

internal struct Pin(object o) : IDisposable
{
    public GCHandle pinHandle = GCHandle.Alloc(o, GCHandleType.Pinned);

    public void Dispose()
    {
        pinHandle.Free();
    }
}

/// <summary>
/// Cache object for the calculated size of an array element.
/// </summary>
/// <typeparam name="T">The type of the element</typeparam>
internal static class ElementSize<T>
{
    private static int CalcSize(T[] testArray)
    {
        using var p = new Pin(testArray);
        return (int)(Marshal.UnsafeAddrOfPinnedArrayElement(testArray, 1).ToInt64() - Marshal.UnsafeAddrOfPinnedArrayElement(testArray, 0).ToInt64());
    }

    public static readonly int Bytes = CalcSize(new T[2]);
}