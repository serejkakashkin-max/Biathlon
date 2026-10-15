using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Biathlon.Domain
{
    /// <summary>
    /// The framework serializer accepts some truncated roots and multiple JSON
    /// values. Require exactly one complete object before deserializing a product document.
    /// This is syntax validation, not a serializer or a gameplay definition.
    /// </summary>
    public sealed class JsonDocumentSyntax
    {
        private readonly string text;
        private int position;
        private JsonDocumentSyntax(string text) {this.text=text;}
        public static void Validate(string text)
        {
            if(text==null)throw new ArgumentNullException(nameof(text));
            var syntax=new JsonDocumentSyntax(text);
            if(text.Length>0&&text[0]=='\uFEFF')syntax.position++;
            syntax.White();if(syntax.Peek!='{')throw Bad();syntax.Value(0);syntax.White();
            if(syntax.position!=text.Length)throw Bad();
        }
        private static SerializationException Bad()=>new SerializationException("Document is not one complete, unambiguous JSON object.");
        private char Peek=>position<text.Length?text[position]:'\0';
        private void White() {while(Peek==' '||Peek=='\t'||Peek=='\r'||Peek=='\n')position++;}
        private void Take(char token) {if(Peek!=token)throw Bad();position++;}
        private void Value(int depth)
        {
            // All supported schemas are shallow; bound hostile recursion without
            // imposing a file-size cap on a legitimate persistent career world.
            if(depth>128)throw Bad();White();
            switch(Peek)
            {
                case '{':
                    position++;White();if(Peek=='}'){position++;return;}
                    var names=new HashSet<string>(StringComparer.Ordinal);
                    while(true)
                    {
                        White();string name=String(true);if(!names.Add(name))throw Bad();White();Take(':');Value(depth+1);White();
                        if(Peek=='}'){position++;return;}Take(',');
                    }
                case '[':
                    position++;White();if(Peek==']'){position++;return;}
                    while(true){Value(depth+1);White();if(Peek==']'){position++;return;}Take(',');}
                case '"':String(false);return;
                case 't':Literal("true");return;
                case 'f':Literal("false");return;
                case 'n':Literal("null");return;
                default:Number();return;
            }
        }
        private void Literal(string token)
        {foreach(char c in token)Take(c);}
        private void Digits()
        {if(Peek<'0'||Peek>'9')throw Bad();while(Peek>='0'&&Peek<='9')position++;}
        private void Number()
        {
            if(Peek=='-')position++;
            if(Peek=='0')position++;else {if(Peek<'1'||Peek>'9')throw Bad();Digits();}
            if(Peek=='.'){position++;Digits();}
            if(Peek=='e'||Peek=='E'){position++;if(Peek=='+'||Peek=='-')position++;Digits();}
        }
        private string String(bool capture)
        {
            Take('"');var value=capture?new StringBuilder():null;
            while(true)
            {
                if(position>=text.Length)throw Bad();char c=text[position++];
                if(c=='"')return value?.ToString();if(c<' ')throw Bad();
                if(c=='\\')
                {
                    if(position>=text.Length)throw Bad();c=text[position++];
                    switch(c)
                    {
                        case '"':case '\\':case '/':break;
                        case 'b':c='\b';break;case 'f':c='\f';break;case 'n':c='\n';break;case 'r':c='\r';break;case 't':c='\t';break;
                        case 'u':
                            int code=0;for(int i=0;i<4;i++)
                            {
                                if(position>=text.Length)throw Bad();char digit=text[position++];
                                int number=digit>='0'&&digit<='9'?digit-'0':digit>='a'&&digit<='f'?digit-'a'+10:digit>='A'&&digit<='F'?digit-'A'+10:-1;
                                if(number<0)throw Bad();code=code*16+number;
                            }
                            c=(char)code;break;
                        default:throw Bad();
                    }
                }
                value?.Append(c);
            }
        }
    }
}
