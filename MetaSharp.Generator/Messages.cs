using MetaSharp.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp {
    public enum Chang { ing, ed }
    public static class Messages {
        //TODO check all messages
        //TODO write test checking id's are not repeated
        public static readonly UnformattedMessage General_Exception = new UnformattedMessage("0000", "Exception occured during generating output: {0} See build output for details.\r\n{1}");

        public static readonly Message DependecyProperty_PropertyTypeMissed = new Message("0001", "Either property type should be explicitly specified or default value should be explicitly typed to generate dependency property");
        public static readonly UnformattedMessage DependecyProperty_IncorrectPropertyName = new UnformattedMessage("0002", "Dependency property field for the the property '{0}' should have '{1}' name.");
        public static readonly Message DependecyProperty_IncorrectOwnerType = new Message("0003", "Owner type doesn't match the enclosing type.");
        public static readonly Message DependecyProperty_UnsupportedSyntax = new Message("0022", "Syntax is not supported.");

        public static readonly UnformattedMessage POCO_PropertyIsNotVirual = new UnformattedMessage("0004", "Cannot make non-virtual property bindable: {0}.");
        public static readonly UnformattedMessage POCO_PropertyHasNoSetter = new UnformattedMessage("0005", "Cannot make property without setter bindable: {0}.");
        public static readonly UnformattedMessage POCO_PropertyHasNoPublicGetter = new UnformattedMessage("0006", "Cannot make property without public getter bindable: {0}.");
        public static readonly UnformattedMessage POCO_SealedClass = new UnformattedMessage("0007", "Cannot create POCO implementation class for the sealed class: {0}.");
        public static readonly Func<Chang, UnformattedMessage> POCO_MoreThanOnePropertyChangedMethod = x => new UnformattedMessage("0008", $"More than one property chang{x} method: {{0}}.");
        public static readonly Func<Chang, UnformattedMessage> POCO_PropertyChangedCantHaveMoreThanOneParameter = x => new UnformattedMessage("0009", $"Property chang{x} method cannot have more than one parameter: {{0}}.");
        public static readonly Func<Chang, UnformattedMessage> POCO_PropertyChangedCantHaveReturnType = x => new UnformattedMessage("0010", $"Property chang{x} method cannot have return type: {{0}}.");
        public static readonly Func<Chang, UnformattedMessage> POCO_PropertyChangedMethodArgumentTypeShouldMatchPropertyType = x => new UnformattedMessage("0011", $"Property chang{x} method argument type should match property type: {{0}}.");
        public static readonly UnformattedMessage POCO_RaisePropertyChangedMethodNotFound = new UnformattedMessage("0012", "Class already supports INotifyPropertyChanged, but RaisePropertyChanged(string) method not found: {0}.");
        public static readonly UnformattedMessage POCO_PropertyIsSealed = new UnformattedMessage("0013", "Cannot override sealed property: {0}.");
        public static readonly UnformattedMessage POCO_TypeImplementsIPOCOViewModel = new UnformattedMessage("0014", "Type should not implement IPOCOViewModel: {0}.");
        public static readonly Func<Chang, UnformattedMessage> POCO_PropertyChangedMethodNotFound = x => new UnformattedMessage("0015", $"Property chang{x} method not found: {{0}}.");
        public static readonly UnformattedMessage POCO_MemberWithSameCommandNameAlreadyExists = new UnformattedMessage("0016", "Member with the same command name already exists: {0}.");
        public static readonly UnformattedMessage POCO_MethodCannotHaveMoreThanOneParameter = new UnformattedMessage("0017", "Method cannot have more than one parameter: {0}.");
        public static readonly UnformattedMessage DependecyProperty_IncorrectAttachedPropertyGetterName = new UnformattedMessage("0018", "Attached dependency property dedicated accessor method name should starts with 'Get' prefix: {0}.");
        public static readonly UnformattedMessage POCO_MethodCannotHaveOutORRefParameters = new UnformattedMessage("0019", "Method cannot have out or reference parameter: {0}.");
        public static readonly UnformattedMessage POCO_CanExecuteMethodHasIncorrectParameters = new UnformattedMessage("0020", "CanExecute method has incorrect parameters: {0}.");
        public static readonly UnformattedMessage POCO_MethodNotFound = new UnformattedMessage("0021", "Method not found: {0}.");
    }
    public struct UnformattedMessage {
        readonly string id;
        public readonly string Text;
        public string FullId => MessagesCore.MessagePrefix + id;
        public UnformattedMessage(string id, string text) {
            this.id = id;
            Text = text;
        }
        public Message Format(params object[] args) {
            return new Message(id, string.Format(Text, args));
        }
    }
    public struct Message {
        readonly string id;
        public readonly string Text;
        public string FullId => MessagesCore.MessagePrefix + id;
        public Message(string id, string text) {
            this.id = id;
            Text = text;
        }
    }
}
