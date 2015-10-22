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
}
