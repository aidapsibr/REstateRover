using Newtonsoft.Json;
using REstate;
using REstate.Configuration;
using REstate.Configuration.Builder;
using REstate.Engine;
using REstate.Engine.Repositories;
using REstate.Engine.Repositories.InMemory;
using REstate.Engine.Services;
using REstate.IoC;
using REstate.IoC.TinyIoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rover.Console
{
    public class REstate
    {
        private REstate()
        {
            Container = new TinyIoCContainerAdapter(TinyIoCContainer.Current);

            Container.Register(new StringSerializer(
                serializer: (obj) => JsonConvert.SerializeObject(obj),
                deserializer: (str) => JsonConvert.DeserializeObject(str)));

            Container.Register<IConnectorFactoryResolver>(new DefaultConnectorFactoryResolver());

            Container.Register<IRepositoryContextFactory>(c =>
                new InMemoryRepositoryContextFactory(
                    c.Resolve<StringSerializer>()));

            Container.Register<ICartographer>(new DotGraphCartographer());

            Container.Register<IStateMachineFactory>(c =>
                new REstateMachineFactory(
                    c.Resolve<IConnectorFactoryResolver>(),
                    c.Resolve<IRepositoryContextFactory>(),
                    c.Resolve<ICartographer>()));

            Container.Register(c =>
                new StateEngine(
                    c.Resolve<IStateMachineFactory>(),
                    c.Resolve<IRepositoryContextFactory>(),
                    c.Resolve<StringSerializer>()));
        }

        private static IComponentContainer Container;

        private static Lazy<StateEngine> EngineInstance = new Lazy<StateEngine>(() =>
        {
            return Container.Resolve<StateEngine>();
        });

        public static StateEngine Engine => EngineInstance.Value;

        public static SchematicBuilder BuildSchematic(string schematicName) =>
            new SchematicBuilder(schematicName);
    }

    public class RoverMatrixConnector : IConnector
    {
        private IDictionary<ValueTuple<int, int>, bool> _matrix;

        public RoverMatrixConnector(IDictionary<ValueTuple<int, int>, bool> matrix)
        {
            _matrix = matrix;
        }

        public string ConnectorKey => "RoverMatrix";

        public Func<CancellationToken, Task> ConstructAction(IStateMachine machineInstance, State state, string contentType, string payload, IDictionary<string, string> configuration)
        {
            throw new NotImplementedException();
        }

        public Func<State, Trigger, string, CancellationToken, Task<bool>> ConstructPredicate(IStateMachine machineInstance, IDictionary<string, string> configuration) =>
            (state, trigger, payload, cancellationToken) =>
            {
                string[] coordinateParts = state.StateName.Split(',');

                int x = int.Parse(coordinateParts[0]);
                int y = int.Parse(coordinateParts[1]);

                string heading = coordinateParts[3];

                switch (heading)
                {
                    case "N":
                        y++;
                        break;
                    case "E":
                        x++;
                        break;
                    case "S":
                        y--;
                        break;
                    case "W":
                        x--;
                        break;
                }

                bool isValidMove = false;

                try
                {
                    isValidMove = _matrix[(x, y)];
                }
                catch
                {
                    //Assume out of bounds.
                }
                
                return Task.FromResult(isValidMove);
            };
    }

    class Program
    {
        static void Main(string[] args)
        {
            IDictionary<ValueTuple<int, int>, bool> matrix = new Dictionary<ValueTuple<int, int>, bool>();

            // Produce cross-product of coordinates with headings.
            var validStates = matrix
                .Where(matrixPair => matrixPair.Value)
                .SelectMany(
                    matrixPair => new[] { "N", "E", "S", "W" },
                    (matrixPair, heading) => $"{matrixPair.Key.Item1}, {matrixPair.Key.Item2}, {heading}");

            var initialState = "0,0,N";

            var schematic = REstate
                .BuildSchematic("Rover")
                .WithStates(validStates, state =>
                {
                    if (state.StateName == initialState)
                        state.AsInitialState();

                    bool isValid;
                    int x;
                    int y;
                    string heading;

                    (x, y, heading) = RotateState(state, Rotation.Clockwise);
                    state.WithTransition("RotateClockwise", $"{x},{y},{heading}");

                    (x, y, heading) = RotateState(state, Rotation.CounterClockwise);
                    state.WithTransition("RotateCounterClockwise", $"{x},{y},{heading}");

                    (x, y, heading) = TranslateState(state, Movement.Forward);
                    if (matrix.TryGetValue((x, y), out isValid) && isValid)
                    {
                        state.WithTransition("MoveForward", $"{x},{y},{heading}");
                    }

                    (x, y, heading) = TranslateState(state, Movement.Reverse);
                    if (matrix.TryGetValue((x, y), out isValid) && isValid)
                    {
                        state.WithTransition("MoveBackwards", $"{x},{y},{heading}");
                    }
                });

            var machine = REstate.Engine.InstantiateMachine(new Schematic
            {
                SchematicName = "",
                InitialState = "",
                StateConfigurations = new[]
                {
                    new StateConfiguration
                    {
                        StateName = "",
                        StateDescription = "",
                        Transitions = new []
                        {
                            new Transition
                            {
                                TriggerName = "",
                                ResultantStateName = ""
                            }
                        }
                    }
                }
            }, null, CancellationToken.None).Result;
        }

        private static (int X, int Y, string Heading) RotateState(IStateConfigurationBuilder state, Rotation direction)
        {
            string[] coordinateParts = state.StateName.Split(',');

            int x = int.Parse(coordinateParts[0]);
            int y = int.Parse(coordinateParts[1]);
            string heading = coordinateParts[3];
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
            string heading = coordinateParts[3];
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
