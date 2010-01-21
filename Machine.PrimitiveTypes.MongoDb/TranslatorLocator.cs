using System;

namespace Machine.PrimitiveTypes.MongoDb
{
  public class TranslatorLocator : ITranslatorLocator
  {
    public ITranslateEntities<T> FindEntityTranslator<T>()
    {
      return (ITranslateEntities<T>)Activator.CreateInstance(typeof(DefaultEntityTranslator<>).MakeGenericType(typeof(T)));
    }
  }
}