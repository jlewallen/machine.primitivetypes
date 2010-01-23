using System;
using MongoDB.Driver;

namespace Machine.PrimitiveTypes.MongoDb
{
  public class TranslatorLocator : ITranslatorLocator
  {
    public ITranslateEntities<T> FindEntityTranslator<T>()
    {
      return (ITranslateEntities<T>)Activator.CreateInstance(typeof(DefaultEntityTranslator<>).MakeGenericType(typeof(T)));
    }

    public ITranslateEntities<object> FindEntityTranslator(Type type)
    {
      var translator = Activator.CreateInstance(typeof(DefaultEntityTranslator<>).MakeGenericType(type));
      var wrapperType = typeof(TranslateEntitiesWrapper<>).MakeGenericType(type);
      return (ITranslateEntities<object>)Activator.CreateInstance(wrapperType, translator);
    }

    public class TranslateEntitiesWrapper<T> : ITranslateEntities<object>
    {
      readonly ITranslateEntities<T> _target;

      public TranslateEntitiesWrapper(ITranslateEntities<T> target)
      {
        _target = target;
      }

      public MongoEntity<object> Translate(Document from)
      {
        var translated = _target.Translate(from);
        return new MongoEntity<object>(translated.Entity, translated.Attributes);
      }

      public Document Translate(MongoEntity<object> from)
      {
        return _target.Translate(new MongoEntity<T>((T)from.Entity, from.Attributes));
      }
    }
  }
}