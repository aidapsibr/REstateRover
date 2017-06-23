using REstate;
using REstate.Configuration;
using REstate.Configuration.Builder;
using REstate.Engine;
using REstate.Engine.Services;
using Rover.Console.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rover.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            (int xBound, int yBound) = (100, 100);

            bool[][] matrix = ConstructMatrix(xBound, yBound, seed: 203);

            List<string> states = CreateStates(xBound, yBound, matrix);

            var initialState = "0,0,N";

            var schematic =
                new SchematicBuilder("Rover")
                .WithStates(states, s =>
                {
                    if (s.StateName == initialState)
                        s.AsInitialState();

                    int x;
                    int y;
                    string heading;

                    (x, y, heading) = RotateState(s, Rotation.Clockwise);
                    s.WithTransition("r", $"{x},{y},{heading}");

                    (x, y, heading) = RotateState(s, Rotation.CounterClockwise);
                    s.WithTransition("l", $"{x},{y},{heading}");

                    (x, y, heading) = TranslateState(s, Movement.Forward);
                    if (x >= 0 && x <= matrix.Length - 1 && y >= 0 && y <= matrix[x].Length - 1 && matrix[x][y])
                    {
                        s.WithTransition("f", $"{x},{y},{heading}");
                    }

                    (x, y, heading) = TranslateState(s, Movement.Reverse);
                    if (x >= 0 && x <= matrix.Length - 1 && y >= 0 && y <= matrix[x].Length - 1 && matrix[x][y])
                    {
                        s.WithTransition("b", $"{x},{y},{heading}");
                    }

                    s.WithOnEntryConnector(new EntryConnector
                    {
                        ConnectorKey = "ConsoleWriter",
                        Description = "Writes rover position to the console.",
                        Configuration = new Dictionary<string, string>
                        {
                            ["Format"] = "Rover moved to position and heading {0}."
                        }
                    });
                })
                .ToSchematic();

            var machine = REstate.Engine.CreateMachine(schematic, null, CancellationToken.None).Result;

            System.Console.WriteLine("Enter f, b, r, or l to navigate the plateau.");

            while (true)
            {
                var input = System.Console.ReadLine().ToLowerInvariant();

                if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
                    break;

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                try
                {
                    machine.FireAsync(input, null, CancellationToken.None).Wait();
                }
                catch (InvalidOperationException invalidException)
                {
                    System.Console.WriteLine(invalidException.Message);
                }
                catch (AggregateException exception)
                    when (exception.InnerException is InvalidOperationException invalidException)
                {
                    System.Console.WriteLine(invalidException.Message);
                }
            }
        }

        private static List<string> CreateStates(int xBound, int yBound, bool[][] matrix)
        {
            List<string> states = new List<string>(xBound * yBound);

            string[] headings = new[] { "N", "E", "S", "W" };

            for (int x = 0; x < xBound; x++)
                for (int y = 0; y < yBound; y++)
                {
                    if (matrix[x][y])
                        for (int i = 0; i < headings.Length; i++)
                            states.Add($"{x},{y},{headings[i]}");

                }

            return states;
        }

        private static bool[][] ConstructMatrix(int xBound, int yBound, int numberOfObstructions = 50, int? seed = null)
        {
            var random = seed != null ? new Random(seed.Value) : new Random();

            var obstructions = new (int, int)[numberOfObstructions];

            for (int i = 0; i < numberOfObstructions; i++)
                obstructions[i] = (random.Next(0, xBound), random.Next(0, yBound));

            bool[][] matrix = new bool[xBound][];

            for (int x = 0; x < xBound; x++)
            {
                matrix[x] = new bool[yBound];

                for (int y = 0; y < yBound; y++)
                {
                    matrix[x][y] = !obstructions.Contains((x, y));
                }
            }

            return matrix;
        }

        private static (int X, int Y, string Heading) RotateState(IStateConfigurationBuilder state, Rotation direction)
        {
            string[] coordinateParts = state.StateName.Split(',');

            int x = int.Parse(coordinateParts[0]);
            int y = int.Parse(coordinateParts[1]);
            string heading = coordinateParts[2];
            switch (heading)
            {
                case "N":
                    heading = direction == Rotation.Clockwise ? "E" : "W";
                    break;
                case "E":
                    heading = direction == Rotation.Clockwise ? "S" : "N";
                    break;
                case "S":
                    heading = direction == Rotation.Clockwise ? "W" : "E";
                    break;
                case "W":
                    heading = direction == Rotation.Clockwise ? "N" : "S";
                    break;
            }

            return (x, y, heading);
        }

        private static (int X, int Y, string Heading) TranslateState(IStateConfigurationBuilder state, Movement direction)
        {
            string[] coordinateParts = state.StateName.Split(',');

            int x = int.Parse(coordinateParts[0]);
            int y = int.Parse(coordinateParts[1]);
            string heading = coordinateParts[2];
            switch (heading)
            {
                case "N":
                    y += (int)direction;
                    break;
                case "E":
                    x += (int)direction;
                    break;
                case "S":
                    y -= (int)direction;
                    break;
                case "W":
                    x -= (int)direction;
                    break;
            }

            return (x, y, heading);
        }

        public enum Movement : int
        {
            Forward = 1,
            Reverse = -1,
        }

        public enum Rotation : int
        {
            Clockwise = 1,
            CounterClockwise = -1,
        }
    }
}
