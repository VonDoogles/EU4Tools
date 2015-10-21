using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;

namespace EU4Tools
{
    public class Utils
    {
        public static string FindGamePath()
        {
            string Result = "";
            string SteamPath = Registry.GetValue( @"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", "" ) as string;
            string EU4Path = string.Format( "{0}/steamapps/common/Europa Universalis IV", SteamPath );

            if ( Directory.Exists( EU4Path ) )
            {
                Result = Path.GetFullPath( EU4Path );
            }
            return Result;
        }

        public static string FindInterfacePath()
        {
            string Result = string.Format( "{0}/interface", FindGamePath() );
            if ( Directory.Exists( Result ) )
            {
                return Path.GetFullPath( Result );
            }
            return "";            
        }
    }
}
