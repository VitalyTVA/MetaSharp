﻿using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using MetaSharp.Native;
using MetaSharp.Test.Meta.POCO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Xunit;
using System.Windows.Data;
using System.Windows.Threading;
using System.Diagnostics;

namespace MetaSharp.Test.Functional {
    public class POCOViewModelsTests {
        #region property changed
        [Fact]
        public void OverridingPropertyTest() {
            Assert.True(typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(POCOViewModel)));
            Assert.True(typeof(ISupportParentViewModel).IsAssignableFrom(typeof(POCOViewModel)));
            Assert.False(typeof(IPOCOViewModel).IsAssignableFrom(typeof(POCOViewModel)));
            Assert.False(typeof(IDataErrorInfo).IsAssignableFrom(typeof(POCOViewModel)));
            var viewModel = POCOViewModel.Create();
            Assert.Equal(viewModel.GetType(), viewModel.GetType().GetProperty("Property1").DeclaringType);

            CheckBindableProperty(viewModel, x => x.Property1, (vm, x) => vm.Property1 = x, "x", "y");
            CheckBindableProperty(viewModel, x => x.Property2, (vm, x) => vm.Property2 = x, "m", "n");
            CheckBindableProperty(viewModel, x => x.Property3, (vm, x) => vm.Property3 = x, "a", "b");
            CheckBindableProperty(viewModel, x => x.Property4, (vm, x) => vm.Property4 = x, 1, 2);
            CheckBindableProperty(viewModel, x => x.Property5, (vm, x) => vm.Property5 = x, new Point(1, 1), new Point(2, 2));
            CheckBindableProperty(viewModel, x => x.Property6, (vm, x) => vm.Property6 = x, 5, null);
            CheckBindableProperty(viewModel, x => x.ProtectedSetterProperty, (vm, x) => vm.SetProtectedSetterProperty(x), "x", "y");
            Assert.Null(viewModel.GetType().GetProperty("ProtectedSetterProperty").GetSetMethod());
            CheckBindableProperty(viewModel, x => x.ProtectedInternalSetterProperty, (vm, x) => vm.ProtectedInternalSetterProperty = x, "x", "y");
            Assert.Null(viewModel.GetType().GetProperty("ProtectedInternalSetterProperty").GetSetMethod());
            CheckBindableProperty(viewModel, x => x.InternalSetterProperty, (vm, x) => vm.InternalSetterProperty = x, "x", "y");

            CheckNotBindableProperty(viewModel, x => x.NotVirtualProperty, (vm, x) => vm.NotVirtualProperty = x, "x", "y");
            CheckNotBindableProperty(viewModel, x => x.NotVirtualPropertyWithPrivateSetter, (vm, x) => vm.NotVirtualPropertyWithPrivateSetter = x, "x", "y");
            CheckNotBindableProperty(viewModel, x => x.NotPublicProperty, (vm, x) => vm.NotPublicProperty = x, "x", "y");
            CheckNotBindableProperty(viewModel, x => x.ProtectedGetterProperty, (vm, x) => vm.ProtectedGetterProperty = x, "x", "y");
            CheckNotBindableProperty(viewModel, x => x.NotAutoImplementedProperty, (vm, x) => vm.NotAutoImplementedProperty = x, "x", "y");
        }

        [Fact]
        public void PropertyChangedTest() {
            POCOViewModel_PropertyChanged viewModel = POCOViewModel_PropertyChanged.Create();
            ((INotifyPropertyChanged)viewModel).PropertyChanged += (o, e) => Assert.False(viewModel.OnProtectedChangedMethodWithParamChangedCalled);
            CheckBindableProperty(viewModel, x => x.ProtectedChangedMethodWithParam, (vm, x) => vm.ProtectedChangedMethodWithParam = x, "x", "y", (x, val) => {
                Assert.True(x.OnProtectedChangedMethodWithParamChangedCalled);
                x.OnProtectedChangedMethodWithParamChangedCalled = false;
                Assert.Equal(val, x.ProtectedChangedMethodWithParamOldValue);
            });

            CheckBindableProperty(viewModel, x => x.PublicChangedMethodWithoutParam, (vm, x) => vm.PublicChangedMethodWithoutParam = x, 1, 2, (x, val) => Assert.Equal(val + 1, x.PublicChangedMethodWithoutParamOldValue));
            CheckBindableProperty(viewModel, x => x.ProtectedInternalChangedMethodWithoutParam, (vm, x) => vm.ProtectedInternalChangedMethodWithoutParam = x, 1, 2, (x, val) => Assert.Equal(val + 1, x.ProtectedInternalChangedMethodWithoutParamOldValue));
            CheckBindableProperty(viewModel, x => x.InternalChangedMethodWithoutParam, (vm, x) => vm.InternalChangedMethodWithoutParam = x, 1, 2, (x, val) => Assert.Equal(val + 1, x.InternalChangedMethodWithoutParamOldValue));
            CheckBindableProperty(viewModel, x => x.PrivateChangedMethodWithoutParam, (vm, x) => vm.PrivateChangedMethodWithoutParam = x, 1, 2, (x, val) => Assert.Equal(val + 1, x.PrivateChangedMethodWithoutParamOldValue));
        }
        [Fact]
        public void PrivateRaisePropertyChangedMethodTest() {
            var vm = PrivateRaisePropertyChangedMethod.Create();
            CheckBindableProperty(vm, x => x.StringProp, (x, val) => x.StringProp = val, "x", "y");
        }
        #endregion

        #region subscribe in constructor
        [Fact]
        public void POCOViewModel_SubscribeInCtorTest() {
            var viewModel = POCOViewModel_SubscribeInCtor.Create();
            Assert.Equal(1, viewModel.propertyChangedCallCount);
        }
        #endregion

        #region property changing
        [Fact]
        public void PropertyChangingTest() {
            var viewModel = POCOViewModel_PropertyChanging.Create();
            viewModel.Property1 = null;
            viewModel.Property1 = "x";
            Assert.Equal("x", viewModel.Property1NewValue);

            viewModel.Property2 = null;
            viewModel.Property2 = "x";
            Assert.Equal(1, viewModel.Property2ChangingCallCount);

            viewModel.Property3 = "x";
        }
        #endregion

        #region metadata
        [Fact]
        public void OverridingPropertyTest_Metadata() {
            var viewModel = POCOViewModel_WithMetadata.Create();
            CheckNotBindableProperty(viewModel, x => x.NotBindableProperty, (vm, x) => vm.NotBindableProperty = x, "x", "y");
            CheckBindableProperty(viewModel, x => x.NotAutoImplementedProperty, (vm, x) => vm.NotAutoImplementedProperty = x, "x", "y");
            CheckBindableProperty(viewModel, x => x.CustomPropertyChanged, (vm, x) => vm.CustomPropertyChanged = x, "x", "y", (x, val) => Assert.Equal(val, x.CustomProperytChangedOldValue));

            viewModel.PropertyChanging = null;
            viewModel.PropertyChanging = "x";
            Assert.Equal("x", viewModel.PropertyChangingNewValue);
        }
        #endregion

        #region interfaces implementation
        [Fact]
        public void RaisePropertyChangedImplementation() {
            var viewModel = POCOViewModel.Create();
            string propertyName = null;
            ((INotifyPropertyChanged)viewModel).PropertyChanged += (o, e) => propertyName = e.PropertyName;
            viewModel.RaisePropertyChangedInternal(x => x.Property1);
            Assert.Equal("Property1", propertyName);

            viewModel.RaisePropertyChangedInternal(x => x.Property5);
            Assert.Equal("Property5", propertyName);

            viewModel.RaisePropertiesChanged();
            Assert.Equal(string.Empty, propertyName);

            viewModel.RaisePropertyChanged(x => x.Property1);
            Assert.Equal("Property1", propertyName);
        }
        [Fact]
        public void GetSetParentViewModel() {
            var viewModel = POCOViewModel.Create();
            Assert.Null(viewModel.GetParentViewModel<Type>());
            var type = GetType();
            Assert.Same(viewModel, viewModel.SetParentViewModel(type));
            Assert.Equal(type, viewModel.GetParentViewModel<Type>());
            Assert.Null(viewModel.OldParentViewModel);
            type = typeof(object);
            Assert.Same(viewModel, viewModel.SetParentViewModel(type));
            Assert.Equal(type, viewModel.GetParentViewModel<Type>());
            Assert.Same(GetType(), viewModel.OldParentViewModel);
        }
        [Fact]
        public void CustomParentViewModelImplementationTest() {
            var model = CustomParentViewModelImplementation.Create();
            CheckBindableProperty(model, x => x.Property, (vm, x) => vm.Property = x, "x", "y");
            ((ISupportParentViewModel)model).ParentViewModel = "x";
            Assert.Equal("x", ((ISupportParentViewModel)model).ParentViewModel);
        }

        [Fact]
        public void InheritBindableBaseTest() {
            var viewModel = POCOViewModel_BindableBaseDescendant.Create();
            var interfaces = viewModel.GetType().GetInterfaces();
            CheckBindableProperty(viewModel, x => x.Property1, (vm, x) => vm.Property1 = x, "x", "y");
            CheckBindableProperty(viewModel, x => x.Property2, (vm, x) => vm.Property2 = x, "x", "y");
            ((ISupportParentViewModel)viewModel).ParentViewModel = "x";
            Assert.Equal("x", ((ISupportParentViewModel)viewModel).ParentViewModel);
        }

        [Fact]
        public void INPCImplementorTest() {
            var viewModel = POCOViewModel_INPCImplementor.Create();
            var interfaces = viewModel.GetType().GetInterfaces();
            CheckBindableProperty(viewModel, x => x.Property1, (vm, x) => vm.Property1 = x, "x", "y");
        }
        #endregion

        #region commands
        [Fact]
        public void CommandsGeneration() {
            POCOCommandsViewModel viewModel = POCOCommandsViewModel.Create();
            CheckCommand(viewModel, x => x.Show(), x => Assert.Equal(1, x.ShowCallCount));
            CheckCommand(viewModel, x => x.ShowAsync(), x => Assert.Equal(1, x.ShowAsyncCallCount), true);
            CheckCommand(viewModel, x => x.Save(), x => Assert.Equal(1, x.SaveCallCount));
            CheckCommand(viewModel, x => x.Close(null), x => Assert.Equal(1, x.CloseCallCount));
            CheckNoCommand(viewModel, "InternalMethod");
            CheckNoCommand(viewModel, "ToString");
            CheckNoCommand(viewModel, "GetHashCode");
            CheckNoCommand(viewModel, "Equals");
            CheckNoCommand(viewModel, "ProtectedAsyncMethod");
            CheckNoCommand(viewModel, "ProtectedMethod");
            CheckNoCommand(viewModel, "get_Property1");
            CheckNoCommand(viewModel, "set_Property1");
            CheckNoCommand(viewModel, "StaticMethod");
            CheckNoCommand(viewModel, "OutParameter");
            CheckNoCommand(viewModel, "RefParameter");
            CheckNoCommand(viewModel, "MethodWithReturnValue");

            Assert.Equal(typeof(ICommand), viewModel.GetType().GetProperty("ShowCommand").PropertyType);
            Assert.Equal(typeof(DelegateCommand<string>), viewModel.GetType().GetProperty("CloseCommand").PropertyType);
            Assert.Equal(typeof(AsyncCommand), viewModel.GetType().GetProperty("ShowAsyncCommand").PropertyType);
        }

        [Fact]
        public void ProtectedCanExecuteMethod() {
            var viewModel = ProtectedAndPrivateCanExecuteMethods.Create();
            Assert.False(viewModel.Method1Command.CanExecute(null));
            viewModel.IsMethod1Enabled = true;
            Assert.True(viewModel.Method1Command.CanExecute(null));

            Assert.False(viewModel.Method2Command.CanExecute(null));
            viewModel.IsMethod2Enabled = true;
            Assert.True(viewModel.Method2Command.CanExecute(null));

            Assert.False(viewModel.Method3Command.CanExecute(null));
            viewModel.IsMethod3Enabled = true;
            Assert.True(viewModel.Method3Command.CanExecute(null));
        }

        [Fact]
        public void CommandsCanExecute() {
            var viewModel = POCOCommandsCanExecute.Create();
            var command = viewModel.ShowCommand;
            Assert.False(command.CanExecute(null));
            viewModel.CanShowValue = true;
            Assert.True(command.CanExecute(null));

            command = viewModel.OpenCommand;
            Assert.True(command.CanExecute("y"));
            Assert.False(command.CanExecute("x"));
            Assert.Equal(0, viewModel.OpenCallCount);
            command.Execute("z");
            Assert.Equal("z", viewModel.OpenLastParameter);
            Assert.Equal(1, viewModel.OpenCallCount);

            command = viewModel.CloseCommand;
            Assert.False(command.CanExecute(9));
            Assert.True(command.CanExecute(13));
            Assert.False(command.CanExecute("9"));
            Assert.True(command.CanExecute("13"));
            Assert.Equal(0, viewModel.CloseCallCount);
            command.Execute("117");
            Assert.Equal(117, viewModel.CloseLastParameter);
            Assert.Equal(1, viewModel.CloseCallCount);
        }

        [Fact]
        public void AsyncCommandsCanExecute() {
            POCOAsyncCommands viewModel = POCOAsyncCommands.Create();
            IAsyncCommand asyncCommand = viewModel.ShowCommand;
            Assert.False(asyncCommand.CanExecute(null));
            viewModel.CanShowValue = true;
            Assert.True(asyncCommand.CanExecute(null));

            asyncCommand = viewModel.OpenCommand;
            Assert.True(asyncCommand.CanExecute("y"));
            Assert.False(asyncCommand.CanExecute("x"));
        }

        [Fact]
        public void AsyncCommandAllowMultipleExecutionAttributeTest() {
            POCOAsyncCommands viewModel = POCOAsyncCommands.Create();
            AsyncCommand asyncCommand1 = viewModel.ShowCommand;
            Assert.False(asyncCommand1.AllowMultipleExecution);
            AsyncCommand<string> asyncCommand2 = viewModel.OpenCommand;
            Assert.True(asyncCommand2.AllowMultipleExecution);
        }

        [Fact]
        public void AsyncCommandAttribute_ViewModelTest() {
            var viewModel = AsyncCommandAttributeViewModel.Create();
            viewModel.SimpleCommand.Execute(null);
            Assert.Equal(0, viewModel.SimpleMethodCallCount);
            WaitFor(() => 1 == viewModel.SimpleMethodCallCount);

            CheckNoCommand(viewModel, "NoAttribute");

            viewModel.MethodWithCommand.Execute(null);
            Assert.Equal(0, viewModel.MethodWithCommandCallCount);
            WaitFor(() => 1 == viewModel.MethodWithCommandCallCount);

            viewModel.MyCommand.Execute(null);
            WaitFor(() => 1 == viewModel.CustomNameCommandCallCount);

            CheckNoCommand(viewModel, "BaseClass");

            Assert.False(viewModel.MethodWithCanExecuteCommand.CanExecute(null));
            viewModel.MethodWithCanExecuteCanExcute = true;
            Assert.True(viewModel.MethodWithCanExecuteCommand.CanExecute(null));
            //if(!IsAsyncCommand) {
            //    viewModel.MethodWithReturnTypeCommand.Execute(null);
            //    button.SetBinding(Button.CommandProperty, new Binding("MethodWithReturnTypeCommand"));
            ////                    button.Command.Execute(null);
            ////                    Assert.AreEqual(1, viewModel.MethodWithReturnTypeCallCount);

            ////                    button.SetBinding(Button.CommandProperty, new Binding("MethodWithReturnTypeAndParameterCommand"));
            ////                    button.Command.Execute("x");
            ////                    Assert.AreEqual(1, viewModel.MethodWithReturnTypeAndParameterCallCount);
            //}

            viewModel.MethodWithParameterCommand.Execute(9);
            Assert.True(viewModel.MethodWithParameterCommand.IsExecuting);
            WaitFor(() => 1 == viewModel.MethodWithParameterCallCount);
            WaitFor(() => !viewModel.MethodWithParameterCommand.IsExecuting);

            Assert.Equal(9, viewModel.MethodWithParameterLastParameter);
            Assert.True(viewModel.MethodWithParameterCommand.CanExecute(9));
            Assert.False(viewModel.MethodWithParameterCommand.CanExecute(13));
            viewModel.MethodWithParameterCommand.Execute(10);
            WaitFor(() => 2 == viewModel.MethodWithParameterCallCount);
            Assert.Equal(10, viewModel.MethodWithParameterLastParameter);
            var methodWithParameterCommandCanExecuteChangedCount = 0;
            CommandManager.InvalidateRequerySuggested();
            DispatcherHelper.DoEvents();
            viewModel.MethodWithParameterCommand.CanExecuteChanged += (o, e) => {
                methodWithParameterCommandCanExecuteChangedCount++;
            };
            CommandManager.InvalidateRequerySuggested();
            DispatcherHelper.DoEvents();
            Assert.Equal(1, methodWithParameterCommandCanExecuteChangedCount);

            Assert.False(viewModel.MethodWithCustomCanExecuteCommand.CanExecute(null));
            viewModel.MethodWithCustomCanExecuteCanExcute = true;
            Assert.True(viewModel.MethodWithCustomCanExecuteCommand.CanExecute(null));
            viewModel.MethodWithCustomCanExecuteCommand.CanExecuteChanged += (o, e) => {
                throw new NotImplementedException();
            };
            CommandManager.InvalidateRequerySuggested();
            DispatcherHelper.DoEvents();
        }

        [Fact]
        public void CommandAttribute_ViewModelTest() {
            var viewModel = CommandAttributeViewModel.Create();
            viewModel.SimpleCommand.Execute(null);
            Assert.Equal(1, viewModel.SimpleMethodCallCount);

            CheckNoCommand(viewModel, "NoAttribute");

            viewModel.MethodWithCommand.Execute(null);
            Assert.Equal(1, viewModel.MethodWithCommandCallCount);

            viewModel.MyCommand.Execute(null);
            Assert.Equal(1, viewModel.CustomNameCommandCallCount);

            CheckNoCommand(viewModel, "BaseClass");

            Assert.False(viewModel.MethodWithCanExecuteCommand.CanExecute(null));
            viewModel.MethodWithCanExecuteCanExcute = true;
            Assert.True(viewModel.MethodWithCanExecuteCommand.CanExecute(null));

            viewModel.MethodWithReturnTypeCommand.Execute(null);
            Assert.Equal(1, viewModel.MethodWithReturnTypeCallCount);
            viewModel.MethodWithReturnTypeAndParameterCommand.Execute("x");
            Assert.Equal(1, viewModel.MethodWithReturnTypeAndParameterCallCount);

            viewModel.MethodWithParameterCommand.Execute(9);
            Assert.Equal(1, viewModel.MethodWithParameterCallCount);

            Assert.Equal(9, viewModel.MethodWithParameterLastParameter);
            Assert.True(viewModel.MethodWithParameterCommand.CanExecute(9));
            Assert.False(viewModel.MethodWithParameterCommand.CanExecute(13));
            viewModel.MethodWithParameterCommand.Execute(10);
            Assert.Equal(2, viewModel.MethodWithParameterCallCount);
            Assert.Equal(10, viewModel.MethodWithParameterLastParameter);
            var methodWithParameterCommandCanExecuteChangedCount = 0;
            CommandManager.InvalidateRequerySuggested();
            DispatcherHelper.DoEvents();
            viewModel.MethodWithParameterCommand.CanExecuteChanged += (o, e) => {
                methodWithParameterCommandCanExecuteChangedCount++;
            };
            CommandManager.InvalidateRequerySuggested();
            DispatcherHelper.DoEvents();
            Assert.Equal(1, methodWithParameterCommandCanExecuteChangedCount);

            Assert.False(viewModel.MethodWithCustomCanExecuteCommand.CanExecute(null));
            viewModel.MethodWithCustomCanExecuteCanExcute = true;
            Assert.True(viewModel.MethodWithCustomCanExecuteCommand.CanExecute(null));
            viewModel.MethodWithCustomCanExecuteCommand.CanExecuteChanged += (o, e) => {
                throw new NotImplementedException();
            };
            CommandManager.InvalidateRequerySuggested();
            DispatcherHelper.DoEvents();

            viewModel.PrivateMethodCommand.Execute(null);
            Assert.Equal(1, viewModel.PrivateMethodCallCount);
        }

        [Fact]
        public void CommandsInViewModelBaseDescendant() {
            var viewModel = POCOViewModel_CommandsInViewModelBaseDescendant.Create();
            var command = CheckCommand(viewModel, x => x.Save(), x => Assert.Equal(1, x.SaveCallCount));
            Assert.NotNull(viewModel.SaveCommand);
            int canExecuteChangedCount = 0;
            command.CanExecuteChanged += (x, e) => canExecuteChangedCount++;
            viewModel.RaiseCanExecuteChangedPublic(() => viewModel.Save());
            DispatcherHelper.DoEvents();
            Assert.Equal(1, canExecuteChangedCount);

            viewModel.RaiseCanExecuteChanged(x => x.Save());
            DispatcherHelper.DoEvents();
            Assert.Equal(2, canExecuteChangedCount);
        }

        [Fact]
        public void AsyncCommandsInViewModelBaseDescendant() {
            var viewModel = ViewModelSource.Create<POCOViewModel_AsyncCommandsInViewModelBaseDescendant>();
            var command = CheckCommand(viewModel, x => x.Save(), x => Assert.Equal(1, x.SaveCallCount), true);
            Assert.NotNull(viewModel.SaveCommand);
            int canExecuteChangedCount = 0;
            command.CanExecuteChanged += (x, e) => canExecuteChangedCount++;
            viewModel.RaiseCanExecuteChangedPublic(() => viewModel.Save());
            DispatcherHelper.DoEvents();
            Assert.Equal(1, canExecuteChangedCount);
        }
        #endregion

        #region services
        [Fact]
        public void CustomSupportServicesImplementationTest() {
            CheckServicesImplementation(CustomSupportServicesImplementation.Create());
        }
        [Fact]
        public void SupportServicesInViewModelBaseDescendant() {
            CheckServicesImplementation(POCOViewModel_CommandsInViewModelBaseDescendant.Create());
        }
        [Fact]
        public void SupportServicesPOCOModel() {
            CheckServicesImplementation(POCOViewModel.Create());
        }
        interface IService1 { }
        interface IService2 { }
        class Service1 : IService1 { }
        class Service2 : IService2 { }
        void CheckServicesImplementation(ISupportServices services) {
            var parent = CustomSupportServicesImplementation.Create();
            ((ISupportParentViewModel)services).ParentViewModel = parent;

            var service1 = new Service1();
            services.ServiceContainer.RegisterService(service1);
            var service2 = new Service2();
            ((ISupportServices)parent).ServiceContainer.RegisterService(service2);

            Assert.Same(service1, services.GetService<IService1>());
            Assert.Same(service1, services.ServiceContainer.GetService<IService1>());
            Assert.Same(service2, services.GetService<IService2>());
            Assert.Same(service2, services.ServiceContainer.GetService<IService2>());
        }
        #endregion

        #region ctors
        [Fact]
        public void CustomConstructorTest() {
            var vm = CustomConstructor.Create("a");
            Assert.Equal("a", vm.A);
            CheckBindableProperty(vm, x => x.A, (x, val) => x.A = val, "x", "y");
        }

        [Fact]
        public void PrivateClassTest() {
            var vm = PrivatePOCOViewModel.Create();
            CheckBindableProperty(vm, x => x.StringProp, (x, val) => x.StringProp = val, "x", "y");
        }
        #endregion

        #region IDataErrorInfo
        [Fact]
        public void ImplementIDataErrorInfo() {
            var vm = AttributedDataErrorInfoClass.Create();
            var asInfo = vm as IDataErrorInfo;

            var hwvm = new HandwrittenDataErrorInfoClass();
            Assert.True(vm is IDataErrorInfo);
            Assert.Equal(hwvm[""], asInfo[""]);
            Assert.Equal(hwvm["StringProp"], asInfo["StringProp"]);
            Assert.Equal(hwvm.Error, asInfo.Error);
            vm.StringProp = "";
            hwvm.StringProp = "";
            Assert.Equal(hwvm["StringProp"], asInfo["StringProp"]);
            Assert.Equal(hwvm.Error, asInfo.Error);
            vm.StringProp = "123";
            hwvm.StringProp = "123";
            Assert.Equal(hwvm["StringProp"], asInfo["StringProp"]);
            Assert.Equal(hwvm.Error, asInfo.Error);
        }
        [Fact]
        public void CustomIDataErrorInfoImplementation() {
            IDataErrorInfo vm = CustomExplicitDataErrorInfoPropertyImplementation.Create();
            var hwvm = new HandwrittenDataErrorInfoClass();
            Assert.Equal(hwvm["StringProp"], vm["StringProp"]);
            Assert.Equal("custom error", vm.Error);
        }
        [Fact]
        public void CustomIDataErrorIndexerImplementation() {
            IDataErrorInfo vm = CustomExplicitDataErrorInfoIndexerImplementation.Create();
            var hwvm = new HandwrittenDataErrorInfoClass();
            Assert.Equal("StringProp error", vm["StringProp"]);
            Assert.Equal(string.Empty, vm.Error);
        }
        [Fact]
        public void CustomIDataErrorInfoImplementation_Implicit() {
            IDataErrorInfo vm = CustomImplicitDataErrorInfoPropertyImplementation.Create();
            var hwvm = new HandwrittenDataErrorInfoClass();
            Assert.Equal(hwvm["StringProp"], vm["StringProp"]);
            Assert.Equal("custom error", vm.Error);
        }
        [Fact]
        public void CustomIDataErrorIndexerImplementation_Implicit() {
            IDataErrorInfo vm = CustomImplicitDataErrorInfoIndexerImplementation.Create();
            var hwvm = new HandwrittenDataErrorInfoClass();
            Assert.Equal("StringProp error", vm["StringProp"]);
            Assert.Equal(string.Empty, vm.Error);
        }
        #endregion

        #region errors
        [Fact]
        public void CallRaiseCommandChangedMethodExtensionMethodForNotPOCOViewModelTest() {
            Assert.Throws<ViewModelSourceException>(() => {
                new POCOCommandsViewModel().RaiseCanExecuteChanged(x => x.Save());
            });
        }
        [Fact]
        public void CallGetCommandMethodExtensionMethodForNotPOCOViewModelTest() {
            Assert.Throws<ViewModelSourceException>(() => {
                new POCOCommandsViewModel().GetCommand(x => x.Save());
            });
        }
        #endregion

        void CheckBindableProperty<T, TProperty>(T viewModel, Expression<Func<T, TProperty>> propertyExpression, Action<T, TProperty> setValueAction, TProperty value1, TProperty value2, Action<T, TProperty> checkOnPropertyChangedResult = null) {
            CheckBindablePropertyCore(viewModel, propertyExpression, setValueAction, value1, value2, true, checkOnPropertyChangedResult);
        }
        void CheckNotBindableProperty<T, TProperty>(T viewModel, Expression<Func<T, TProperty>> propertyExpression, Action<T, TProperty> setValueAction, TProperty value1, TProperty value2) {
            CheckBindablePropertyCore(viewModel, propertyExpression, setValueAction, value1, value2, false, null);
        }
        void CheckBindablePropertyCore<T, TProperty>(T viewModel, Expression<Func<T, TProperty>> propertyExpression, Action<T, TProperty> setValueAction, TProperty value1, TProperty value2, bool bindable, Action<T, TProperty> checkOnPropertyChangedResult) {
            Assert.NotEqual(value1, value2);
            Func<T, TProperty> getValue = propertyExpression.Compile();

            int propertyChangedFireCount = 0;
            PropertyChangedEventHandler handler = (o, e) => {
                Assert.Equal(viewModel, o);
                Assert.Equal(ExpressionExtensions.GetPropertyNameFast(propertyExpression), e.PropertyName);
                propertyChangedFireCount++;
            };
            ((INotifyPropertyChanged)viewModel).PropertyChanged += handler;
            Assert.Equal(0, propertyChangedFireCount);
            TProperty oldValue = getValue(viewModel);
            setValueAction(viewModel, value1);
            if(checkOnPropertyChangedResult != null)
                checkOnPropertyChangedResult(viewModel, oldValue);
            if(bindable) {
                Assert.Equal(value1, getValue(viewModel));
                Assert.Equal(1, propertyChangedFireCount);
            } else {
                Assert.Equal(0, propertyChangedFireCount);
            }
            ((INotifyPropertyChanged)viewModel).PropertyChanged -= handler;
            setValueAction(viewModel, value2);
            setValueAction(viewModel, value2);
            if(checkOnPropertyChangedResult != null)
                checkOnPropertyChangedResult(viewModel, value1);
            if(bindable) {
                Assert.Equal(value2, getValue(viewModel));
                Assert.Equal(1, propertyChangedFireCount);
            } else {
                Assert.Equal(0, propertyChangedFireCount);
            }
        }

        ICommand CheckCommand<T>(T viewModel, Expression<Action<T>> methodExpression, Action<T> checkExecuteResult, bool isAsyncCommand = false) {
            string commandName = GetCommandName<T>(methodExpression);
            ICommand command = (ICommand)TypeHelper.GetPropertyValue(viewModel, commandName);
            Assert.NotNull(command);
            Assert.Same(command, TypeHelper.GetPropertyValue(viewModel, commandName));
            Assert.True(command.CanExecute(null));
            command.Execute(null);
            if(isAsyncCommand)
                Thread.Sleep(400);
            checkExecuteResult(viewModel);
            return command;
        }
        void CheckNoCommand<T>(T viewModel, string methodName) {
            Assert.NotNull(typeof(T).GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));
            string commandName = methodName + "Command";
            Assert.Null(TypeHelper.GetProperty(viewModel, commandName));
        }
        static string GetCommandName<T>(Expression<Action<T>> methodExpression) {
            return ((MethodCallExpression)methodExpression.Body).Method.Name + "Command";
        }
        static void WaitFor(Func<bool> condition) {
            var sw = new Stopwatch();
            sw.Start();
            while(!condition()) {
                if(sw.ElapsedMilliseconds > 200)
                    throw new TimeoutException();
                DispatcherHelper.DoEvents();
            }
        }
    }
    public static class TypeHelper {
        public static PropertyInfo GetProperty(object obj, string propertyName) {
            Type type = obj.GetType();
            PropertyInfo res = null;
            foreach(PropertyInfo info in type.GetProperties())
                if(info.Name == propertyName) {
                    res = info;
                    break;
                }
            return res;
        }
        public static object GetPropertyValue(object obj, string propertyName) {
            Type type = obj.GetType();
            PropertyInfo pInfo = GetProperty(obj, propertyName);
            return pInfo != null ? pInfo.GetValue(obj, null) : null;
        }
        public static AttributeType GetPropertyAttribute<AttributeType>(object obj, string propertyName) where AttributeType : Attribute {
            Type type = obj.GetType();
            PropertyInfo property = GetProperty(obj, propertyName);
            Type attributeType = typeof(AttributeType);

            List<object> result = new List<object>();
            do {
                result.AddRange(property.GetCustomAttributes(true));
                MethodInfo getMethod = property.GetGetMethod();
                if(getMethod == null)
                    break;
                MethodInfo baseMethod = getMethod.GetBaseDefinition();
                if(baseMethod == getMethod)
                    break;
                property = baseMethod.DeclaringType.GetProperty(property.Name);
            } while(property != null);

            foreach(Attribute attribute in result) {
                if(attributeType.IsAssignableFrom(attribute.GetType()))
                    return (AttributeType)attribute;
            }
            return null;
        }
    }
    internal static class DispatcherHelper {
        static Dispatcher CurrentDispatcherReference;
        static DispatcherHelper() {
            IncreasePriorityContextIdleMessages();
        }
        static void IncreasePriorityContextIdleMessages() {
            CurrentDispatcherReference = Dispatcher.CurrentDispatcher;
            Dispatcher.CurrentDispatcher.Hooks.OperationPosted += (d, e) => {
                if(e.Operation.Priority == DispatcherPriority.ContextIdle)
                    e.Operation.Priority = DispatcherPriority.Background;
            };
        }
        static object ExitFrame(object f) {
            ((DispatcherFrame)f).Continue = false;

            return null;
        }
        public static void ForceIncreasePriorityContextIdleMessages() {
        }
        public static void UpdateLayoutAndDoEvents(UIElement element) { UpdateLayoutAndDoEvents(element, DispatcherPriority.Background); }
        public static void UpdateLayoutAndDoEvents(UIElement element, DispatcherPriority priority) {
            element.UpdateLayout();
            DoEvents(priority);
        }
        public static void DoEvents() {
            DoEvents(DispatcherPriority.Background);
        }
        public static void DoEvents(DispatcherPriority priority) {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                priority,
                new DispatcherOperationCallback(ExitFrame),
                frame);
            Dispatcher.PushFrame(frame);
        }
    }
}
