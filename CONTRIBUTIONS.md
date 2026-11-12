# Contributions

Who built what. Each member wrote their own row in their own commit. Phase 0-3 work was
reviewed through the pull requests listed (PR #1 merged; #2-#10 closed after the team moved to
committing on a single `main` branch, with their commits kept in `main`). Phase 4-7 work was
committed directly to `main`; `git log --author="<name>"` lists every commit of a member.

| Member | GitHub | Services owned | Shared pieces | Cross-boundary work | Pull requests | Other contributions |
| --- | --- | --- | --- | --- | --- | --- |
| Dusty Cruz | [@dustycruz](https://github.com/dustycruz) | Catalog (contract, entities, migration, seed, controllers, development-only chaos switch); Ocelot Gateway (routes, JWT at the edge, login rate limit, QoS, X-Correlation-ID) | `Store.Observability` (correlation id middleware, accessor and DelegatingHandler, Problem Details + IExceptionHandler, logging scopes); `Directory.Build.props` / `Directory.Packages.props` | Generated Catalog client + adapter + `AddStandardResilienceHandler` in Order.Api and Inventory.Api; X-Correlation-ID carried through RabbitMQ headers in `Store.Messaging`; Dockerfiles for Catalog and the gateway; `docker-compose.yml` | [#1](https://github.com/dustycruz/AdvancedSystemIntegrationandArchitecture/pull/1) Phase 0 scaffold, [#2](https://github.com/dustycruz/AdvancedSystemIntegrationandArchitecture/pull/2) Catalog contract, [#5](https://github.com/dustycruz/AdvancedSystemIntegrationandArchitecture/pull/5) Observability + Catalog data, [#8](https://github.com/dustycruz/AdvancedSystemIntegrationandArchitecture/pull/8) Catalog API + generated clients | Repository setup, README, `docs/architecture.md` / `.mmd` / `.png`, gateway rate-limit and circuit-breaker fixes found in integration testing, clean-clone test, `v1.0` tag |
