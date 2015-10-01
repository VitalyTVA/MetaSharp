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
        //TODO auto generate default private ctor if none
        //TODO error if existing ctor not private
        public static string Generate(SemanticModel model, INamedTypeSymbol type) {
            var properties = type.Properties()
                .Where(p => p.IsVirtual && p.DeclaredAccessibility == Accessibility.Public)
                .Select(p => {
                    return
$@"public override {p.TypeDisplayString(model)} {p.Name} {{
    get {{ return base.{p.Name}; }}
    set {{
        if(base.{p.Name} == value)
            return;
        base.{p.Name} = value;
        RaisePropertyChanged(""{p.Name}"");
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