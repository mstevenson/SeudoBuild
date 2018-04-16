# SeudoBuild

Cake is a generalized distributed build system created with the Unity game engine in mind.

## Configuration

SeudoBuild uses a JSON build configuration file that define project and target parameters.

## Modes

### Build

Create a local build with a given target name and output directory.

#### Scan

Lists build agents discovered on the local network.

#### Submit

Submits a build request for a remote build agent to fulfill.

#### Queue

Waits build requests.

#### Deploy

Waits for deployment requests.

## Build Submission

A build request can be sent to any agent that is currently listening in 'queue' mode. Get a list of agents available on the local network using the "scan" verb.

### REST API

Request header:
Field:  Content-Type
Value:  application/json

* /info (GET) – Gets information about the build agent.
* /build (POST) – Build the default target in the given project configuration.
* /build/{target_name} (POST) – Build a specific target within a given project configuration.
* /queue (GET) – Returns a list with information about all active build tasks.
* /queue/{id_int} (GET) – Returns information about a specific build task.
* /queue/{id_int}/cancel (POST) – Cancels a specific build task.

