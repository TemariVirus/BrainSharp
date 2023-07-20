using System.Collections.Immutable;

static class Program
{
    static void Main(string[] args)
    {
        // Parse args
        int cellSize = 65536;
        bool circularBuffer = false;
        string? path = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-c":
                    circularBuffer = true;
                    break;
                case "-s":
                    cellSize = int.Parse(args[++i]);
                    break;
                default:
                    if (args[i].StartsWith('-'))
                    {
                        throw new Exception($"Unknown option: {args[i]}");
                    }

                    path = args[i];
                    break;
            }
        }

        if (path == null)
        {
            Console.WriteLine(
                "Usage: BrainSharp <OPTIONS> <filename>\nOptions:\n\t-c\tMake buffer circular\n\t-s <size>\tCell buffer size"
            );
            return;
        }
        if (!File.Exists(path))
        {
            Console.WriteLine($"File not found: {path}");
            return;
        }

        Run(path, cellSize, circularBuffer);
    }

    static void Run(string path, int cellSize, bool circularBuffer)
    {
        // Preprocess
        string code = File.ReadAllText(path);
        (code, var loopEnds) = Preprocess(code);

        // Setup
        Span<byte> cells = stackalloc byte[cellSize];
        int cellPtr = 0;
        Stack<int> stack = new();

        // Run
        for (int i = 0; i < code.Length; i++)
        {
            try
            {
                switch (code[i])
                {
                    case '>':
                        cellPtr++;
                        if (circularBuffer)
                        {
                            cellPtr %= cells.Length;
                        }
                        break;
                    case '<':
                        cellPtr--;
                        if (circularBuffer)
                        {
                            cellPtr += cells.Length;
                            cellPtr %= cells.Length;
                        }
                        break;
                    case '+':
                        cells[cellPtr]++;
                        break;
                    case '-':
                        cells[cellPtr]--;
                        break;
                    case '.':
                        Console.Write((char)cells[cellPtr]);
                        break;
                    case ',':
                        cells[cellPtr] = (byte)Console.Read();
                        break;
                    case '[':
                        if (cells[cellPtr] == 0)
                        {
                            i = loopEnds[i];
                        }
                        else
                        {
                            stack.Push(i);
                        }
                        break;
                    case ']':
                        if (cells[cellPtr] == 0)
                        {
                            stack.Pop();
                        }
                        else
                        {
                            i = stack.Peek();
                        }
                        break;
                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine(
                    $"Exception: Cell pointer was at an invalid position ({cellPtr}). \n\tat {path}:{i}"
                );
                return;
            }
        }
    }

    static (string, ImmutableDictionary<int, int>) Preprocess(string code)
    {
        code = new(code.Where(c => "><+-.,[]".Contains(c)).ToArray());
        Dictionary<int, int> loopEnds = new();
        for (int i = 0; i < code.Length; i++)
        {
            if (code[i] != '[')
            {
                continue;
            }

            int depth = 1;
            int j = i + 1;
            for (; j < code.Length & depth > 0; j++)
            {
                if (code[j] == '[')
                {
                    depth++;
                }
                else if (code[j] == ']')
                {
                    depth--;
                }
            }
            if (depth != 0)
            {
                throw new Exception("Unmatched '['");
            }
            loopEnds[i] = j - 1;
        }

        return (code, loopEnds.ToImmutableDictionary());
    }
}
