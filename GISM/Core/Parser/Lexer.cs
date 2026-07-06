using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GISM.Core.Parser
{


    public enum TokenType
    {
        // Structural
        Dash,           // -
        Equals,         // =
        OpenBracket,    // [
        CloseBracket,   // ]
        OpenBrace,      // {
        CloseBrace,     // }
        Indent,         // Suffix of spaces increasing
        Outdent,        // Suffix of spaces decreasing
        NewLine,        // End of a line

        // Values & Identifiers
        Identifier,     // Property names, type names, or raw values (e.g., Name, _x, TopLeft)
        TypeCast,       // Content inside < > (e.g., GameObject, String)
        Reference,      // The ID inside REF("...")
        StringLiteral,  // "Main Camera"
        NumberLiteral,  // 0, 200, 16
        BooleanLiteral, // true, false
        NullLiteral,    // null

        EndOfFile
    }

    public struct Token
    {
        public TokenType Type
        {
            get;
        }
        public string Value
        {
            get;
        }
        public int Line
        {
            get;
        }

        public Token(TokenType type, string value, int line)
        {
            Type = type;
            Value = value;
            Line = line;
        }

        public override string ToString() => $"{Type}({Value}) on Line {Line}";
    }

public class Lexer
    {
        private readonly string _input;
        private int _index = 0;
        private int _currentLine = 1;
        private readonly Stack<int> _indentStack = new Stack<int>();

        public Lexer(string input)
        {
            _input = input;
            _indentStack.Push(0); // Base level of 0 spaces
        }

        private char Peek() => _index < _input.Length ? _input[_index] : '\0';
        private char Advance() => _index < _input.Length ? _input[_index++] : '\0';

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while(_index < _input.Length)
            {
                // 1. Handle Indentation at the start of a line
                if(_index == 0 || _input[_index - 1] == '\n' || _input[_index - 1] == '\r')
                {
                    HandleIndentation(tokens);
                    if(_index >= _input.Length)
                        break;
                }

                char c = Peek();

                // Skip inline spaces/tabs (only care about leading spaces for indent)
                if(c == ' ' || c == '\t')
                {
                    Advance();
                    continue;
                }

                // Handle newlines explicitly
                if(c == '\r' || c == '\n')
                {
                    if(c == '\r')
                        Advance(); // Handle CR
                    if(Peek() == '\n')
                        Advance(); // Handle LF
                    tokens.Add(new Token(TokenType.NewLine, "\\n", _currentLine));
                    _currentLine++;
                    continue;
                }

                // 2. Structural Symbols
                if(c == '-')
                {
                    Advance();
                    tokens.Add(new Token(TokenType.Dash, "-", _currentLine));
                    continue;
                }
                if(c == '=')
                {
                    Advance();
                    tokens.Add(new Token(TokenType.Equals, "=", _currentLine));
                    continue;
                }
                if(c == '[')
                {
                    Advance();
                    tokens.Add(new Token(TokenType.OpenBracket, "[", _currentLine));
                    continue;
                }
                if(c == ']')
                {
                    Advance();
                    tokens.Add(new Token(TokenType.CloseBracket, "]", _currentLine));
                    continue;
                }
                if(c == '{')
                {
                    Advance();
                    tokens.Add(new Token(TokenType.OpenBrace, "{", _currentLine));
                    continue;
                }
                if(c == '}')
                {
                    Advance();
                    tokens.Add(new Token(TokenType.CloseBrace, "}", _currentLine));
                    continue;
                }

                // 3. Type Casts <TypeName>
                if(c == '<')
                {
                    Advance(); // Consume starting '<'
                    var sb = new StringBuilder();
                    int bracketDepth = 1;

                    while(bracketDepth > 0 && Peek() != '\0')
                    {
                        char next = Peek();
                        if(next == '<')
                            bracketDepth++;
                        if(next == '>')
                            bracketDepth--;

                        if(bracketDepth > 0)
                            sb.Append(Advance());
                    }
                    if(Peek() == '>')
                        Advance(); // Consume final closing '>'

                    tokens.Add(new Token(TokenType.TypeCast, sb.ToString(), _currentLine));
                    continue;
                }

                // 4. Strings "..."
                if(c == '"')
                {
                    Advance(); // Consume starting quote
                    var sb = new StringBuilder();
                    while(Peek() != '"' && Peek() != '\0')
                        sb.Append(Advance());
                    if(Peek() == '"')
                        Advance(); // Consume ending quote
                    tokens.Add(new Token(TokenType.StringLiteral, sb.ToString(), _currentLine));
                    continue;
                }

                // 5. Word-based tokens (Identifiers, REFs, Booleans, Null)
                if(char.IsLetter(c) || c == '_')
                {
                    var sb = new StringBuilder();
                    while(char.IsLetterOrDigit(Peek()) || Peek() == '_')
                        sb.Append(Advance());
                    string word = sb.ToString();

                    // Check for REF("id")
                    if(word == "REF" && Peek() == '(')
                    {
                        Advance(); // consume '('
                        if(Peek() == '"')
                            Advance(); // consume '"'
                        var refId = new StringBuilder();
                        while(Peek() != '"' && Peek() != '\0')
                            refId.Append(Advance());
                        if(Peek() == '"')
                            Advance(); // consume '"'
                        if(Peek() == ')')
                            Advance(); // consume ')'
                        tokens.Add(new Token(TokenType.Reference, refId.ToString(), _currentLine));
                    }
                    else if(word == "true" || word == "false")
                    {
                        tokens.Add(new Token(TokenType.BooleanLiteral, word, _currentLine));
                    }
                    else if(word == "null")
                    {
                        tokens.Add(new Token(TokenType.NullLiteral, word, _currentLine));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Identifier, word, _currentLine));
                    }
                    continue;
                }

                // 6. Numbers (including negative numbers and floats)
                if(char.IsDigit(c) || (c == '-' && char.IsDigit(Peek())))
                {
                    var sb = new StringBuilder();
                    sb.Append(Advance()); // Consume digit or negative sign
                    while(char.IsDigit(Peek()) || Peek() == '.')
                        sb.Append(Advance());
                    tokens.Add(new Token(TokenType.NumberLiteral, sb.ToString(), _currentLine));
                    continue;
                }

                // If we run into an unknown character, skip or throw an error
                Advance();
            }

            // Clean up remaining open indents at EOF
            while(_indentStack.Count > 1)
            {
                _indentStack.Pop();
                tokens.Add(new Token(TokenType.Outdent, "", _currentLine));
            }

            tokens.Add(new Token(TokenType.EndOfFile, "EOF", _currentLine));
            return tokens;
        }

        private void HandleIndentation(List<Token> tokens)
        {
            int spaces = 0;
            while(Peek() == ' ' || Peek() == '\t')
            {
                char space = Advance();
                spaces += (space == '\t') ? 4 : 1; // Convert tabs to 4 spaces standard
            }

            // Skip completely empty lines without creating false outdents
            if(Peek() == '\r' || Peek() == '\n')
                return;

            int currentIndent = _indentStack.Peek();

            if(spaces > currentIndent)
            {
                _indentStack.Push(spaces);
                tokens.Add(new Token(TokenType.Indent, spaces.ToString(), _currentLine));
            }
            else if(spaces < currentIndent)
            {
                while(_indentStack.Count > 0 && _indentStack.Peek() > spaces)
                {
                    _indentStack.Pop();
                    tokens.Add(new Token(TokenType.Outdent, spaces.ToString(), _currentLine));
                }
            }
        }
    }


}

