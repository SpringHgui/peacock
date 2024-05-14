
dotnet tool install --global dotnet-ef
dotnet tool update dotnet-ef

db first
```
dotnet ef dbcontext scaffold "server=101.37.166.120;Port=31002;user id=root;database=bxjob;pooling=true;password=bxparking@2018;" MySql.EntityFrameworkCore --schema bxjob --context-dir Data --output-dir Models --no-onconfiguring --force
```

code first
```
dotnet ef migrations add InitialCreate
dotnet ef database update
```