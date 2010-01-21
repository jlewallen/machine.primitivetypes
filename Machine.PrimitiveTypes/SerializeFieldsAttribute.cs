using System;
using System.Collections.Generic;

namespace Machine.PrimitiveTypes
{
  public class SerializeFieldsAttribute : Attribute
  {
    public static bool Has(Type type)
    {
      return type.GetCustomAttributes(typeof(SerializeFieldsAttribute), true).Length > 0;
    }
  }
}
