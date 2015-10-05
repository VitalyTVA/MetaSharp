using MetaSharp.Native;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp {
    static class ViewModelCompleter {
        //TODO auto calc dependent properties
        //TODO auto generate default private ctor if none, use explicit factory methods directly
        //TODO error if base class ctor is used
        //TODO error if existing ctor not private
        //TODO implement INPC in class, not in inherited class, so you can call RaisePropertyChanged without extension methods
        //TODO INotifyPropertyChanging support
        public static string Generate(SemanticModel model, INamedTypeSymbol type) {
            var methods = type.Methods()
                .ToImmutableDictionary(x => x.Name, x => x);
            var properties = type.Properties()
                .Where(p => p.IsVirtual
                    && p.DeclaredAccessibility == Accessibility.Public
                    && p.GetMethod.DeclaredAccessibility == Accessibility.Public
                    && p.IsAutoImplemented()
                )
                .Select(p => {
                    var setterModifier = p.SetMethod.DeclaredAccessibility.ToAccessibilityModifier(p.DeclaredAccessibility);
                    var onChangedMethod = methods
                        .GetValueOrDefault($"On{p.Name}Changed");
                    var needOldValue = onChangedMethod.Return(x => x.Parameters.Length == 1, () => false);
                    var oldValueStorage = needOldValue ? $"var oldValue = base.{p.Name};".AddTabs(2) : null;
                    var oldValueName = needOldValue ? "oldValue".AddTabs(2) : null;
                    var onChangedMethodCall = onChangedMethod.With(x => $"{x.Name}({oldValueName});");
                    return
$@"public override {p.TypeDisplayString(model)} {p.Name} {{
    get {{ return base.{p.Name}; }}
    {setterModifier}set {{
        if(base.{p.Name} == value)
            return;
{oldValueStorage}
        base.{p.Name} = value;
        RaisePropertyChanged(""{p.Name}"");
{onChangedMethodCall}
    }}
}}";
                })
                .ConcatStringsWithNewLines();
            return
//TODO what if System.ComponentModel is already in context?
$@"using System.ComponentModel;
partial class {type.Name} {{
    public static {type.Name} Create() {{
        return new {type.Name}Implementation();
    }}
    class {type.Name}Implementation : {type.Name}, INotifyPropertyChanged {{
{properties.AddTabs(2)}
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged(string property) {{
            var handler = PropertyChanged;
            if(handler != null)
                handler(this, new PropertyChangedEventArgs(property));
        }}
    }}
}}";
        }
    }
}