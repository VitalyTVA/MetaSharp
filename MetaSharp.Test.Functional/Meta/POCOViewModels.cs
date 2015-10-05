using MetaSharp;
using System.Windows;
using Xunit;

namespace MetaSharp.Test.Meta.POCO {
    [MetaCompleteViewModel]
    public partial class POCOViewModel {
        internal string NotPublicProperty { get; set; }
        public string NotVirtualProperty { get; set; }
        public string NotVirtualPropertyWithPrivateSetter { get; set; }
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
        public virtual string InternalSetterProperty { get; internal set; }

        internal void SetProtectedSetterProperty(string value) {
            ProtectedSetterProperty = value;
        }
    }
    public partial class POCOViewModel_PropertyChangedBase {
        protected virtual void OnProtectedChangedMethodWithParamChanged(string oldValue) { }
        public virtual bool SealedProperty { get; set; }
    }
    [MetaCompleteViewModel]
    public partial class POCOViewModel_PropertyChanged : POCOViewModel_PropertyChangedBase {
        public string ProtectedChangedMethodWithParamOldValue;
        public bool OnProtectedChangedMethodWithParamChangedCalled;
        public virtual string ProtectedChangedMethodWithParam { get; set; }
        protected override void OnProtectedChangedMethodWithParamChanged(string oldValue) {
            Assert.NotEqual(ProtectedChangedMethodWithParam, ProtectedChangedMethodWithParamOldValue);
            OnProtectedChangedMethodWithParamChangedCalled = true;
            ProtectedChangedMethodWithParamOldValue = oldValue;
        }
        public sealed override bool SealedProperty { get; set; }

        public int PublicChangedMethodWithoutParamOldValue;
        public virtual int PublicChangedMethodWithoutParam { get; set; }
        public void OnPublicChangedMethodWithoutParamChanged() {
            PublicChangedMethodWithoutParamOldValue++;
        }

        public int ProtectedInternalChangedMethodWithoutParamOldValue;
        public virtual int ProtectedInternalChangedMethodWithoutParam { get; set; }
        protected internal void OnProtectedInternalChangedMethodWithoutParamChanged() {
            ProtectedInternalChangedMethodWithoutParamOldValue++;
        }
    }
}
