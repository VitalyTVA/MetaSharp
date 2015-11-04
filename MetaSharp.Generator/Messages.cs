using MetaSharp.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaSharp {
    public static class Messages {
        const string MessagePrefix = MessagesCore.MessagePrefix;
        //TODO check all messages
        public static readonly UnfomattedMessage Exception = new UnfomattedMessage("0000", "Exception occured during generating output: {0} See build output for details.\r\n{1}");
        public static readonly Message PropertyTypeMissed = new Message("0001", "Either property type should be explicitly specified or default value should be explicitly typed to generate dependency property");

        public const string IncorrectPropertyName_Id = MessagePrefix + "0002";
        public const string IncorrectPropertyName_Message = "Dependency property field for the the property '{0}' should have '{1}' name.";

        public const string IncorrectOwnerType_Id = MessagePrefix + "0003";
        public const string IncorrectOwnerType_Message = "Owner type doesn't match the enclosing type.";

        public const string PropertyIsNotVirual_Id = MessagePrefix + "0004";
        public const string PropertyIsNotVirual_Message = "Cannot make non-virtual property bindable: {0}.";
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
