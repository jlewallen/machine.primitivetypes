using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Machine.PrimitiveTypes
{
  public abstract class PrimitiveTypeGraphConverter<TD>
  {
    readonly Dictionary<Type, Func<object, object>> _toPrimitiveType = new Dictionary<Type, Func<object, object>>();
    readonly Dictionary<Type, Func<object, object>> _fromPrimitiveType = new Dictionary<Type, Func<object, object>>();
    readonly List<PropertyInfo> _skipProperties = new List<PropertyInfo>();
    readonly Func<TD> _dictionaryFactory;

    protected PrimitiveTypeGraphConverter(Func<TD> dictionaryFactory)
    {
      _dictionaryFactory = dictionaryFactory;
      Allow<string>();
      Allow<Int32>();
      Allow<Int16>();
      Allow<Int64>();
      Allow<Decimal>();
      Allow<Single>();
      Allow<Double>();
      Allow<Boolean>();
      Allow<Guid>();
      Allow<DateTime>();
    }

    public abstract void AddToDictionary(TD dictionary, string key, object value);

    public abstract bool IsNull(Type type, object value);

    public void Allow<T>()
    {
      Override(typeof(T), value => value, value => value);
    }

    public void Skip<T>(Expression<Func<T, object>> expression)
    {
      var member = expression.Body as MemberExpression;
      if (member != null)
      {
        var propertyInfo = member.Member as PropertyInfo;
        if (propertyInfo != null)
        {
          _skipProperties.Add(propertyInfo);
          return;
        }
      }
      throw new ArgumentException("Must pass a Property expression");
    }

    public void Override<T>(Func<object, object> overrideTo, Func<object, object> overrideFrom)
    {
      Override(typeof(T), overrideTo, overrideFrom);
    }

    public void Override(Type type, Func<object, object> overrideTo, Func<object, object> overrideFrom)
    {
      _toPrimitiveType[type] = overrideTo;
      _fromPrimitiveType[type] = overrideFrom;
    }

    object ToSimpleType(Type type, object value)
    {
      try
      {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
          return ToSimpleType(type.GetGenericArguments().First(), value);
        }
        if (_toPrimitiveType.ContainsKey(type))
        {
          return _toPrimitiveType[type](value);
        }
        if (type.IsEnum)
        {
          return value.ToString();
        }
        if (typeof(IDictionary).IsAssignableFrom(type))
        {
          var dictionary = (IDictionary)value;
          var converted = _dictionaryFactory();
          foreach (var key in dictionary.Keys)
          {
            var dictionaryValue = dictionary[key];
            var convertedKey = ToSimpleType(key.GetType(), key).ToString();
            var convertedValue = ToSimpleType(dictionaryValue.GetType(), dictionaryValue);
            AddToDictionary(converted, convertedKey, convertedValue);
          }
          return converted;
        }
        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
          var itemType = typeof(object);
          var enumerable = (IEnumerable)value;
          var converted = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
          foreach (var item in enumerable)
          {
            converted.Add(ToSimpleType(item.GetType(), item));
          }
          return converted;
        }
        return ObjectToDictionary(type, value);
      }
      catch (Exception error)
      {
        throw new Exception("Error converting to JSON: " + type, error);
      }
    }

    TD ObjectToDictionary(Type type, object value)
    {
      if (value == null)
        return default(TD);
      var dictionary = _dictionaryFactory();
      var attributes = Attributes.For(type, _skipProperties);
      foreach (var attribute in attributes)
      {
        var propertyValue = attribute.Get(value);
        if (propertyValue != null)
        {
          AddToDictionary(dictionary, attribute.Name, ToSimpleType(attribute.AttributeType, propertyValue));
        }
      }
      return dictionary;
    }

    static object Coerce(object value, Type sourceType, Type destinyType)
    {
      var sourceConverter = TypeDescriptor.GetConverter(sourceType);
      var destinyConverter = TypeDescriptor.GetConverter(destinyType);
      return destinyConverter.ConvertFromInvariantString(sourceConverter.ConvertToInvariantString(value));
    }

    object FromPrimitiveType(Type type, object value)
    {
      try
      {
        if (IsNull(type, value))
        {
          return null;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
          return FromPrimitiveType(type.GetGenericArguments().First(), value);
        }
        if (_fromPrimitiveType.ContainsKey(type))
        {
          var actual = _fromPrimitiveType[type](value);
          var actualType = actual.GetType();
          if (actualType != type)
            return Coerce(actual, actualType, type);
          return actual;
        }
        if (type.IsEnum)
        {
          return Enum.Parse(type, value.ToString().Replace("Recievable", "Receivable"));
        }
        if (type.IsArray)
        {
          var collection = (ICollection)value;
          var converted = Array.CreateInstance(type.GetElementType(), collection.Count);
          var i = 0;
          foreach (var item in collection)
          {
            converted.SetValue(FromPrimitiveType(type.GetElementType(), item), i++);
          }
          return converted;
        }
        if (typeof(IDictionary).IsAssignableFrom(type))
        {
          var ifaceType = type.FindInterfaces((o, p) => true, null).
            Where(t => t.IsGenericType).
            Where(t => t.GetGenericTypeDefinition() == typeof(IDictionary<,>)).
            Single();
          var keyType = ifaceType.GetGenericArguments().First();
          var valueType = ifaceType.GetGenericArguments().Last();
          var dictionary = (IDictionary)value;
          var converted = (IDictionary)Activator.CreateInstance(type);
          foreach (var key in dictionary.Keys)
          {
            var convertedKey = FromPrimitiveType(keyType, key);
            converted[convertedKey] = FromPrimitiveType(valueType, dictionary[key]);
          }
          return converted;
        }
        if (typeof(IList).IsAssignableFrom(type))
        {
          var enumerable = (IEnumerable)value;
          var converted = (IList)Activator.CreateInstance(type);
          var itemType = type.GetGenericArguments().First();
          foreach (var item in enumerable)
          {
            converted.Add(FromPrimitiveType(itemType, item));
          }
          return converted;
        }
        return DictionaryToObject(type, (IDictionary)value);
      }
      catch (Exception error)
      {
        throw new Exception("Error converting from JSON: " + type, error);
      }
    }

    object DictionaryToObject(Type type, IDictionary dictionary)
    {
      if (dictionary == null)
        return null;
      var converted = new Dictionary<string, object>();
      var attributes = Attributes.For(type, _skipProperties);
      foreach (var attribute in attributes)
      {
        var value = FromPrimitiveType(attribute.AttributeType, dictionary[attribute.Name]);
        converted[attribute.Name] = value;
      }
      try
      {
        var accessorsByName = attributes.ToDictionary(k => k.Name);
        var instance = CreateInstanceOf(type, converted);
        foreach (var entry in converted)
        {
          var attribute = accessorsByName[entry.Key];
          try
          {
            attribute.Set(instance, entry.Value);
          }
          catch (Exception error)
          {
            throw new Exception("Error setting attribute " + attribute.Name, error);
          }
        }
        return instance;
      }
      catch (Exception error)
      {
        throw new Exception("Error converting " + type, error);
      }
    }

    public object ToPrimitiveTypeGraph<T>(T value)
    {
      return ToSimpleType(typeof(T), value);
    }

    public T FromPrimitiveTypeGraph<T>(object value)
    {
      return (T)FromPrimitiveType(typeof(T), value);
    }

    static object CreateInstanceOf(Type type, IEnumerable<KeyValuePair<string, object>> attributes)
    {
      var lowercaseKeys = attributes.ToDictionary(k => k.Key.ToLower(), v => v.Value);
      var ctors = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
      foreach (var ctor in ctors.OrderByDescending(c => c.GetParameters().Length))
      {
        var parameters = new List<object>();
        foreach (var parameter in ctor.GetParameters().Select(p => p.Name.ToLower()))
        {
          if (lowercaseKeys.ContainsKey(parameter))
          {
            parameters.Add(lowercaseKeys[parameter]);
          }
          else
          {
            parameters = null;
            break;
          }
        }
        if (parameters != null)
        {
          return ctor.Invoke(parameters.ToArray());
        }
      }
      return Activator.CreateInstance(type);
    }
  }

  public static class StringHelpers
  {
    public static string ToAttributeName(this string value)
    {
      if (value.StartsWith("_"))
        value = value.Substring(1);
      return Char.ToUpper(value[0]) + value.Substring(1);
    }
  }

  public static class TypeHelpers
  {
    public static bool IsImplementationOfAny(this PropertyInfo property, IEnumerable<PropertyInfo> ifaceProperties)
    {
      return ifaceProperties.Aggregate(false, (a, b) => a || property.IsImplementationOf(b));
    }

    public static bool IsImplementationOf(this PropertyInfo property, PropertyInfo ifaceProperty)
    {
      if (!ifaceProperty.DeclaringType.IsAssignableFrom(property.DeclaringType))
        return false;
      var map = property.DeclaringType.GetInterfaceMap(ifaceProperty.DeclaringType);
      var getMethod = ifaceProperty.GetGetMethod();
      var setMethod = ifaceProperty.GetSetMethod();
      var getMethodIndex = Array.IndexOf(map.InterfaceMethods, getMethod);
      var setMethodIndex = Array.IndexOf(map.InterfaceMethods, setMethod);
      return property.GetGetMethod() == map.TargetMethods[getMethodIndex] &&
             property.GetSetMethod() == map.TargetMethods[setMethodIndex];
    }
  }
}
