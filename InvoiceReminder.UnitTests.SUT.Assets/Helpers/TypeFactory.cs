using Microsoft.CSharp;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace InvoiceReminder.UnitTests.SUT.Assets.Helpers;

public interface ITypeFactory
{
    bool CanCreateInstance(Type type);
    object CreateRandomValue(Type type);
}

[ExcludeFromCodeCoverage]
/// <summary>
///     Static helper class to help with the construction of types and random values
/// </summary>
public class TypeFactory : ITypeFactory
{
    protected readonly Random Random = new();

    /// <summary>
    ///     Indicates whether the CreateRandomValue method is able to create an instance of
    ///     the specified type.
    /// </summary>
    /// <param name="type">The type to test</param>
    /// <returns>true or false</returns>
    public virtual bool CanCreateInstance(Type type)
    {
        return (HasDefaultConstructor(type) || type == typeof(string) || type == typeof(Type) || type.IsArray) &&
               !type.IsGenericTypeDefinition;
    }

    /// <summary>
    ///     Generates a random value of the specified type
    /// </summary>
    /// <param name="type">The type to be generated</param>
    /// <returns>A random value or the default instance for unknown types</returns>
    public virtual object CreateRandomValue(Type type)
    {
        // First, is the type a nullable type? if so return a value based on the
        // generic argument.
        if (IsNullableType(type))
        {
            var subType = type.GetGenericArguments()[0];
            return CreateRandomValue(subType);
        }

        if (type.IsArray)
            return Array.CreateInstance(type.GetElementType(), Random.Next(50));

        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.GetValue(Random.Next(values.Length));
        }

        if (type == typeof(Guid))
            return Guid.NewGuid();

        if (type == typeof(Type))
            return GenerateNewType();

        if (type == typeof(TimeSpan))
            return new TimeSpan(Random.Next(int.MaxValue));

        var typeCode = Type.GetTypeCode(type);

        return typeCode switch
        {
            TypeCode.Boolean => Random.Next(2) == 1,
            TypeCode.Byte => Convert.ToByte(Random.Next(byte.MinValue, byte.MaxValue)),
            TypeCode.Char => Convert.ToChar(Random.Next(char.MinValue, char.MaxValue)),
            TypeCode.DateTime => new DateTime(Random.Next(int.MaxValue), DateTimeKind.Utc),
            TypeCode.Decimal => Convert.ToDecimal(Random.Next(int.MaxValue)),
            TypeCode.Double => Random.NextDouble(),
            TypeCode.Int16 => Convert.ToInt16(Random.Next(short.MinValue, short.MaxValue)),
            TypeCode.Int32 => Random.Next(int.MinValue, int.MaxValue),
            TypeCode.Int64 => Convert.ToInt64(Random.Next(int.MinValue, int.MaxValue)),
            TypeCode.SByte => Convert.ToSByte(Random.Next(sbyte.MinValue, sbyte.MaxValue)),
            TypeCode.Single => Convert.ToSingle(Random.Next(sbyte.MinValue, sbyte.MaxValue)),
            TypeCode.String => Guid.NewGuid().ToString(),
            TypeCode.UInt16 => Convert.ToUInt16(Random.Next(0, ushort.MaxValue)),
            TypeCode.UInt32 => Convert.ToUInt32(Random.Next(0, int.MaxValue)),
            TypeCode.UInt64 => Convert.ToUInt64(Random.Next(0, int.MaxValue)),
            _ => Activator.CreateInstance(type),
        };
    }

    /// <summary>
    ///     Compiles an entirely new Class (with a random name) and returns its System.Type representation
    /// </summary>
    /// <returns>System.Type for a generated, random class</returns>
    protected virtual Type GenerateNewType()
    {
        const string generatedNamespace = "__GeneratedNamespace";
        var className = "_" + Guid.NewGuid().ToString("N");

        var ns = new CodeNamespace(generatedNamespace);
        ns.Imports.Add(new CodeNamespaceImport("System"));

        var cls = new CodeTypeDeclaration(className)
        {
            IsClass = true,
            TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed
        };

        _ = ns.Types.Add(cls);

        var ccu = new CodeCompileUnit();
        _ = ccu.Namespaces.Add(ns);

        using var cs = new CSharpCodeProvider();
        var compilerParameters = new CompilerParameters
        {
            GenerateInMemory = true
        };

        var compilerResults = cs.CompileAssemblyFromDom(compilerParameters, ccu);

        if (compilerResults.Errors.Count > 0)
            throw new InvalidOperationException(string.Format(
                "There were {2} error(s) compiling the new type (first error shown only):{0}{1}",
                Environment.NewLine,
                compilerResults.Errors[0].ErrorText,
                compilerResults.Errors.Count));

        var generatedType = compilerResults.CompiledAssembly
            .GetType(generatedNamespace + "." + className, true);

        var result = Activator.CreateInstance(generatedType);

        return result.GetType();
    }

    /// <summary>
    ///     Indicates whether a type has a default public constructor, e.g. can be 'newed' up
    ///     unlike System.String which cannot
    /// </summary>
    /// <param name="type">The type to test</param>
    /// <returns>true or false</returns>
    protected virtual bool HasDefaultConstructor(Type type)
    {
        // Is this a struct? If so - can be new'ed up.
        if (type.IsSubclassOf(typeof(ValueType)))
            return true;

        // otherwise test for an actual default constructor
        return type.GetConstructor([]) != null;
    }

    /// <summary>
    ///     Indicates whether the type is a nullable type (e.g. int? or Nullable&lt;int>)
    /// </summary>
    /// <param name="type">The type to test</param>
    /// <returns></returns>
    protected virtual bool IsNullableType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
}
