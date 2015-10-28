using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp.Test.Meta.POCO {
    public partial class POCOViewModel_PropertyChangedBase {
        protected virtual void OnProtectedChangedMethodWithParamChanged(string oldValue) { }
        public virtual bool SealedProperty { get; set; }
    }
    public abstract class CommandAttributeViewModelBaseCounters {
        public int BaseClassCommandCallCount;
        public int SimpleMethodCallCount;
        public int MethodWithCommandCallCount;
        public int CustomNameCommandCallCount;
        public bool MethodWithCanExecuteCanExcute = false;
        public int MethodWithReturnTypeCallCount;
        public int MethodWithReturnTypeAndParameterCallCount;
        public int MethodWithParameterCallCount;
        public int MethodWithParameterLastParameter;
        public bool MethodWithCustomCanExecuteCanExcute = false;
    }
    public abstract class CommandAttributeViewModelBase : CommandAttributeViewModelBaseCounters {
        public void BaseClass() { BaseClassCommandCallCount++; }
    }

    public class POCOViewModel_INPCImplementorBase : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChangedCore(string propertyName) {
            if(PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public virtual void RaisePropertyChanged(string propertyName) {
        }
    }
}
