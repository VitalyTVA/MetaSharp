namespace MetaSharp.Sample {


    partial class Incomplete {

        public Incomplete(int foo, int boo) {
            Foo = foo;
            Boo = boo;
        }
    }
}
namespace MetaSharp.Sample {


    using System.ComponentModel;
    partial class ViewModel {
        public static ViewModel Create() {
            return new ViewModelImplementation();
        }
        class ViewModelImplementation : ViewModel, INotifyPropertyChanged {
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
            public event PropertyChangedEventHandler PropertyChanged;
            void RaisePropertyChanged(string property) {
                var handler = PropertyChanged;
                if(handler != null)
                    handler(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}