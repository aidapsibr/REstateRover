using System;
using System.Collections.Generic;

namespace REstate.Configuration.Builder
{
    public interface IStateConfigurationBuilder
    {
        string StateName { get; }

        string ParentStateName { get; }

        string StateDescription { get; }

        EntryConnector OnEntry { get; }
        
        IDictionary<string, Transition> Transitions { get; }

        IStateConfigurationBuilder AsInitialState();

        IStateConfigurationBuilder WithTransition(string triggerName, string resultantStateName, GuardConnector guard = null)
    }

    public class StateConfigurationBuilder : IStateConfigurationBuilder
    {
        private SchematicBuilder _builder;

        public StateConfigurationBuilder(SchematicBuilder builder, string stateName)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));

            if (stateName == null)
                throw new ArgumentNullException(nameof(stateName));
            if (string.IsNullOrWhiteSpace(stateName))
                throw new ArgumentException("No value provided.", nameof(stateName));

            StateName = stateName;
        }

        public string StateName { get; }
        public string ParentStateName { get; private set; }
        public string StateDescription { get; private set; }
        public IDictionary<string, Transition> Transitions { get; } = new Dictionary<string, Transition>();
        public EntryConnector OnEntry { get; private set; }

        public IStateConfigurationBuilder AsInitialState()
        {
            _builder.SetInitialState(StateName);

            return this;
        }

        public IStateConfigurationBuilder WithTransition(string triggerName, string resultantStateName, GuardConnector guard = null)
        {
            if (triggerName == null)
                throw new ArgumentNullException(nameof(triggerName));
            if (string.IsNullOrWhiteSpace(triggerName))
                throw new ArgumentException("No value provided.", nameof(triggerName));

            if (resultantStateName == null)
                throw new ArgumentNullException(nameof(resultantStateName));
            if (string.IsNullOrWhiteSpace(resultantStateName))
                throw new ArgumentException("No value provided.", nameof(resultantStateName));

            try
            {
                Transitions.Add(triggerName, new Transition
                {
                    TriggerName = triggerName,
                    Guard = guard,
                    ResultantStateName = resultantStateName
                });
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException($"An trigger matching: [ {triggerName} ] is already defined on state: [ {StateName} ]", ex);
            }

            return this;
        }
    }
}