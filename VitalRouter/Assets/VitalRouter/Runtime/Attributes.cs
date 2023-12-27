using System;

namespace VitalRouter;

public class PreserveAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class)]
public class RoutesAttribute : PreserveAttribute
{
}
