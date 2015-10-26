using System.ComponentModel;

namespace MetaSharp.Test.Meta.POCO {
    using MetaSharp;
    using System.Windows;
    using Xunit;
    using DevExpress.Mvvm.DataAnnotations;
    using System;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

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

        internal void RaisePropertyChangedInternal<T>(Expression<Func<POCOViewModel, T>> property) {
            RaisePropertyChanged(property);
        }
        internal void RaisePropertiesChanged() {
            RaisePropertyChanged(string.Empty);
        }
        internal object OldParentViewModel;
        partial void OnParentViewModelChanged(object oldParentViewModel) {
            OldParentViewModel = oldParentViewModel;
        }
    }

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

        public int InternalChangedMethodWithoutParamOldValue;
        public virtual int InternalChangedMethodWithoutParam { get; set; }
        internal void OnInternalChangedMethodWithoutParamChanged() {
            InternalChangedMethodWithoutParamOldValue++;
        }

        public int PrivateChangedMethodWithoutParamOldValue;
        public virtual int PrivateChangedMethodWithoutParam { get; set; }
        void OnPrivateChangedMethodWithoutParamChanged() {
            PrivateChangedMethodWithoutParamOldValue++;
        }
    }

    public partial class POCOViewModel_SubscribeInCtor {
        public POCOViewModel_SubscribeInCtor() {
            ((INotifyPropertyChanged)this).PropertyChanged += POCOViewModel_SubscribeInCtor_PropertyChanged;
            Property = "x";
        }
        public int propertyChangedCallCount;
        void POCOViewModel_SubscribeInCtor_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            propertyChangedCallCount++;
        }
        public virtual string Property { get; set; }
    }

    public partial class POCOViewModel_PropertyChanging {
        public virtual string Property1 { get; set; }
        public string Property1NewValue;
        protected void OnProperty1Changing(string newValue) {
            Assert.NotEqual(newValue, Property1);
            Property1NewValue = newValue;
        }

        public virtual string Property2 { get; set; }
        public int Property2ChangingCallCount;
        void OnProperty2Changing() {
            Assert.Equal(null, Property2);
            Property2ChangingCallCount++;
        }

        string property3;
        [BindableProperty]
        public virtual string Property3 { get { return property3; } set { property3 = value; } }
        protected void OnProperty3Changing() {
            throw new NotImplementedException();
        }
        void OnProperty3Changed() {
            throw new NotImplementedException();
        }
    }
    public partial class POCOViewModel_WithMetadata {
        [BindableProperty(false)]
        public virtual string NotBindableProperty { get; set; }

        string notAutoImplementedProperty;
        [BindableProperty]
        public virtual string NotAutoImplementedProperty { get { return notAutoImplementedProperty; } set { notAutoImplementedProperty = value; } }

        public string CustomProperytChangedOldValue;
        string customProperytChanged;
        [BindableProperty(OnPropertyChangedMethodName = "OnCustomProperytChanged")]
        public virtual string CustomProperytChanged { get { return customProperytChanged; } set { customProperytChanged = value; } }
        protected void OnCustomProperytChanged(string oldValue) {
            CustomProperytChangedOldValue = oldValue;
        }

        [BindableProperty(OnPropertyChangingMethodName = "MyPropertyChanging")]
        public virtual string PropertyChanging { get; set; }
        public string PropertyChangingNewValue;
        protected void MyPropertyChanging(string newValue) {
            Assert.NotEqual(newValue, PropertyChanging);
            PropertyChangingNewValue = newValue;
        }
    }

    public partial class POCOCommandsViewModel {
        public virtual string Property1 { get; set; }

        protected int _ShowCommand;
        public int ShowCallCount;
        public void Show() {
            ShowCallCount++;
        }
        public int ShowAsyncCallCount;

        public Task ShowAsync() {
            return Task.Factory.StartNew(() =>
                ShowAsyncCallCount++
            );
        }

        public int SaveCallCount;
        public void Save() {
            SaveCallCount++;
        }
        public int CloseCallCount;
        public void Close(string param) {
            CloseCallCount++;
        }

        public static void StaticMethod() { }
        internal void InternalMethod() { }
        protected Task ProtectedAsyncMethod() { return null; }
        protected void ProtectedMethod() { }
        public void OutParameter(out int x) { x = 0; }
        public void RefParameter(ref int x) { x = 0; }
        public int MethodWithReturnValue() { return 0; }
    }
}
