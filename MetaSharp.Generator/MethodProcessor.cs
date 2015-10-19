using MetaSharp.Native;
using MetaSharp.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GeneratorResult = MetaSharp.Either<System.Collections.Immutable.ImmutableArray<MetaSharp.MetaError>, System.Collections.Immutable.ImmutableArray<string>>;

namespace MetaSharp {
    public static class MethodProcessor {
        public static Either<ImmutableArray<MetaError>, ImmutableArray<Output>> GetMethodOutput(MethodContext methodContext, Environment environment) {
            //TODO check args
            //TODO check return type
            var parameters = methodContext.Method.GetParameters().Length == 1
                ? methodContext.Context.YieldToArray()
                : null;
            try {
                Func<string, ImmutableArray<Output>> getDefaultOutput = s => new Output(s, GetOutputFileName(methodContext.Method, methodContext.FileName, environment)).YieldToImmutable();
                var methodResult = methodContext.Method.Invoke(null, parameters);
                if(methodContext.Method.ReturnType.IsGenericType && methodContext.Method.ReturnType.GetGenericTypeDefinition() == typeof(Either<,>)) {
                    var method = typeof(MethodProcessor).GetMethod("ToMethodOutput", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(methodContext.Method.ReturnType.GetGenericArguments());
                    return (Either<ImmutableArray<MetaError>, ImmutableArray<Output>>)method.Invoke(null, new[] { methodResult, methodContext.Method.ReturnType.GetGenericArguments().Last(), getDefaultOutput });
                }
                ImmutableArray<Output> output = ValueToOutputs(methodResult, methodContext.Method.ReturnType, getDefaultOutput);
                return Either<ImmutableArray<MetaError>, ImmutableArray<Output>>.Right(output);
            } catch(TargetInvocationException e) {
                var error = Generator.CreateError(Messages.Exception_Id, methodContext.FileName, string.Format(Messages.Exception_Message, e.InnerException.Message, e.InnerException), methodContext.MethodSpan);
                return Either<ImmutableArray<MetaError>, ImmutableArray<Output>>.Left(error.YieldToImmutable());
            }
        }
        static Either<ImmutableArray<MetaError>, ImmutableArray<Output>> ToMethodOutput<TLeft, TRight>(Either<TLeft, TRight> value, Type valueType, Func<string, ImmutableArray<Output>> getDefaultOutput) {
            return value.Transform(
                error => ImmutableArray<MetaError>.Empty,
                val => ValueToOutputs(val, valueType, getDefaultOutput)
            );
        }
        static ImmutableArray<Output> ValueToOutputs(object value, Type valueType, Func<string, ImmutableArray<Output>> getDefaultOutput) {
            if(valueType == typeof(string))
                return getDefaultOutput((string)value);
            else if(typeof(IEnumerable<string>).IsAssignableFrom(valueType))
                return getDefaultOutput(((IEnumerable<string>)value).ConcatStringsWithNewLines());
            else if(valueType == typeof(Output))
                return ((Output)value).YieldToImmutable();
            else
                return ((IEnumerable<Output>)value).ToImmutableArray();
        }
        static OutputFileName GetOutputFileName(MethodInfo method, string fileName, Environment environment) {
            var location = method.GetCustomAttribute<MetaLocationAttribute>()?.Location
                ?? method.DeclaringType.GetCustomAttribute<MetaLocationAttribute>()?.Location
                ?? default(MetaLocationKind);
            return environment.CreateOutput(fileName, location);
        }
    }
}