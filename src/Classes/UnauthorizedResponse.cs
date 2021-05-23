using System;

namespace HugoCrossPoster.Classes
{
  public class UnauthorizedResponseException : Exception
  {
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