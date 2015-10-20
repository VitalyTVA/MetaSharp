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
            //TODO use user-friendly types to return errors instead of Either
            //TODO check args
            //TODO check return value type
            //TODO check return error type
            var parameters = methodContext.Method.GetParameters().Length == 1
                ? methodContext.Context.YieldToArray()
                : null;
            try {
                Func<string, ImmutableArray<Output>> getDefaultOutput = s => new Output(s, GetOutputFileName(methodContext.Method, methodContext.FileName, environment)).YieldToImmutable();
                var methodResult = methodContext.Method.Invoke(null, parameters);
                if(methodContext.Method.ReturnType.IsGenericType && methodContext.Method.ReturnType.GetGenericTypeDefinition() == typeof(Either<,>)) {
                    var method = typeof(MethodProcessor).GetMethod("ToMethodOutput", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(methodContext.Method.ReturnType.GetGenericArguments());
                    return (Either<ImmutableArray<MetaError>, ImmutableArray<Output>>)method.Invoke(null, new[] { methodResult, getDefaultOutput });
                }
                ImmutableArray<Output> output = ValueToOutputs(methodResult, methodContext.Method.ReturnType, getDefaultOutput);
                return output;
            } catch(TargetInvocationException e) {
                var error = Generator.CreateError(Messages.Exception_Id, methodContext.FileName, string.Format(Messages.Exception_Message, e.InnerException.Message, e.InnerException), methodContext.MethodSpan);
                return error.YieldToImmutable();
            }
        }
        static Either<ImmutableArray<MetaError>, ImmutableArray<Output>> ToMethodOutput<TLeft, TRight>(Either<TLeft, TRight> value, Func<string, ImmutableArray<Output>> getDefaultOutput) {
            return value.Transform(
                error => {
                    if(typeof(TLeft) == typeof(MetaError))
                        return (error as MetaError).YieldToImmutable();
                    else
                        return (error as IEnumerable<MetaError>).ToImmutableArray();
                },
                val => ValueToOutputs(val, typeof(TRight), getDefaultOutput)
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
            var methodAttribute = method.GetCustomAttribute<MetaLocationAttribute>();
            var typeAttribute = method.DeclaringType.GetCustomAttribute<MetaLocationAttribute>();

            var location = methodAttribute?.Location ?? typeAttribute?.Location
                ?? (environment.BuildConstants.GeneratorMode == GeneratorMode.ConsoleApp ? MetaLocation.Project : MetaLocation.IntermediateOutput);
            var outputFileName = (methodAttribute?.FileName ?? typeAttribute?.FileName).With(x => string.Format(x, Path.GetFileNameWithoutExtension(fileName))) 
                ?? GetOutputFileName(location, fileName);

            var path = Path.Combine(GetOutputDirectory(location, environment.BuildConstants), outputFileName);
            return new OutputFileName(path, location != MetaLocation.Project);
        }
        static string GetOutputDirectory(MetaLocation location, BuildConstants buildConstants) {
            switch(location) {
            case MetaLocation.IntermediateOutput:
                return buildConstants.IntermediateOutputPath;
            case MetaLocation.Project:
                return string.Empty;
            default:
                throw new InvalidOperationException();
            }
        }
        static string GetOutputFileName(MetaLocation location, string fileName) {
            switch(location) {
            case MetaLocation.IntermediateOutput:
                return fileName.ReplaceEnd(Generator.CShaprFileExtension, Generator.DefaultOutputFileEnd);
            case MetaLocation.Project:
                return fileName.ReplaceEnd(Generator.CShaprFileExtension, Generator.ProjectOutputFileEnd);
            default:
                throw new InvalidOperationException();
            }
        }
    }
}