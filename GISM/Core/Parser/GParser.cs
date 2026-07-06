using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GISM.Core.Parser
{
    public class GParser
    {
        private readonly List<Token> _tokens;
        private int _index = 0;
        private readonly GISMParserSettings _settings;

        public GParser(List<Token> tokens, GISMParserSettings settings)
        {
            _tokens = tokens;
            _settings = settings ?? new GISMParserSettings();
        }

        // Helper navigation methods
        private Token Peek()
        {
            if(_tokens == null || _index >= _tokens.Count)
            {
                return new Token(TokenType.EndOfFile, "EOF", 0);
            }
            return _tokens[_index];
        }

        private Token Advance()
        {
            if(_tokens == null || _index >= _tokens.Count)
            {
                return new Token(TokenType.EndOfFile, "EOF", 0);
            }
            return _tokens[_index++];
        }
        private bool Match(TokenType type)
        {
            if(Peek().Type == type)
            {
                Advance();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Uses settings to locate the real C# type from its markup string name
        /// </summary>
        public Type ResolveType(string typeName)
        {
            if(string.IsNullOrWhiteSpace(typeName))
                return _settings.DefaultInferredType;

            // Clean up common markup variations like Nullable<Guid> or System.Single
            if(typeName.StartsWith("Nullable<") && typeName.EndsWith(">"))
            {
                string innerTypeName = typeName.Substring(9, typeName.Length - 10);
                Type innerType = ResolveType(innerTypeName);
                if(innerType != null)
                    return typeof(Nullable<>).MakeGenericType(innerType);
            }

            // Check short-hand aliases manually
            switch(typeName.ToLower())
            {
                case "string":
                    return typeof(string);
                case "boolean":
                case "bool":
                    return typeof(bool);
                case "single":
                case "float":
                    return typeof(float);
                case "guid":
                    return typeof(Guid);
            }

            // Search through your passed custom assemblies!
            foreach(var assembly in _settings.TypeAssemblies)
            {
                Type t = assembly.GetType(typeName) ?? assembly.GetType($"System.{typeName}");
                if(t != null)
                    return t;
            }

            // Fallback to executing assembly or system core
            return Type.GetType(typeName) ?? _settings.DefaultInferredType;
        }

        public FileRootNode Parse()
        {
            var root = new FileRootNode();
            int lastIndex = -1;

            while(Peek().Type != TokenType.EndOfFile)
            {
                // Safety Break: Stop infinite lock if index fails to increment
                if(_index == lastIndex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GParser] Force-breaking root loop to prevent hang at index {_index} (Token: {Peek().Type})");
                    break;
                }
                lastIndex = _index;

                if(Match(TokenType.NewLine))
                    continue;

                if(Peek().Type == TokenType.Dash)
                {
                    root.RootObjects.Add(ParseObject());
                }
                else
                {
                    Advance(); // Ensure we move past unhandled tokens
                }
            }

            return root;
        }

        private ObjectNode ParseObject()
        {
            var objNode = new ObjectNode { Line = Peek().Line };
            Match(TokenType.Dash); // Consume the '-'

            // Handle your polymorphic object line declarations:
            // Case 1: - <GameObject> REF("id_2")
            // Case 2: - Player_Controller <GameObject>
            // Case 3: - GameObject

            Token firstToken = Advance();

            if(firstToken.Type == TokenType.TypeCast)
            {
                objNode.TypeName = firstToken.Value;
            }
            else if(firstToken.Type == TokenType.Identifier)
            {
                // Could be a Typename or an Instance Name
                if(Peek().Type == TokenType.TypeCast)
                {
                    objNode.InstanceName = firstToken.Value;
                    objNode.TypeName = Advance().Value; // Gather the actual explicit cast type
                }
                else
                {
                    objNode.TypeName = firstToken.Value; // Treated as Typename fallback
                }
            }

            // Check if an inline instance anchor immediately follows: REF("id_x")
            if(Peek().Type == TokenType.Reference)
            {
                objNode.ReferenceId = Advance().Value;
            }

            // Expect end of declaration line
            Match(TokenType.NewLine);

            // If the next token is an Indent, consume its child properties!
            if(Match(TokenType.Indent))
            {
                while(Peek().Type != TokenType.Outdent && Peek().Type != TokenType.EndOfFile)
                {
                    if(Match(TokenType.NewLine))
                        continue;

                    if(Peek().Type == TokenType.Identifier)
                    {
                        objNode.Properties.Add(ParseProperty());
                    }
                    else
                    {
                        // If it's a token we didn't expect (like structural whitespace or comments),
                        // we MUST advance past it, otherwise we freeze!
                        Advance();
                    }
                }
                Match(TokenType.Outdent); // Consume closing structural outdent
            }

            return objNode;
        }

        private PropertyNode ParseProperty()
        {
            var prop = new PropertyNode { Line = Peek().Line };
            prop.Name = Advance().Value; // Gather the identifier

            // Read optional type cast: Name <String> = "Main Camera"
            if(Peek().Type == TokenType.TypeCast)
            {
                prop.ExplicitType = Advance().Value;
            }

            // Ensure we hit the assignment operator
            if(!Match(TokenType.Equals))
            {
                // Symmetrical assignment formatting fallback if '=' is written on next indent block
                Match(TokenType.NewLine);
                Match(TokenType.Indent);
            }

            // Determine what type of value lives on the right side
            prop.Value = ParseValueExpression();
            return prop;
        }

        private ASTNode ParseValueExpression()
        {
            // Skip leading spacing markers with safety check
            while(Peek().Type == TokenType.NewLine && Peek().Type != TokenType.EndOfFile)
            {
                Advance();
            }

            Token t = Peek();

            // Array Collection Case: [ ]
            if(Match(TokenType.OpenBracket))
            {
                var listNode = new ListNode { Line = t.Line };
                Match(TokenType.NewLine);
                Match(TokenType.Indent);

                int lastListIdx = -1;
                while(Peek().Type != TokenType.CloseBracket && Peek().Type != TokenType.EndOfFile)
                {
                    if(_index == lastListIdx)
                    {
                        Advance();
                        continue;
                    }
                    lastListIdx = _index;

                    if(Match(TokenType.NewLine))
                        continue;

                    if(Peek().Type == TokenType.Dash)
                        listNode.Elements.Add(ParseObject());
                    else
                        listNode.Elements.Add(ParseValueExpression());
                }
                Match(TokenType.CloseBracket);
                Match(TokenType.Outdent);
                return listNode;
            }

            // Dictionary Collection Case: { }
            if(Match(TokenType.OpenBrace))
            {
                var dictNode = new DictionaryNode { Line = t.Line };
                Match(TokenType.NewLine);
                Match(TokenType.Indent);

                while(Peek().Type != TokenType.CloseBrace && Peek().Type != TokenType.EndOfFile)
                {
                    if(Match(TokenType.NewLine))
                        continue;
                    if(Peek().Type == TokenType.Identifier)
                    {
                        dictNode.Entries.Add(ParseProperty());
                    }
                }
                Match(TokenType.CloseBrace);
                Match(TokenType.Outdent);
                return dictNode;
            }

            // Pointer/Reference Tracker Case
            if(t.Type == TokenType.Reference)
            {
                Advance();
                return new ReferenceNode(t.Value) { Line = t.Line };
            }

            // Standard Primitive / Text Line Capture
            var literalToken = Advance();

            string wholeValue = literalToken.Value;
            while(Peek().Type != TokenType.NewLine &&
                   Peek().Type != TokenType.EndOfFile &&
                   Peek().Type != TokenType.CloseBracket &&
                   Peek().Type != TokenType.CloseBrace)
            {
                int safetyCheck = _index;
                wholeValue += Advance().Value;

                // Safety break: if for some reason advance fails to move the index forward
                if(_index == safetyCheck)
                {
                    break;
                }
            }

            return new LiteralValueNode(wholeValue, literalToken.Type) { Line = literalToken.Line };
        }
    }
}

