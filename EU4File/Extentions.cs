using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace EU4Tools
{
    static class ExtendRegex
    {
        public static string GetMatchName( this Regex Re, Match M )
        {
            string Name = "";
            for ( int Idx = 0; Idx < M.Groups.Count; ++Idx )
            {
                if ( M.Groups[ Idx ].Success )
                {
                    Name = Re.GroupNameFromNumber( Idx );
                }
            }
            return Name;
        }
    }

    static class ExtendStreamReader
    {
        public static string ReadLineWithEOL( this StreamReader Reader )
        {
            if ( Reader.Peek() >= 0 )
            {
                StringBuilder Builder = new StringBuilder();

                while ( Reader.Peek() >= 0 )
                {
                    char Ch = (char)Reader.Read();
                    Builder.Append( Ch );

                    if ( Ch == '\r' )
                    {
                        if ( Reader.Peek() == '\n' )
                        {
                            Builder.Append( (char)Reader.Read() );
                        }
                        break;
                    }
                    else if ( Ch == '\n' )
                    {
                        break;
                    }
                }

                return Builder.ToString();
            }
            return null;
        }
    }
}
