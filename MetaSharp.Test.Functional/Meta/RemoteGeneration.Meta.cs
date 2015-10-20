using MetaSharp;

namespace MetaSharp.Test.Meta {
    [MetaLocation(Location = MetaLocation.Project)]
    static class RemoteGeneration {
        public static string Create(MetaContext context) {
            return context.WrapMembers(RemoteClassGenerator.Generate());
        }
    }
}