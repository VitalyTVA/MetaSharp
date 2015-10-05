
namespace MetaSharp.Sample {
    [MetaCompleteClass]
    public partial class Incomplete {
        public int Foo { get; }
        public int Boo { get; }
    }
    [MetaCompleteViewModel]
    public partial class ViewModel {
        public virtual string BooProperty { get; set; }
        public virtual int IntProperty { get; set; }
    }
}
