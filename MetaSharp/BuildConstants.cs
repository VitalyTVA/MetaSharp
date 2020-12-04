namespace MetaSharp {
    public enum GeneratorMode {
        MsBuild, ConsoleApp
    }

    public class BuildConstants {
        public readonly string IntermediateOutputPath, TargetPath;
        public readonly GeneratorMode GeneratorMode;
        public BuildConstants(string intermediateOutputPath, string targetPath, GeneratorMode generatorMode) {
            IntermediateOutputPath = intermediateOutputPath;
            TargetPath = targetPath;
            GeneratorMode = generatorMode;
        }
    }
}
