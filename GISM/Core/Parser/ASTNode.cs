using GISM.Core.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public abstract class ASTNode
    {
        public int Line
        {
            get; set;
        }
    }


    public class LiteralValueNode : ASTNode
    {
        public string RawValue
        {
            get; set;
        }
        public TokenType Type
        {
            get; set;
        } // Helps us know if it was a StringLiteral, NumberLiteral, etc.

        public LiteralValueNode(string rawValue, TokenType type)
        {
            RawValue = rawValue;
            Type = type;
        }
    }

    public class ReferenceNode : ASTNode
    {
        public string Id
        {
            get; set;
        }

        public ReferenceNode(string id)
        {
            Id = id;
        }
    }

    public class ListNode : ASTNode
    {
        // A list in your language can hold Literals, References, or even full ObjectNodes
        public List<ASTNode> Elements { get; set; } = new List<ASTNode>();
    }

    // Represents a dictionary: Components = { Key = Value }
    public class DictionaryNode : ASTNode
    {
        // Keeps track of assignments inside the braces
        public List<PropertyNode> Entries { get; set; } = new List<PropertyNode>();
    }

    // Represents a field/property assignment: Name <String> = "Main Camera"
    public class PropertyNode : ASTNode
    {
        public string Name
        {
            get; set;
        }
        public string ExplicitType
        {
            get; set;
        } // Stored if <TypeCast> is present; null otherwise
        public ASTNode Value
        {
            get; set;
        }       // Can be a Literal, Reference, ListNode, DictionaryNode, or ObjectNode
    }

    // Represents an object instance: - <GameObject> REF("id_2")
    public class ObjectNode : ASTNode
    {
        public string TypeName
        {
            get; set;
        }      // From your <GameObject> cast or raw type name identifier
        public string InstanceName
        {
            get; set;
        }  // Handles your shorthand case: - Player_Controller
        public string ReferenceId
        {
            get; set;
        }   // Populated if REF("id_x") immediately follows

        // The collection of internal indented fields/properties belonging to this object
        public List<PropertyNode> Properties { get; set; } = new List<PropertyNode>();
    }

    // The root node of the entire file layout
    public class FileRootNode : ASTNode
    {
        // A file is essentially a collection of top-level root objects
        public List<ObjectNode> RootObjects { get; set; } = new List<ObjectNode>();
    }
