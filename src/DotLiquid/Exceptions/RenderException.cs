using System;

namespace DotLiquid.Exceptions
{
    [Serializable]
    public abstract class RenderException : Exception
    {
        protected RenderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected RenderException(string message)
            : base(message)
        {
        }

        protected RenderException()
        {
        }
    }
}
