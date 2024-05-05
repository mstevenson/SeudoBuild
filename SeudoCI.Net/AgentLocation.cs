namespace SeudoCI.Net
{
    /// <summary>
    /// Describes a running build agent process.
    /// </summary>
    public class AgentLocation
    {
        public string AgentName { get; set; }
        public string Address { get; set; }

        public override int GetHashCode()
        {
            return AgentName.GetHashCode() * 17 + Address.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var item = obj as AgentLocation;

            if (item == null)
            {
                return false;
            }

            return AgentName.Equals(item.AgentName) && Address.Equals(item.Address);
        }
    }
}
