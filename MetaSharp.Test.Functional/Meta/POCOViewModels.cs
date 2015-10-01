using MetaSharp;
namespace MetaSharp.Test.Meta.POCO {
    using System.Windows;
    [MetaCompleteViewModel]
    public partial class POCOViewModel {
        //internal string NotPublicProperty { get; set; }
        public string NotVirtualProperty { get; set; }
        //public virtual string ProtectedGetterProperty { protected internal get; set; }
        //public virtual string InternalSetterProperty { get; internal set; }
        string notAutoImplementedProperty;
        public virtual string NotAutoImplementedProperty { get { return notAutoImplementedProperty; } set { notAutoImplementedProperty = value; } }

        public virtual string Property1 { get; set; }
        public virtual string Property2 { get; set; }
        public virtual object Property3 { get; set; }
        public virtual int Property4 { get; set; }
        public virtual Point Property5 { get; set; }
        public virtual int? Property6 { get; set; }
        //public virtual string ProtectedSetterProperty { get; protected internal set; }
    }
}
