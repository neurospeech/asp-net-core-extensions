# ef-core-live-migration
Live Migration support for EF Core

# Features

1. Creates Missing Table
2. Creates Missing Columns
3. Renames old column with same name, creates new column and migrates data if no loss occurs
4. Renames old indiexes and creates new ones
5. Creates indexes based on foreign keys

# Installation

```
	PM> Install-Package NeuroSpeech.EFCoreLiveMigration
```

# ASP.NET Core

```c#

public void Configure(IApplicationBuilder app, IHostingEnvironment env){

	if(env.IsDevelopment()){
		app.UseDeveloperExceptionPage();

		using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
		{
			using (var db = scope.ServiceProvider.GetRequiredService<AppModelContext>())
			{
				MigrationHelper.ForSqlServer(db).Migrate();
			}
		}
	}

}

```
