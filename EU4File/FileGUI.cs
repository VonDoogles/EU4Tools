using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;

namespace EU4Tools
{
    public class FileGUI
    {
        public static Element Read( string FilePath )
        {
            string ReComment = @"(?<Comment>\#[^\r\n]*)";
            string ReEOL = @"(?<EOL>\r\n|\r|\n)";
            string ReEqual = @"(?<Equal>=)";
            string ReNum = @"(?<Num>\d+)";
            string ReStr = @"""(?<Str>.*)""";
            string ReID = @"(?<ID>\w+)";
            string ReObjBegin = @"(?<ObjBegin>\{)";
            string ReObjEnd = @"(?<ObjEnd>\})";
            string ReWhite = @"(?<White>[\t ]+)";

            List<string> ReList = new List<string>()
            {
                ReComment,
                ReEqual,
                ReNum,
                ReStr,
                ReID,
                ReObjBegin,
                ReObjEnd,
                ReWhite,
                ReEOL
            };

            string ReAll = string.Join( "|", ReList );
            Regex Re = new Regex( ReAll, RegexOptions.Compiled | RegexOptions.ExplicitCapture );

            Element Doc = new Element() { Name = "Root" };
            Element Obj = Doc;

            DateTime StartTime = DateTime.UtcNow;
            UInt32 LineNum = 0;

            Func<Token, bool> NonWhiteSpace = Tk =>
            {
                bool IsWhiteSpace = false;
                IsWhiteSpace |= Tk.Type == "White";
                IsWhiteSpace |= Tk.Type == "EOL";
                return !IsWhiteSpace;
            };

            using ( StreamReader Reader = File.OpenText( FilePath ) )
            {
                Token Token = null;
                List<Token> TokenList = new List<Token>();

                String Input;
                while ( ( Input = Reader.ReadLineWithEOL() ) != null )
                {
                    bool FoundMatch = false;
                    LineNum++;

                    Match M = Re.Match( Input );
                    while ( M.Success )
                    {
                        FoundMatch = true;
                        string MatchName = Re.GetMatchName( M );

                        //Console.WriteLine( string.Format( "{0}  matched {1}", MatchName, M.Value ) );

                        Token = new Token() { Type = MatchName, Value = M.Value, LineNum = LineNum };
                        TokenList.Add( Token );

                        switch ( Token.Type )
                        {
                            case "Comment":
                                Obj.AppendChild( new Element()
                                {
                                    Name = "Comment",
                                    Value = Token.Value,
                                    InnerText = Token.Value,
                                    OuterText = TokenList.Aggregate( "", ( Result, Next ) => Result + Next.Value )
                                } );

                                TokenList.Clear();
                                break;
                            case "Num":
                            case "Str":
                            case "ID":
                                if ( TokenList.Count( Tk => Tk.Type == "Equal" ) == 1 )
                                {
                                    Token ID = TokenList.Find( Tk => Tk.Type == "ID" );

                                    Obj.AppendChild( new Element()
                                    {
                                        Name = ID.Value,
                                        Value = Token.Value,
                                        InnerText = Token.Value,
                                        OuterText = TokenList.Aggregate( "", ( Result, Next ) => Result + Next.Value )
                                    } );

                                    TokenList.Clear();
                                }
                                break;
                            case "ObjBegin":
                                if ( TokenList.Count( Tk => Tk.Type == "Equal" ) == 1 )
                                {
                                    Token ID = TokenList.Find( Tk => Tk.Type == "ID" );

                                    Obj = Obj.AppendChild( new Element()
                                    {
                                        Name = ID.Value,
                                        OuterText = TokenList.Aggregate( "", ( Result, Next ) => Result + Next.Value ),
                                    } );

                                    TokenList.Clear();
                                }
                                break;
                            case "ObjEnd":
                                StringBuilder Builder = new StringBuilder( Obj.OuterText );

                                Obj.ItemList.ForEach( It => Builder.Append( It.OuterText ) );
                                TokenList.ForEach( It => Builder.Append( It.Value ) );

                                Obj.OuterText = Builder.ToString();
                                Obj = Obj.Parent;

                                TokenList.Clear();
                                break;
                        }

                        M = M.NextMatch();
                    }

                    if ( !FoundMatch )
                    {
                        Console.WriteLine( string.Format( "No Match For {0}", Input ) );
                    }
                }
            }

            Doc.BuildText();

            DateTime EndTime = DateTime.UtcNow;
            Console.WriteLine( string.Format( "Time Elapsed = {0}", ( EndTime - StartTime ) ) );

            return Doc;
        }

        public static Element ReadGFX( string FilePath )
        {
            FilePath = Path.ChangeExtension( FilePath, "gfx" );
            return Read( FilePath );
        }

        [DebuggerDisplay( "{Name}" )]
        public class Element
        {
            public Element Parent;
            public List<Element> ItemList = new List<Element>();
            public string Name { get; set; }
            public string Value { get; set; }
            public string InnerText { get; set; }
            public string OuterText { get; set; }

            public bool IsRoot
            {
                get { return Parent == null; }
            }

            public Element AppendChild( Element Child )
            {
                Child.Parent = this;
                ItemList.Add( Child );
                return Child;
            }

            public Element AppendChild( string Name, string Value )
            {
                Element Child = new Element()
                {
                    Parent = this,
                    Name = Name,
                    Value = Value,
                    InnerText = Value
                };

                ItemList.Add( Child );
                return Child;
            }

            public void AppendText( string Text )
            {
                InnerText += Text;
            }

            public void BuildText()
            {
                if ( string.IsNullOrEmpty( InnerText ) )
                {
                    StringBuilder SB = new StringBuilder();

                    foreach ( Element Child in ItemList )
                    {
                        Child.BuildText();
                        SB.Append( Child.OuterText );
                    }

                    InnerText = SB.ToString();
                }

                if ( IsRoot )
                {
                    OuterText = InnerText;
                }
                else if ( string.IsNullOrEmpty( OuterText ) )
                {
                    OuterText = string.Format( "{0}={1}", Name, InnerText );
                }
            }

            public Element this[ string Key ]
            {
                get
                {
                    return ItemList.Find( It => It.Name == Key );
                }
            }

            public T Field<T>( string Key )
            {
                T Result = (T)GetDefault( typeof(T) );

                Element Item = this[ Key ];
                if ( Item != null )
                {
                    Result = (T)TypeDescriptor.GetConverter( typeof( T ) ).ConvertFromInvariantString( Item.Value );
                }

                return Result;
            }

            object GetDefault( Type T )
            {
                return T.IsValueType ? Activator.CreateInstance( T ) : null;
            }

            public Element FindFirst( Predicate<Element> Predicate )
            {
                if ( Predicate( this ) )
                {
                    return this;
                }

                foreach (Element Item in ItemList)
                {
                    Element Found = Item.FindFirst( Predicate );
                    if (Found != null)
                    {
                        return Found;
                    }
                }

                return null;
            }
        }

        [DebuggerDisplay( "Type = {Type}, Value = {Value}" )]
        public class Token
        {
            public string Type;
            public string Value;
            public UInt32 LineNum;
        }
    }
}
