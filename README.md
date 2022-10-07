# Integration Testing ASP.NET Core 6 Web API
The following text is written in the style of a blog and not a traditional readme, which is why it is quite verbose, but hopefully helpful for anyone who would like to learn about this subject, as I did. The reader who is only interested in the implementation should focus on the code snippets below, or if experienced could go straight to the code in the test- and api-project. Enjoy.

## Introduction
Let's start simple. What is an integration test?

It can be understood in contrast to unit tests, which test only one function at a time, isolating it from its environment, controlling the input and state, then examining the result. Unit testing is a science.

Integration testing however is black magic.

Not really, but it involves so many parts it that when we first learned about it during early days of the [Salt .NET Fullstack Bootcamp](https://www.salt.study/our-hubs/stockholm/code-bootcamps), it did seem like black magic.

I recently took a course called `ASP.NET Core 6 Web API: Best Practices` on the magnificent website [Pluralsight](https://app.pluralsight.com/) and the veteran dev Steve Smith who held the course, did indeed describe integration testing in ASP.NET as "so easy, there is no excuse not to do it".

Bless you Steve, I hope I will be like you one day (triple namaste emoji).

The goal of this blog post is for the reader and also the author to understand integration tests as a science, and learn how to leverage some of the tools supplied to us by ASP.NET and EF Core.

## Disclaimer
I will make a lot of statements here, I gone did my best to make sure everything is accurate but my experience is limited and so is time. Therefore, reader beware the risk that I say some incorrect or incomplete tings. There, now I am free.

## What It Is
An integration test is making sure that the parts of a program work together, one picks a chain of units to test, inputs in one end, and asserts the result on the other end.

The environment must still be controlled, so the principle is similar to a unit test but the reality is more complex, the tests run slower and we encounter many different combinations of units and inputs to test. Too many! We will work smart, have trust in our unit tests, and greatly limit the number of integration tests we run.

![The pyramid of code testing](/screenshot-pyramid.webp)

## Why It Is
So, the reason we do integration tests is to see that our parts work together, and it allows it to test things unit tests can't, such as

- routing
- filters
- dependency injection
- model validation

and more if one thinks harder than I have.

## How Do It Good
The approach I am describing here applies to .NET 6, it builds upon knowledge from the aforementioned [Pluralsight course](https://app.pluralsight.com/library/courses/aspdotnet-core-6-web-api-best-practices/table-of-contents), on the excellent [Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-6.0) and last but not least on wisdom from Salt's very own [Marcus Hammarberg](https://www.marcusoft.net/2021/11/testing-webapi-with-aspnetcoremvctesting-and-xunit-collections.html).

I have broken it down into three parts
1. Configuring a `WebApplicationFactory` in a child class `WebApiApplication`, which is here referred to here either by name or as `factory`.
2. Using EF Core to seed the database
3. Setting up a xUnit test project

What we will do is use a WebApplicationFactory to create in **in-memory** real instance of our app, all our units fully integrated. The phrase "in-memory" was confusing to me, don't all apps run in memory? Yes, but this one runs in the same process as our test project, even though they are separate projects!

That means that our two projects don't need to communicate over localhost HTTP, for us this means speedy tests.

### 1. WebApplicationFactory

This is the difficult part, but the beautiful thing we will see is that the solution is so general, that if we only solve it once we can continue using the same solution for all the integration tests we want.

The NuGet package `Microsoft.AspNetCore.Mvc.Testing` provides us with this application factory, I will dump the code to configure it here, then reflect on it part by part.

```cs
class WebApiApplication : WebApplicationFactory<Program>
{
  protected override IHost CreateHost(IHostBuilder builder)
  {
    // 1.
    builder.UseEnvironment("Testing");

    // NOTE Here we could use builder.ConfigureLogging to configure logging, which I understand is how we can read what is logged inside our API, even though it is running in-memory of the test.

    builder.ConfigureServices(services => 
    {
      // 2.
      var descriptor = services.SingleOrDefault(
        d => d.ServiceType == typeof(DbContextOptions<ItemsContext>)
      );
      services.Remove(descriptor);
      
      // 3.
      string dbName = "DbForTesting";
      services.AddDbContext<ItemsContext>(options =>
      {
        options.UseInMemoryDatabase(dbName);
      });

      // 4.
      var serviceProvider = services.BuildServiceProvider();
      using (var scope = serviceProvider.CreateScope())
      using (var dbContext = scope.ServiceProvider.GetRequiredService<ItemsContext>())
      {
        try
        {
          // NOTE Using EnsureCreated is not recommended for relational db if one plans to use EF Migrations, see MS Docs link in end
          dbContext.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
          // TODO Log error here
          throw;
        }
      }
    });    

    // 5.
    return base.CreateHost(builder);
  }
}
```

What we first do is define our own class WebApiApplication, a child of WebApplicationFactory<Program>. The type `Program` here is the Program class from our API, it tells WebApplicationFactory that this is where the entry point to our API is and that is the program it should build.

Here is a difference from earlier versions of .NET, the Program class is now implicit and hidden. We must make sure that our test project can find it, and the way we do that is to go to the csproj of the API and add these lines

```html
<ItemGroup>
  <InternalsVisibleTo Include="WebApi.Tests" />
</ItemGroup>
```

Easy!

The next thing we do is override the `CreateHost` method, inside this method we do five things.

1. Set the environment to "Testing". This could be used inside the API for conditional behavior.
2. In ConfigureServices we find and remove the normal database context,
3. then replace it with a context connecting to an in-memory database (supplied to us by EF Core).
4. We create a scope which is necessary because inside it we can call appContext.Database.EnsureCreated() which in turn will trigger SeedData to populate our DB, more on this later.
5. Finally, we call the parent to make sure we also run its default CreateHost

Factory complete.

### 2. Seed Database
This is something we will set up in our WebApi, even though the main reason we want to do this (right now anyway) is for testing purposes.

For each instance of the test class we will have a fresh new and empty database, since it runs in memory, EF Core provides tools for that let us automatically seed it with values we can test. First, we create a static class that returns a collection of items for us, instances of our Model to store in the DB.

```cs
internal class SeedData
{
  internal static Item[] Items()
  {
    var id = 1;

    var items = new Item[]
    {
      new Item
      {
        Id = id++,
        Name = "Item1",
        IsComplete = false
      },
      new Item
      {
        Id = id++,
        Name = "Item2",
        IsComplete = false
      }
    };

    return items;
  }
}
```

Now, remember how inside the factory we called `appContext.Database.EnsureCreated()` earlier? That will create our in-memory DB and call a method called `OnModelCreating` on our `DbContext` object. We override that method like so:

```cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
  base.OnModelCreating(modelBuilder);

  modelBuilder.Entity<Item>().HasData(SeedData.Items());
}
```

`HasData` seeds the DB with the collection it is given by `SeedData.Items()`. Seeding complete, we are now ready to write our tests!

### 3. xUnit Test Project

A reason xUnit is a good choice over something like nUnit is that xUnit creates a new instance of the test class for each test that is run. 

This means that our tests are independent of each other, with no side effects across and we can even put common code in the test class constructor, without sharing the object instances, very useful!

Let's look at the actual test.

```cs
public class ItemsControllerTests
{
  private readonly HttpClient _client;

  public ItemsControllerTests()
  {
    var factory = new WebApiApplication();
    _client = factory.CreateClient();
  }

  [Fact]
  public async Task api_should_return_items()
  {
    // Arrange
    var items = SeedData.Items();

    // Act
    var response = await _client.GetFromJsonAsync<Item[]>("/api/items");

    // Assert
    response.Should().BeEquivalentTo(items);
  }
}
```

The factory creates an instance of our app, and the client is how we send it HTTP-requests, just as we talk to a normal API.

With a configured factory, and having made sure the in-memory test DB has data, we are free to call the client with any request we can think of and assert the output, it really couldn't be simpler.

Here we can also note a second use of our SeedData class is we can actually call it from our test to get the collection of resources we know are supposed to be be in the DB, this relieves us from hard coding any resource data in our test, and avoid use of magic strings, so elegant!

![screenshot of terminal indicating passing tests](/screenshot.png)

And look at that speed, what will we do with all the time we have now?? So much time for activities!

# About Changes From [Marcusofts](https://www.marcusoft.net/2021/11/testing-webapi-with-aspnetcoremvctesting-and-xunit-collections.html) implementation, and a Finishing Word
Since this post is aimed towards salties I will make some comments about how and why things are different from Marcus' solution. 

- His solution targets .NET 5, mine targets .NET 6
- His test class makes use of [IClassFixture](https://xunit.net/docs/shared-context#class-fixture) to share the WebApplicationFactory instance between tests. I removed this because
  - a) I prefer to have a new instance for each test in order to fully decouple the in-memory app state and
  - b) it is simpler, and simplicity is king **if it accomplishes what we need**
- The data seeding is done differently to instead use EF Core's built in way, in line with [this article](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding?source=recommendations), which I believe is a new feature.

I put a solid amount of research into this, but I cannot understate how smoothly everything went. The test, the seeder, the factory everything ran near perfectly the very first run, this only happens to me in C#, I am telling you this .NET thing will be big.

# Resources
1. https://app.pluralsight.com/library/courses/aspdotnet-core-6-web-api-best-practices/table-of-contents (NOT FREE)
2. https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-6.0
3. https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding?source=recommendations
4. https://www.marcusoft.net/2021/11/testing-webapi-with-aspnetcoremvctesting-and-xunit-collections.html
