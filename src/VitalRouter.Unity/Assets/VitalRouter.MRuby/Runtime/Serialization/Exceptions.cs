using System;

namespace VitalRouter.MRuby
{
    public class MRubySerializationException : Exception
    {
        public MRubySerializationException(string message) : base(message) { }
    }
}