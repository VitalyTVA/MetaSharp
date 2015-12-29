using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp {
    static class MemberVisibilityExtensions {
        public static string ToCSharp(this MemberVisibility value, MemberVisibility defaultValue) {
            if(value == defaultValue) return "";
            switch(value) {
            case MemberVisibility.Internal: return "internal ";
            case MemberVisibility.Private: return "private ";
            case MemberVisibility.Protected: return "protected ";
            case MemberVisibility.ProtectedInternal: return "protected internal ";
            case MemberVisibility.Public: return "public ";
            default: throw new InvalidOperationException();
            }
        }
    }
}
