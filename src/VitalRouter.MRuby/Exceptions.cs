using System;

namespace VitalRouter.MRuby;

public class MRubyRoutingException : Exception
{
    public MRubyRoutingException(string message) : base(message)
    {
    }
}