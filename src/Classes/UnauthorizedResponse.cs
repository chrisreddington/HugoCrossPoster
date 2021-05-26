using System;
using System.Runtime.Serialization;

namespace HugoCrossPoster.Classes
{
  [Serializable]
  public class UnauthorizedResponseException : Exception
  {
    private UnauthorizedResponseException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
      // ...
    }

    public UnauthorizedResponseException()
    {
    }

    public UnauthorizedResponseException(string message)
        : base(message)
    {
    }

    public UnauthorizedResponseException(string message, Exception inner)
        : base(message, inner)
    {
    }
  }
}