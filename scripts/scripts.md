# Scripts Quick Start

Here are the three main commands for development, building, and testing:

### 1. Run Everything (Docker-first build, run, and smoke tests)
Builds all service containers, runs the Docker Compose stack, and executes all smoke tests:
```bash
./scripts/run-all.sh
```

### 2. Compile Services Locally (on the Host machine)
Builds all services locally on your machine using dotnet:
```bash
./scripts/local-build.sh
```

### 3. Start Docker Compose Stack (forces rebuilding containers)
Spins up the Docker Compose environment and forces rebuilding of updated service images:
```bash
./scripts/docker-up-build.sh
```
