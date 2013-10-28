using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfApplicationLauncher.DataSourcing.FileSearchProvider
{
    /// <summary>
    /// Container for executable and arguments details
    /// </summary>
    public class ExecutableContext
    {
        public string Name { get; set; }
        public string Executable { get; set; }
        public string Arguments { get; set; }

        public ExecutableContext()
        {
            Name = string.Empty;
            Executable = string.Empty;
            Arguments = string.Empty;
        }

        /// <summary>
        /// A String.Format like method for ExecutableContext with marked-up content.
        /// </summary>
        /// <param name="args">The arguments to use when Formatting the new ExecutableContext.</param>
        /// <returns>A new ExecutableContext whose members are not marked-up.</returns>
        public ExecutableContext Format(params string[] args)
        {
            ExecutableContext retVal = new ExecutableContext();

            retVal.Name = this.Name;
            retVal.Executable = String.Format(this.Executable, args);
            retVal.Arguments = String.Format(this.Arguments, args);

            return retVal;
        }

        public override string ToString()
        {
            return String.IsNullOrEmpty(Name) ? base.ToString() : Name;
        }
    }
}
