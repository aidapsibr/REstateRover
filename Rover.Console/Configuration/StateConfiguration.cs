using REstate.Configuration.Builder;
using System.ComponentModel.DataAnnotations;

namespace REstate.Configuration
{
    public class StateConfiguration : IStateConfigurationBuilder
    {
        [Required]
        public string StateName { get; set; }
        public string ParentStateName { get; set; }
        public string StateDescription { get; set; }
        public Transition[] Transitions { get; set; }
        public EntryConnector OnEntry { get; set; }
    }
}
