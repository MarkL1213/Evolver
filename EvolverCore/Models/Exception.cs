using System;

namespace EvolverCore
{
    public class EvolverException : Exception
    {
        public EvolverException() : base() { }
        public EvolverException(string? message) : base(message) { }
        public EvolverException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
