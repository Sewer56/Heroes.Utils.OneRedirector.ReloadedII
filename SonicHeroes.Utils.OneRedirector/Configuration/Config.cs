using System.ComponentModel;

namespace SonicHeroes.Utils.OneRedirector.Configuration;

public class Config : Configurable<Config>
{
    /*
        User Properties:
            - Please put all of your configurable properties here.
            - Tip: Consider using the various available attributes https://stackoverflow.com/a/15051390/11106111
    
        By default, configuration saves as "Config.json" in mod folder.    
        Need more config files/classes? See Configuration.cs
    */


    [DisplayName("Always Build Archives")]
    [Description("Rebuilds the .ONE archive every time the game opens the files. This allows you to swap out files while the game is running at the expense or more time taken to load the file(s).")]
    public bool AlwaysBuildArchive { get; set; } = false;
}