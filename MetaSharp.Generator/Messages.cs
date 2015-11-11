using MetaSharp.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp {
    public static class Messages {
        //TODO check all messages
        //TODO write test checking id's are not repeated
        public static readonly UnfomattedMessage General_Exception = new UnfomattedMessage("0000", "Exception occured during generating output: {0} See build output for details.\r\n{1}");

        public static readonly Message DependecyProperty_PropertyTypeMissed = new Message("0001", "Either property type should be explicitly specified or default value should be explicitly typed to generate dependency property");
        public static readonly UnfomattedMessage DependecyProperty_IncorrectPropertyName = new UnfomattedMessage("0002", "Dependency property field for the the property '{0}' should have '{1}' name.");
        public static readonly Message DependecyProperty_IncorrectOwnerType = new Message("0003", "Owner type doesn't match the enclosing type.");

        public static readonly UnfomattedMessage POCO_PropertyIsNotVirual = new UnfomattedMessage("0004", "Cannot make non-virtual property bindable: {0}.");
        public static readonly UnfomattedMessage POCO_PropertyHasNoSetter = new UnfomattedMessage("0005", "Cannot make property without setter bindable: {0}.");
        public static readonly UnfomattedMessage POCO_PropertyHasNoPublicGetter = new UnfomattedMessage("0006", "Cannot make property without public getter bindable: {0}.");
        public static readonly UnfomattedMessage POCO_SealedClass = new UnfomattedMessage("0007", "Cannot create POCO implementation class for the sealed class: {0}.");
        public static readonly UnfomattedMessage POCO_MoreThanOnePropertyChangedMethod = new UnfomattedMessage("0008", "More than one property changed method: {0}.");
        public static readonly UnfomattedMessage POCO_PropertyChangedCantHaveMoreThanOneParameter = new UnfomattedMessage("0009", "Property changed method cannot have more than one parameter: {0}.");
        public static readonly UnfomattedMessage POCO_PropertyChangedCantHaveReturnType = new UnfomattedMessage("0010", "Property changed method cannot have return type: {0}.");
        public static readonly UnfomattedMessage POCO_PropertyChangedMethodArgumentTypeShouldMatchPropertyType = new UnfomattedMessage("0011", "Property changed method argument type should match property type: {0}.");
    }
    public struct UnfomattedMessage {
        readonly string id;
        public readonly string Text;
        public string FullId => MessagesCore.MessagePrefix + id;
        public UnfomattedMessage(string id, string text) {
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
