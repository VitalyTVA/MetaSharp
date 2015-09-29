﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MetaSharp.Native;
using System.Linq.Expressions;

namespace MetaSharp {
    public class MetaContext {
        public string Namespace { get; }
        public IEnumerable<string> Usings { get; }
        public MetaContext(string @namespace, IEnumerable<string> usings) {
            Namespace = @namespace;
            Usings = usings;
        }
    }
    public static class MetaContextExtensions {
        //TODO replace all string types with tree string builder
        public static string WrapMembers(this MetaContext metaContext, string members) {
            var usings = metaContext.Usings.ConcatStringsWithNewLines();
            return 
$@"namespace {metaContext.Namespace} {{
{usings}

{members.AddIndent(4)}
}}";
        }
    }
    public enum MetaLocationKind {
        IntermediateOutput,
        IntermediateOutputNoIntellisense,
        Designer,
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class MetaLocationAttribute : Attribute {
        public MetaLocationAttribute(MetaLocationKind location = default(MetaLocationKind)) {
            Location = location;
        }
        public MetaLocationKind Location { get; set; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class MetaIncludeAttribute : Attribute {
        public MetaIncludeAttribute(string fileName) {
            FileName = fileName;
        }
        public string FileName { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class MetaProtoAttribute : Attribute {
        public MetaProtoAttribute(string fileName) {
            FileName = fileName;
        }
        public string FileName { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MetaCompleteClassAttribute : Attribute {
    }

    public enum RelativeLocation {
        Project,
        TargetPath,
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class MetaReferenceAttribute : Attribute {
        public MetaReferenceAttribute(string dllName, RelativeLocation relativeLocation = RelativeLocation.Project) {
            DllName = dllName;
            RelativeLocation = relativeLocation;
        }
        public string DllName { get; private set; }
        public RelativeLocation RelativeLocation { get; private set; }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MetaRewriteTypeArgsAttribute : Attribute {
        //TODO specify method to rewrite explicitly
        //TODO apply to classes, not only methods
    }
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class MetaRewriteLambdaParamAttribute : Attribute {
    }
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class MetaRewriteParamAttribute : Attribute {
    }
    public static class ClassGenerator {
        [MetaRewriteTypeArgs]
        public static ClassGenerator<T> Class<T>() {
            throw new NotImplementedException();
        }
        public static ClassGenerator_ Class(string name)
            => new ClassGenerator_(name, ClassModifiers.Public, false);
    }
    public class ClassGenerator<T> {
        [MetaRewriteTypeArgs]
        public ClassGenerator<T> Property<TProperty>([MetaRewriteLambdaParam] Func<T, TProperty> property, [MetaRewriteParam] TProperty defaultValue = default(TProperty)) {
            throw new NotImplementedException();
        }
        public string Generate() {
            throw new NotImplementedException();
        }
    }
    public enum ClassModifiers {
        Public,
        Partial,
    }
    public class ClassGenerator_ {
        struct PropertyInfo {
            public readonly string Type, Name, DefaultValue;
            public string CtorParameterName => Name.ToCamelCase();
            public PropertyInfo(string type, string name, string defaultValue) {
                Type = type;
                Name = name;
                DefaultValue = defaultValue;
            }
        }
        readonly string name;
        readonly ClassModifiers modifiers;
        readonly bool skipProperties;
        //TODO make immutable
        readonly List<PropertyInfo> properties;
        public ClassGenerator_(string name, ClassModifiers modifiers, bool skipProperties) {
            this.name = name;
            this.modifiers = modifiers;
            this.skipProperties = skipProperties;
            this.properties = new List<PropertyInfo>();
        }
        public ClassGenerator_ Property(string propertyType, string propertyName, string defaultValue = null) {
            properties.Add(new PropertyInfo(propertyType, propertyName, defaultValue));
            return this;
        }
        //TODO all properties with default value should be in the end, but try preserve original order
        public string Generate() {
            var propertiesList = skipProperties 
                ? string.Empty 
                : properties
                    .Select(x => $"public {x.Type} {x.Name} {{ get; }}")
                    .ConcatStringsWithNewLines();

            var arguments = properties
                .Select(x => {
                    var defaultValuePart = !string.IsNullOrEmpty(x.DefaultValue) ? (" = " + x.DefaultValue) : string.Empty;
                    return $"{x.Type} {x.CtorParameterName}{defaultValuePart}";
                })
                .ConcatStrings(", ");

            var assignments = properties
                .Select(x => {
                    return $"{x.Name} = {x.CtorParameterName};";
                })
                .ConcatStringsWithNewLines();

            return
$@"{modifiers.ToString().ToLower()} class {name} {{
{propertiesList.AddIndent(4)}
    public {name}({arguments}) {{
{assignments.AddIndent(8)}
    }}
}}";
        }
    }
}
