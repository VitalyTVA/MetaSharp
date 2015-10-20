using MetaSharp;

namespace MetaSharp.Test.Meta {
    static class RemoteGeneration {
        public static string Create(MetaContext context) {
            return context.WrapMembers(RemoteClassGenerator.Generate());
        }
    }
}