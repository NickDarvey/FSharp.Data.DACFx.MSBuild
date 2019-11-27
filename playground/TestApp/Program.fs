open FSharp.Data

[<Literal>]
let CompileConnectionStringA =
    @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=TestDatabase;Integrated Security=True"

//[<Literal>]
//let CompileConnectionStringB =
//    @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=TestOtherDatabase;Integrated Security=True"

type SelectCustomers = SqlCommandProvider<"SELECT * FROM Customers", CompileConnectionStringA>
//type SelectUsers = SqlCommandProvider<"SELECT * FROM Users", CompileConnectionStringB>


[<EntryPoint>]
let main argv =
    let runtimeA = @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=TestDatabaseRuntime;Integrated Security=True"
    let cmdA     = SelectCustomers.Create (runtimeA)
    let cs      = cmdA.Execute()
    printfn "Customers"
    for c in cs do printfn "Customer %i: %s" c.Id (c.Name |> Option.defaultValue "No name")
    //let runtimeB = @"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=TestOtherDatabaseRuntime;Integrated Security=True"
    //let cmdB    = SelectUsers.Create (runtimeB)
    //let us      = cmdB.Execute()
    //printfn "Users"
    //for u in us do printfn "Customer %i: some user" u.Id

    0 // return an integer exit code
