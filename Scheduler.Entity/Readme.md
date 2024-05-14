
dotnet tool install --global dotnet-ef
dotnet tool update dotnet-ef

db first
```
dotnet ef dbcontext scaffold "server=svrhz10-11.bx.com.cn;Port=3306;user id=root;database=bxjob;pooling=true;password=BaoXin8888;" MySql.EntityFrameworkCore --schema bxjob --context-dir Data --output-dir Models --no-onconfiguring --force
```

code first
```
dotnet ef migrations add InitialCreate
dotnet ef database update
```