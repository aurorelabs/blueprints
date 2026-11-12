CONNECTION ?= Host=127.0.0.1;Database=blueprints-api-service;Username=postgres;Password=postgres
MIGRATIONS := tools/Tools.Migrations
NAME := $(word 2,$(MAKECMDGOALS))

.PHONY: test test-local report clean migration-add migration-remove migration-script migration-check db-update db-drop

test:
	docker compose --profile tests run --build --rm tests

test-local:
	dotnet test --solution Blueprints.ApiService.slnx

report:
	cmd.exe /c start artifacts\coverage-report\index.html

clean:
	rm -rf artifacts

migration-add:
	dotnet ef migrations add "$(or $(NAME),InitialDb)" --project $(MIGRATIONS) --output-dir History -- "$(CONNECTION)"

migration-remove:
	dotnet ef migrations remove --project $(MIGRATIONS) -- "$(CONNECTION)"

migration-script:
	dotnet ef migrations script --project $(MIGRATIONS) -- "$(CONNECTION)"

migration-check:
	dotnet ef migrations has-pending-model-changes --project $(MIGRATIONS) -- "$(CONNECTION)"

db-update:
	dotnet ef database update --project $(MIGRATIONS) -- "$(CONNECTION)"

db-drop:
	dotnet ef database drop --project $(MIGRATIONS) --force -- "$(CONNECTION)"

%:
	@:
