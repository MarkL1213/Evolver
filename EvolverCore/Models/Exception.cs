using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore
{
    public class EvolverException : Exception
    {
        public EvolverException() : base() { }
        public EvolverException(string? message) : base(message) { }
        public EvolverException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
