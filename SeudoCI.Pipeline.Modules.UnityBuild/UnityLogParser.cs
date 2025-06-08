namespace SeudoCI.Pipeline.Modules.UnityBuild;

using System.Collections;

public class UnityLogParser
{
    enum State
    {
        None,
        Start,
        CompilingScripts,
        Cancelled
    }

    private State _currentState = State.Start;
    private readonly Matches _universalMatches;
    private readonly Dictionary<State, Matches> _stateMatches;


    public class Match
    {
        public readonly string text;
        public readonly Func<string?, string?> output;

        public Match(string text, Func<string?, string?> output)
        {
            this.text = text;
            this.output = output;
        }
    }

    public class Matches : IEnumerable<Match>
    {
        readonly List<Match> matches = new List<Match>();

        public void Add(string text, Func<string?, string?> output)
        {
            matches.Add(new Match(text, output));
        }

        public IEnumerator<Match> GetEnumerator()
        {
            return matches.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


    public UnityLogParser()
    {
        _universalMatches = new Matches
        {
            { "WARNING: ", line => line },
            { "ERROR: ", line => line },
            { "Aborting batchmode due to failure", (line) => line },
            { "- starting compile", (line) => line },
            { "Compilation failed", (line) => line },
            { "Finished compile", (line) => line },
            { "building ", (line) => line },
            { "*** Cancelled", (line) => { _currentState = State.Cancelled; return "Cancelled build"; } },
            { "*** Completed", (line) => line }
        };

        _stateMatches = new Dictionary<State, Matches>
        {
            { State.Start, new Matches
                {
                    { "Initialize mono", (line) => { _currentState = State.None; return "Loading Unity project"; } }
                }
            },
            { State.Cancelled, new Matches
                {
                    { "is an incorrect path for a scene file", (line) => { return line; } }
                }
            }
        };
    }

    public string? ProcessLogLine(string? line)
    {
        if (line == null)
        {
            return null;
        }

        foreach (var match in _universalMatches)
        {
            if (line.Contains(match.text))
            {
                string? result = match.output.Invoke(line);
                return result;
            }
        }

        if (_stateMatches.TryGetValue(_currentState, out var matches))
        {
            foreach (var match in matches)
            {
                if (line.Contains(match.text))
                {
                    string? result = match.output.Invoke(line);
                    return result;
                }
            }
        }

        return null;
    }
}