using MetaSharp;
using System.Windows;
namespace MetaSharp.Test.Meta.POCO {
    [MetaCompleteViewModel]
    public partial class POCOViewModel {
        internal string NotPublicProperty { get; set; }
        public string NotVirtualProperty { get; set; }
        public virtual string ProtectedGetterProperty { protected internal get; set; }
        string notAutoImplementedProperty;
        public virtual string NotAutoImplementedProperty { get { return notAutoImplementedProperty; } set { notAutoImplementedProperty = value; } }

        public virtual string Property1 { get; set; }
        public virtual string Property2 { get; set; }
        public virtual object Property3 { get; set; }
        public virtual int Property4 { get; set; }
        public virtual Point Property5 { get; set; }
        public virtual int? Property6 { get; set; }
        public virtual string ProtectedSetterProperty { get; protected set; }
        public virtual string ProtectedInternalSetterProperty { get; protected internal set; }
        //public virtual string InternalSetterProperty { get; internal set; }

        internal void SetProtectedSetterProperty(string value) {
            ProtectedSetterProperty = value;
        }
    }
}
