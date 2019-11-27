# FSharp.Data.SqlClient.DACFx.MSBuild

An MSBuild tools package to unite [FSharp.Data.SqlClient](http://fsprojects.github.io/FSharp.Data.SqlClient/) and [SSDT](https://visualstudio.microsoft.com/vs/features/ssdt/).

## Goals
I wanted a development experience where I can make a tweak to a table definition and see how I've broken my application without running a thing. 
SSDT database projects and FSharp.Data.SqlClient gave me most of that, but the experience of connecting them wasn't as neat as the experience of using them.
That's what `FSharp.Data.SqlClient.DACFx.MSBuild` fixes.

## Prerequisites
* Visual Studio 2019 with SQL Server Data Tools
* A development database (e.g. (LocalDb)\MSSQLLocalDB)

## I need a database to compile my code?
Requiring a local database to compile my app code did feel a little off to me at first too. 
I realized though that:
* I always have a local database accessible, even during continuous integration.
  At some point, I want to run integration tests or run my code for real, so there's always been a local database (even if that's one running in Docker.
* I prefer finding out that I broke something

Yes, it'd be cool if `FSharp.Data.SqlClient` supported pulling the schema right out of a DACPAC, but it doesn't.

Also if you're the type that likes splitting their app code and database schema into separate repositories, this probably isn't a project for you. However I will try and evangelize [vertical slicing](https://blogs.msdn.microsoft.com/progressive_development/2009/03/27/motley-says-vertical-slices-sounds-like-something-you-do-to-salami/) to you.

## Getting started
1. Create a 'Query Language -> SQL Server Database Project' (a `*.sqlproj`) and add a table, e.g. `Customers`
1. Create a 'F# -> Console App (.NET Core)' (or library or whichever kinda app you like, a `*.fsproj`)
1. Reference your `*.sqlproj` from your `*.fsproj`, e.g.
   `<ProjectReference Include="..\TestDatabase\TestDatabase.sqlproj" />`
1. Install the packages `FSharp.Data.SqlClient` and `FSharp.Data.SqlClient.DACFx.MSBuild` into your `*.fsproj`, e.g.
   ```
   <PackageReference Include="FSharp.Data.SqlClient" Version="2.0.6" />
   <PackageReference Include="FSharp.Data.SqlClient.DACFx.MSBuild" Version="1.0.0" />
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

## TODO
[ ] Fix the up-to-date check so Visual Studio builds when the dacpac changes
[ ] Set up some kinda build for this package? Maybe.
[ ] Name this package properly.
    What about `FSharp.Data.DACFx.MSBuild`? Based on [guidance](https://fsharp.github.io/2014/09/19/fsharp-libraries.html)
    I ought to pick a name not in `FSharp.Data.SqlClient`'s namespace. It also isn't entirely unique to FSharp.Data.SqlClient, you could use it quite happily with a different SQL type provider I imagine.
[ ] Expose other (all?) [sqlpackage.exe parameters](https://docs.microsoft.com/en-us/sql/tools/sqlpackage?view=sql-server-ver15#publish-parameters-properties-and-sqlcmd-variables).
    Users might want to use SQL Server in a Docker container which will probably need a username and password, not just a server.
[ ] Publish only the database that changes.
    If you have multiple SSDT projects and make a change to one, both databases will be published with the current implementation.