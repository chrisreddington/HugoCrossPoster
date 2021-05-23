using System;
using System.Runtime.Serialization;

namespace HugoCrossPoster.Classes
{
  [Serializable]
  public class UnprocessableEntityException : Exception
  {
    private UnprocessableEntityException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
      // ...
    }

    public UnprocessableEntityException()
    {
    }

    public UnprocessableEntityException(string message)
        : base(message)
    {
    }

    public UnprocessableEntityException(string message, Exception inner)
        : base(message, inner)
    {
    }
  }
}