using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Machine.PrimitiveTypes
{
  public class Attributes
  {
    readonly static Dictionary<Type, ObjectAttribute[]> _cache = new Dictionary<Type, ObjectAttribute[]>();
    readonly static ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    public static IEnumerable<ObjectAttribute> For(Type type, IEnumerable<PropertyInfo> skipProperties)
    {
      _lock.EnterReadLock();
      var writing = false;
      try
      {
        if (!_cache.ContainsKey(type))
        {
          _lock.ExitReadLock();
          _lock.EnterWriteLock();
          writing = true;
          if (!_cache.ContainsKey(type))
          {
            _cache[type] = Get(type, skipProperties).ToArray();
          }
        }
        return _cache[type];
      }
      catch (Exception error)
      {
        throw new Exception("Error getting attributes for " + type, error);
      }
      finally
      {
        if (writing)
          _lock.ExitWriteLock();
        else
          _lock.ExitReadLock();
      }
    }

    static IEnumerable<ObjectAttribute> Get(Type type, IEnumerable<PropertyInfo> skipProperties)
    {
      if (SerializeFieldsAttribute.Has(type))
        return Fields(type, skipProperties);
      return Properties(type, skipProperties);
    }

    static IEnumerable<ObjectAttribute> Fields(Type type, IEnumerable<PropertyInfo> skipProperties)
    {
      var flags = BindingFlags.NonPublic | BindingFlags.Instance;
      var attributes = new List<ObjectAttribute>();
      var fields = type.GetFields(flags).
        Where(fi => fi.GetCustomAttributes(typeof(CompilerGeneratedAttribute), true).Length == 0).
        ToList();
      foreach (var field in fields)
      {
        attributes.Add(new ObjectAttribute {
          Name = field.Name.ToAttributeName(),
          AttributeType = field.FieldType,
          FieldInfo = field,
        });
      }
      return attributes;
    }

    static IEnumerable<ObjectAttribute> Properties(Type type, IEnumerable<PropertyInfo> skipProperties)
    {
      var flags = BindingFlags.Instance | BindingFlags.Public;
      var attributes = new List<ObjectAttribute>();
      var properties = type.GetProperties(flags).
        Where(property => property.GetIndexParameters().Length == 0).
        Where(p => !p.IsImplementationOfAny(skipProperties)).
        Where(p => p.CanWrite).
        ToList();
      foreach (var property in properties)
      {
        attributes.Add(new ObjectAttribute {
          Name = property.Name,
          AttributeType = property.PropertyType,
          PropertyInfo = property,
        });
      }
      return attributes;
    }
  }

  public class ObjectAttribute
  {
    public string Name { get; set; }
    public Type AttributeType { get; set; }
    public PropertyInfo PropertyInfo { get; set; }
    public FieldInfo FieldInfo { get; set; }
    
    public object Get(object instance)
    {
      if (FieldInfo != null)
        return FieldInfo.GetValue(instance);
      return PropertyInfo.GetValue(instance, new object[0]);
    }

    public void Set(object instance, object value)
    {
      if (FieldInfo != null)
        FieldInfo.SetValue(instance, value);
      else
        PropertyInfo.SetValue(instance, value, new object[0]);
    }
  }
}