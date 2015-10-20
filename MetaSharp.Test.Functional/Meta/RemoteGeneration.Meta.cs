using MetaSharp;

namespace MetaSharp.Test.Meta {
    [MetaLocation(Location = MetaLocationKind.Designer)]
    static class RemoteGeneration {
        public static string Create(MetaContext context) {
            return context.WrapMembers(RemoteClassGenerator.Generate());
        }
    }
}