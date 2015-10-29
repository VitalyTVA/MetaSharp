using System.ComponentModel;

namespace MetaSharp.Test.Meta.POCO {
    using MetaSharp;
    using System.Windows;
    using Xunit;
    using DevExpress.Mvvm.DataAnnotations;
    using System;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using DevExpress.Mvvm;

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

        //protected int _ShowCommand; //TODO how to handle it?
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

    public partial class ProtectedAndPrivateCanExecuteMethods {
        public bool IsMethod1Enabled;
        public void Method1() {
            throw new NotImplementedException();
        }
        protected bool CanMethod1() {
            return IsMethod1Enabled;
        }

        public bool IsMethod2Enabled;
        public void Method2() {
            throw new NotImplementedException();
        }
        protected internal bool CanMethod2() {
            return IsMethod2Enabled;
        }

        public bool IsMethod3Enabled;
        public void Method3() {
            throw new NotImplementedException();
        }
        bool CanMethod3() {
            return IsMethod3Enabled;
        }
    }

    public partial class POCOCommandsCanExecute {
        public int ShowCallCount;
        public void Show() {
            ShowCallCount++;
        }
        public bool CanShowValue;
        public bool CanShow() {
            return CanShowValue;
        }

        public int OpenCallCount;
        public string OpenLastParameter;
        public void Open(string parameter) {
            OpenCallCount++;
            OpenLastParameter = parameter;
        }
        public bool CanOpen(string parameter) {
            return parameter != "x";
        }

        public int CloseCallCount;
        public int CloseLastParameter;
        public void Close(int parameter) {
            CloseCallCount++;
            CloseLastParameter = parameter;
        }
        public bool CanClose(int parameter) {
            return parameter != 9;
        }
    }

    public partial class POCOAsyncCommands {
        public Task Show() {
            return null;
        }
        public bool CanShowValue;
        public bool CanShow() {
            return CanShowValue;
        }
        [AsyncCommand(AllowMultipleExecution = true)]
        public Task Open(string parameter) {
            return null;
        }
        public bool CanOpen(string parameter) {
            return parameter != "x";
        }
    }

    public partial class AsyncCommandAttributeViewModel : CommandAttributeViewModelBase {
        public Task Simple() {
            return Task.Factory.StartNew(() => SimpleMethodCallCount++);
        }
        public Task MethodWith() {
            return Task.Factory.StartNew(() => MethodWithCommandCallCount++);
        }

        [AsyncCommand(false)]
        public Task NoAttribute() { return null; }

        [AsyncCommand(Name = "MyCommand")]
        public Task CustomName() {
            return Task.Factory.StartNew(() => CustomNameCommandCallCount++);

        }

        public Task MethodWithCanExecute() { return null; }
        public bool CanMethodWithCanExecute() { return MethodWithCanExecuteCanExcute; }

        public Task MethodWithParameter(int parameter) {
            return Task.Factory.StartNew(() => {
                MethodWithParameterCallCount++;
                MethodWithParameterLastParameter = parameter;
            });
        }
        public bool CanMethodWithParameter(int parameter) { return parameter != 13; }

        [Command(CanExecuteMethodName = "CanMethodWithCustomCanExecute_", UseCommandManager = false)]
        public Task MethodWithCustomCanExecute() { return null; }
        public bool CanMethodWithCustomCanExecute_() { return MethodWithCustomCanExecuteCanExcute; }
    }

    public partial class CommandAttributeViewModel : CommandAttributeViewModelBase {
        public void Simple() { SimpleMethodCallCount++; }
        public void MethodWith() { MethodWithCommandCallCount++; }

        [Command(false)]
        public void NoAttribute() { }

        [Command(Name = "MyCommand")]
        public void CustomName() { CustomNameCommandCallCount++; }

        public void MethodWithCanExecute() { }
        public bool CanMethodWithCanExecute() { return MethodWithCanExecuteCanExcute; }

        [Command]
        int MethodWithReturnType() { MethodWithReturnTypeCallCount++; return 0; }
        [Command]
        public int MethodWithReturnTypeAndParameter(string param) { Assert.Equal("x", param); MethodWithReturnTypeAndParameterCallCount++; return 0; }

        public void MethodWithParameter(int parameter) { MethodWithParameterCallCount++; MethodWithParameterLastParameter = parameter; }
        public bool CanMethodWithParameter(int parameter) { return parameter != 13; }

        [Command(CanExecuteMethodName = "CanMethodWithCustomCanExecute_"
#if !SILVERLIGHT
, UseCommandManager = false
#endif
)]
        public void MethodWithCustomCanExecute() { }
        public bool CanMethodWithCustomCanExecute_() { return MethodWithCustomCanExecuteCanExcute; }
    }

    public partial class CustomParentViewModelImplementation : ISupportParentViewModel {
        public virtual string Property { get; set; }
        object ISupportParentViewModel.ParentViewModel { get; set; }
        void OnParentViewModelChanged(object oldParentViewModel) {
            throw new InvalidOperationException();
        }
    }

    public partial class POCOViewModel_INPCImplementor : POCOViewModel_INPCImplementorBase, INotifyPropertyChanged {
        public virtual string Property1 { get; set; }
        public override void RaisePropertyChanged(string propertyName) {
            RaisePropertyChangedCore(propertyName);
        }
    }

    public partial class POCOViewModel_BindableBaseDescendant : BindableBase {
        public virtual string Property1 { get; set; }

        string property2;
        public string Property2 {
            get { return property2; }
            set { SetProperty(ref property2, value, () => Property2); }
        }
    }

    public partial class POCOViewModel_CommandsInViewModelBaseDescendant : ViewModelBase {
        [Command]
        public void Save() { SaveCallCount++; }
        public int SaveCallCount;

        public void RaiseCanExecuteChangedPublic(Expression<Action> commandMethodExpression) {
            RaiseCanExecuteChanged(commandMethodExpression);
        }
    }

    public partial class POCOViewModel_AsyncCommandsInViewModelBaseDescendant : ViewModelBase {
        public Task Save() {
            return Task.Factory.StartNew(() => SaveCallCount++);
        }
        public int SaveCallCount;
        public void RaiseCanExecuteChangedPublic(Expression<Action> commandMethodExpression) {
            RaiseCanExecuteChanged(commandMethodExpression);
        }
    }

    public partial class CustomSupportServicesImplementation : ISupportServices {
        IServiceContainer _ServiceContainer;
        IServiceContainer ISupportServices.ServiceContainer { get { return _ServiceContainer ?? (_ServiceContainer = new ServiceContainer(this)); } }
    }
}
