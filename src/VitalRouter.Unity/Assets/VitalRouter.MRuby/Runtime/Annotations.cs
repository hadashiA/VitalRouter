using System;

namespace VitalRouter.MRuby
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MRubyCommandAttribute : PreserveAttribute
    {
        public string Key { get; }
        public Type CommandType { get; }

        public MRubyCommandAttribute(string key, Type commandType)
        {
            Key = key;
            CommandType = commandType;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class MRubyObjectAttribute : PreserveAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class MRubyMemberAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class MRubyIgnoreAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Constructor)]
    public class MRubyConstructorAttribute : Attribute
    {
    }
}
