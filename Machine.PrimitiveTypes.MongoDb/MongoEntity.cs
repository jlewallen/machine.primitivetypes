using System;
using System.Collections.Generic;

namespace Machine.PrimitiveTypes.MongoDb
{
  public class MongoEntity
  {
    public static MongoEntity<T> New<T>(T entity)
    {
      return new MongoEntity<T>(entity);
    }
  }

  public class MongoEntity<T>
  {
    public T Entity
    {
      get;
      set;
    }

    public System.Collections.IDictionary Attributes
    {
      get;
      set;
    }

    public MongoEntity(T entity, System.Collections.IDictionary attributes)
    {
      Entity = entity;
      Attributes = attributes;
    }

    public MongoEntity(T entity)
    {
      Entity = entity;
      Attributes = null;
    }
  }
}
