using System;
using System.Collections.Generic;
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
}
