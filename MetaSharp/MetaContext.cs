using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MetaSharp.Native;

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

    //TODO make immutable
    public class ClassGenerator {
        public static ClassGenerator Class<T>() {
            throw new NotImplementedException();
        }
        public static ClassGenerator Class_(string name)
            => new ClassGenerator(name);
        readonly string name;
        readonly List<string> properties;

        public ClassGenerator(string name) {
            this.name = name;
            this.properties = new List<string>();
        }

        public ClassGenerator Property<T>() {
            //TODO implement
            throw new NotImplementedException();
        }
        public ClassGenerator Property_(string propertyType) {
            properties.Add(propertyType);
            return this;
        }

        public string Generate() {
            var propertiesList = properties
                .Select((x, i) => $"public {x} Property{i} {{ get; set; }}")
                .ConcatStringsWithNewLines();
            return
$@"public class {name} {{
{propertiesList.AddIndent(4)}
}}".AddIndent(4);
        }
    }
}
