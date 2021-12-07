# FSharp.Data.DACFx.MSBuild

An MSBuild tools package to unite [SSDT](https://visualstudio.microsoft.com/vs/features/ssdt/) and type providers like [FSharp.Data.SqlClient](http://fsprojects.github.io/FSharp.Data.SqlClient/).

## What?

This package is an MSBuild target which will build and publish changes to an SSDT project so that it's available to a type provider.

## Why?
I wanted a development experience where I can make a tweak to a table definition and see how I've broken my application without running a thing. 
SSDT database projects and FSharp.Data.SqlClient gave me most of that, but the experience of connecting them wasn't as neat as the experience of using them.
That's what `FSharp.Data.DACFx.MSBuild` fixes.

I'm not sure I'm going to change the way you work in the README of a library, but for those of you who are curious about why these two technologies…

### Why [SSDT](https://visualstudio.microsoft.com/vs/features/ssdt/)?
I started using SSDT because I wanted to try out state-based database development as I'd found migration-based development irksome.

(With **migration-based** development you write (or generate) change scripts to evolve your database schema.
With **state-based** development you declare a desired state and get some deploy-time tool to figure out how to get there.)

I worked on a database-heavy project for a while with lots and lots of tables with lots and lots of churn on the schema.
We used Liquibase for managing migrations. It was pretty good, though tending to the migrations by hand was a chore even though they were boring changes 95% of the time.
We'd also have to maintain [POCOs](https://en.wikipedia.org/wiki/Plain_old_CLR_object) which exactly mirrored our database schema, so it was like we were writing our schema twice.

I've also used Entity Framework's migration in a bunch of web apps. Also fine. Less effort with auto-generated migrations and there's no duplicating schema when using code-first.
While the (lack of) virtues of using an ORM in your application is a different discussion, I do feel some of the issues reveal themselves just on the change-making side—I find SQL does a better job of expressing SQL things than C#.
Sometimes, I do actually want that stored procedure or materialized view.

The latest approach I'm trying is state-based development, specifically with [SQL Server Data Tools for Visual Studio](https://visualstudio.microsoft.com/vs/features/ssdt/).
I've had a positive experience so far. It feels much like the way I'm used to doing infrastructure nowadays: declaratively. (Make it like _this_, please!).
I've shipped one product with it and I think I'll try it again for the next thing I work on.
The shortcomings I'm aware of so far are:
* it can only infer so much (not that I've reached that limit, yet)
* it requires 'old' MSBuild (full framework), [though that might change](https://github.com/microsoft/DACExtensions/issues/20#issuecomment-521004761), and
* it's tied to SQL Server (I don't personally prioritize being database agnostic, but I understand others care a lot about this)

~~If this repository isn't updated in a long while I've probably discovered that this was absolutely the wrong approach and I now hate it, or I am such a fantastic developer I made this feature-complete and bug-free so no more commits were needed.~~
Turns out I have kept using this. (And it's doing so very little I haven't run into a bug, yet.)

(For probably better definitions and someone else's perspective, ['State-based or migrations-based database development' by Redgate](http://assets.red-gate.com/solutions/database-devops/state-or-migrations-based-database-development.pdf).)

### Why [type providers](https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/type-providers/)?
I think using type providers for the first time was the most magical experience I've had while programming.

```
let GetCustomers = new SqlCommand<"select * from customer", ConnectionString>
                                  ~~~~~~~~~~~~~~~~~~~~~~~~
                                  > SQL error: Invalid Object Name 'dbo.customer'

```
Here I am writing some SQL in a string in F# and I can see I forgot the tail 's' on 'customers'. Wow! Cool!
(Or I can get told that not all of our `users` have a `hatSize` so I had better consider that property optional, etc. etc.)

If you're on the .NET stack anyway I'd almost recommend trying out F# for type providers alone. There's ones out there for [OpenAPI](https://github.com/fsprojects/SwaggerProvider), [GraphQL](https://github.com/fsprojects/FSharp.Data.GraphQL), and [others](https://fsharp.github.io/FSharp.Data/). 

On the [database-heavy project I mentioned](#why-ssdt) we had enough issues with mismatching schemas and ended up writing integration tests which ensured the schema represented in our application matched the schema as represented in our database.
(i.e. we tested that our POCO's properties had matching columns in the database.)
You don't have the same issue with code-first Entity Framework and simple tables but, if you do end up with some stored procedures or calling `.FromSql("...")` you lose the type safety.

Not so with a SQL type provider! I can express all the things I want in plain ol' SQL but still get type safety. It is pretty delightful.

## Getting started
**Prerequisites**
* Visual Studio 2019 with SQL Server Data Tools
* A development database (e.g. (LocalDb)\MSSQLLocalDB)[*](#what-i-need-a-database-to-compile-my-code)

1. Create a 'Query Language -> SQL Server Database Project' (a `*.sqlproj`) and add a table, e.g. `Customers`
1. Create a 'F# -> Console App (.NET Core)' (or library or whichever kinda app you like, a `*.fsproj`)
1. Reference your `*.sqlproj` from your `*.fsproj`, e.g.
   `<ProjectReference Include="..\TestDatabase\TestDatabase.sqlproj" />`
1. Install the packages `FSharp.Data.SqlClient` and `FSharp.Data.DACFx.MSBuild` into your `*.fsproj`, e.g.
   ```
   <PackageReference Include="FSharp.Data.SqlClient" Version="2.0.6" />
   <PackageReference Include="FSharp.Data.DACFx.MSBuild" Version="1.0.0" />
   ```
1. Build your solution for the first time
1. Set your compile-time connection string
   ```fsharp
   [<Literal>]
   let CompileConnectionString =
    @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=TestDatabase;Integrated Security=True"
   ```
1. Write some queries in code!
   ```fsharp
   type SelectCustomers = SqlCommandProvider<"SELECT * FROM Customers", CompileConnectionString>
   ```


## Customizing
### Local database
Defaults to `(LocalDb)\MSSQLLocalDB`. You can override this with:
```xml
<PropertyGroup>
  <SqlServer>(LocalDb)\MyOtherDevelopmentDatabase</SqlServer>
</PropertyGroup>
```

## Building with .NET Core
You can build DACPACs with the `dotnet` toolchain (i.e. outside Visual Studio) using SDK-style projects with [MSBuild.Sdk.SqlProj](https://github.com/jmezach/MSBuild.Sdk.SqlProj)
This comes at the cost of losing the IntelliSense and such you get with SSDT tools in Visual Studio, but if you need to use `dotnet` whatcha gonna do?

I couldn't figure out how to detect that certain `.csproj` were actually using the `MSBuild.Sdk.SqlProj` SDK so instead you need to take one more step: rename that `csproj` back to `sqlproj`.

If you're having trouble with Visual Studio after trying to use `MSBuild.Sdk.SqlProj`, check your solution. You want to have the project GUIDs that force Visual Studio to load the project using the new project system as described [here](https://github.com/dotnet/project-system/blob/master/docs/opening-with-new-project-system.md#project-type-guids). i.e.
```
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "MyDatabaseProject", "MyDatabaseProject.sqlproj", "{ADFEAAF5-225C-4E13-8B65-77057AAC44B8}"
EndProject
```


## TODO
 - [x] Fix the up-to-date check so Visual Studio builds when the dacpac changes
 - [x] Name this package properly.
       What about `FSharp.Data.DACFx.MSBuild`? Based on [guidance](https://fsharp.github.io/2014/09/19/fsharp-libraries.html)
       I ought to pick a name not in `FSharp.Data.SqlClient`'s namespace. It also isn't entirely unique to FSharp.Data.SqlClient, you could use it quite happily with a different SQL type provider I imagine.
 - [ ] Update when SQL files change to support things like ['external *.sql files' in FSharp.Data.SqlClient](https://fsprojects.github.io/FSharp.Data.SqlClient/configuration%20and%20input.html)
 - [ ] Set up some kinda build for this package? Maybe.
 - [ ] Expose other (all?) [sqlpackage.exe parameters](https://docs.microsoft.com/en-us/sql/tools/sqlpackage?view=sql-server-ver15#publish-parameters-properties-and-sqlcmd-variables).
       Users might want to use SQL Server in a Docker container which will probably need a username and password, not just a server.
 - [ ] Publish only the database that changes.
       If you have multiple SSDT projects and make a change to one, both databases will be published with the current implementation.

## FAQ
### What?! I need a database to compile my code?
Requiring a local database to compile my app code did feel a little off to me at first too. 
I realized though that:
* I always have a local database accessible, even during continuous integration.
  At some point, I want to run integration tests or run my code for real, so there's always been a local database (even if that's one running in Docker.
* I prefer finding out that I broke something

Yes, it'd be cool if `FSharp.Data.SqlClient` supported pulling the schema right out of a DACPAC, but it doesn't.

Also if you're the type that likes splitting their app code and database schema into separate repositories, this probably isn't a project for you. However I will try and evangelize [vertical slicing](https://blogs.msdn.microsoft.com/progressive_development/2009/03/27/motley-says-vertical-slices-sounds-like-something-you-do-to-salami/) to you.
