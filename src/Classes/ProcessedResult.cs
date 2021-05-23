using System;
using System.Collections.Generic;

namespace HugoCrossPoster.Classes
{
    public class ProcessedResultsCollection
    {
        public ThirdPartyService service;
        public List<ProcessedResultObject> results;
    }

    public class ProcessedResultObject
    {
        public Result result;
        public string jsonObject;
    }

    public enum Result
    {
        Success,
        Unauthorized,
        Unprocessable
    }
}