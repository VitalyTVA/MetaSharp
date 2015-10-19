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
        public const string Exception_Id = MessagePrefix + "0000";
        public const string Exception_Message = "Exception occured during generating output: {0} See build output for details.\r\n{1}";

        public const string PropertyTypeMissed_Id = MessagePrefix + "0001";
        public const string PropertyTypeMissed_Message = "Either property type should be explicitly specified or default value should be explicitly typed to generate dependency property";

        public const string IncorrectPropertyName_Id = MessagePrefix + "0002";
        public const string IncorrectPropertyName_Message = "Dependency property field for the the property '{0}' should have '{1}' name.";

        public const string IncorrectOwnerType_Id = MessagePrefix + "0003";
        public const string IncorrectOwnerType_Message = "Owner type doesn't match the enclosing type.";
    }
}
