using System;

namespace RemoteTechWormholeBridge.Core
{
    public sealed class RegistryIssue
    {
        public RegistryIssue(string subject, string message)
        {
            Subject = subject ?? String.Empty;
            Message = message ?? String.Empty;
        }

        public string Subject { get; private set; }
        public string Message { get; private set; }

        public override string ToString()
        {
            return Subject + ": " + Message;
        }
    }
}
