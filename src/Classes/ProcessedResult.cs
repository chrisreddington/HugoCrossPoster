using System.Collections.Generic;

namespace HugoCrossPoster.Classes
{
  public class ProcessedResultsCollection
  {
    public ThirdPartyService service { get; set; }
    public List<ProcessedResultObject> results { get; set; }
  }

  public class ProcessedResultObject
  {
    public Result result { get; set; }
    public string jsonObject { get; set; }
  }

  public enum Result
  {
    Success,
    Unauthorized,
    Unprocessable
  }
}