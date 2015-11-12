namespace MetaSharp.Sample {


    partial class Incomplete {

        public Incomplete(int foo, int boo) {
            Foo = foo;
            Boo = boo;
        }
    }
}
namespace MetaSharp.Sample {
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;

    partial class ViewModel : INotifyPropertyChanged, ISupportParentViewModel, ISupportServices {
        public static ViewModel Create() {
            return new ViewModelImplementation();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged(string property) {
            var handler = PropertyChanged;
            if(handler != null)
                handler(this, new PropertyChangedEventArgs(property));
        }
        void RaisePropertyChanged<T>(Expression<Func<ViewModel, T>> property) {
            RaisePropertyChanged(DevExpress.Mvvm.Native.ExpressionHelper.GetPropertyName(property));
        }
        object parentViewModel;
        object ISupportParentViewModel.ParentViewModel {
            get { return parentViewModel; }
            set {
                if(parentViewModel == value)
                    return;
                var oldParentViewModel = parentViewModel;
                parentViewModel = value;
                OnParentViewModelChanged(oldParentViewModel);
            }
        }
        partial void OnParentViewModelChanged(object oldParentViewModel);
        IServiceContainer _ServiceContainer;
        IServiceContainer ISupportServices.ServiceContainer { get { return _ServiceContainer ?? (_ServiceContainer = new ServiceContainer(this)); } }

        class ViewModelImplementation : ViewModel, IPOCOViewModel {
            public ViewModelImplementation() 
                :base() { }
            public override string BooProperty {
                get { return base.BooProperty; }
                set {
                    if(base.BooProperty == value)
                        return;


                    base.BooProperty = value;
                    RaisePropertyChanged("BooProperty");

                }
            }
            public override int IntProperty {
                get { return base.IntProperty; }
                set {
                    if(base.IntProperty == value)
                        return;


                    base.IntProperty = value;
                    RaisePropertyChanged("IntProperty");

                }
            }
            void IPOCOViewModel.RaisePropertyChanged(string propertyName) {
                RaisePropertyChanged(propertyName);
            }
        }
    }
}