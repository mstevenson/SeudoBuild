using System;
namespace SeudoBuild
{
    public class UnityLogParser
    {
        enum State
        {
            None,
            CompilingScripts
        }

        State state;

        public string ProcessLogLine(string line)
        {
            if (line == null)
            {
                return null;
            }

            switch (state)
            {
                case State.CompilingScripts:
                    break;
                default:
                    
                    if (line.Contains("WARNING: "))
                    {
                        return line;
                    }
                    if (line.Contains("ERROR: "))
                    {
                        return line;
                    }
                    
                    if (line.Contains("Initialize mono"))
                    {
                        return "Loading Unity project";
                    }
                    if (line.Contains("- starting compile"))
                    {
                        return line;
                    }
                    //if (line.Contains("Compilation succeeded"))
                    //{
                    //    BuildConsole.WriteLine(line);
                    //}
                    if (line.Contains("Compilation failed"))
                    {
                        return line;
                    }
                    if (line.Contains("Finished compile"))
                    {
                        return line;
                    }
                    //if (line.Contains("Launched and connected shader compiler"))
                    //{
                    //    BuildConsole.WriteLine("Running shader compiler");
                    //}
                    //if (line.Contains("Total AssetImport time"))
                    //{
                    //    BuildConsole.WriteLine(line);
                    //}
                    if (line.Contains("building "))
                    {
                        return line;
                    }
                    if (line.Contains("Opening scene "))
                    {
                        return line;
                    }
                    if (line.Contains("*** Completed"))
                    {
                        return line;
                    }
                    break;
            }
            return null;
        }
    }
}
