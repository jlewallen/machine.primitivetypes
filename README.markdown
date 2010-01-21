Not sure how useful this really is.

What It Does
---------

Basically, this project's goal is to transform an object hierarchy back and forth between a kind of intermediate representation made up of only primitive types, Lists and Dictionaries. No type information is preserved. The resulting object graph can be very easily serialized to JSON, for example. I wrote this because I found myself rewriting this kind of code often. It's much easier to think of serialization/persistence in the context of a JSON like data structure and not have to be writing the code over and over to transform between them.

It has the ability to deal with a variety of object graph styles.

Examples
---------

    public class Person
    {
      public string Name { get; set; }
      public DateTime Birthday { get; set; }
    }

    [SerializeFields]
    public class Person
    {
      readonly string _name;
      readonly DateTime _birthday;

      public Person(string name, DateTime birthday)
      {
        _name = name;
        _birthday = birthday;
      }
    }

    new Person('Jacob', DateTime.Parse('4/23/1982'));

Both of the above class will produce the same resulting intermediate form:

    {
      'Name': 'Jacob',
      'Birthday': '4/23/1982 12:00AM'
    }

Deserializing will also work (the declared constructor will be selected and it'll do its best to match up parameters) Arrays and child objects are also supported:

    public class Post
    {
      public string Body { get; set; }
      public List<Comment> Comments { get; set; }
    }

    public class Comment
    {
      public string Body { get; set; }
    }

    new Post {
      Body = 'I don't blog anymore, I suck',
      Comments = new List<Comment> {
        new Comment { Body = 'Was that a post, though?' },
        new Comment { Body = 'You suck at blogging and not blogging, haha!' }
      }
    }

Will yield:

    {
      'Body': 'I don't blog anymore, I suck.',
      'Comments': [
        { 'Body': 'Was that a post, though?' },
        { 'Body', 'You suck at blogging and not blogging, haha!' }
      ]
    }

I didn't really tests these examples... So... I hope they DO work...
