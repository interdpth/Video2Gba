using System;
using System.Runtime.Serialization;

namespace Video2Gba.LibIpsNet.Exceptions
{
    [Serializable]
    public class IpsIdenticalException : Exception
    {
        public IpsIdenticalException()
            : base() { }

        public IpsIdenticalException(string message)
            : base(message) { }

        public IpsIdenticalException(string format, params object[] args)
            : base(string.Format(format, args)) { }

        public IpsIdenticalException(string message, Exception innerException)
            : base(message, innerException) { }

        public IpsIdenticalException(string format, Exception innerException, params object[] args)
            : base(string.Format(format, args), innerException) { }

        protected IpsIdenticalException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
